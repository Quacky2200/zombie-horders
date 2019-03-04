<Serializable()>
Public Class Message
    Public Msg As String
    Public From As String
    Sub New(ByVal Who As String, ByVal Message As String)
        Msg = Message
        From = Who
    End Sub
End Class