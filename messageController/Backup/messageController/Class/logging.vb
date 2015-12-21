Imports System.IO
Imports System.Text
Imports System.Threading

Public Enum LogTypeConstants As Integer
    Exception = 0
    Information
End Enum

Public Class logging

#Region " Variables "

    Private Const MIN_FILE_SIZE As Integer = 10 ' in KB.

    Private m_logName As String = String.Empty
    Private m_logPath As String = String.Empty
    Private m_fileExtension As String = ".txt"
    Private m_maximumFileSize As Integer = MIN_FILE_SIZE
    Private m_dateTimeFormat As String = My.Settings.dateTimeFormat.ToString
    Private m_declaringAssembly As System.Reflection.Assembly = Nothing
    Private m_writeAssemblyInfo As Boolean = True

#End Region

#Region " Constructors/Destructor "

    Public Sub New()
        MyBase.New()

        Try
            m_declaringAssembly = System.Reflection.Assembly.GetCallingAssembly()
        Catch ex As Exception
            m_declaringAssembly = Me.GetType().Assembly
        End Try
    End Sub

    Public Sub New(ByVal inLogName As String, ByVal inLogPath As String, ByVal inMaxFileSize As Integer)
        MyBase.New()
        Me.LogName = inLogName
        Me.LogPath = inLogPath
        Me.MaximumFileSize = inMaxFileSize
        Me.InternalInitializeAssembly()
    End Sub

#End Region

#Region " Methods "

    Public Sub WriteInfo(ByVal inMessage As String)
        Dim fileName As String = String.Empty
        Dim errorMessage As String = String.Empty

        Try
            fileName = Me.InternalInitializeFile("information")

            If (Not Me.InternalWriteFile(fileName, inMessage, errorMessage)) Then
                Return
            End If
        Catch ex As Exception
            Return
        End Try
    End Sub

    Public Sub WriteError(ByVal inMessage As String)
        Dim fileName As String = String.Empty
        Dim errorMessage As String = String.Empty

        Try
            fileName = Me.InternalInitializeFile("exception")

            If (Not Me.InternalWriteFile(fileName, inMessage, errorMessage)) Then
                Return
            End If
        Catch ex As Exception
            Return
        End Try
    End Sub

    Private Sub InternalInitializeAssembly()
        Try
            m_declaringAssembly = System.Reflection.Assembly.GetCallingAssembly()
        Catch ex As Exception
            m_declaringAssembly = Me.GetType().Assembly
        End Try
    End Sub

    Private Function InternalInitializeFile(ByVal inPrefix As String) As String
        Dim fi As FileInfo = Nothing
        Dim fileName As String = String.Empty
        Dim writer As StreamWriter = Nothing

        SyncLock Me
            Try
                fileName = Me.InternalGetFileName(inPrefix)
                fi = New FileInfo(fileName)

                If Not (fi.Exists) Then
                    Dim header1 As String = "[LogName:='" & m_logName.ToUpper & "'; LogType:='" & inPrefix.ToUpper() & "']"
                    Dim header2 As String = "[DateTimeFormat:='" & m_dateTimeFormat & "'; MaxFileSize:='" & m_maximumFileSize.ToString & "KB']"
                    Dim lineLength As Integer = Math.Max(header1.Length, header2.Length)

                    Try
                        writer = fi.CreateText()
                        With writer
                            .WriteLine("{0}", New String("=", lineLength))
                            .WriteLine(header1)
                            .WriteLine(header2)
                            .WriteLine()

                            If (m_writeAssemblyInfo) Then
                                .WriteLine("[Date/Time] - [Assembly] - [Message]")
                            Else
                                .WriteLine("[Date/Time] - [Message]")
                            End If

                            .WriteLine("{0}", New String("=", lineLength))
                            .WriteLine()
                        End With
                    Catch ex As Exception
                    End Try
                End If
            Catch ex As Exception
            Finally
                If Not (writer Is Nothing) Then
                    writer.Flush()
                    writer.Close()
                    writer = Nothing
                End If
                fi = Nothing
            End Try
        End SyncLock

        Return fileName
    End Function

    Private Function InternalGetFileName(ByVal inPrefix As String) As String
        Dim di As DirectoryInfo = Nothing
        Dim fi As FileInfo = Nothing
        Dim result As StringBuilder = Nothing
        Dim writer As StreamWriter = Nothing
        Dim index As Integer = 0

        Try
            di = New DirectoryInfo(m_logPath)

            If Not (di.Exists) Then
                di.Create()
            End If

            di = New DirectoryInfo(m_logPath & Now.ToString("yyyyMM"))

            If (Not di.Exists) Then
                di.Create()
            End If

            While True
                result = New StringBuilder()

                With result
                    .Append(di.FullName)

                    If Not (result.ToString.EndsWith("\")) Then
                        .Append("\")
                    End If

                    .Append(m_logName.ToUpper())
                    .Append("." & inPrefix.ToUpper())
                    .Append("." & Now.ToString("yyyyMMdd"))
                    .Append("-" & index.ToString)
                    .Append(m_fileExtension.ToUpper())
                    fi = New FileInfo(.ToString)
                End With

                If (Not fi.Exists) Then
                    Exit While
                Else
                    If ((fi.Length \ 1024) <= m_maximumFileSize) Then
                        Exit While
                    End If
                End If

                index += 1
            End While
        Catch ex As Exception
        Finally
            di = Nothing
            fi = Nothing
        End Try

        Return result.ToString
    End Function

    Private Function InternalWriteFile(ByVal inFileName As String, ByVal inText As String, ByRef outErrorMessage As String) As Boolean
        Dim fi As FileInfo = Nothing
        Dim writer As StreamWriter = Nothing
        Dim assemblyInfo As StringBuilder = New StringBuilder()
        Dim dateInfo As String = Format(Now, m_dateTimeFormat)
        Dim asmName As System.Reflection.AssemblyName = Nothing

        SyncLock Me
            Try
                If (inFileName Is Nothing) OrElse (inFileName.Length < 0) Then
                    outErrorMessage = "[InternalWriteFile] - Failed to initialize log file."
                    Return False
                End If

                If (m_writeAssemblyInfo) Then
                    Try
                        asmName = m_declaringAssembly.GetName(True)

                        With assemblyInfo
                            .Append(asmName.Name)
                            .Append(" - v")
                            .Append(asmName.Version.ToString)
                        End With
                    Catch ex As Exception
                        assemblyInfo = New StringBuilder("?")
                    End Try

                    fi = New FileInfo(inFileName)
                    writer = fi.AppendText()

                    With writer
                        .Write(dateInfo.PadRight(20, " ") & vbTab)
                        .Write(assemblyInfo.ToString.PadRight(20, " ") & vbTab)
                        .Write(inText)
                        .WriteLine()
                    End With
                Else
                    fi = New FileInfo(inFileName)
                    writer = fi.AppendText()

                    With writer
                        .Write(dateInfo.PadRight(20, " ") & vbTab)
                        .Write(inText)
                        .WriteLine()
                    End With
                End If
            Catch ex As Exception
                outErrorMessage = "[InternalWriteFile] - " & ex.Message
                Return False
            Finally
                If Not (writer Is Nothing) Then
                    writer.Flush()
                    writer.Close()
                    writer = Nothing
                End If
                fi = Nothing
            End Try
        End SyncLock

        Return True
    End Function

#End Region

#Region " Properties "

    ''' <summary>
    ''' Gets or sets the application's name.
    ''' </summary>
    Public Property LogName() As String
        Get
            Return m_logName
        End Get
        Set(ByVal value As String)
            If (Not value Is Nothing) Then
                m_logName = value
            End If
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets the path of the log file.
    ''' </summary>
    Public Property LogPath() As String
        Get
            Return m_logPath
        End Get
        Set(ByVal value As String)
            If (Not value Is Nothing) Then
                m_logPath = value

                If (Not m_logPath.EndsWith("\")) Then
                    m_logPath &= "\"
                End If
            End If
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets the extension of the log file.
    ''' </summary>
    Public Property FileExtension() As String
        Get
            Return m_fileExtension
        End Get
        Set(ByVal value As String)
            If (Not value Is Nothing) Then
                m_fileExtension = value
            End If
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets the maximum size of the log file in Kilobytes.
    ''' </summary>
    Public Property MaximumFileSize() As Integer
        Get
            Return m_maximumFileSize
        End Get
        Set(ByVal Value As Integer)
            If (Value >= MIN_FILE_SIZE) Then
                m_maximumFileSize = Value
            End If
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets the date/time format used to write the log file contents.
    ''' </summary>
    Public Property DateTimeFormat() As String
        Get
            Return m_dateTimeFormat
        End Get
        Set(ByVal value As String)
            If (Not value Is Nothing) Then
                m_dateTimeFormat = value
            End If
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets the declaring/calling assembly.
    ''' </summary>
    Public Property DeclaringAssembly() As System.Reflection.Assembly
        Get
            Return m_declaringAssembly
        End Get
        Set(ByVal value As System.Reflection.Assembly)
            m_declaringAssembly = value
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets the value whether to write the assembly info or not.
    ''' </summary>
    Public Property WriteAssemblyInfo() As Boolean
        Get
            Return m_writeAssemblyInfo
        End Get
        Set(ByVal value As Boolean)
            m_writeAssemblyInfo = value
        End Set
    End Property

#End Region

End Class
