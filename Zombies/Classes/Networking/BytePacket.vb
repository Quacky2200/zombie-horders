<Serializable()>
Public Class BytePacket
    Public ID As String = ""
    Public Bytes(1024) As Byte
    Public PackageSize As Integer
    Sub New(B() As Byte, Size As Integer, Name As String)
        Bytes = B
        PackageSize = Size
        ID = Name
    End Sub
End Class
<Serializable()>
Public Class BytePacketRecieved
    Public ID As String = ""
    Sub New(ID As String)
        Me.ID = ID
    End Sub
End Class
