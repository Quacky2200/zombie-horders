Imports Zombies.Shared.Serialization

<Serializable()>
Public Class GameSettings
    Public Property NewGame As Boolean = False
    Public Property Level As Integer
    Public Property Background As Byte()
    Public BackgroundArea(,) As Boolean
    Public Players As Character()

    Sub New(b() As Byte, ba(,) As Boolean, lvl As Integer, PL As Character(), NG As Boolean)
        Background = b
        BackgroundArea = ba
        Level = lvl
        Players = PL
        NewGame = NG
    End Sub
End Class