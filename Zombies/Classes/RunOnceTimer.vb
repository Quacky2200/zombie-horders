Public Class RunOnceTimer
    Private RunTimer As Timers.Timer
    Sub New(interval As Integer, EventHandler As Action)
        RunTimer = New Timers.Timer(interval)
        AddHandler RunTimer.Elapsed, Sub()
                                         EventHandler.Invoke()
                                         RunTimer.Stop()
                                     End Sub
        RunTimer.Start()
    End Sub
    Sub Stop_RunOnceTimer()
        RunTimer.Stop()
    End Sub
    Sub Resume_RunOnceTimer()
        RunTimer.Start()
    End Sub
End Class
