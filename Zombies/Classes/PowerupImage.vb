Imports System.Windows.Media.Animation
Public Class PowerupImage
    Inherits Image
    Public Event OnPowerUpActivate(ByVal type As Powerup_Type)
    Public Event OnPowerUpTimesUp(P As PowerupImage)
    Public Type As Powerup_Type
    Enum Powerup_Type As Byte
        Nuke = 0
        Ammo = 1
        MP5 = 2
        Sniper = 3
        Pistol = 4
        Health = 5
        General = 6
    End Enum
    Sub New(Position As Point, P As Powerup_Type)
        Type = P
        Me.Width = 74
        Me.Height = 74
        Me.VerticalAlignment = Windows.VerticalAlignment.Top
        Me.HorizontalAlignment = Windows.HorizontalAlignment.Left
        Select Case Type
            Case Powerup_Type.Nuke
                Me.Source = New BitmapImage(New Uri(My.Computer.FileSystem.CurrentDirectory & "\powerups\nuke.png"))
            Case Powerup_Type.Ammo
                Me.Source = New BitmapImage(New Uri(My.Computer.FileSystem.CurrentDirectory & "\powerups\ammo.png"))
            Case Powerup_Type.MP5
                Me.Source = New BitmapImage(New Uri(My.Computer.FileSystem.CurrentDirectory & "\powerups\mp5.png"))
            Case Powerup_Type.Sniper
                Me.Source = New BitmapImage(New Uri(My.Computer.FileSystem.CurrentDirectory & "\powerups\sniper.png"))
            Case Powerup_Type.Pistol
                Me.Source = New BitmapImage(New Uri(My.Computer.FileSystem.CurrentDirectory & "\powerups\pistol.png"))
            Case Powerup_Type.Health
                Me.Source = New BitmapImage(New Uri(My.Computer.FileSystem.CurrentDirectory & "\powerups\health.png"))
        End Select
        Me.Margin = New Thickness(Position.X, Position.Y, 0, 0)
    End Sub
    Sub Invoke_OnPowerUpActivate()
        RaiseEvent OnPowerUpActivate(Type)
    End Sub
    Sub Invoke_OnPowerUpTimesUp()
        RaiseEvent OnPowerUpTimesUp(Me)
    End Sub
    Public WaitToExpire As RunOnceTimer
    Sub New_Loaded() Handles Me.Loaded
        WaitToExpire = New RunOnceTimer(12000, Sub()
                                                   Dispatcher.Invoke(Sub()
                                                                         Dim Anim As New DoubleAnimation(0.0, TimeSpan.FromMilliseconds(900))
                                                                         Anim.AutoReverse = True
                                                                         Dim times As Integer
                                                                         AddHandler Anim.Completed, Sub()
                                                                                                        If times = 3 Then
                                                                                                            RaiseEvent OnPowerUpTimesUp(Me)
                                                                                                        Else
                                                                                                            times += 1
                                                                                                            Me.BeginAnimation(FrameworkElement.OpacityProperty, Anim)
                                                                                                        End If
                                                                                                    End Sub
                                                                         Me.BeginAnimation(FrameworkElement.OpacityProperty, Anim)
                                                                     End Sub)
                                               End Sub)
    End Sub
End Class