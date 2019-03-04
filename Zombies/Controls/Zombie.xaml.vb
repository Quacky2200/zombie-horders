Imports System.Windows.Media.Animation

Public Class Zombie
#Region "Zombie Properties"
    Public Property ID As String = Guid.NewGuid.ToString
    Public Dying As Boolean = False
    Public Event OnMoneyEarned()
    Public Event OnDeath(Z As Zombie)
    Public Property Target As CharacterControl
    Private _Angle As Double = 0
    Private Property _Health As Integer = 20
    Function DistanceToTarget() As Double
        Try
            Return GetDistanceFromPointsXY(Target.Position, Me.Position)
        Catch : End Try
        Return Nothing
    End Function
    Public Property Angle As Double
        Get
            Return _Angle
        End Get
        Set(value As Double)
            Try
                DirectCast(Me.RenderTransform, RotateTransform).Angle = value
            Catch
            Finally
                _Angle = value
            End Try
        End Set
    End Property
    Public Property Position As Point
        Get
            Return New Point(Me.Margin.Left, Me.Margin.Top)
        End Get
        Set(value As Point)
            Me.Margin = New Thickness(value.X, value.Y, 0, 0)
        End Set
    End Property
    Public Property Health As Integer
        Get
            Return _Health
        End Get
        Set(value As Integer)
            If _Health <> value And Dying = False Then
                _Health = value
                RaiseEvent OnMoneyEarned()
                If _Health <= 0 Then
                    Dying = True
                    Dim Anim As New DoubleAnimation(0.0, TimeSpan.FromSeconds(0.3))
                    AddHandler Anim.Completed, Sub()
                                                   AttackTarget.Stop()
                                                   RaiseEvent OnDeath(Me)
                                               End Sub
                    Me.BeginAnimation(FrameworkElement.OpacityProperty, Anim)
                End If
            End If
        End Set
    End Property
#End Region
#Region "General"
    Public Sub New(Optional Pos As Point = Nothing, Optional Health As Integer = 20, Optional T As CharacterControl = Nothing)
        ' This call is required by the designer.
        InitializeComponent()
        ' Add any initialization after the InitializeComponent() call.
        Me.Position = Pos
        _Health = Health
        Target = T
    End Sub
#End Region
#Region "Zombie Movement"
    Public WithEvents AttackTarget As New Timers.Timer(1)
    Public Timeout As Integer
    Private Sub Zombie_Attack() Handles AttackTarget.Elapsed
        Try
            Dispatcher.Invoke(Sub()
                                  AttackTarget.Interval = 850 'Prevent the attack getting spammed by the movement
                                  Target.Health -= CByte(25) 'Take away the chunk of health we have
                              End Sub)
        Catch
        End Try
    End Sub
#End Region
End Class