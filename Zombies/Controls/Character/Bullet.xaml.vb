Imports System.Windows.Media.Animation
Public Class Bullet
    Public StartPos As Point
    Public EndPos As Point
    Public Transform As Double
    Public Property ID As String = Guid.NewGuid.ToString
    Public Target As Zombie
    Public Event Remove(B As Bullet)
    Public Event ShotZombie(T As Zombie)
    Public Type As String
    Sub New(Start As Point, Ending As Point, Rotation As Double, Z As Zombie)
        ' This call is required by the designer.
        InitializeComponent()
        ' Add any initialization after the InitializeComponent() call.
        StartPos = Start
        EndPos = Ending
        Transform = Rotation
        Target = Z
        Me.Margin = New Thickness(StartPos.X, StartPos.Y, 0, 0)
        Dim CheckEvery_Pixels As Integer = 10
        Dim CheckPixels As Integer
        AddHandler Me.LayoutUpdated, Sub()
                                         If CheckPixels >= CheckEvery_Pixels Then
                                             If Not IsNothing(Target) Then
                                                 If GetDistanceFromPointsXY(New Point(Me.Margin.Left, Me.Margin.Top), Target.Position) <= 75 Then
                                                     RaiseEvent ShotZombie(Target)
                                                     Me.Visibility = Windows.Visibility.Hidden
                                                     RaiseEvent Remove(Me)
                                                 End If
                                             End If
                                             If Me.Margin.Left < 0 Or Me.Margin.Top < 0 Or Me.Margin.Top > 750 Or Me.Margin.Left > 1022 Then
                                                 Me.Visibility = Windows.Visibility.Hidden
                                                 RaiseEvent Remove(Me)
                                             End If
                                             CheckPixels = 0
                                         End If
                                         CheckPixels += 1
                                     End Sub
    End Sub
    Dim AnimationClock As AnimationClock
    Private Sub BulletContainer_Loaded(sender As Object, e As RoutedEventArgs) Handles BulletContainer.Loaded
        DirectCast(Me.RenderTransform, RotateTransform).Angle = Transform
        Dim Anim As New ThicknessAnimation(New Thickness(StartPos.X, StartPos.Y, 0, 0), New Thickness(EndPos.X, EndPos.Y, 0, 0), TimeSpan.FromMilliseconds(GetDistanceFromPointsXY(StartPos, EndPos) / 1.5))
        AddHandler Anim.Completed, Sub()
                                       RaiseEvent Remove(Me) 'Remove just in case it can't detect it
                                   End Sub
        AnimationClock = Anim.CreateClock()
        Me.ApplyAnimationClock(FrameworkElement.MarginProperty, AnimationClock)
    End Sub
    Public Sub PauseBullet()
        AnimationClock.Controller.Pause()
    End Sub
    Public Sub ResumeBullet()
        AnimationClock.Controller.Resume()
    End Sub
End Class
