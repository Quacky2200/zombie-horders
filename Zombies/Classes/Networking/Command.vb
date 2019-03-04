<Serializable()>
Public Class Command
    Sub New(ByVal Message As String)
        Me.Message = Message
    End Sub
    Public Property Message As String
End Class