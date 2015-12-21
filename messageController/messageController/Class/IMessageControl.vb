Public Interface IMessageControl
    Sub prosesStarted()
    Sub WriteLog(ByVal message As String, ByVal type As Integer, ByVal display As Boolean, ByVal save As Boolean)
    Sub prosesStopped()
    Sub prosesRunning()
End Interface


