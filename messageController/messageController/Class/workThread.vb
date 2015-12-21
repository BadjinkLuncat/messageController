Imports System.Data
Imports System.IO
Imports System.Messaging
Imports System.Threading
Imports System.Data.SqlClient

Public Class workThread
    Implements IDisposable
    Private disposedValue As Boolean = False        ' To detect redundant calls
    Private father As Form1 = Nothing
    Private idChild As Integer
    Private running As Boolean = False
    Private threadName As String = String.Empty
    Private _threadStarter As ThreadStart = Nothing   ' Thread starter/pointer to thread proc.
    Private _thread As Thread = Nothing               ' Thread to create and command

    ' IDisposable
    Protected Overridable Sub Dispose(ByVal disposing As Boolean)
        If Not Me.disposedValue Then
            If disposing Then
                '........
                If (Not (Me._thread Is Nothing)) Then
                    Me._thread = Nothing
                End If
            End If

            ' TODO: free shared unmanaged resources
        End If
        Me.disposedValue = True
    End Sub

#Region " IDisposable Support "
    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub
#End Region


    Public Sub New(ByVal manager As Form1, ByVal id As Integer)
        MyBase.New()
        father = manager
        idChild = id
    End Sub

    Private Sub InternalWriteInformation(ByVal message As String, Optional ByVal display As Boolean = True, Optional ByVal save As Boolean = True)
        Try
            father.InternalWriteInformation(".." & threadName & " " & message, display, save)
        Catch ex As Exception
        End Try
    End Sub

    Private Sub InternalWriteException(ByVal message As String, Optional ByVal display As Boolean = True, Optional ByVal save As Boolean = True)
        Try
            father.InternalWriteException(".." & threadName & " " & message, display, save)
        Catch ex As Exception
        End Try
    End Sub


    Public Sub Start()
        Me.InternalWriteInformation("workerThread starting...")

        Dim errorMessage As String = ""
        Dim connectionString As String = ""
        Dim result As Boolean = False

        Try
            If (running) Then
                Throw New Exception("The thread '" & threadName & "' is already started.")
            End If


            father.signalWorkThreadCreated()
            If (_threadStarter Is Nothing) Then
                _threadStarter = New ThreadStart(AddressOf InternalProcess)
            End If

            _thread = New Thread(_threadStarter)
            threadName = "<wK" & idChild.ToString("00") & ">"
            _thread.Name = threadName
            _thread.Start()
            Me.InternalWriteInformation("Started.")
        Catch ex As Exception
            Me.InternalWriteInformation("[Start] Error : " & ex.Message)
        End Try
    End Sub

    Private Sub InternalProcess()
        Me.InternalWriteInformation("Thread process started.")
        Dim jobs As DataSet = Nothing

        Try
            Me.father.SignalThreadWorkerStarted()

            While (True)
                Me.InternalWriteInformation("Waiting jobs.. ")
                jobs = father.getJobs()

                If jobs Is Nothing Then
                    Me.InternalWriteInformation("Thread stopping.")
                    Exit While
                End If

                Me.InternalWriteInformation("Processing.. jobs: ")
                Me.executeJobs(jobs)
                Me.InternalWriteInformation("End processing.. jobs: ")

                Try
                    jobs.Dispose()
                Catch ex As Exception
                    Me.InternalWriteInformation("Error on Dispose jobs : " & ex.Message)
                Finally
                    jobs = Nothing
                End Try
                Me.father.signalThreadWorkerAvailable()
                Thread.Sleep(CInt(My.Settings.restThread))
            End While
        Catch ex As Exception
            Me.InternalWriteInformation("Exception on thread! Exiting..")
            Me.InternalWriteException(ex.Message)
        Finally
            Me.InternalWriteInformation("Thread stopped.")
            Me.father.SignalThreadWorkerStopped()
            Me.Dispose()
        End Try

    End Sub

    Private Sub executeJobs(ByVal item As DataSet)
        Dim xmlSMS As xmlMessage = New xmlMessage()
        Dim userType As Integer
        Dim totalSMSUpload As Integer = 0
        Dim errorMessage As String = String.Empty
        Dim countSend As Integer = 0
        Dim newFilterHashTable As Hashtable = New Hashtable
        Dim fileHashTable As Hashtable = New Hashtable
        Dim msisdn As String = String.Empty
        Dim message As String = String.Empty
        Dim division As String = String.Empty
        Dim tunnel As String = String.Empty
        Dim mq As String = String.Empty
        Dim oprCode As String = String.Empty
        Dim connString As String = "user id=" & My.Settings.userDB & ";password=" & My.Settings.passDB & ";data source=" & My.Settings.dataSourceDB & ";initial catalog=" & My.Settings.initalCatalogDB
        Dim sqlConn As SqlConnection = Nothing
        Dim sqlComm As SqlCommand = Nothing
        Dim totalMSISDNSent As Integer

        Try
            For Each row As DataRow In item.Tables(0).Rows
                xmlSMS.FileName = row("fileName").ToString
                xmlSMS.FileID = row("fileId").ToString
                xmlSMS.UploadBy = row("uploadBy").ToString
                xmlSMS.UploadDate = row("uploadDate").ToString
                xmlSMS.ClientName = row("clientName").ToString
                Try
                    xmlSMS.DivisionName = row("divisionName").ToString
                Catch ex As Exception
                    xmlSMS.DivisionName = ""
                End Try
                xmlSMS.Masking = row("masking").ToString
                userType = CInt(row("userType"))
                totalSMSUpload = CInt(row("totalMessageUpload"))
            Next


            '' filtering and validating msisdn and message
            If Not Me.filteringMSISDNandMessage(xmlSMS.FileID, xmlSMS.FileName, newFilterHashTable, errorMessage) Then
                Throw New Exception(errorMessage)
            End If


            If newFilterHashTable.Count > 0 Then
                For Each key As Object In newFilterHashTable.Keys
                    Try
                        msisdn = key.ToString.Split(vbTab)(0)
                        message = key.ToString.Split(vbTab)(1)

                        oprCode = cekPrefix(msisdn)
                        If oprCode = "ERROR" Then
                            Throw New Exception("unknown prefix while try push to queue")
                        End If

                        Try
                            tunnel = father.tunnelSetting(CStr(xmlSMS.ClientName & "|" & oprCode).ToUpper)
                        Catch ex As Exception
                            Throw New Exception("unknown tunnel/cannot find tunnel while try push to queue")
                        End Try

                        Try
                            mq = father.MqSetting(CStr(tunnel & "|" & oprCode).ToUpper)
                        Catch ex As Exception
                            Throw New Exception("unknown mq/cannot find mq while try push to queue")
                        End Try

                        Dim uniqid As String = Guid.NewGuid.ToString

                        xmlSMS.UniqId = uniqid
                        xmlSMS.Msisdn = msisdn
                        xmlSMS.Message = message.Replace("[enter]", ControlChars.CrLf)
                        xmlSMS.Tunnel = tunnel
                        xmlSMS.OprCode = oprCode

                        Dim totalSMS As Integer = 1
                        If xmlSMS.Message.Length Mod 160 = 0 Then
                            totalSMS = xmlSMS.Message.Length \ 160
                        Else
                            totalSMS = (xmlSMS.Message.Length \ 160) + 1
                        End If

                        xmlSMS.Reserve1 = CStr(totalSMS)

                        If Me.insertMq(xmlSMS, mq) Then
                            countSend += 1
                            ''create file                            
                            totalMSISDNSent = totalMSISDNSent + totalSMS
                            Me.createFile(xmlSMS, fileHashTable)
                        End If
                        Me.InternalWriteInformation(xmlSMS.Msisdn & " processed ")
                    Catch ex As Exception
                        Me.InternalWriteInformation("[loop insert to queue]" & xmlSMS.Msisdn & " error : " & ex.Message)
                    End Try
                Next
            End If


            ''cek userType
            If userType = 1 Then
                If totalSMSUpload > totalMSISDNSent Then
                    Dim roolbackToken As Integer = totalSMSUpload - totalMSISDNSent
                    '' call SP roll back token
                    Try
                        If tokenUpdate(xmlSMS.UploadBy, roolbackToken, "4", My.Settings.NamedForToken.ToString) Then
                            If tokenUpdate(xmlSMS.UploadBy, roolbackToken, "3", My.Settings.NamedForToken.ToString) Then
                                insertHistoryToken(xmlSMS.UploadBy, roolbackToken, "Rollback Token", My.Settings.NamedForToken.ToString)
                            Else
                                Throw New Exception("update Rollback token Failed, please check your user name")
                            End If
                        Else
                            Throw New Exception("rollback token Failed, please check your user name is registerd or no")
                        End If
                    Catch ex As Exception
                        Me.InternalWriteInformation("[" & xmlSMS.ClientName & "]-[" & xmlSMS.UploadBy & "] token must rollback [" & CStr(roolbackToken) & "] Error " & ex.Message)
                    End Try
                End If
            End If

            Dim Query As String
            If countSend > 0 Then
                Query = "UPDATE UPLOADDATA SET processDate = getdate(), STATUS = 3, totalMSISDNSent = @totalMSISDNSent,fileNameFilter='filter.txt',totalMessageSent=@totalMessageSent WHERE FILEID = @FILEID "
            Else
                Query = "UPDATE UPLOADDATA SET processDate = getdate(), STATUS = -1 WHERE FILEID = @FILEID "
            End If
            ''update uploadData
            If sqlConn Is Nothing Then
                sqlConn = New SqlConnection(connString)
                sqlConn.Open()
            End If

            sqlComm = New SqlCommand(Query, sqlConn)
            sqlComm.Parameters.AddWithValue("@totalMSISDNSent", countSend)
            sqlComm.Parameters.AddWithValue("@totalMessageSent", totalMSISDNSent)
            sqlComm.Parameters.AddWithValue("@FILEID", xmlSMS.FileID)
            sqlComm.ExecuteNonQuery()
        Catch ex As Exception
            ''update status failed and insert error to table upload Data
            Me.InternalWriteException("executeJobs : " & ex.Message)
            Try
                If sqlConn Is Nothing Then
                    sqlConn = New SqlConnection(connString)
                    sqlConn.Open()
                End If
                
                Dim Query As String = "UPDATE UPLOADDATA SET processDate = getdate(), STATUS = -2, errorDescription = @errorDescription WHERE FILEID = @FILEID "
                sqlComm = New SqlCommand(Query, sqlConn)
                sqlComm.Parameters.AddWithValue("@errorDescription", ex.Message)
                sqlComm.Parameters.AddWithValue("@FILEID", xmlSMS.FileID)
                sqlComm.ExecuteNonQuery()
            Catch exp As Exception
                Me.InternalWriteInformation("update upload Data Error while updateing data tobe failed : " & exp.Message)
            End Try
        Finally
            If Not (sqlConn Is Nothing) Then
                sqlConn.Close()
                sqlConn.Dispose()
                sqlConn = Nothing
            End If

            If Not (sqlComm Is Nothing) Then
                sqlComm.Dispose()
                sqlComm = Nothing
            End If

            If Not (newFilterHashTable Is Nothing) Then
                newFilterHashTable = Nothing
            End If

            If Not (fileHashTable Is Nothing) Then
                fileHashTable = Nothing
            End If
        End Try
    End Sub

    Private Function filteringMSISDNandMessage(ByVal fileID As String, ByVal fileName As String, ByRef filterHashTable As Hashtable, ByRef errorMessage As String) As Boolean
        Dim blReturn As Boolean = True
        Dim objectfileInfo As FileInfo = Nothing
        Dim objectFileStream As FileStream = Nothing
        Dim objectFileRead As StreamReader = Nothing
        Dim msisdn As String = String.Empty
        Dim message As String = String.Empty
        Dim line As Integer = 1
        Dim division As String = String.Empty
        Dim readline As String


        Try

            objectfileInfo = New FileInfo(IO.Path.Combine(My.Settings.dataPath.ToString, fileID & fileName))

            If (Not (objectfileInfo.Exists)) Then
                Me.InternalWriteInformation("File Is No Exist")
                Throw New Exception("File Is No Exist - " & objectfileInfo.FullName)
            End If

            ''objectFileStream = New System.IO.FileStream(objectfileInfo.FullName, IO.FileMode.Open)
            ''objectFileRead = New System.IO.StreamReader(objectFileStream)
            objectFileRead = New StreamReader(objectfileInfo.OpenRead)

            While (objectFileRead.Peek >= 0)
                Try
                    readline = objectFileRead.ReadLine
                    msisdn = readline.Split(ControlChars.Tab)(0)
                    message = readline.Split(ControlChars.Tab)(1)

                    If Not cekMSISDN(msisdn) Then
                        Throw New Exception("Invalid MSISDN")
                    End If

                    If cekBlacklist(msisdn) Then
                        Throw New Exception("Black List MSISDN")
                    End If

                    If Not cekMessage(message) Then
                        Throw New Exception("Invalid Message")
                    End If
                    If Not filterHashTable.Contains(msisdn & vbTab & message) Then
                        filterHashTable.Add(msisdn & vbTab & message, msisdn & vbTab & message)
                    End If
                    line += 1
                Catch ex As Exception
                    Me.InternalWriteInformation("line " & CStr(line) & " error : " & ex.Message, False)
                End Try
            End While
        Catch ex As Exception
            blReturn = False
            errorMessage = ex.Message
        End Try
        Return blReturn
    End Function

    Private Function cekMSISDN(ByRef MSISDN As String) As Boolean
        Dim validMSISDN As String = My.Settings.validMSISDN.ToString

        If (MSISDN.Substring(0, 1) = "+") Then
            MSISDN = MSISDN.Substring(1)
        End If

        If (MSISDN.Substring(0, 1) = "0") Then
            MSISDN = ("62" & MSISDN.Substring(1))
        End If

        For Each C As Char In MSISDN.ToCharArray
            If validMSISDN.IndexOf(C) < 0 Then
                Return False
            End If
        Next
        Return True
    End Function

    Private Function cekBlacklist(ByRef MSISDN As String) As Boolean
        Dim boolRet As Boolean = False

        Dim connString As String = "user id=" & My.Settings.userDB & ";password=" & My.Settings.passDB & ";data source=" & My.Settings.dataSourceDB & ";initial catalog=" & My.Settings.initalCatalogDB
        Dim sqlconn As SqlConnection = Nothing
        Dim sqlDat As SqlDataAdapter = Nothing
        Dim dsData As DataSet = Nothing

        Try
            Dim query As String = "SELECT * FROM blackListMSISDN where msisdn = @msisdn"
            sqlconn = New SqlConnection(connString)
            sqlDat = New SqlDataAdapter(query, sqlconn)
            dsData = New DataSet
            sqlDat.SelectCommand.Parameters.AddWithValue("@msisdn", MSISDN)
            sqlDat.Fill(dsData)

            If dsData.Tables(0).Rows.Count > 0 Then
                boolRet = True
            Else
                boolRet = False
            End If
        Catch ex As Exception
            Throw New Exception("failed cek black list")
        Finally
            If (Not (sqlDat Is Nothing)) Then
                sqlDat.Dispose() : sqlDat = Nothing
            End If
            If (Not (dsData Is Nothing)) Then
                dsData.Dispose() : dsData = Nothing
            End If
            If (Not (sqlconn Is Nothing)) Then
                sqlconn.Close()
                sqlconn.Dispose() : sqlconn = Nothing
            End If
        End Try
        Return boolRet
    End Function

    Private Function cekMessage(ByVal message As String) As Boolean
        Dim validChar As String = My.Settings.validMessage & vbCrLf
        Dim ObjChar As Char
        For Each ObjChar In message.ToLower.ToCharArray
            If validChar.IndexOf(ObjChar) < 0 Then
                Return False
            End If
        Next
        Return True
    End Function

    Private Function cekPrefix(ByVal msisdn As String) As String
        Dim strReturn As String = "ERROR"
        Dim success As Boolean
        Dim i As Integer = 5
        Try
            While (Not success) AndAlso (i > 0)
                Try
                    strReturn = Me.father.prefixSetting.Item(msisdn.Substring(0, i))

                    If (strReturn.Trim.Length > 0) Then
                        success = True
                    End If
                Catch ex As Exception
                End Try

                i -= 1
            End While
        Catch ex As Exception
        End Try

        Return strReturn
    End Function

    Private Function insertMq(ByRef xmlOut As xmlMessage, ByVal mqPath As String) As Boolean
        Dim blreturn As Boolean = False
        Dim messageOut As Message = Nothing
        Dim mqSend As MessageQueue = Nothing

        Try
            messageOut = New Message(xmlOut.ToXmlDocument())

            If (MessageQueue.Exists(".\private$\" & mqPath)) Then
                mqSend = New MessageQueue(".\private$\" & mqPath)
            Else
                mqSend = MessageQueue.Create(".\private$\" & mqPath)
                mqSend.SetPermissions("everyone", MessageQueueAccessRights.FullControl)
            End If
            xmlOut.InqueueDate = Now.ToString
            Me.InternalWriteInformation("XML : " & xmlOut.ToXmlString, False)
            mqSend.Send(messageOut)
            blreturn = True
        Catch ex As Exception
            Me.InternalWriteInformation("Exception on: [insertMq].")
            Me.InternalWriteException("[insertMq] : " & ex.Message)
        Finally
            If (Not (messageOut Is Nothing)) Then
                messageOut.Dispose() : messageOut = Nothing
            End If

            If (Not (mqSend Is Nothing)) Then
                mqSend.Dispose() : mqSend = Nothing
            End If
        End Try
        Return blreturn
    End Function

    Private Sub createFile(ByVal xmlFile As xmlMessage, ByRef fileHashTable As Hashtable)
        Dim keyword As String = xmlFile.FileID

        If Not fileHashTable.Contains(keyword) Then
            '' jika tidak ada create baru
            Dim dirChecker As DirectoryInfo = Nothing
            Dim fileChecker As FileInfo = Nothing
            Dim newFileName As String = String.Empty
            Dim writer As StreamWriter = Nothing


            Try
                dirChecker = New DirectoryInfo(My.Settings.executeDataPath)
                If (Not dirChecker.Exists) Then
                    dirChecker.Create()
                End If

                newFileName = xmlFile.FileID & "filter.txt"
                fileChecker = New FileInfo(IO.Path.Combine(dirChecker.FullName, newFileName))
                writer = fileChecker.CreateText

                With writer
                    .AutoFlush = True
                    .Write(xmlFile.Msisdn & vbTab & xmlFile.Message)
                End With
                fileHashTable.Add(keyword, fileChecker.FullName)
            Catch ex As Exception
                Me.InternalWriteException("failed init create for file - [" & newFileName & "] - with - [" & xmlFile.Msisdn & " " & xmlFile.Message & "]")
            Finally
                If (Not writer Is Nothing) Then
                    writer.Dispose() : writer = Nothing
                End If

                fileChecker = Nothing
                dirChecker = Nothing
            End Try
        Else
            '' jika sudah dicreate, sesuai value yang ada
            Dim fileChecker As FileInfo = Nothing
            Dim afternewFileName As String = String.Empty
            Dim writer As StreamWriter = Nothing

            Try
                afternewFileName = fileHashTable(keyword)
                fileChecker = New FileInfo(afternewFileName.Split("|")(0))
                writer = fileChecker.AppendText()
                With writer
                    .AutoFlush = True
                    .WriteLine()
                    .Write(xmlFile.Msisdn & vbTab & xmlFile.Message)
                End With
            Catch ex As Exception
                Throw New Exception("failed append for file - [" & afternewFileName.Split("|")(0).ToString & "] - with - [" & xmlFile.Msisdn & " " & xmlFile.Message & "]")
            Finally
                If (Not writer Is Nothing) Then
                    writer.Dispose() : writer = Nothing
                End If
                fileChecker = Nothing
                afternewFileName = Nothing
            End Try
        End If
    End Sub


    Private Function tokenUpdate(ByVal userName As String, ByVal amountToken As Long, ByVal methodToken As String, ByVal userID As String) As Boolean
        Dim retBoolean As Boolean = False
        Dim connString As String = "user id=" & My.Settings.userDB & ";password=" & My.Settings.passDB & ";data source=" & My.Settings.dataSourceDB & ";initial catalog=" & My.Settings.initalCatalogDB
        Dim dbConnection As SqlConnection = Nothing
        Dim dbCommand As SqlCommand = Nothing
        Dim dbDataAdapter As SqlDataAdapter = Nothing
        Dim commandText As String = ""
        Dim status As SqlParameter
        Dim dtTable As New DataTable

        Try

            dbConnection = New SqlConnection(connString)
            dbConnection.Open()

            dbCommand = New SqlCommand(My.Settings.SP_TokenName.ToString, dbConnection)
            dbCommand.CommandType = Data.CommandType.StoredProcedure
            status = New SqlParameter("@STATUS", Data.SqlDbType.Char)
            status.Direction = Data.ParameterDirection.Output
            status.DbType = Data.DbType.StringFixedLength
            status.Size = 1
            dbCommand.Parameters.AddWithValue("@userName", userName)
            dbCommand.Parameters.AddWithValue("@AddToken", amountToken)
            dbCommand.Parameters.AddWithValue("@userid", userID)
            dbCommand.Parameters.AddWithValue("@MethodId", methodToken)
            dbCommand.Parameters.Add(status)

            dbDataAdapter = New SqlDataAdapter(dbCommand)
            dbDataAdapter.Fill(dtTable)

            If status.Value = "1" Then
                retBoolean = True
            End If
        Catch ex As Exception
            Throw New Exception("[tokenUpdate][" & userName & "][" & amountToken & "][" & methodToken & "] : " & ex.Message)
        Finally
            If Not (dbCommand Is Nothing) Then
                dbCommand.Dispose()
                dbCommand = Nothing
            End If

            If Not (dbDataAdapter Is Nothing) Then
                dbDataAdapter.Dispose()
                dbDataAdapter = Nothing
            End If

            If Not (dtTable Is Nothing) Then
                dtTable.Dispose()
                dtTable = Nothing
            End If

            If Not (dbConnection Is Nothing) Then
                dbConnection.Close()
                dbConnection.Dispose()
                dbConnection = Nothing
            End If
        End Try

        Return retBoolean
    End Function

    Private Sub insertHistoryToken(ByVal userName As String, ByVal amountToken As Long, ByVal methodToken As String, ByVal userID As String)
        Dim retBoolean As Boolean = False
        Dim connString As String = "user id=" & My.Settings.userDB & ";password=" & My.Settings.passDB & ";data source=" & My.Settings.dataSourceDB & ";initial catalog=" & My.Settings.initalCatalogDB
        Dim dbConnection As SqlConnection = Nothing
        Dim dbCommand As SqlCommand = Nothing
        Dim commandText As String = ""
        Dim value As Integer = 0

        Try

            dbConnection = New SqlConnection(connString)
            dbConnection.Open()

            commandText = "INSERT INTO TOKENHISTORY" & Format(Now, "yyyyMM") & _
                          "  VALUES (@USERNAME,@AMOUNTTOKEN,GETDATE(),@CREATEDBY,@DESCRIPTION)"

            dbCommand = New SqlCommand(commandText, dbConnection)
            dbCommand.Parameters.AddWithValue("@USERNAME", userName)
            dbCommand.Parameters.AddWithValue("@AMOUNTTOKEN", amountToken)
            dbCommand.Parameters.AddWithValue("@CREATEDBY", userID)
            dbCommand.Parameters.AddWithValue("@DESCRIPTION", methodToken)
            dbCommand.ExecuteNonQuery()

        Catch ex As Exception
            Throw New Exception("[insertHistoryToken][" & userName & "][" & amountToken & "][" & methodToken & "] : " & ex.Message)
        Finally
            If Not (dbCommand Is Nothing) Then
                dbCommand.Dispose()
                dbCommand = Nothing
            End If

            If Not (dbConnection Is Nothing) Then
                dbConnection.Close()
                dbConnection.Dispose()
                dbConnection = Nothing
            End If
        End Try
    End Sub

End Class
