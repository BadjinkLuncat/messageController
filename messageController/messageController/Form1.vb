' Oktober 2011

Imports System.Threading
Imports System.Data.SqlClient


Public Class Form1
    Implements IMessageControl

    Private Logger As logging = Nothing

    Private Running As Boolean = False
    Private RunningThread As Boolean = False
    Private _isAllWorkersStopped As Boolean = False
    Private WorkThreadCreated As Boolean = False
    Private _isRestartlock As Boolean = False

    Private WorkThread As ArrayList = Nothing

    Private WorkThreadTotal As Integer = 0
    Private _availableWorkersCount As Integer = 0
    Private _activeWorkersCount As Integer = 0

    Private _waitWorkerStoppedLocker As New Object()
    Private WaitWorkThreadReady As New Object()
    Private _waitWorkerAvailableLocker As New Object()
    Private _waitWorkerReadyLocker As New Object()
    Private _taskLocker As New Object()
    Private TunnelLocker As New Object()
    Private MqLocker As New Object()
    Private PrefixLocker As New Object()

    Private _taskQueue As New Queue()

    Private TunnelSettingHash As Hashtable = Nothing
    Private MqSettingHash As Hashtable = Nothing
    Private PrefixSettingHash As Hashtable = Nothing


    Private Delegate Sub DelegateWriteLog(ByVal inMessage As String, ByVal inLogType As LogTypeConstants, ByVal inDisplay As Boolean, ByVal inSave As Boolean)
    Private Delegate Sub DelegateStartStop()

    Private Sub Form1_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        If (Running) Then
            ' Do not quit, inform user to stop the process before quitting.
            e.Cancel = True
            MessageBox.Show(Me, "Please use the Stop button to stop the process before exiting.", "Stopping", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If
    End Sub

    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        Dim logName As String = ""
        Dim logPath As String = ""
        Dim logSize As Integer = 0

        If (Logger Is Nothing) Then
            Try
                logName = My.Settings.logName
                logPath = My.Settings.logPath
                logSize = My.Settings.logSize
            Catch ex As Exception
                logPath = "C:\Temp\" & Application.ProductName
                logName = Application.ProductName
                logSize = 500
            End Try
        End If

        Logger = New logging(logName, logPath, logSize)
        btnStart.Enabled = True
        btnStop.Enabled = False
    End Sub

    Private Sub btnStart_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnStart.Click
        If Not (Running) Then
            Dim sb As New System.Text.StringBuilder
            Try

                btnStart.Enabled = False
                Running = True
                Me.txtStatus.Text = "Starting...."
                Me.txtError.Clear()
                Application.DoEvents()
                btnStop.Enabled = False

                sb.Append(vbNewLine & New String("-", 30) & vbNewLine)
                sb.Append("tunnel info" & vbNewLine)
                sb.Append(New String("-", 30) & vbNewLine)
                For Each key As Object In Me.tunnelSetting.Keys
                    sb.Append(" - " & key & ": " & Me.tunnelSetting.Item(key) & vbNewLine)
                Next

                sb.Append(vbNewLine & New String("-", 30) & vbNewLine)
                sb.Append("mq info" & vbNewLine)
                sb.Append(New String("-", 30) & vbNewLine)
                For Each key As Object In Me.MqSetting.Keys
                    sb.Append(" - " & key & ": " & Me.MqSetting.Item(key) & vbNewLine)
                Next

                sb.Append(vbNewLine & New String("-", 30) & vbNewLine)
                sb.Append("prefix info" & vbNewLine)
                sb.Append(New String("-", 30) & vbNewLine)
                For Each key As Object In Me.prefixSetting.Keys
                    sb.Append(" - " & key & ": " & Me.prefixSetting.Item(key) & vbNewLine)
                Next

                sb.Append(New String("-", 30) & vbNewLine)
                Me.InternalWriteInformation(sb.ToString)

                Dim t As New Threading.Thread(AddressOf Me.internalProses)
                t.IsBackground = True
                t.Start()
            Catch ex As Exception
                Me.InternalWriteInformation("Exception on [Start]")
                Me.InternalWriteException("Start" & ex.Message)
                Me.txtStatus.Text = "Stop"
                Me.btnStart.Enabled = True
                Running = False
            Finally
                If (Not (sb Is Nothing)) Then
                    sb = Nothing
                End If
            End Try

        End If
    End Sub

    Private Sub btnStop_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnStop.Click
        btnStop.Enabled = False
        _isRestartlock = False
        Me.txtStatus.Text = "Stopping....."
        Me.RunningThread = False
        While (Not (Me._isRestartlock))
            Application.DoEvents()
            Threading.Thread.Sleep(1)
        End While

        Running = False
    End Sub

    Private Sub prosesRunning() Implements IMessageControl.prosesRunning
        If (Me.InvokeRequired) Then
            Dim d As DelegateStartStop = New DelegateStartStop(AddressOf InternalTaskRunning)

            Me.Invoke(d)
        Else
            Me.InternalTaskRunning()
        End If
    End Sub
    Private Sub prosesStarted() Implements IMessageControl.prosesStarted
        If (Me.InvokeRequired) Then
            Dim d As DelegateStartStop = New DelegateStartStop(AddressOf InternalTaskStarted)

            Me.Invoke(d)
        Else
            Me.InternalTaskStarted()
        End If
    End Sub

    Private Sub prosesStopped() Implements IMessageControl.prosesStopped
        If (Me.InvokeRequired) Then
            Dim d As DelegateStartStop = New DelegateStartStop(AddressOf InternalTaskStopped)

            Me.Invoke(d)
        Else
            Me.InternalTaskStopped()
        End If
    End Sub

    Private Sub WriteLog(ByVal message As String, ByVal type As Integer, ByVal display As Boolean, ByVal save As Boolean) Implements IMessageControl.WriteLog
        If (Me.InvokeRequired) Then
            Dim d As DelegateWriteLog = New DelegateWriteLog(AddressOf InternalWriteLog)
            Dim args() As Object = New Object() {message, type, display, save}

            Me.Invoke(d, args)
        Else
            Me.InternalWriteLog(message, type, display, save)
        End If
    End Sub

    Private Sub InternalTaskStarted()
        Me.btnStart.Enabled = False
        Me.btnStop.Enabled = True
        Me.txtStatus.Text = "Started"
        Me.InternalWriteLog("Agent started.", LogTypeConstants.Information, True, True)
    End Sub

    Private Sub InternalTaskRunning()
        Me.txtStatus.Text = "Running"
    End Sub

    Private Sub InternalTaskStopped()
        Me.btnStart.Enabled = True
        Me.btnStop.Enabled = False
        Me.txtStatus.Text = "Stoped"
        Me.InternalWriteLog("Agent stopped ", LogTypeConstants.Information, True, True)
    End Sub

    Private Sub InternalWriteLog(ByVal message As String, ByVal logType As LogTypeConstants, ByVal displayLog As Boolean, ByVal saveLog As Boolean)
        Try
            If (displayLog) Then
                Select Case logType
                    Case LogTypeConstants.Information
                        If txtInfo.Text.Length + message.Length > 5000 Then
                            txtInfo.Clear()
                        End If
                        Me.txtInfo.AppendText("[" & DateTime.Now.ToString(My.Settings.dateTimeFormat) & "] " & message & vbCrLf)
                    Case LogTypeConstants.Exception
                        If txtError.Text.Length + message.Length > 5000 Then
                            txtError.Clear()
                        End If
                        Me.txtError.AppendText("[" & DateTime.Now.ToString(My.Settings.dateTimeFormat) & "] " & message & vbCrLf)
                End Select
            End If

            If (saveLog) Then
                Select Case logType
                    Case LogTypeConstants.Information
                        Logger.WriteInfo(message)
                    Case LogTypeConstants.Exception
                        Logger.WriteError(message)
                End Select
            End If
        Catch ex As Exception
        End Try
    End Sub


    Private Function InternalCreateWorkerThread() As Boolean
        Dim result As Boolean = False
        Dim count As Integer = 0
        Dim total As Integer = CInt(My.Settings.workThreadCount)
        Dim work As workThread = Nothing

        Try
            If Me.WorkThread Is Nothing Then
                WorkThread = New ArrayList
            Else
                WorkThread.Clear()
            End If
            WorkThreadCreated = False
            For count = 0 To total - 1
                work = New workThread(Me, count)
                WorkThread.Add(work)
                work.Start()

                SyncLock (Me.WaitWorkThreadReady)
                    While (Me.Running) And (Not WorkThreadCreated)
                        Monitor.Wait(Me.WaitWorkThreadReady, 100)
                    End While

                    WorkThreadCreated = False

                End SyncLock

                work = Nothing
            Next

            result = True
        Catch ex As Exception
            Me.InternalWriteException("Error Create WorkThread : " & ex.Message, True, True)
        End Try

        Return result
    End Function

    Friend Sub signalWorkThreadCreated()
        SyncLock (Me.WaitWorkThreadReady)
            WorkThreadCreated = True
            Interlocked.Increment(Me.WorkThreadTotal)
            Monitor.Pulse(Me.WaitWorkThreadReady)
        End SyncLock
    End Sub

    Friend Sub InternalWriteInformation(ByVal message As String, Optional ByVal display As Boolean = True, Optional ByVal save As Boolean = True)
        Try
            WriteLog(message, LogTypeConstants.Information, display, save)
        Catch ex As Exception
        End Try
    End Sub

    Friend Sub InternalWriteException(ByVal message As String, Optional ByVal display As Boolean = True, Optional ByVal save As Boolean = True)
        Try
            WriteLog(message, LogTypeConstants.Exception, display, save)
        Catch ex As Exception
        End Try
    End Sub

    Private Function CheckWorkingTime(ByVal startHour As Integer, ByVal stopHour As Integer, ByVal hourToCheck As Integer) As Boolean
        Dim result As Boolean = False

        If (stopHour > startHour) Then
            If (hourToCheck >= startHour) AndAlso (hourToCheck < stopHour) Then
                result = True
            End If
        ElseIf (stopHour < startHour) Then
            If (hourToCheck >= startHour) OrElse (hourToCheck < stopHour) Then
                result = True
            End If
        ElseIf (stopHour = startHour) Then
            If (hourToCheck = startHour) AndAlso (hourToCheck = stopHour) Then
                result = True
            End If
        End If

        Return result
    End Function

    Friend Sub SignalThreadWorkerStarted()
        SyncLock (Me._waitWorkerReadyLocker)
            Interlocked.Increment(Me._activeWorkersCount)

            If (Me._activeWorkersCount >= Me.WorkThread.Count) Then
                Me._availableWorkersCount = Me._activeWorkersCount
                Me.prosesRunning()
            End If

            Monitor.Pulse(Me._waitWorkerReadyLocker)
        End SyncLock
    End Sub

    Friend Sub signalThreadWorkerAvailable()
        SyncLock (Me._waitWorkerAvailableLocker)
            Interlocked.Increment(Me._availableWorkersCount)
            Monitor.Pulse(Me._waitWorkerAvailableLocker)
        End SyncLock
    End Sub

    Friend Sub SignalThreadWorkerStopped()
        SyncLock (Me._waitWorkerStoppedLocker)
            Interlocked.Decrement(Me._activeWorkersCount)

            If (Me._activeWorkersCount < 1) Then
                Me._isAllWorkersStopped = True
                Monitor.Pulse(Me._waitWorkerStoppedLocker)
            End If
        End SyncLock
    End Sub

    Friend Function InsertJobs(ByVal item As DataSet, ByRef SqlConn As SqlConnection) As Boolean
        Dim retBool As Boolean = True
        Dim sqlComm As SqlCommand = Nothing
        Dim fileID As String = String.Empty

        SyncLock (Me._taskLocker)
            If (Not (item Is Nothing)) Then
                Try
                    ''update data processing
                    Dim row As DataRow
                    For Each row In item.Tables(0).Rows
                        fileID = row("FILEID")
                    Next
                    Dim Query As String = "UPDATE UPLOADDATA SET STATUS = 2 WHERE FILEID = @FILEID "
                    sqlComm = New SqlCommand(Query, SqlConn)
                    sqlComm.Parameters.AddWithValue("@FILEID", fileID)
                    Dim value As Integer = sqlComm.ExecuteNonQuery
                    If value > 0 Then
                        Me._taskQueue.Enqueue(item)
                        Interlocked.Decrement(Me._availableWorkersCount)
                        Monitor.PulseAll(Me._taskLocker)
                    End If
                Catch expSql As SqlClient.SqlException
                    Me.InternalWriteInformation("Exception on [EnqueueTask]")
                    Me.InternalWriteException("EnqueueTask : " & expSql.Message)
                    retBool = False
                Catch ex As Exception
                    Me.InternalWriteInformation("Exception on [EnqueueTask]")
                    Me.InternalWriteException("EnqueueTask : " & ex.Message)
                    retBool = False
                Finally
                    If (Not (sqlComm Is Nothing)) Then sqlComm.Dispose()
                End Try
            Else
                Try
                    Me._taskQueue.Enqueue(item)
                    Interlocked.Decrement(Me._availableWorkersCount)
                    Monitor.PulseAll(Me._taskLocker)
                Catch ex As Exception
                    Me.InternalWriteInformation("Exception on [EnqueueTask]queue nothing")
                    Me.InternalWriteException("EnqueueTask - queue nothing :: " & ex.Message)
                    retBool = False
                End Try
            End If
        End SyncLock
        Return retBool
    End Function


    Friend Function getJobs() As DataSet
        Dim result As DataSet = Nothing

        SyncLock (Me._taskLocker)
            While (Me._taskQueue.Count = 0)
                Monitor.Wait(Me._taskLocker, 100)
            End While

            result = Me._taskQueue.Dequeue()
        End SyncLock

        Return result
    End Function

    Friend ReadOnly Property tunnelSetting() As Hashtable
        Get
            If (Me.TunnelSettingHash Is Nothing) Then
                SyncLock Me.TunnelLocker
                    Dim connString As String = "user id=" & My.Settings.userDB & ";password=" & My.Settings.passDB & ";data source=" & My.Settings.dataSourceDB & ";initial catalog=" & My.Settings.initalCatalogDB
                    Dim sqlconn As SqlConnection = Nothing
                    Dim sqlDat As SqlDataAdapter = Nothing
                    Dim dsData As DataSet = Nothing

                    Try
                        Dim query As String = "SELECT * FROM tunnelSetting"
                        sqlconn = New SqlConnection(connString)
                        sqlDat = New SqlDataAdapter(query, sqlconn)
                        dsData = New DataSet
                        sqlDat.Fill(dsData)

                        If dsData.Tables(0).Rows.Count > 0 Then
                            Me.TunnelSettingHash = New Hashtable()
                            For Each row As DataRow In dsData.Tables(0).Rows
                                Me.TunnelSettingHash.Add(row("clientName").ToString.ToUpper & "|" & row("oprCode").ToString.ToUpper, row("tunnelName").ToString)
                            Next
                        Else
                            Throw New Exception("No data at TunnelSetting Table")
                        End If
                    Catch ex As Exception
                        Throw New Exception("Failed to load settings from table TUNNELSETTING. " & ex.Message)
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
                End SyncLock
            End If

            Return Me.TunnelSettingHash
        End Get
    End Property

    Friend ReadOnly Property MqSetting() As Hashtable
        Get
            If (Me.MqSettingHash Is Nothing) Then
                SyncLock Me.MqLocker
                    Dim connString As String = "user id=" & My.Settings.userDB & ";password=" & My.Settings.passDB & ";data source=" & My.Settings.dataSourceDB & ";initial catalog=" & My.Settings.initalCatalogDB
                    Dim sqlconn As SqlConnection = Nothing
                    Dim sqlDat As SqlDataAdapter = Nothing
                    Dim dsData As DataSet = Nothing

                    Try
                        Dim query As String = "SELECT * FROM MQSETTING"
                        sqlconn = New SqlConnection(connString)
                        sqlDat = New SqlDataAdapter(query, sqlconn)
                        dsData = New DataSet
                        sqlDat.Fill(dsData)

                        If dsData.Tables(0).Rows.Count > 0 Then
                            Me.MqSettingHash = New Hashtable()
                            For Each row As DataRow In dsData.Tables(0).Rows
                                Me.MqSettingHash.Add(row("tunnelName").ToString.ToUpper & "|" & row("oprCode").ToString.ToUpper, row("mqName").ToString)
                            Next
                        Else
                            Throw New Exception("No data at mqSetting Table")
                        End If
                    Catch ex As Exception
                        Throw New Exception("Failed to load settings from table MQSETTING. " & ex.Message)
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
                End SyncLock
            End If

            Return Me.MqSettingHash
        End Get
    End Property


    Friend ReadOnly Property prefixSetting() As Hashtable
        Get
            If (Me.PrefixSettingHash Is Nothing) Then
                SyncLock Me.PrefixLocker
                    Dim connString As String = "user id=" & My.Settings.userDB & ";password=" & My.Settings.passDB & ";data source=" & My.Settings.dataSourceDB & ";initial catalog=" & My.Settings.initalCatalogDB
                    Dim sqlconn As SqlConnection = Nothing
                    Dim sqlDat As SqlDataAdapter = Nothing
                    Dim dsData As DataSet = Nothing

                    Try
                        Dim query As String = "SELECT * FROM PREFIX"
                        sqlconn = New SqlConnection(connString)
                        sqlDat = New SqlDataAdapter(query, sqlconn)
                        dsData = New DataSet
                        sqlDat.Fill(dsData)

                        If dsData.Tables(0).Rows.Count > 0 Then
                            Me.PrefixSettingHash = New Hashtable()
                            For Each row As DataRow In dsData.Tables(0).Rows
                                Me.PrefixSettingHash.Add(row("prefix").ToString.ToUpper, row("oprCode").ToString)
                            Next
                        Else
                            Throw New Exception("No data at prefix Table")
                        End If
                    Catch ex As Exception
                        Throw New Exception("Failed to load settings from table PREFIX. " & ex.Message)
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
                End SyncLock
            End If

            Return Me.PrefixSettingHash
        End Get
    End Property


    Private Function InternalDestroyWorkers() As Boolean
        Dim result As Boolean = True      ' Assume success

        Try
            If (Not (Me.WorkThread Is Nothing)) Then
                For i As Integer = 0 To (Me.WorkThread.Count - 1)
                    ' Stop the worker by enqueing a null task.
                    Me.InsertJobs(Nothing, Nothing)
                Next
            End If
        Catch ex As Exception
            result = False
        End Try

        Return result
    End Function

    Private Sub internalProses()
        Dim workingHour() As String = CStr(My.Settings.workThread).Split("-")
        Dim startHour As Integer = CInt(workingHour(0))
        Dim stopHour As Integer = CInt(workingHour(1))
        Dim isSleeping As Boolean = False
        Dim isSearching As Boolean = False
        Dim connString As String = "user id=" & My.Settings.userDB & ";password=" & My.Settings.passDB & ";data source=" & My.Settings.dataSourceDB & ";initial catalog=" & My.Settings.initalCatalogDB
        Dim sqlConn As SqlConnection = Nothing
        Dim sqlDat As SqlDataAdapter = Nothing
        Dim dsData As DataSet = Nothing


        Try

            '' creating thread workThread
            If Not Me.InternalCreateWorkerThread() Then
                Exit Try
            End If
            Me.prosesStarted()
            RunningThread = True

            While (RunningThread)
                Dim isWorkingTime As Boolean = CheckWorkingTime(startHour, stopHour, Now.Hour)
                If (isWorkingTime) Then
                    ' Wait for any available workers.
                    SyncLock (Me._waitWorkerAvailableLocker)
                        While (Me.RunningThread) And (Me._availableWorkersCount < 1)
                            Monitor.Wait(Me._waitWorkerAvailableLocker, 100)
                        End While
                    End SyncLock

                    If (RunningThread) Then
                        Dim connected As Boolean = False

                        While Not connected
                            Try
                                sqlConn = New SqlConnection(connString)
                                sqlConn.Open()
                                connected = True
                            Catch ex As Exception
                                connected = False
                                If Not (sqlConn Is Nothing) Then
                                    sqlConn.Close()
                                    sqlConn.Dispose()
                                    sqlConn = Nothing
                                End If
                            End Try
                            If Not (Running) Then
                                Exit While
                            End If
                        End While

                        If connected Then

                            If (Not (isSearching)) Then
                                isSearching = True
                                Me.InternalWriteInformation("Searching for uploaded files..")
                            End If

                            Dim sqlQuery As String = "SELECT TOP 1 * FROM UPLOADDATA WHERE STATUS = 0 AND SENTDATE <= GETDATE() ORDER BY UPLOADDATE"
                            sqlDat = New SqlDataAdapter(sqlQuery, sqlConn)
                            dsData = New DataSet
                            sqlDat.Fill(dsData)

                            If dsData.Tables(0).Rows.Count > 0 Then
                                isSleeping = False
                                isSearching = False
                                Me.InsertJobs(dsData, sqlConn)
                            Else
                                If (Not (isSleeping)) Then
                                    ' Write the 'Sleeping' text just to inform user.
                                    Me.InternalWriteInformation("Data is not available, waiting..")
                                    isSleeping = True
                                End If
                            End If
                        End If
                    End If
                Else
                    If (Not (isSleeping)) Then
                        isSleeping = True
                        Me.InternalWriteInformation("Not in working time, sleeping..")
                    End If
                    If (Me.Running) AndAlso (CInt(My.Settings.sleepThread) > 0) Then
                        Thread.Sleep(CInt(My.Settings.sleepThread))
                    End If
                End If

                If Not (sqlConn Is Nothing) Then
                    sqlConn.Close()
                    sqlConn.Dispose()
                    sqlConn = Nothing
                End If

                If Not (sqlDat Is Nothing) Then
                    sqlDat.Dispose()
                    sqlDat = Nothing
                End If

                If Not (dsData Is Nothing) Then
                    dsData.Dispose()
                    dsData = Nothing
                End If

            End While
            '' do smothing here
            Me.InternalWriteInformation("Thread is stopping, waiting all workThread to stop..")
            Me.InternalDestroyWorkers()

            ' Wait for all workers to stop.
            SyncLock (Me._waitWorkerStoppedLocker)
                While (Not (Me._isAllWorkersStopped) And Me._activeWorkersCount > 0)
                    Monitor.Wait(Me._waitWorkerStoppedLocker, 100)
                End While

                Me._isAllWorkersStopped = False
            End SyncLock

            Me.InternalWriteInformation("All workThread stopped")

        Catch ex As Exception
            Me.InternalWriteInformation("Exception on [internalProses]")
            Me.InternalWriteException("[internalProses]: " & ex.Message)
        Finally
            If Not (sqlConn Is Nothing) Then
                sqlConn.Close()
                sqlConn.Dispose()
                sqlConn = Nothing
            End If

            If Not (sqlDat Is Nothing) Then
                sqlDat.Dispose()
                sqlDat = Nothing
            End If

            If Not (dsData Is Nothing) Then
                dsData.Dispose()
                dsData = Nothing
            End If

            Me.InternalWriteInformation("Thread stopped.")
            Me.prosesStopped()
            Me._isRestartlock = True
        End Try
       
    End Sub
End Class

