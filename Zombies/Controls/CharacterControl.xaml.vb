Imports System.Net
Public Class CharacterControl
#Region "Properties"
    Private Property _Angle As Double = 0
    Private Property _Health As Integer = 100
    Private Property _Points As Integer = 0
    Public Property Dead As Boolean = False
    Public Property Gun As String = "Pistol"
    Public Property Bullets As Integer = 750
    Public Property BulletStrength As Integer = 10
    Public Property BulletsInClip As Integer = 15
    Public Property Kills As Integer = 0
    Public Property ID As String = Guid.NewGuid.ToString
    Public Property EndPoint As IPEndPoint
    Public Property isReady As Boolean = False
    Public Property isShooting As Boolean = False
    'Perhaps think about this restoring...
    Public WithEvents HealthRestoreSlowly As New Timers.Timer(650)
    Public Event HealthChanged(Health As Integer)
    Public Event OnDeath()
    Public Event OnFirstStart()
    Public Event OnPointChange(Points As Integer, Amount As Integer)
    Public Event OnHeal()
    Public Event OnBulletChange()
    Public Event OnReload()
    Public Event OnLoad()
    Public Event OnNoAmmo()
    Public Event OnNewAmmo()
    Public Reloading As Boolean = False
    Public GunSounds As New System.Media.SoundPlayer(My.Computer.FileSystem.CurrentDirectory & "\Sounds\" & Gun & ".wav")
    Public Property Talking As Boolean = False
    Public Property Username As String
    Public Property Cursor As Point
    Sub LoadNewGun(GunString As String)
        Gun = GunString
    End Sub
    Sub LoadNewGun(gunString As String, ByVal bullets As Integer, clipamount As Integer, strength As Integer)
        Gun = gunString
        _Bullets = bullets
        BulletsInClip = clipamount
        BulletStrength = strength
        RaiseEvent OnLoad()
        RaiseEvent OnReload()
    End Sub
    Public Property BulletAmount As Integer
        Get
            Return _Bullets
        End Get
        Set(value As Integer)
            If _Bullets <> value Then 'If the bullets have changed
                If _Bullets = 0 And value > 0 Then 'Detect if we get new ammo after we had no ammo
                    RaiseEvent OnNewAmmo()
                ElseIf _Bullets <> (value + 1) Then
                    'Assume we've changed bullet amounts on new gun
                    RaiseEvent OnReload()
                End If
                _Bullets = value 'Then we'll change the amount
                RaiseEvent OnBulletChange() 'But we'll also have to change the bullet container to show the amount
                Dim CheckBullets As Double = _Bullets / BulletsInClip 'Get the amount of bullets in decimal
                If CheckBullets = Int(CheckBullets) Then 'So that we can check if we need to reload (in other words, when the number = .0 then we know that we need to change clips)
                    If value > 0 Then 'Only reload if the number of bullets is higher than 0
                        Reloading = True 'prevents bullets being used when we're reloading
                        RaiseEvent OnReload() 'Raise the event
                    ElseIf value = 0 Then
                        RaiseEvent OnNoAmmo()
                    End If
                End If
            End If
        End Set
    End Property
    Public ReadOnly Property Rectangle As Rect
        Get
            Return New Rect(Me.Position, New Size(Me.Width, Me.Height))
        End Get
    End Property
    Public Property Position As Point
        Get
            Return New Point(Me.Margin.Left, Me.Margin.Top)
        End Get
        Set(value As Point)
            If Me.Dead = False Then
                Me.Margin = New Thickness(value.X, value.Y, 0, 0)
            End If
        End Set
    End Property
    Public Property Angle As Double
        Get
            Return _Angle
        End Get
        Set(value As Double)
            If Dead = False Then
                _Angle = value
                DirectCast(Me.RenderTransform, RotateTransform).Angle = value
            End If
        End Set
    End Property
    Public Property Health As Integer
        Get
            Return _Health
        End Get
        Set(value As Integer)
            If value = 0 Then
                Me.Dead = True
                RaiseEvent OnDeath()
            Else
                Me._Health = value
            End If
            RaiseEvent HealthChanged(value)
        End Set
    End Property
    Public Property Points As Integer
        Get
            Return Me._Points
        End Get
        Set(value As Integer)
            Dim PointsOld As Integer = Me._Points
            Me._Points = If(value < 0, 0, value)
            RaiseEvent OnPointChange(Me._Points, value - PointsOld)
        End Set
    End Property
    Public ReadOnly Property isDead As Boolean
        Get
            Return Dead
        End Get
    End Property
    Public Sub Heal() Handles HealthRestoreSlowly.Elapsed
        Try
            Dispatcher.Invoke(Sub()
                                  If Health <> 100 Then
                                      Health += CByte(25)
                                  Else
                                      HealthRestoreSlowly.Stop()
                                      RaiseEvent OnHeal()
                                      Me.Dead = False
                                  End If
                              End Sub)
        Catch
        End Try
    End Sub
    Public Sub HealInterupt()
        HealthRestoreSlowly.Stop()
        Health = 0
        Me.Dead = True
    End Sub
#End Region
#Region "Bullet Properties"
    Private _MouseStatus As MouseButton
    Public Property MouseStatus As MouseButton
        Get
            Return _MouseStatus
        End Get
        Set(value As MouseButton)
            If value <> _MouseStatus Then
                _MouseStatus = value
                If value = MouseButton.Down Then
                    Shoot.Start()
                    isShooting = True
                ElseIf value = MouseButton.Up Then
                    Shoot.Stop()
                    Shoot.Interval = 1
                    isShooting = False
                End If
            End If
        End Set
    End Property
    Private WithEvents Shoot As New Timers.Timer(1)
    Private Sub Shooting() Handles Shoot.Elapsed
        Dispatcher.Invoke(Sub()
                              ShootBullet(Me)
                          End Sub)
    End Sub
    Sub ShootBullet(Character As CharacterControl)
        If Character.BulletAmount > 0 And Not Character.Reloading Then
            Dim CharacterCenter As New Point(Character.Margin.Left + (Character.Width / 2), Character.Margin.Top + (Character.Height / 2))
            Dim Cursor As Point = Character.Cursor
            Dim Difference As New Point(Cursor.X - CharacterCenter.X, Cursor.Y - CharacterCenter.Y)
            Dim ShootBullet As New Bullet(CharacterCenter, New Point(Cursor.X + (Difference.X * 10), Cursor.Y + (Difference.Y * 10)), Character.Angle, GetZombieFromAngle(Cursor, CharacterCenter))
            CType(Me.Parent, Grid).Children.Add(ShootBullet) 'Add it to the container
            Character.BulletAmount -= 1 'Decrease our bullets
            AddHandler ShootBullet.Remove, Sub(b As Bullet)
                                               CType(Me.Parent, Grid).Children.Remove(b)
                                           End Sub
            AddHandler ShootBullet.ShotZombie, Sub(Target As Zombie)
                                                   'Decrease the zombie health but this doesn't mean death, we just get points for being able to shoot it.
                                                   Target.Health -= Character.BulletStrength
                                               End Sub
            Character.GunSounds.Play()
            If Character.Gun <> "MP5" Then
                Shoot.Stop()
                Shoot.Interval = 1
            End If
            Shoot.Interval = 120
        End If
    End Sub
    Public Function GetZombieFromAngle(CursorPoint As Point, CharacterPoint As Point) As Zombie
        'Aim: The bullet will spawn, if the zombie is wihin the angle then whichever is closest will be killed with the bullet
        'The bullet will de-spawn at the Zombie distance
        Dim GetZombieAngle As Double = GetAngleOfPoints(CursorPoint, CharacterPoint) 'GetAngleOfPoints(New Point(Cursor.X + (Difference.X * 10), Cursor.Y + (Difference.Y * 10)), CharacterCenter) 'Get approx. zombie angle
        Dim ZombieFound As Zombie = Nothing 'Set to nothing in case we don't shoot a zombie
        Dim SortByClosest As New Dictionary(Of String, Double)
        For Each Zombie In CType(Me.Parent, Grid).Children.OfType(Of Zombie)() 'Look at each zombie
            If Zombie.Angle >= GetZombieAngle And Zombie.Angle <= (GetZombieAngle + 25) Or Zombie.Angle <= GetZombieAngle And Zombie.Angle >= (GetZombieAngle - 25) And Zombie.Dying = False Then
                SortByClosest.Add(Zombie.ID, Zombie.DistanceToTarget) 'Add to list of possible zombies
            End If
        Next
        ZombieFound = If(SortByClosest.Count > 0, ClosestZombie(SortByClosest), Nothing) 'However we want to sort by the closest zombie since that's the one that's going to get hit...
        Return ZombieFound
    End Function
    Function ClosestZombie(Radius As Dictionary(Of String, Double)) As Zombie
        Dim Closest As String = Radius.Keys(0)
        For i = 0 To Radius.Values.Count - 1
            If i > 0 Then
                If Radius.Values(i) < Radius.Item(Closest) Then
                    Closest = Radius.Keys(i)
                End If
            End If
        Next
        Return CType(Me.Parent, Grid).Children.OfType(Of Zombie).Where(Function(x) x.ID = Closest).First
    End Function
#End Region
    Private Sub UserControl_Loaded_1(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        RaiseEvent OnLoad()
    End Sub
    Private Sub CharacterImage_Loaded(sender As Object, e As RoutedEventArgs) Handles CharacterImage.Loaded
        Me.HorizontalAlignment = Windows.HorizontalAlignment.Left
        Me.VerticalAlignment = Windows.VerticalAlignment.Top
        Me.Width = 72
        Me.Height = 72
    End Sub
End Class
Public Enum MouseButton As Short
    Up = 0
    Down = 1
End Enum