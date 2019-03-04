Public Class ManualAddress
    Private ActionsForButtons() As Action(Of ManualAddress)
    Sub New(Actions() As Action(Of ManualAddress))
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        ActionsForButtons = Actions
    End Sub

    Private Sub btnJoin_Click(sender As Object, e As RoutedEventArgs) Handles btnJoin.Click
        ActionsForButtons(1).Invoke(Me)
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As RoutedEventArgs) Handles btnCancel.Click
        ActionsForButtons(0).Invoke(Me)
    End Sub
End Class
