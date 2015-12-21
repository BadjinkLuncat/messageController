Imports System.ComponentModel
Imports System.Xml

Public Class xmlMessage

#Region " Variables "

    Private Const c_rootPath As String = "sms"
    Private Const c_uniqIdPath As String = c_rootPath & "/uniqid"
    Private Const c_messagePath As String = c_rootPath & "/message"
    Private Const c_msisdnPath As String = c_rootPath & "/msisdn"
    Private Const c_filenamePath As String = c_rootPath & "/filename"
    Private Const c_fileidPath As String = c_rootPath & "/fileid"
    Private Const c_uploadbyPath As String = c_rootPath & "/uploadby"
    Private Const c_maskingPath As String = c_rootPath & "/masking"
    Private Const c_clientnamePath As String = c_rootPath & "/clientname"
    Private Const c_divisionnamePath As String = c_rootPath & "/divisionname"
    Private Const c_uploaddatePath As String = c_rootPath & "/uploaddate"
    Private Const c_inqueuedatePath As String = c_rootPath & "/inqueuedate"
    Private Const c_hitdatePath As String = c_rootPath & "/hitdate"
    Private Const c_responsedatePath As String = c_rootPath & "/responsedate"
    Private Const c_tunnelPath As String = c_rootPath & "/tunnel"
    Private Const c_smsidPath As String = c_rootPath & "/smsid"
    Private Const c_oprcodePath As String = c_rootPath & "/oprcode"
    Private Const c_statushitPath As String = c_rootPath & "/statushit"
    Private Const c_reserve1Path As String = c_rootPath & "/reserve1"
    Private Const c_reserve2Path As String = c_rootPath & "/reserve2"
    Private Const c_reserve3Path As String = c_rootPath & "/reserve3"
    Private Const c_reserve4Path As String = c_rootPath & "/reserve4"

    Private Const c_xmlheader1 As String = "<?xml version=" & """" & "1.0" & """" & "?>"
    Private Const c_xmlheader1b As String = "<?xml version=" & "'" & "1.0" & "'" & "?>"

    Private m_document As XmlDocument = Nothing

#End Region

#Region " Constructors/Destructor "

    Public Sub New(Optional ByVal xmlString As String = Nothing)
        MyBase.New()

        Dim buffer As String = ""

        Try
            If (xmlString Is Nothing) OrElse (xmlString.Trim.Length < 1) Then
                buffer = xmlMessage.GetBlankXml()
            Else
                buffer = xmlString.Replace(xmlMessage.c_xmlheader1, "")
            End If

            m_document = New XmlDocument()
            m_document.LoadXml(buffer)
        Catch ex As Exception
            Throw New Exception("Unable to initialize '" & Me.GetType().Name & "' using: '" & xmlString & "'. More info: " & ex.Message)
        End Try
    End Sub

#End Region

#Region " Properties "

    Public Property UniqId() As String
        Get
            Return m_document.SelectSingleNode(xmlMessage.c_uniqIdPath).InnerText
        End Get
        Set(ByVal value As String)
            If (value Is Nothing) Then
                m_document.SelectSingleNode(xmlMessage.c_uniqIdPath).InnerText = ""
            Else
                m_document.SelectSingleNode(xmlMessage.c_uniqIdPath).InnerText = value
            End If
        End Set
    End Property

    Public Property Message() As String
        Get
            Return m_document.SelectSingleNode(xmlMessage.c_messagePath).InnerText
        End Get
        Set(ByVal value As String)
            If (value Is Nothing) Then
                m_document.SelectSingleNode(xmlMessage.c_messagePath).InnerText = ""
            Else
                m_document.SelectSingleNode(xmlMessage.c_messagePath).InnerText = value
            End If
        End Set
    End Property

    Public Property Msisdn() As String
        Get
            Return m_document.SelectSingleNode(xmlMessage.c_msisdnPath).InnerText
        End Get
        Set(ByVal value As String)
            If (value Is Nothing) Then
                m_document.SelectSingleNode(xmlMessage.c_msisdnPath).InnerText = ""
            Else
                m_document.SelectSingleNode(xmlMessage.c_msisdnPath).InnerText = value
            End If
        End Set
    End Property

    Public Property FileName() As String
        Get
            Return m_document.SelectSingleNode(xmlMessage.c_filenamePath).InnerText
        End Get
        Set(ByVal value As String)
            If (value Is Nothing) Then
                m_document.SelectSingleNode(xmlMessage.c_filenamePath).InnerText = ""
            Else
                m_document.SelectSingleNode(xmlMessage.c_filenamePath).InnerText = value
            End If
        End Set
    End Property

    Public Property FileID() As String
        Get
            Return m_document.SelectSingleNode(xmlMessage.c_fileidPath).InnerText
        End Get
        Set(ByVal value As String)
            If (value Is Nothing) Then
                m_document.SelectSingleNode(xmlMessage.c_fileidPath).InnerText = ""
            Else
                m_document.SelectSingleNode(xmlMessage.c_fileidPath).InnerText = value
            End If
        End Set
    End Property

    Public Property UploadBy() As String
        Get
            Return m_document.SelectSingleNode(xmlMessage.c_uploadbyPath).InnerText
        End Get
        Set(ByVal value As String)
            If (value Is Nothing) Then
                m_document.SelectSingleNode(xmlMessage.c_uploadbyPath).InnerText = ""
            Else
                m_document.SelectSingleNode(xmlMessage.c_uploadbyPath).InnerText = value
            End If
        End Set
    End Property

    Public Property Masking() As String
        Get
            Return m_document.SelectSingleNode(xmlMessage.c_maskingPath).InnerText
        End Get
        Set(ByVal value As String)
            If (value Is Nothing) Then
                m_document.SelectSingleNode(xmlMessage.c_maskingPath).InnerText = ""
            Else
                m_document.SelectSingleNode(xmlMessage.c_maskingPath).InnerText = value
            End If
        End Set
    End Property

    Public Property ClientName() As String
        Get
            Return m_document.SelectSingleNode(xmlMessage.c_clientnamePath).InnerText
        End Get
        Set(ByVal value As String)
            If (value Is Nothing) Then
                m_document.SelectSingleNode(xmlMessage.c_clientnamePath).InnerText = ""
            Else
                m_document.SelectSingleNode(xmlMessage.c_clientnamePath).InnerText = value
            End If
        End Set
    End Property

    Public Property DivisionName() As String
        Get
            Return m_document.SelectSingleNode(xmlMessage.c_divisionnamePath).InnerText
        End Get
        Set(ByVal value As String)
            If (value Is Nothing) Then
                m_document.SelectSingleNode(xmlMessage.c_divisionnamePath).InnerText = ""
            Else
                m_document.SelectSingleNode(xmlMessage.c_divisionnamePath).InnerText = value
            End If
        End Set
    End Property

    Public Property UploadDate() As String
        Get
            Return m_document.SelectSingleNode(xmlMessage.c_uploaddatePath).InnerText
        End Get
        Set(ByVal value As String)
            If (value Is Nothing) Then
                m_document.SelectSingleNode(xmlMessage.c_uploaddatePath).InnerText = ""
            Else
                m_document.SelectSingleNode(xmlMessage.c_uploaddatePath).InnerText = value
            End If
        End Set
    End Property

    Public Property InqueueDate() As String
        Get
            Return m_document.SelectSingleNode(xmlMessage.c_inqueuedatePath).InnerText
        End Get
        Set(ByVal value As String)
            If (value Is Nothing) Then
                m_document.SelectSingleNode(xmlMessage.c_inqueuedatePath).InnerText = ""
            Else
                m_document.SelectSingleNode(xmlMessage.c_inqueuedatePath).InnerText = value
            End If
        End Set
    End Property

    Public Property HitDate() As String
        Get
            Return m_document.SelectSingleNode(xmlMessage.c_hitdatePath).InnerText
        End Get
        Set(ByVal value As String)
            If (value Is Nothing) Then
                m_document.SelectSingleNode(xmlMessage.c_hitdatePath).InnerText = ""
            Else
                m_document.SelectSingleNode(xmlMessage.c_hitdatePath).InnerText = value
            End If
        End Set
    End Property

    Public Property ResponseDate() As String
        Get
            Return m_document.SelectSingleNode(xmlMessage.c_responsedatePath).InnerText
        End Get
        Set(ByVal value As String)
            If (value Is Nothing) Then
                m_document.SelectSingleNode(xmlMessage.c_responsedatePath).InnerText = ""
            Else
                m_document.SelectSingleNode(xmlMessage.c_responsedatePath).InnerText = value
            End If
        End Set
    End Property

    Public Property Tunnel() As String
        Get
            Return m_document.SelectSingleNode(xmlMessage.c_tunnelPath).InnerText
        End Get
        Set(ByVal value As String)
            If (value Is Nothing) Then
                m_document.SelectSingleNode(xmlMessage.c_tunnelPath).InnerText = ""
            Else
                m_document.SelectSingleNode(xmlMessage.c_tunnelPath).InnerText = value
            End If
        End Set
    End Property

    Public Property SMSId() As String
        Get
            Return m_document.SelectSingleNode(xmlMessage.c_smsidPath).InnerText
        End Get
        Set(ByVal value As String)
            If (value Is Nothing) Then
                m_document.SelectSingleNode(xmlMessage.c_smsidPath).InnerText = ""
            Else
                m_document.SelectSingleNode(xmlMessage.c_smsidPath).InnerText = value
            End If
        End Set
    End Property

    Public Property OprCode() As String
        Get
            Return m_document.SelectSingleNode(xmlMessage.c_oprcodePath).InnerText
        End Get
        Set(ByVal value As String)
            If (value Is Nothing) Then
                m_document.SelectSingleNode(xmlMessage.c_oprcodePath).InnerText = ""
            Else
                m_document.SelectSingleNode(xmlMessage.c_oprcodePath).InnerText = value
            End If
        End Set
    End Property

   
    Public Property StatusHit() As String
        Get
            Return m_document.SelectSingleNode(xmlMessage.c_statushitPath).InnerText
        End Get
        Set(ByVal value As String)
            If (value Is Nothing) Then
                m_document.SelectSingleNode(xmlMessage.c_statushitPath).InnerText = ""
            Else
                m_document.SelectSingleNode(xmlMessage.c_statushitPath).InnerText = value
            End If
        End Set
    End Property

    Public Property Reserve1() As String
        Get
            Return m_document.SelectSingleNode(xmlMessage.c_reserve1Path).InnerText
        End Get
        Set(ByVal value As String)
            If (value Is Nothing) Then
                m_document.SelectSingleNode(xmlMessage.c_reserve1Path).InnerText = ""
            Else
                m_document.SelectSingleNode(xmlMessage.c_reserve1Path).InnerText = value
            End If
        End Set
    End Property

    Public Property Reserve2() As String
        Get
            Return m_document.SelectSingleNode(xmlMessage.c_reserve2Path).InnerText
        End Get
        Set(ByVal value As String)
            If (value Is Nothing) Then
                m_document.SelectSingleNode(xmlMessage.c_reserve2Path).InnerText = ""
            Else
                m_document.SelectSingleNode(xmlMessage.c_reserve2Path).InnerText = value
            End If
        End Set
    End Property

    Public Property Reserve3() As String
        Get
            Return m_document.SelectSingleNode(xmlMessage.c_reserve3Path).InnerText
        End Get
        Set(ByVal value As String)
            If (value Is Nothing) Then
                m_document.SelectSingleNode(xmlMessage.c_reserve3Path).InnerText = ""
            Else
                m_document.SelectSingleNode(xmlMessage.c_reserve3Path).InnerText = value
            End If
        End Set
    End Property

    Public Property Reserve4() As String
        Get
            Return m_document.SelectSingleNode(xmlMessage.c_reserve4Path).InnerText
        End Get
        Set(ByVal value As String)
            If (value Is Nothing) Then
                m_document.SelectSingleNode(xmlMessage.c_reserve4Path).InnerText = ""
            Else
                m_document.SelectSingleNode(xmlMessage.c_reserve4Path).InnerText = value
            End If
        End Set
    End Property

#End Region

#Region " Methods "

    Public Function ToXmlString() As String
        Dim result As String = m_document.OuterXml.Replace("<?xml version=" & """" & "1.0" & """" & "?>", "")

        Return result
    End Function

    Public Function ToXmlDocument() As XmlDocument
        Dim xmlString As String = m_document.OuterXml.Replace("<?xml version=" & """" & "1.0" & """" & "?>", "")
        Dim result As XmlDocument = New XmlDocument()

        result.LoadXml(xmlString)
        Return result
    End Function

    Public Shared Function GetBlankXml() As String
        Return "<" & c_rootPath & ">" & _
         "<uniqid></uniqid>" & _
         "<message></message>" & _
         "<msisdn></msisdn>" & _
         "<filename></filename>" & _
         "<fileid></fileid>" & _
         "<uploadby></uploadby>" & _
         "<masking></masking>" & _
         "<clientname></clientname>" & _
         "<divisionname></divisionname>" & _
         "<uploaddate></uploaddate>" & _
         "<inqueuedate></inqueuedate>" & _
         "<hitdate></hitdate>" & _
         "<responsedate></responsedate>" & _
         "<tunnel></tunnel>" & _
         "<smsid></smsid>" & _
         "<oprcode></oprcode>" & _
         "<statushit></statushit>" & _
         "<reserve1></reserve1>" & _
         "<reserve2></reserve2>" & _
         "<reserve3></reserve3>" & _
         "<reserve4></reserve4>" & _
         "</" & c_rootPath & ">"
    End Function

#End Region

End Class
