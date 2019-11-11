<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Form1))
        Me.listSerialPorts = New System.Windows.Forms.ListBox()
        Me.SerialPort1 = New System.IO.Ports.SerialPort(Me.components)
        Me.TCPTimer = New System.Windows.Forms.Timer(Me.components)
        Me.Label1 = New System.Windows.Forms.Label()
        Me.txtInfo = New System.Windows.Forms.TextBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.SystemTrayIcon = New System.Windows.Forms.NotifyIcon(Me.components)
        Me.NotificationMenu = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.btnQuit = New System.Windows.Forms.ToolStripMenuItem()
        Me.btnSettings = New System.Windows.Forms.ToolStripMenuItem()
        Me.NotificationMenu.SuspendLayout()
        Me.SuspendLayout()
        '
        'listSerialPorts
        '
        Me.listSerialPorts.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.listSerialPorts.FormattingEnabled = True
        Me.listSerialPorts.Location = New System.Drawing.Point(12, 25)
        Me.listSerialPorts.Name = "listSerialPorts"
        Me.listSerialPorts.Size = New System.Drawing.Size(135, 121)
        Me.listSerialPorts.TabIndex = 0
        '
        'SerialPort1
        '
        '
        'TCPTimer
        '
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(9, 9)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(61, 13)
        Me.Label1.TabIndex = 1
        Me.Label1.Text = "Select port:"
        '
        'txtInfo
        '
        Me.txtInfo.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.txtInfo.Location = New System.Drawing.Point(153, 25)
        Me.txtInfo.Multiline = True
        Me.txtInfo.Name = "txtInfo"
        Me.txtInfo.ReadOnly = True
        Me.txtInfo.Size = New System.Drawing.Size(192, 121)
        Me.txtInfo.TabIndex = 2
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(150, 9)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(97, 13)
        Me.Label2.TabIndex = 1
        Me.Label2.Text = "Debug Information:"
        '
        'SystemTrayIcon
        '
        Me.SystemTrayIcon.BalloonTipTitle = "Remote Data Relay"
        Me.SystemTrayIcon.ContextMenuStrip = Me.NotificationMenu
        Me.SystemTrayIcon.Icon = CType(resources.GetObject("SystemTrayIcon.Icon"), System.Drawing.Icon)
        Me.SystemTrayIcon.Text = "Remote Data Relay"
        '
        'NotificationMenu
        '
        Me.NotificationMenu.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.btnSettings, Me.btnQuit})
        Me.NotificationMenu.Name = "NotificationMenu"
        Me.NotificationMenu.ShowImageMargin = False
        Me.NotificationMenu.Size = New System.Drawing.Size(156, 70)
        '
        'btnQuit
        '
        Me.btnQuit.Name = "btnQuit"
        Me.btnQuit.Size = New System.Drawing.Size(155, 22)
        Me.btnQuit.Text = "Quit"
        '
        'btnSettings
        '
        Me.btnSettings.Name = "btnSettings"
        Me.btnSettings.Size = New System.Drawing.Size(155, 22)
        Me.btnSettings.Text = "Settings..."
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(357, 161)
        Me.Controls.Add(Me.txtInfo)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.listSerialPorts)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.MinimumSize = New System.Drawing.Size(258, 109)
        Me.Name = "Form1"
        Me.Text = "Data Relay"
        Me.NotificationMenu.ResumeLayout(False)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents listSerialPorts As ListBox
    Friend WithEvents SerialPort1 As IO.Ports.SerialPort
    Friend WithEvents TCPTimer As Timer
    Friend WithEvents Label1 As Label
    Friend WithEvents txtInfo As TextBox
    Friend WithEvents Label2 As Label
    Friend WithEvents SystemTrayIcon As NotifyIcon
    Private WithEvents NotificationMenu As ContextMenuStrip
    Friend WithEvents btnQuit As ToolStripMenuItem
    Friend WithEvents btnSettings As ToolStripMenuItem
End Class
