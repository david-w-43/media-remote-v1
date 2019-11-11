Imports System.Windows.Forms

Public Class Settings

    Private Sub OK_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OK_Button.Click
        'Validate input
        Dim valid As Boolean = True

        'If port invalid
        If (IsNumeric(txtPort.Text) = False) Or (Val(txtPort.Text) > 65535) Or (Val(txtPort.Text) < 0) Then
            valid = False
        End If
        If Not System.IO.File.Exists(txtFilepath.Text) Then
            valid = False
        End If
        If txtHostname.Text <> "localhost" Then
            Dim response As DialogResult = MsgBox("This is not the local machine. Continue?", MsgBoxStyle.YesNo)
            If response = DialogResult.No Then
                Exit Sub
            End If
        End If

        'Set settings
        My.Settings.VLCPATH = txtFilepath.Text
        My.Settings.HOSTNAME = txtHostname.Text
        My.Settings.PORT = txtPort.Text

        'Save settings
        My.Settings.Save()

        'Pre-existing actions
        Me.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.Close()
    End Sub

    Private Sub Cancel_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Cancel_Button.Click
        'Pre-existing actions
        Me.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub btnFind_Click(sender As Object, e As EventArgs) Handles btnFind.Click
        'Define dialogbox
        Dim dialog As New OpenFileDialog()
        dialog.InitialDirectory = Environment.SpecialFolder.MyComputer
        dialog.Title = "Select VLC executable"
        dialog.Filter = "VLC Executable|vlc.exe"

        'If 'OK' pressed, set textbox as necessary
        If dialog.ShowDialog() = DialogResult.OK Then
            txtFilepath.Text = dialog.FileName
        End If
    End Sub

    Private Sub Settings_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'Load textboxes with current values
        txtFilepath.Text = My.Settings.VLCPATH
        txtHostname.Text = My.Settings.HOSTNAME
        txtPort.Text = My.Settings.PORT
    End Sub
End Class
