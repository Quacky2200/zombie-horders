Option Strict Off

Imports System.Windows.Media.Animation
Imports System.Runtime.InteropServices
Imports System.Net.Sockets
Imports System.Net
Imports System.Text
Imports System.Runtime.Serialization
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.Reflection
Imports System.Windows.Threading
Imports Zombies.Shared.Serialization

Namespace Correctness
    MustInherit Class GameObject

    End Class
    Class Zombie
        Inherits GameObject
        Sub Update()
            ' Show movement towards the player

            'Update the position of the zombie

        End Sub
        Sub Tick()
            ' Here is the zombie thinking about going towards the player...

            ' Give calculation to next move
        End Sub
    End Class
End Namespace




Public Class MainWindow
    Public Players As New List(Of CharacterControl)
    Public Event OnLevelEnded()
    Private PowerUpSounds As New Dictionary(Of PowerupImage.Powerup_Type, List(Of String))
    Public WithEvents Character As CharacterControl
#Region "Character Management using Window Control"
    Iterator Function PlayersData() As IEnumerable(Of Character)
        For Each Player In Players
            Yield Player.Data
        Next
    End Function
    Iterator Function ReturnPlayerEndPoints() As IEnumerable(Of IPEndPoint)
        For Each Player In Players
            Yield Player.Data.EndPoint
        Next
    End Function
    Sub AddCharacterControl(CharacterCtr As CharacterControl)
        ContainerGrid.Children.Add(CharacterCtr)
        If Not Players.Contains(CharacterCtr) Then
            Players.Add(CharacterCtr)
        End If
    End Sub
    Sub RemoveCharacterControl(CharacterCtr As CharacterControl)
        ContainerGrid.Children.Remove(CharacterCtr)
        If Players.Contains(CharacterCtr) Then
            Players.Remove(CharacterCtr)
        End If
    End Sub
#End Region

#Region "General (Startup etc...)"
    Public renderingTier As Integer = (RenderCapability.Tier >> 16) 'Get the type of graphics card 
    Public TimerRefreshRate As Integer = CInt(1000 / If(renderingTier = 2, 40, If(renderingTier = 1, 30, 21))) ' So that we can base the rate of movement from it
    Sub SinglePlayerStart() 'What to start for single players
        Character = New CharacterControl
        ContainerGrid.Children.Add(Character)
        Character.LoadNewGun("Pistol", 750, 15, 10) 'Load the default weapon
        Gun1.GunImage.Source = New BitmapImage(New Uri(Environment.CurrentDirectory & "\Guns\pistol.png"))
        music_MediaEnded(Nothing, Nothing) ' Start music
        AddControlHandlers() 'Add the mouse cursor abilities
        WaitForLastZombieExit() 'Call up a new level
        Character.Width = 72
        Character.Height = 72
        Character.Position = New Point((Me.Width / 2) - (Character.Width / 2), (Me.Height / 2) - (Character.Height / 2))
    End Sub
    Sub DebugToScreen(Str As String)
        If DebugStats.Text = "Nothing to report" Then
            DebugStats.Text = ""
        End If
        DebugStats.Text += Str & Environment.NewLine
        DebugStatsScroll.ScrollToBottom()
    End Sub
    Sub New()
        ' This call is required by the designer.
        InitializeComponent()
        ' Add any initialization after the InitializeComponent() call.s
        'MsgBox("This version is limited because it has been made to the requirements")
        'Dim msgboxr As MsgBoxResult = MsgBox("Check out the latest version! Do you want me to redirect you?", MsgBoxStyle.YesNo, "Latest Version")
        'If msgboxr = MsgBoxResult.Yes Then
        '    System.Diagnostics.Process.Start("https://drive.google.com/folderview?id=0B8Q5V2NDr4X7RlhRdUF6UGNuVjA&usp=sharing")
        'End If
        LanSpindle.Pause()
    End Sub
    Sub Me_Started() Handles Me.Loaded
        GrabMaps() 'Find the maps before starting the game
        GrabVoiceEffects() ' Find the voice effects for the game
        VolumeKnobController.Margin = New Thickness((My.Settings.Volume / 100) * 110, 0, 0, 0)
        voice.Volume = My.Settings.Volume / 100
        sounds.Volume = My.Settings.Volume / 100
        music.Volume = (My.Settings.Volume / 2) / 100
    End Sub
    Sub GrabVoiceEffects()
        Dim FileCol As IEnumerable(Of String) = System.IO.Directory.EnumerateFiles(System.IO.Directory.GetCurrentDirectory() & "\Sounds\powerups")
        For Each Str As String In FileCol
            If Str.EndsWith(".wav") Then
                If PowerUpSounds.ContainsKey(CheckEnum(Str)) Then
                    PowerUpSounds.Item(CheckEnum(Str)).Add(Str)
                Else
                    PowerUpSounds.Add(CheckEnum(Str), New List(Of String))
                    PowerUpSounds.Item(CheckEnum(Str)).Add(Str)
                End If
            End If
        Next
    End Sub
    Public Function CheckEnum(ByRef Text As String) As PowerupImage.Powerup_Type
        For Each Value As PowerupImage.Powerup_Type In [Enum].GetValues(GetType(PowerupImage.Powerup_Type)) 'We get each value in the enumeration
            If Text.ToLower.Contains(Value.ToString.ToLower) Then 'detect whether it contains the subject name (allows us to comment on the subject line like //(own)emotions)
                Return Value 'Return the value we want
                Exit Function 'We won't need to search the other lists (esp. if the list's long)
            End If
        Next
        Return PowerupImage.Powerup_Type.General 'If we never found a suitable subject, we'll ignore the word/letter
    End Function
    Private Sub Frame_Closing(sender As Object, e As ComponentModel.CancelEventArgs) Handles Frame.Closing
        'If Lan.Visibility = Windows.Visibility.Visible Then 'Detect whether we're looking for a network game
        '    btnLanBack_MouseUp(Nothing, Nothing)
        'End If
        StopAllZombieActivity = True
    End Sub
    Function ReturnZombies() As Zombie()
        Return ContainerGrid.Children.OfType(Of Zombie)().ToArray
    End Function
    Function ReturnPowerups() As PowerupImage()
        Return ContainerGrid.Children.OfType(Of PowerupImage)().ToArray
    End Function
    Function ReturnBullets() As Bullet()
        Return ContainerGrid.Children.OfType(Of Bullet)().ToArray()
    End Function
    Function ReturnZombie(ID As String) As Zombie
        For Each ev In ReturnZombies.Where(Function(x) x.ID = ID)
            Return ev
            Exit Function
        Next
        Return Nothing
    End Function
#End Region
#Region "Map Functions"
    Dim MapLibrary As New List(Of String)
    Dim SelectedMap As Integer = 0
    Dim SelectedMapArea(1022, 750) As Boolean
    Sub GenerateMap()
        Dim PastMap As Integer = SelectedMap ' Get the previous map to prevent getting the same random integer
        Do Until SelectedMap <> PastMap 'Find a number so that it's different from the previous
            SelectedMap = (New Random).Next(0, MapLibrary.Count - 1)
        Loop
        Map.Background = New ImageBrush(DirectCast(New BitmapImage(New System.Uri(MapLibrary(SelectedMap))), ImageSource)) ' Put in the Map image
        GrabBoundries(New System.Uri(MapLibrary(SelectedMap).Replace(".png", "-boundries.png"))) 'We need to grab the boundries too (this is fairly simple and quite fast)
    End Sub
    Sub GrabMaps() 'Find the maps for the game
        Dim FileCol As IEnumerable(Of String) = System.IO.Directory.EnumerateFiles(System.IO.Directory.GetCurrentDirectory() & "\Maps") 'Grab a list of filenames from this directory
        For Each Str As String In FileCol 'go through each image map
            Debug.Print(Str)
            If Str.EndsWith(".png") And Str.Contains("-boundries") = False Then 'Select the ones that end in .png and not the boundries!
                MapLibrary.Add(Str)
                Debug.Print(Str)
            End If
        Next
    End Sub
    Sub GrabBoundries(Uri As System.Uri)
        'Try
        Dim BitmapImage As New BitmapImage(Uri) 'Make a new bitmap image from the URI to get a basic boundries system working (based on black and white). We're going to 'copy' the pixels
        Dim stride = BitmapImage.PixelWidth * 4 'Make the strides to get each individual pixel
        Dim size = BitmapImage.PixelHeight * stride 'Make the size for the array
        Dim parray(size) As Byte 'Make temp color array to store each color pixel seperately
        BitmapImage.CopyPixels(parray, stride, 0) 'Copy the pixels to the array
        For x = 0 To BitmapImage.PixelWidth - 4 'Go through each pixel in width
            Dim binarystr As String = ""
            For y = 0 To BitmapImage.PixelHeight - 4 'go through the y pixels
                Dim i As Integer = y * stride + 4 * x 'Get the necessary pixel
                If parray(i) = 0 And parray(i + 1) = 0 And parray(i + 2) = 0 Then
                    SelectedMapArea(x, y) = False 'if it's a boundry, add to pixels
                Else
                    SelectedMapArea(x, y) = True 'otherwise allow the player to move
                End If
            Next y
        Next x
        'Catch ex As Exception
        '     MsgBox(ex.Message)
        ' End Try
    End Sub
    Function WithinBounds(p As Point) As Boolean
        If WithinWindow(p) AndAlso SelectedMapArea(CInt(p.X), CInt(p.Y)) Then
            Return True
        Else
            Return False
        End If
    End Function
    Function WithinWindow(p As Point) As Boolean
        If p.X > 0 And p.Y > 0 And p.X < 1022 And p.Y < 750 Then
            Return True
        Else
            Return False
        End If
    End Function
#End Region
#Region "Keyboard And Mouse Controls"
    Public Enum Keyboard As Short 'This enum makes it easier to tell which direction we're going in
        Left = 0
        Right = 1
        Up = 2
        Down = 3
    End Enum
    Dim WithEvents KeyDownH, KeyDownV As New Timers.Timer(TimerRefreshRate - 10) 'Use a few timers to make the character be able to move
    Dim Horizontal, Vertical As Keyboard 'We also need to tell the difference in the horizontal and vertical keys
    Sub Horizontal_Keys() Handles KeyDownH.Elapsed
        Dispatcher.Invoke(Sub()
                              Try
                                  If Horizontal = Keyboard.Left And WithinBounds(New Point(Character.Position.X + 7, Character.Position.Y + 4)) Then
                                      'If we're trying to go left and the character is within the boundries ^^ and if the character is in the window, then we'll move them
                                      Character.Position = New Point(Character.Margin.Left - If(KeyDownV.Enabled = True, 4, 6), Character.Margin.Top)
                                  ElseIf Horizontal = Keyboard.Right And WithinBounds(New Point(Character.Position.X + Character.Width - 7, Character.Position.Y + 7)) Then
                                      'If we're trying to go right and the character is within the boundries ^^ and if the character is in the window, then we'll move them
                                      Character.Position = New Point(Character.Margin.Left + If(KeyDownV.Enabled = True, 4, 6), Character.Margin.Top)
                                  End If
                                  MouseHandler(Nothing, Nothing) 'Let's make the character angle change with this method
                              Catch
                                  KeyDownH.Stop()
                                  'DebugToScreen(Character.Position.ToString)
                              End Try
                          End Sub)
    End Sub
    Sub Vertical_Keys() Handles KeyDownV.Elapsed
        Dispatcher.Invoke(Sub()
                              Try
                                  If Vertical = Keyboard.Up And WithinBounds(New Point(Character.Position.X + 7, Character.Position.Y + 6)) Then
                                      'If we're trying to go up and the character is within the boundries ^^ and if the character is in the window, then we'll move them
                                      Character.Position = New Point(Character.Margin.Left, Character.Margin.Top - If(KeyDownH.Enabled = True, 4, 6))
                                  ElseIf Vertical = Keyboard.Down And WithinBounds(New Point(Character.Position.X + 7, Character.Position.Y + Character.Height - 6)) Then
                                      'If we're trying to go down and the character is within the boundries ^^ and if the character is in the window, then we'll move them
                                      Character.Position = New Point(Character.Margin.Left, Character.Margin.Top + If(KeyDownH.Enabled = True, 4, 6))
                                  End If
                                  MouseHandler(Nothing, Nothing) 'Make sure we update the rotation of the character
                              Catch
                                  'DebugToScreen(Character.Position.ToString)
                                  KeyDownV.Stop()
                              End Try
                          End Sub)
    End Sub
    Sub AddControlHandlers()
        'Let's be able to add these mouse handlers when we start the game
        AddHandler MyBase.MouseLeftButtonDown, AddressOf MainWindow_MouseLeftButtonDown
        AddHandler MyBase.MouseLeftButtonUp, AddressOf MainWindow_MouseLeftButtonUp
        AddHandler MyBase.MouseMove, AddressOf MouseHandler
        AddHandler MyBase.KeyDown, AddressOf Frame_KeyDown
        AddHandler MyBase.KeyUp, AddressOf Frame_KeyUp
    End Sub
    Sub RemoveControlHandlers()
        'However, we'll also need to remove them later...
        RemoveHandler MyBase.MouseLeftButtonDown, AddressOf MainWindow_MouseLeftButtonDown
        RemoveHandler MyBase.MouseLeftButtonUp, AddressOf MainWindow_MouseLeftButtonUp
        RemoveHandler MyBase.KeyDown, AddressOf Frame_KeyDown
        RemoveHandler MyBase.KeyUp, AddressOf Frame_KeyUp
        RemoveHandler MyBase.MouseMove, AddressOf MouseHandler
    End Sub
    Private Sub MouseHandler(sender As Object, e As Windows.Input.MouseEventArgs)
        If Not IsNothing(sender) AndAlso Not IsNothing(e) Then
            Character.Data.Cursor = e.GetPosition(CType(sender, IInputElement))
        End If
        Dim Cursor As New Point(System.Windows.Forms.Cursor.Position.X - Me.Left, System.Windows.Forms.Cursor.Position.Y - Me.Top)
        Character.Angle = GetAngleOfPoints(New Point(Character.Margin.Left + Character.Width / 2, Character.Margin.Top + Character.Height / 2), Cursor)
        If Leaderboard.Visibility = Windows.Visibility.Hidden And Menu.Visibility = Windows.Visibility.Hidden Then
            CursorContainer.Margin = New Thickness(Cursor.X - (CursorContainer.Width / 2), Cursor.Y - (CursorContainer.Height / 2), 0, 0)
        End If
    End Sub
    Dim musicpausedtimespan As TimeSpan
    Dim musicpausedsource As Uri
    Private Sub Frame_KeyDown(sender As Object, e As KeyEventArgs) 'Simple relay to start the movement until we remove our fingers
        If (e.Key = Key.A) Then
            Horizontal = Keyboard.Left
            KeyDownH.Start()
        ElseIf (e.Key = Key.D) Then
            Horizontal = Keyboard.Right
            KeyDownH.Start()
        ElseIf (e.Key = Key.W) Then
            Vertical = Keyboard.Up
            KeyDownV.Start()
        ElseIf (e.Key = Key.S) Then
            Vertical = Keyboard.Down
            KeyDownV.Start()
        ElseIf (e.Key = Key.Escape) Then
            If PauseMenu.Visibility = Windows.Visibility.Hidden Then
                PauseMenu.Visibility = Windows.Visibility.Visible
                RemoveControlHandlers()
                AddHandler Me.KeyDown, AddressOf PausedFrame_KeyDown
                KeyDownH.Stop()
                KeyDownV.Stop()
                music.Pause()
                ZM.Stop()
                ContainerGrid.Cursor = Cursors.Arrow
                NewZombieInterval.Stop()
                Dim BR As New Effects.BlurEffect
                BR.Radius = 50
                ContainerGrid.Effect = BR
                CurrentScoreLabel.Content = ScoreDisplay.Text
                CurrentLevelLabel.Content = CStr(Level)
                CurrentKillsLabel.Content = CStr(Character.Data.Kills)
                For Each powerup In ReturnPowerups()
                    powerup.WaitToExpire.Stop_RunOnceTimer()
                Next
                For Each Bullet In ReturnBullets()
                    Bullet.PauseBullet()
                Next
                musicpausedsource = music.Source
                musicpausedtimespan = music.Position
            End If
        End If
    End Sub
    Private Sub PausedFrame_KeyDown(sender As Object, e As KeyEventArgs)
        If (e.Key = Key.Escape) Then
            If PauseMenu.Visibility = Windows.Visibility.Visible Then
                Resume_MouseUp()
            End If
        End If
    End Sub
    Private WithEvents VolumeTimer As New Timers.Timer(TimerRefreshRate)
    Private VolumeE As MouseButtonEventArgs
    Private Sub VolumeTimer_Elapsed() Handles VolumeTimer.Elapsed
        Dispatcher.Invoke(Sub()
                              Dim CursorX As Integer = CInt((System.Windows.Forms.Cursor.Position.X - Left) - VolumeGrid.Margin.Left) - 25
                              Dim VolumeToCursor As Integer = If(CursorX > 20, If(CursorX < 150, CursorX, 150), 20)
                              VolumeKnobController.Margin = New Thickness(VolumeToCursor, 0, 0, 0)
                              voice.Volume = (VolumeToCursor - 20) / 110
                              sounds.Volume = (VolumeToCursor - 20) / 110
                              music.Volume = (((VolumeToCursor - 20) / 2) / 110)
                              My.Settings.Volume = CInt((VolumeToCursor / 110) * 100)
                              My.Settings.Save()
                              If Not VolumeE.LeftButton = MouseButtonState.Pressed Then
                                  VolumeTimer.Stop()
                                  music.Position = musicpausedtimespan
                                  music.Stop()
                              End If
                          End Sub)
    End Sub
    Private Sub VolumeKnobController_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles VolumeKnobController.MouseDown
        VolumeE = e
        VolumeTimer.Start()
        music.Play()
    End Sub
    Private Sub Resume_MouseUp() Handles ResumeLabel.MouseUp
        AddControlHandlers()
        NewZombieInterval.Start()
        ZM.Start()
        ContainerGrid.Cursor = Cursors.None
        Dim BR As New Effects.BlurEffect
        BR.Radius = 0
        ContainerGrid.Effect = BR
        ContainerGrid.Effect = Nothing
        For Each powerup In ReturnPowerups()
            powerup.WaitToExpire.Resume_RunOnceTimer()
        Next
        For Each Bullet In ReturnBullets()
            Bullet.ResumeBullet()
        Next
        music.Source = musicpausedsource
        music.Position = musicpausedtimespan
        music.Play()
        PauseMenu.Visibility = Windows.Visibility.Hidden
        RemoveHandler Me.KeyDown, AddressOf PausedFrame_KeyDown
    End Sub
    Private Sub Frame_KeyUp(sender As Object, e As KeyEventArgs) 'Make sure we can stop the character when required
        If e.Key = Key.W And Vertical = Keyboard.Up Or e.Key = Key.S And Vertical = Keyboard.Down Then
            KeyDownV.Stop()
        ElseIf e.Key = Key.A And Horizontal = Keyboard.Left Or e.Key = Key.D And Horizontal = Keyboard.Right Then
            KeyDownH.Stop()
        End If
    End Sub
#End Region
#Region "Character Properties"
    Private Sub SyncCharacterChanges() Handles Character.SyncChanges
        If Not IsNothing(Client) Then
            If Client.Connected Then
                Client.Send(ServerIP, Character.Data)
            End If
        End If
    End Sub
    Private Sub Character_Health_Change(ByVal Health As Integer) Handles Character.HealthChanged 'Animate the healthbar when our health changes
        Dim Anim As New DoubleAnimation(Health, TimeSpan.FromSeconds(0.3))
        Healthbar.BeginAnimation(FrameworkElement.WidthProperty, Anim)
    End Sub
    Private Sub Character_On_Death() Handles Character.OnDeath 'show the leaderboard/you died menu
        RemoveControlHandlers()
        Character.Points -= 200
        StopAllZombieActivity = True
        ContainerGrid.Cursor = Cursors.Arrow
        Dim BR As New Effects.BlurEffect
        BR.Radius = 50
        ContainerGrid.Effect = BR
        Leaderboard.Visibility = Windows.Visibility.Visible
        ScoreOutput.Content = ScoreDisplay.Text
        LevelOutput.Content = CStr(Level)
        KillsOutput.Content = CStr(Character.Data.Kills)
    End Sub
    Private Sub Character_On_Points_Change(value As Integer, amount As Integer) Handles Character.OnPointChange 'Show the points and animate them
        ScoreDisplay.Text = CStr(value)
        ScoreAmount.Content = If(amount > 0, CStr("+" & amount), CStr(amount))
        ScoreAmount.Margin = New Thickness(ScoreAmount.Margin.Left, ScoreAmount.Margin.Top, ScoreDisplay.Text.Length * 15, ScoreAmount.Margin.Bottom)
        Dim Anim As New DoubleAnimation(1.0, 0.0, TimeSpan.FromSeconds(0.2))
        ScoreAmount.BeginAnimation(FrameworkElement.OpacityProperty, Anim)
    End Sub
    Private Sub MainWindow_Closing(sender As Object, e As ComponentModel.CancelEventArgs) Handles Me.Closing
        KeyDownH.Stop()
        KeyDownV.Stop()
        ZM.Stop()
    End Sub
    Private Sub Character_Bullets() Handles Character.OnBulletChange 'This event handler happens when we shoot
        Dim WidthPerBullet = 100 / Character.Data.BulletsInClip
        Dim CurrentClip = (CInt(Mid(CStr(BulletAmountLabel.Content), 2)) * Character.Data.BulletsInClip)
        Dim BulletsLeft = Character.BulletAmount - CurrentClip
        BulletsInClipAmountLabel.Width = WidthPerBullet * BulletsLeft
        'What we do here is we have to determine a few things. We need to get the amount we have for each bullet so we devide the total bullets in a clip by the total width.
        'We then get the amount of bullets we have and take it away from the clips so that we know how many bullets are left.
    End Sub
    Private Sub Character_Reloading() Handles Character.OnReload 'This will only happen when needing to change clips (AKA, reloading)
        Character.GunSounds = New System.Media.SoundPlayer(My.Computer.FileSystem.CurrentDirectory & "\Sounds\reload.wav")
        Character.GunSounds.Play()
        Character.GunSounds = New System.Media.SoundPlayer(My.Computer.FileSystem.CurrentDirectory & "\Sounds\" & Character.Data.Gun & ".wav")
        Dim Anim As New DoubleAnimation(100, TimeSpan.FromSeconds(1.5))
        BulletAmountLabel.Content = "x" & CStr(CInt((Character.BulletAmount / Character.Data.BulletsInClip) - 1)) 'We have to change the total of clips we have
        Anim.FillBehavior = FillBehavior.Stop
        Dim HighlightShow As New DoubleAnimation(1.0, TimeSpan.FromSeconds(0.8))
        HighlightShow.FillBehavior = FillBehavior.Stop
        HighlightShow.AutoReverse = True
        Dim StopAnimation As Boolean = False
        AddHandler Anim.Completed, Sub()
                                       StopAnimation = True
                                       Character.Data.Reloading = False 'We make sure we can shoot again afterwards now the process is done.
                                       BulletsInClipAmountLabel.Width = 100
                                   End Sub
        BulletsInClipAmountLabel.BeginAnimation(FrameworkElement.WidthProperty, Anim)
        Reloading.Visibility = Windows.Visibility.Visible
        Reloading.BeginAnimation(FrameworkElement.OpacityProperty, HighlightShow)
        AddHandler HighlightShow.Completed, Sub()
                                                If StopAnimation = False Then
                                                    Reloading.BeginAnimation(FrameworkElement.OpacityProperty, HighlightShow)
                                                Else
                                                    Reloading.Opacity = 0
                                                    Reloading.Visibility = Windows.Visibility.Hidden
                                                End If
                                            End Sub
        NoAmmo.BeginAnimation(FrameworkElement.OpacityProperty, HighlightShow)
    End Sub
    Private Sub Character_LoadGun() Handles Character.OnLoad
        BulletAmountLabel.Content = "x" & CStr((Character.BulletAmount / Character.Data.BulletsInClip) - 1)
        BulletsInClipAmountLabel.Width = 100
    End Sub
    Private Sub Character_RunningOnEmpty() Handles Character.OnNoAmmo
        NoAmmo.Visibility = Windows.Visibility.Visible
        Dim HighlightShow As New DoubleAnimation(1.0, TimeSpan.FromSeconds(0.8))
        HighlightShow.FillBehavior = FillBehavior.Stop
        HighlightShow.AutoReverse = True
        Dim StopAnimation As Boolean = False
        NoAmmo.BeginAnimation(FrameworkElement.OpacityProperty, HighlightShow)
        AddHandler Character.OnNewAmmo, Sub()
                                            StopAnimation = True
                                        End Sub
        AddHandler HighlightShow.Completed, Sub()
                                                If StopAnimation = False Then
                                                    NoAmmo.BeginAnimation(FrameworkElement.OpacityProperty, HighlightShow)
                                                Else
                                                    NoAmmo.Opacity = 0
                                                    NoAmmo.Visibility = Windows.Visibility.Hidden
                                                End If
                                            End Sub
        NoAmmo.BeginAnimation(FrameworkElement.OpacityProperty, HighlightShow)
    End Sub
#End Region
#Region "Bullet Properties"
    Private Sub MainWindow_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
        Character.MouseStatus = MouseButton.Down
    End Sub
    Private Sub MainWindow_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs)
        Character.MouseStatus = MouseButton.Up
    End Sub
#End Region
#Region "Zombie AI"
    'Public Shared Zombies As New CustomDictionary(Of String, Zombie)
    ''' <summary>
    ''' Closest player recognition and zombie collision detection.
    ''' </summary>
    ''' <remarks></remarks>
    Function ClosestPlayer(RadiusList As List(Of Double)) As CharacterControl
        Dim Closest As Double = -1 'The actual value we get
        Dim ClosestPicked As Integer 'The value of the player
        For i = 0 To RadiusList.Count - 1
            If Closest = -1 Or RadiusList(i) < Closest Then
                Closest = RadiusList(i) 'Set the lowest we've found
                ClosestPicked = i 'Set the player number for later
            End If
        Next
        Return Players(ClosestPicked)
    End Function
#End Region
#Region "Zombie  Movement"
    Dim StopAllZombieActivity As Boolean = False
    Dim WithEvents ZM As New Timers.Timer(TimerRefreshRate - 5)
    Dim CheckCounter As Byte = 0
    Sub ZM_Elapsed() Handles ZM.Elapsed
        Dispatcher.Invoke(Sub()
                              If StopAllZombieActivity = True Then
                                  Try
                                      For Each Z In ReturnZombies()
                                          Z.AttackTarget.Stop()
                                      Next
                                      ZM.Stop()
                                  Catch
                                  End Try
                              Else
                                  If CheckCounter = 2 Then
                                      For Z = 1 To ReturnZombies.Count - 1
                                          With ReturnZombies(Z)
                                              'Detect if any zombies are close to each other, if so, temporarily disable their movement
                                              Dim OlderZombie As Zombie = ReturnZombies(Z - 1) 'Retrieve the older zombie
                                              If GetDistanceFromPointsXY(.Position, OlderZombie.Position) < 72 Then 'If it's near/within it's boundries
                                                  If .DistanceToTarget < OlderZombie.DistanceToTarget Then 'Then we need to stop whichever one is behind
                                                      OlderZombie.Timeout = 4 'So that the one ahead can go first.
                                                      'The Older zombie must be in behind if the distance is greater
                                                  Else
                                                      .Timeout = 4 'Otherwise we disable the one we were checking
                                                  End If
                                              End If
                                              'Get the Closest Player if on multiplayer (still needs perfecting)
                                              If Players.Count > 1 Then
                                                  Dim ClosestFromMean As New List(Of Double)
                                                  For Each Player In Players
                                                      If Player.Margin.Left - .Margin.Left >= 0 Then
                                                          If Player.Margin.Top - .Margin.Top >= 0 Then
                                                              ClosestFromMean.Add(((Player.Margin.Left - .Margin.Left) + (Player.Margin.Top - .Margin.Top)) / 2)
                                                          Else
                                                              ClosestFromMean.Add(((Player.Margin.Left - .Margin.Left) + (.Margin.Top - Player.Margin.Top)) / 2)
                                                          End If
                                                      Else
                                                          If Player.Margin.Top - .Margin.Top >= 0 Then
                                                              ClosestFromMean.Add(((.Margin.Left - Player.Margin.Left) + (Player.Margin.Top - .Margin.Top)) / 2)
                                                          Else
                                                              ClosestFromMean.Add(((.Margin.Left - Player.Margin.Left) + (.Margin.Top - Player.Margin.Top)) / 2)
                                                          End If
                                                      End If
                                                  Next
                                                  .Target = ClosestPlayer(ClosestFromMean)
                                              Else

                                              End If
                                          End With
                                      Next Z
                                      CheckCounter = 0
                                  End If
                                  CheckCounter = CByte(CheckCounter + 1)
                                  For Each Z In ReturnZombies()
                                      If Not IsNothing(Z) And Not IsNothing(Z.Target) Then 'And Not IsNothing(Z.Target) Then
                                          Dim Difference As New Point(Z.Position.X - Z.Target.Position.X, Z.Position.Y - Z.Target.Position.Y)
                                          Dim DifferenceAlwaysPositive As New Point(If(Difference.X < 0, -Difference.X * 1, Difference.X), If(Difference.Y < 0, -Difference.Y * 1, Difference.Y))
                                          '*this will get how close it is no-matter if it's left or right of the zombie etc...Simply by always making it positive
                                          If DifferenceAlwaysPositive.X >= 65 Or DifferenceAlwaysPositive.Y >= 65 Then 'If the zombie is *anywhere* near 45 to the target then attack
                                              Z.AttackTarget.Stop() 'If the target does move, stop attacking and 
                                              Z.AttackTarget.Interval = 1 'Set the attack to 1
                                              Dim NewPosition As Point = New Point(Z.Position.X - If(Difference.X < 0, -1, 1), Z.Position.Y - If(Difference.Y < 0, -1, 1))
                                              'The new position is where the Zombie will go to (in order to attack the target)
                                              Dim FirstPoint As New Point(Z.Target.Margin.Left + (Z.Target.Width / 2), Z.Target.Margin.Top + (Z.Target.Height / 2)) 'Get the central point of the Player
                                              Dim SecondPoint As New Point(Z.Margin.Left + Z.Width / 2, Z.Margin.Top + (Z.Height / 2)) 'Get the position of the zombie
                                              Z.Angle = GetAngleOfPoints(SecondPoint, FirstPoint) 'Set the angle
                                              If Z.Timeout = 0 Then 'Just go if the movement is true
                                                  Z.Position = NewPosition
                                              Else
                                                  Z.Timeout -= 1 'However we set a delay to allow the other zombie to move away
                                              End If
                                          ElseIf Z.Dying = False Then : Z.AttackTarget.Start() 'Otherwise, if we're near the target, we start attacking
                                          End If
                                      End If
                                  Next
                              End If
                              For Each P In ReturnPowerups()
                                  Dim CharRect As New Rect(Character.Position, New Size(Character.Width, Character.Height))
                                  Dim PowerRect As New Rect(New Point(P.Margin.Left, P.Margin.Top), New Size(P.Width, P.Height))
                                  If CharRect.IntersectsWith(PowerRect) Then
                                      P.Invoke_OnPowerUpActivate()
                                      P.Invoke_OnPowerUpTimesUp()
                                  End If
                              Next
                          End Sub)
    End Sub
#End Region
#Region "Menu"
    Private Sub btnQuit_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles btnQuit.MouseDown
        Me.Close()
        End
    End Sub
    Private Sub btnHelp_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles btnHelp.MouseUp
        Help.Visibility = Windows.Visibility.Visible
    End Sub
    Private Sub Help_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles Help.MouseUp
        Help.Visibility = Windows.Visibility.Hidden
    End Sub
    Private Sub btnLanBack_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles btnLanBack.MouseUp
        Lan.Visibility = Windows.Visibility.Hidden
        Try
            If Client.Connected = True And ServerIP.Address.ToString <> LocalHost() Then
                Client.Send(ServerIP, New Command("/ZH --disconnect " & Environment.UserName))
            ElseIf Client.Connected And Not IsNothing(Server) And ServerIP.Address.ToString = LocalHost() Then
                Server.Send(ReturnPlayerEndPoints().ToArray, New Command("/ZH --ServerClose"))
            End If
        Catch
            Debug.Print("WASNT CONNECTED")
        End Try
        WaitForPlayers.Interval = 100
        WaitForPlayers.Stop()
        LANReadyUp.Visibility = Windows.Visibility.Hidden
        Client.Connected = False
        LanSpindle.Pause()
        '  Client.Players = Nothing
        LANPlayers.Items.Clear()
        Client.StopClient()
        If Not IsNothing(Server) Then : Server.StopClient() : End If
        LANProgressStatus.Content = "Searching..."
    End Sub
    Private Sub btnLan_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles btnLan.MouseUp
        Lan.Visibility = Windows.Visibility.Visible
        LanSpindle.Resume()
        'start detecting clients/servers
        Client = New ZombieClient(30010, 80)
        UserStatus.Content = ""
    End Sub
    Private Sub btnSolo_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles btnSolo.MouseUp
        Menu.Visibility = Windows.Visibility.Hidden
        SinglePlayerStart()
    End Sub
    Private Sub btnMenu_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles btnMenu.MouseUp
        ContainerGrid.Effect = Nothing
        Character = Nothing
        Menu.Visibility = Windows.Visibility.Visible
        Leaderboard.Visibility = Windows.Visibility.Hidden
        Character = New CharacterControl
        music.Stop()
        Process.Start(Environment.CurrentDirectory & "\Zombie Horders.exe")
        End
    End Sub
    Private Sub LANReadyUp_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles LANReadyUp.MouseUp
        If Client.Connected = True Or Lan.Visibility = Windows.Visibility.Visible Then
            Client.Send(ServerIP, New Command("/ZH --ready"))
            LANReadyUp.Visibility = Windows.Visibility.Hidden
        End If
    End Sub

    Private Sub Setup_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles Setup.MouseUp
        WaitForPlayers.Start()
    End Sub

    Private Sub ManualIP_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles ManualIP.MouseUp
        Dim newaddress As String = ""
        Dim addr As IPEndPoint = Nothing
        Dim tmrconnect As New Timers.Timer(1000)
        AddHandler tmrconnect.Elapsed, Sub()
                                           Dispatcher.Invoke(Sub()
                                                                 If Not Client.Connected Then
                                                                     LANProgressStatus.Content = "Joining " & newaddress
                                                                     Client.Send(addr, New Command("/ZH --join " & Environment.UserName))
                                                                     'Try

                                                                     'Catch
                                                                     '    MsgBox("Couldn't connect to address, address is incorrect or null")
                                                                     '    tmrconnect.Stop()
                                                                     'End Try
                                                                 End If
                                                             End Sub)
                                       End Sub
        Dim IPAddr As New ManualAddress({Sub(X As ManualAddress)
                                             X.txtAddr.Text = ""
                                             X.Hide()
                                         End Sub,
                                         Sub(X As ManualAddress)
                                             newaddress = X.txtAddr.Text
                                             Try
                                                 addr = New IPEndPoint(IPAddress.Parse(newaddress), 80)
                                                 tmrconnect.Start()
                                             Catch ex As Exception
                                                 MsgBox("Incorrect IP Address")
                                                 Exit Sub
                                             End Try
                                             X.txtAddr.Text = ""
                                             X.Hide()
                                         End Sub})
        IPAddr.Show()

    End Sub
#End Region
#Region "Level Controls"
    Dim _CurrentLevel As Integer = 0
    Public CurrentAmountOfZombies As Integer
    Public MaxAmountOfZombies As Integer = 5
    Dim WithEvents NewZombieInterval As New Timers.Timer(100)
    Public Property Level As Integer
        Get
            Return _CurrentLevel
        End Get
        Set(value As Integer)
            _CurrentLevel = value
            LevelDisplay.Text = "Lvl " & value
        End Set
    End Property
    Sub WaitForLastZombieExit() Handles Me.OnLevelEnded
        Level += 1
        NewLevel(Level)
    End Sub
    Sub NewLevel(int As Integer)
        sounds.Source = New Uri(My.Computer.FileSystem.CurrentDirectory & "\Sounds\newlevel.mp3")
        sounds.Play()
        Character.Position() = New Point((Me.Width / 2) - (Character.Width / 2), (Me.Height / 2) - (Character.Height / 2))
        GenerateMap()
        For Each powerup In ReturnPowerups.Where(Function(x) Not WithinBounds(New Point(x.Margin.Left, x.Margin.Top)))
            ContainerGrid.Children.Remove(powerup)
        Next
        MaxAmountOfZombies += (New Random).Next(3, 6)
        CurrentAmountOfZombies = MaxAmountOfZombies
        NewZombieInterval.Start()
    End Sub
    Sub NewZombieInterval_Elapsed() Handles NewZombieInterval.Elapsed
        Dispatcher.Invoke(Sub()
                              Try
                                  NewZombieInterval.Interval = (New Random).Next(1250, 2500)
                                  If CurrentAmountOfZombies = 0 Or Character.Data.Dead Then
                                      NewZombieInterval.Stop()
                                  ElseIf ReturnZombies.Count < 10 And CurrentAmountOfZombies <> 0 AndAlso Character.Data.Dead = False Then
                                      Dispatcher.Invoke(Sub()
                                                            CurrentAmountOfZombies -= 1 'Take away the amount of zombies now that we've generated one
                                                            Dim R As New Random
                                                            Dim Side As Integer = R.Next(1, 4)
                                                            Dim Position As Point 'Let's make it spawn around the Map/Window
                                                            Select Case Side 'Let's select which place it's going to spawn with a random number
                                                                Case 1
                                                                    Position = New Point(R.Next(0, CInt(Me.Width)), R.Next(-200, 0)) 'Top
                                                                Case 2
                                                                    Position = New Point(R.Next(-200, 0), R.Next(0, CInt(Me.Height))) 'Left
                                                                Case 3
                                                                    Position = New Point(R.Next(CInt(Me.Width), CInt(Me.Width) + 200), R.Next(0, CInt(Me.Height))) 'Right
                                                                Case 4
                                                                    Position = New Point(R.Next(0, CInt(Me.Width)), R.Next(CInt(Me.Height), CInt(Me.Height) + 200)) 'Bottom
                                                            End Select
                                                            Dim newZombie As New Zombie(Position, CInt(Level * 10 + 0.2), If(Players.Count > 1, Nothing, Players(0))) ' Make a new zombie that will target first player if playing solo
                                                            ContainerGrid.Children.Add(newZombie)
                                                            ZM.Start()
                                                            AddHandler newZombie.OnMoneyEarned, Sub()
                                                                                                    Character.Points += 10 'Give money when we've tried to kill the zombie
                                                                                                End Sub
                                                            AddHandler newZombie.OnDeath, Sub(Z As Zombie)
                                                                                              'Make sure we delete the zombie after they die
                                                                                              If ReturnZombies.Count = 1 And CurrentAmountOfZombies = 0 Then
                                                                                                  'end of the level
                                                                                                  RaiseEvent OnLevelEnded()
                                                                                                  DebugToScreen("Level " & Level & " Ended. Total Kills (so far): " & Character.Kills)
                                                                                              ElseIf WithinBounds(New Point(Z.Position.X + (Z.Width / 2), Z.Position.Y + (Z.Height / 2))) And Not Override Then
                                                                                                  Dim Thrown As Integer = R.Next(0, 11) 'a lucky dip will determine whether to put down a powerup
                                                                                                  If Thrown = 3 And ReturnPowerups.Count < 2 Then 'We then want to detect if the thrown number was 3 and if there is a space on the container grid (so that we don't add more than 2)
                                                                                                      Dim NewPowerup As PowerupImage.Powerup_Type 'We then make a variable to a hold temporary type
                                                                                                      Do Until NewPowerup <> LastPowerUp And [Enum].GetName(GetType(PowerupImage.Powerup_Type), NewPowerup).ToString <> Character.Data.Gun And NewPowerup <> PowerupImage.Powerup_Type.General And NewPowerup <> 7
                                                                                                          'We get a new powerup that isn't the gun we're using, isn't the general type(used for audio) and also wasn't the last used powerup
                                                                                                          NewPowerup = CType(R.Next(0, 7), PowerupImage.Powerup_Type)
                                                                                                      Loop
                                                                                                      LastPowerUp = NewPowerup
                                                                                                      Dim TempPowerup As New PowerupImage(Z.Position, NewPowerup)
                                                                                                      AddHandler TempPowerup.OnPowerUpActivate, Sub(t As PowerupImage.Powerup_Type)
                                                                                                                                                    PowerUpImage_ActivateHandler(t)
                                                                                                                                                End Sub
                                                                                                      AddHandler TempPowerup.OnPowerUpTimesUp, Sub(powerup As PowerupImage)
                                                                                                                                                   ContainerGrid.Children.Remove(powerup)
                                                                                                                                               End Sub
                                                                                                      DebugToScreen("A " & TempPowerup.Type.ToString & " was delivered!")
                                                                                                      ContainerGrid.Children.Add(TempPowerup)
                                                                                                  End If
                                                                                              Else
                                                                                                  DebugToScreen("No Dice. Reason: Either out of bounds or overrided.")
                                                                                              End If
                                                                                              Character.Kills += 1 'Add to kills
                                                                                              ContainerGrid.Children.Remove(Z) 'Remove from the container grid
                                                                                              Character.Points += 80
                                                                                          End Sub
                                                        End Sub)
                                  End If
                              Catch
                              End Try
                          End Sub)

    End Sub
#End Region
#Region "Music"
    Dim MusicPlaylist As New List(Of String)
    Dim MusicLastPlayed As Integer = 0
    Private Sub music_MediaEnded(sender As Object, e As RoutedEventArgs) Handles music.MediaEnded
        If MusicPlaylist.Count = 0 Then
            Dim FileCol As IEnumerable(Of String) = System.IO.Directory.EnumerateFiles(System.IO.Directory.GetCurrentDirectory() & "\Music")
            For Each Str As String In FileCol
                If Str.ToLower.EndsWith(".mp3") Then
                    MusicPlaylist.Add(Str)
                End If
            Next
            music_MediaEnded(Nothing, Nothing)
        Else
            Dim NewMusicRandom As Integer = 0
            Do Until NewMusicRandom <> MusicLastPlayed
                NewMusicRandom = (New Random).Next(0, MusicPlaylist.Count - 1)
            Loop
            MusicLastPlayed = NewMusicRandom
            music.Source = New Uri(MusicPlaylist(NewMusicRandom))
            music.Play()
        End If
    End Sub
#End Region
#Region "Powerups"
    Private LastPowerUp As Integer = 0
    Private Override As Boolean = False
    Sub PowerUpImage_ActivateHandler(T As PowerupImage.Powerup_Type)
        Dispatcher.Invoke(Sub()
                              Select Case T
                                  Case PowerupImage.Powerup_Type.Nuke
                                      DebugToScreen("Nuke powerup override has been enabled")
                                      Override = True
                                      Dim Reactivate As New RunOnceTimer(850, Sub()
                                                                                  Dispatcher.Invoke(Sub()
                                                                                                        DebugToScreen("Nuke powerup override is disabled")
                                                                                                        Override = False
                                                                                                    End Sub)
                                                                              End Sub)
                                      Dim AddUpPoints As Integer = 0
                                      For Each Z In ReturnZombies.Where(Function(x) WithinWindow(New Point(x.Position.X + (x.Width / 2), x.Position.Y + (x.Height / 2))))
                                          AddUpPoints += 120
                                          Z.Health = 0
                                      Next
                                      If AddUpPoints > 0 Then
                                          Character.Points += AddUpPoints
                                      End If
                                  Case PowerupImage.Powerup_Type.Ammo
                                      DebugToScreen("Ammo added")
                                      Character.BulletAmount += 320
                                  Case PowerupImage.Powerup_Type.MP5
                                      DebugToScreen("MP5 Activated")
                                      Character.LoadNewGun("MP5", 4200, 120, 40)
                                      Gun1.GunImage.Source = New BitmapImage(New Uri(Environment.CurrentDirectory & "\Guns\MP5.png"))
                                  Case PowerupImage.Powerup_Type.Sniper
                                      DebugToScreen("Sniper activated")
                                      Character.LoadNewGun("Sniper", 420, 5, 120)
                                      Gun1.GunImage.Source = New BitmapImage(New Uri(Environment.CurrentDirectory & "\Guns\Sniper.png"))
                                  Case PowerupImage.Powerup_Type.Pistol
                                      DebugToScreen("Pistol activated")
                                      Character.LoadNewGun("Pistol", 520, 25, 35)
                                      Gun1.GunImage.Source = New BitmapImage(New Uri(Environment.CurrentDirectory & "\Guns\pistol38cal.png"))
                                  Case PowerupImage.Powerup_Type.Health
                                      DebugToScreen("Health is regenerating")
                                      Character.HealthRestoreSlowly.Start()
                              End Select
                              'Play the sound
                              If Character.Talking = False Then
                                  AddHandler voice.MediaEnded, Sub()
                                                                   Character.Talking = False
                                                               End Sub
                                  Dim SelectGeneralOrOther As Integer = (New Random).Next(1, 3)
                                  Select Case SelectGeneralOrOther
                                      Case 1
                                          Dim SelectedList = PowerUpSounds.Item(T)
                                          voice.Source = New Uri(SelectedList.Item((New Random).Next(0, SelectedList.Count - 1)))
                                          voice.Play()
                                      Case 2
                                          Dim SelectedList = PowerUpSounds.Item(PowerupImage.Powerup_Type.General)
                                          voice.Source = New Uri(SelectedList.Item((New Random).Next(0, SelectedList.Count - 1)))
                                          voice.Play()
                                  End Select
                                  Character.Talking = True
                              End If
                          End Sub)
    End Sub
#End Region
#Region "Networking"
    Dim WithEvents WaitForPlayers As New Timers.Timer(100)
    Public WithEvents Client As ZombieClient
    Public WithEvents Server As ZombieClient
    Dim ServerIP As IPEndPoint
    Dim WithEvents GameStart As New Timers.Timer(1000)
    Private Sub WaitForPlayers_Elapsed() Handles WaitForPlayers.Elapsed
        If IsNothing(Server) Then
            Server = New ZombieClient(80, 30010)
        Else
            WaitForPlayers.Interval = 2500
            Server.Send(New IPEndPoint(IPAddress.Parse("255.255.255.255"), 30010), New Command("/Calling all Zombie Horders"))
        End If
    End Sub
    Private GameStartCountdown As Integer = 5
    Private Sub GameStart_Elapsed() Handles GameStart.Elapsed
        Dispatcher.Invoke(Sub()
                              Server.Send(ReturnPlayerEndPoints.ToArray, New Message("Server", "Game starting in " & GameStartCountdown & "..."))
                              GameStartCountdown -= 1
                              If GameStartCountdown <= 0 Then
                                  GenerateMap()
                                  Dim BitmapImage As New BitmapImage(New Uri(MapLibrary(SelectedMap))) 'Make a new bitmap image from the URI to get a basic boundries system working (based on black and white). We're going to 'copy' the pixels
                                  Dim stride = BitmapImage.PixelWidth * 4 'Make the strides to get each individual pixel
                                  Dim size = BitmapImage.PixelHeight * stride 'Make the size for the array
                                  Dim parray(size) As Byte 'Make temp color array to store each color pixel seperately
                                  BitmapImage.CopyPixels(parray, stride, 0) 'Copy the pixels to the array
                                  'For i = 0 To Players.Count - 1
                                  '    Select Case i
                                  '        Case 0
                                  '            Players(i).HighlightColor.Color = Colors.Orange
                                  '        Case 1
                                  '            Players(i).HighlightColor.Color = Colors.Green
                                  '        Case 2
                                  '            Players(i).HighlightColor.Color = Colors.Blue
                                  '        Case 3
                                  '            Players(i).HighlightColor.Color = Colors.Red
                                  '    End Select
                                  'Next
                                  Server.Send(ReturnPlayerEndPoints.ToArray, New GameSettings(parray, SelectedMapArea, Level + 1, PlayersData.ToArray, True))
                                  'This is huge and makes it lag because it's sending so much data ^
                                  GameStart.Stop()
                              End If
                          End Sub)
    End Sub
    Private Sub Client_OnStatusMessage(Str As String) Handles Client.OnStatusMessage, Server.OnStatusMessage
        Dispatcher.Invoke(Sub()
                              UserStatus.Content = Str
                          End Sub)
    End Sub

    Private WithEvents GameTimer As New Zytonic_Framework.Utilities.Timers.IntervalTimer(10, True)

    Private ServerQueue As New SortedList(Of Integer, ProcessQueueItem) 'thats the client getting the byte packets
    Dim BytePacketList As New Dictionary(Of String, List(Of BytePacket))
    Private Sub Client_OnMessage(Data As Object, Address As IPEndPoint) Handles Client.OnMessage
        Dispatcher.InvokeAsync(Sub()
                                   '' Try
                                   'Dim Meta As MetaData = DirectCast(Data, MetaData)
                                   'Dim TypeOfObject As String = Meta.DataType.Remove(0, Meta.DataType.LastIndexOf(".") + 1)
                                   'Select TypeOfObject
                                   Dim Meta As MetaData = DirectCast(Data, MetaData)
                                   Dim TypeOfObject As String = Meta.DataType.Remove(0, Meta.DataType.LastIndexOf(".") + 1)
                                   Dim TestType As Type = System.Type.GetType(Meta.DataType)
                                   'Dim TestObj As Object = TestType.GetConstructor({TestType}).Invoke(Meta.Properties.Values.ToArray)
                                   Dim MetaObject As Object = Activator.CreateInstance(TestType, Meta.Properties.Values.ToArray)
                                   Select Case MetaObject.GetType.Name
                                       Case "Character[]" '.Contains("Character[]")
                                           LANPlayers.Items.Clear()
                                           Dim Characters As Character() = CType(Meta.Properties("Characters"), Character())
                                           'Dim NewCharacterList As New List(Of CharacterControl)
                                           'For Each child In BasicCharacters
                                           '    NewCharacterList.Add(child.Convert())
                                           'Next
                                           'Players = NewCharacterList
                                           For Each Player In Characters
                                               LANPlayers.Items.Add(Player.Username & If(Player.isReady, " (Ready)", ""))
                                           Next
                                       Case "Message"
                                           Dim Msg1 = CType(Data, Message)
                                           'RaiseEvent OnMessage(M.From & ": " & M.Msg)
                                           Client_OnStatusMessage(Msg1.From & ": " & Msg1.Msg)
                                       Case "Command"
                                           Select Case True
                                               Case Meta.Properties("Message").Contains("/Calling all Zombie Horders") And Client.Connected = False
                                                   Client.Send(Address, New Command("/ZH --join " & Environment.UserName))
                                               Case Meta.Properties("Message").Contains("/ZH --join success") 'The server wil reply with success
                                                   ServerIP = Address 'Set the default host for easy access (for later?)
                                                   Client.Connected = True 'Make sure we don't connect again
                                                   LANProgressStatus.Content = "Connected to " & If(Address.Address.ToString = LocalHost(), "Localhost", Address.Address.ToString)
                                               Case Meta.Properties("Message") = "/ZH --ServerClose"
                                                   btnLanBack_MouseUp(Nothing, Nothing)
                                                   If Not Address.Address.ToString = LocalHost() And Not Address.Address.ToString = "127.0.0.1" Then
                                                       MsgBox("Sorry, the connection to the server has closed unexpectedly")
                                                   End If
                                               Case Meta.Properties("Message") = "/ZH --ReadyVote"
                                                   If LANReadyUp.Visibility = Windows.Visibility.Hidden Then
                                                       LANReadyUp.Visibility = Windows.Visibility.Visible
                                                   Else
                                                       LANReadyUp.Visibility = Windows.Visibility.Hidden
                                                   End If
                                               Case Meta.Properties("Message") = "/ZH StartGame"
                                                   If Lan.Visibility = Windows.Visibility.Visible Then
                                                       Lan.Visibility = Windows.Visibility.Hidden
                                                       Menu.Visibility = Windows.Visibility.Hidden
                                                   End If
                                           End Select
                                       Case "Character"
                                           Dim RetrievedCharacter = CType(Data, Character)
                                           Try
                                               For Each Player In ContainerGrid.Children.OfType(Of CharacterControl)()
                                                   If Player.Data.Username = RetrievedCharacter.Username And RetrievedCharacter.Username <> Environment.UserName Then
                                                       Player.Data = RetrievedCharacter
                                                   End If
                                               Next
                                           Catch ex As Exception
                                           End Try
                                       Case "BytePacket"
                                           LANProgressStatus.Content = "Syncing Game Settings..."

                                           ' Dim BP = CType(Data, BytePacket)
                                           ' TODO:
                                           ' REPLACE ALL CASTS WITH Meta.Properties("PropertyNameGoesHere")

                                           If BytePacketList.ContainsKey(Meta.Properties("ID")) Then
                                               'BytePacketList.Item(Meta.Properties("ID")).Add(BP)
                                               ''Dim newBP As New BytePacket(Meta.Properties("ID"))
                                           Else
                                               'BytePacketList.Add(Meta.Properties("ID"), New List(Of BytePacket))
                                               'BytePacketList.Item(Meta.Properties("ID")).Add(BP)
                                           End If
                                           Client.Send(ServerIP, New BytePacketRecieved(Meta.Properties("ID")))
                                           Debug.Print("BytePacket Recieved")
                                           If BytePacketList(Meta.Properties("ID")).Count * BytePacketList(Meta.Properties("ID"))(0).Bytes.Length >= BytePacketList(Meta.Properties("ID"))(0).PackageSize Then
                                               LANProgressStatus.Content = "BytePacket w/ ID: " & Meta.Properties("ID") & " -> Transfer Complete"
                                               LANProgressStatus.Content = "Awaiting Game Status"
                                               Dim NewStream As New IO.MemoryStream
                                               For Each BPI In BytePacketList.Item(Meta.Properties("ID"))
                                                   NewStream.Write(BPI.Bytes, 0, BPI.Bytes.Length)
                                               Next
                                               Dim bfTemp As New BinaryFormatter
                                               NewStream.Position = 0
                                               Client_OnMessage(bfTemp.UnsafeDeserialize(NewStream, Nothing), Address)

                                           End If
                                       Case "GameSettings"
                                           'This should indicate a new level with a background and area...
                                           SelectedMapArea = Meta.Properties("GameSettings").BackgroundArea
                                           Dim BitmapImage As New WriteableBitmap(1022, 750, 96, 96, PixelFormats.Bgr32, Nothing)
                                           BitmapImage.WritePixels(New Int32Rect(0, 0, 1022, 750), Meta.Properties("GameSettings").Background, 1022 * 4, 0)
                                           Me.Background = New ImageBrush(DirectCast(ConvertWritableBitmapToBitmapImage(BitmapImage), ImageSource))
                                           Dim NewCharacterList As New List(Of CharacterControl)
                                           'For Each child In Meta.Properties("GameSettings").Players
                                           '    NewCharacterList.Add(child.Convert())
                                           'Next
                                           'Players = NewCharacterList
                                           'For Each Player In Players
                                           '    Player.Margin = New Thickness((Me.Width / 2) - 36, (Me.Height / 2) - 36, 0, 0)
                                           '    Player.LoadNewGun("pistol", 750, 15, 10)
                                           '    If Player.Data.EndPoint.Address.ToString = "127.0.0.1" Or Player.Data.EndPoint.Address.ToString = GetIPv4Address() Then
                                           '        If Player.Data.Username = Environment.UserName Then
                                           '            Character = Player
                                           '            AddControlHandlers()
                                           '        End If
                                           '    End If
                                           '    ContainerGrid.Children.Add(Player)
                                           'Next
                                           'TESTTTTTT!!!
                                           Level = Meta.Properties("Level")
                                           LANProgressStatus.Content = "Game Settings Transmitted"
                                       Case Else
                                           MsgBox(TypeOfObject)
                                   End Select
                                   'Catch ex As Exception
                                   '    MsgBox(ex.Message)
                                   'End Try
                               End Sub)
    End Sub
    'these two statements control the whole server stuff for the multiplayer game
    Private Sub Server_OnMessage(Data As Object, Address As IPEndPoint) Handles Server.OnMessage
        Dim objtest = Data
        Dim TypeOfObject As String = Data.GetType.UnderlyingSystemType.ToString
        ServerQueue.Add(ServerQueue.Count, New ProcessQueueItem(Address, Data))
    End Sub
    Private Sub ProcessMessage(P As ProcessQueueItem)
        ' TODO:
        ' Implement Metadata
        Dim Meta As MetaData = DirectCast(P.Data, MetaData)
        Dim TypeOfObject As String = Meta.DataType.Remove(0, Meta.DataType.LastIndexOf(".") + 1)
        Dim TestType As Type = System.Type.GetType(Meta.DataType)
        'Dim TestObj As Object = TestType.GetConstructor({TestType}).Invoke(Meta.Properties.Values.ToArray)
        Dim MetaObject As Object = Activator.CreateInstance(TestType, Meta.Properties.Values.ToArray)
        Select Case MetaObject.GetType.Name
            Case "Command"
                Select Case True
                    Case MetaObject.Message.Contains("/ZH --join ") And Players.Count < 4
                        Server.Send(P.Address, New Command("/ZH --join success"))
                        Dim CharObj As New Character With {.Username = MetaObject.Message.Replace("/ZH --join ", ""), .EndPoint = P.Address}
                        AddCharacterControl(New CharacterControl With {.Data = CharObj})
                        Server.Send(ReturnPlayerEndPoints.ToArray, PlayersData.ToArray)
                        Server.Send(ReturnPlayerEndPoints.ToArray, New Message("Server", PlayersData(PlayersData.Count - 1).Username.ToString & " joined"))
                        If ReturnPlayerEndPoints.Count > 2 Then
                            Server.Send(ReturnPlayerEndPoints.ToArray, New Command("/ZH --ReadyVote"))
                        End If
                        Exit Select
                    Case MetaObject.Message.Contains("/ZH --disconnect ")
                        Dim CharacterFound As CharacterControl = Players.Where(Function(x) x.Data.EndPoint.Address.ToString = P.Address.Address.ToString).First
                        RemoveCharacterControl(CharacterFound)
                        Server.Send(ReturnPlayerEndPoints.ToArray, PlayersData.ToArray)
                        Server.Send(ReturnPlayerEndPoints.ToArray, New Message("Server", CharacterFound.Data.Username & " Quit"))
                        CharacterFound = Nothing
                    Case MetaObject.Message.Contains("/ZH --ready") AndAlso Not GameStart.Enabled
                        Players.Where(Function(x) x.Data.EndPoint.Address.ToString = P.Address.Address.ToString).First.Data.isReady = True
                        Server.Send(ReturnPlayerEndPoints.ToArray, PlayersData.ToArray)
                        For Each x In PlayersData()
                            If Not x.isReady Then Exit Select
                        Next
                        If Players.Count > 1 Then
                            'Start game if we get here
                            WaitForPlayers.Stop()
                            GameStart.Start()
                        End If

                End Select
                If GameStart.Enabled = False Then
                    Dim ChangeText As New RunOnceTimer(2500, Sub() Server.Send(ReturnPlayerEndPoints.ToArray, New Message("Server", "Waiting for Players")))

                End If
            Case "Character"
                Server.Send(ReturnPlayerEndPoints.ToArray, CType(P.Data, Character))
            Case Else
                MsgBox(TypeOfObject.GetType.Name)
        End Select
    End Sub
    Private Sub Server_BytePacketTransmissionEnded() Handles Server.OnBytePacketTransmissionFinished
        Dim WaitingForSync As New RunOnceTimer(2000, Sub() Server.Send(ReturnPlayerEndPoints.ToArray, New Command("/ZH StartGame")))
    End Sub

    Private Class ProcessQueueItem

        Public Property Data As Object
        Public Property Address As IPEndPoint

        Public Sub New(Address As IPEndPoint, Data As Object)
            Me.Address = Address
            Me.Data = Data
        End Sub

    End Class

#End Region

    Private Sub GameTimer_Started() Handles GameTimer.Started
        ' // Game Logic Started

    End Sub

    Private Sub GameTimer_Stopped() Handles GameTimer.Stopped
        ' // Game Logic Ended

    End Sub

    Private Sub GameTimer_Tick() Handles GameTimer.Tick
        Dispatcher.Invoke(Sub()
                              ' // Process the tick
                              ' This will happen every tick event (10 ms)
                              For i As Integer = ServerQueue.Keys().Count - 1 To 0 Step -1
                                  Dim k As Integer = ServerQueue.Keys(i)
                                  Dim P As ProcessQueueItem = ServerQueue(k)
                                  ProcessMessage(P)
                              Next
                          End Sub)
    End Sub
End Class

<Serializable()>
Public Class BasicZombie
    Public Property Position As Point
    Public Property Health As Integer
    Public Property Rotation As Double
    Public Property ID As String
    Sub New(Zombie As Zombie)
        Me.Position = Zombie.Position
        Me.Health = Zombie.Health
        Me.Rotation = Zombie.Angle
        Me.ID = Zombie.ID
    End Sub
End Class
Namespace Networking
    Public Enum CommandType As Short
        Disconnect = -1
        Broadcast = 0
        Connect = 1
        Join = 2
        JoinSuccessful = 3
        Other = 4
    End Enum
    Public Class CommandPacket
        Sub New(Command As CommandType, Optional CommandAttachment As String = Nothing)
            _Command = Command
            _CommandAttachment = CommandAttachment
        End Sub
        Private _Command As CommandType
        Public ReadOnly Property Command As CommandType
            Get
                Return _Command
            End Get
        End Property
        Private _CommandAttachment As String
        Public ReadOnly Property CommandAttachment As String
            Get
                Return _CommandAttachment
            End Get
        End Property
    End Class
    Public Class NetworkController
        Private Class UDPClient

        End Class
        Private WithEvents Client As UDPClient
        Private WithEvents Server As ZombieClient
        Public Property ServerIP As IPEndPoint
        Public Function LocalHost() As String
            Dim strHostName = System.Net.Dns.GetHostName()
            Return System.Net.Dns.GetHostEntry(strHostName).AddressList(1).ToString()
        End Function
    End Class
End Namespace 'Just some trashy testing stuff