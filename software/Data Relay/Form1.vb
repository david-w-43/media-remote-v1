Public Class Form1
    Const TCPTimerInterval As UInt16 = 250
    Const baudRate As UInt16 = 19200

    Dim serialReceivedLines(16) As String
    Dim TCPStream As Net.Sockets.NetworkStream
    Dim metadata As Metas
    Dim playback As PlaybackParameters

    Private Event TrackChanged()

    Structure Metas
        Dim title As String
        Dim artist As String
        Dim timeLength As UInt16
        Dim fileName As String
    End Structure

    Structure PlaybackParameters
        Dim currentTime As UInt16
        Dim currentVolume As UInt16
        Dim shuffle As Boolean
        Dim repeatMode As UInt16 '0 - None, 1 - Loop, 2 - Repeat
    End Structure

    Private Sub applicationExit()
        Try
            SerialPort1.WriteLine("EXIT") 'Attempt to send exit command to remote
        Catch
            'Do nothing
        End Try

        SystemTrayIcon.Dispose()
        System.Threading.Thread.Sleep(50)
        Application.Exit()
    End Sub

    Private Sub applicationRestart()
        SystemTrayIcon.Dispose()
        Application.Restart()
    End Sub

    Private Sub sendMetadata() Handles Me.TrackChanged 'When the track changes

        Dim toSend As String = "META:" & vbLf &        'Form a message to send 
                              metadata.title & vbLf &
                              metadata.artist & vbLf &
                              metadata.timeLength

        SerialPort1.WriteLine(toSend) 'Send data over serial

        'Debug.WriteLine(toSend)
    End Sub

    Private Sub sendTime()
        Dim toSend As String = "TIME:" & vbLf &         'Form a message to send
                              playback.currentTime

        SerialPort1.WriteLine(toSend) 'Send data over serial
        'Debug.WriteLine(toSend)
    End Sub

    Private Sub sendParameters()
        Dim toSend As String = "PARA:" & vbLf &         'Form a message to send
                              playback.currentVolume & vbLf &
                              playback.shuffle & vbLf &
                              playback.repeatMode       '0 - None, 1 - Loop, 2 - Repeat

        SerialPort1.WriteLine(toSend) 'Send data over serial
        'Debug.WriteLine(toSend)
    End Sub

    Private Sub getInformation() Handles TCPTimer.Tick


        If Not My.Computer.Ports.SerialPortNames.Contains(SerialPort1.PortName) Then 'If serial was disconnected
            applicationExit()
        End If


        Try
            'Store current metadata and parameters for comparison
            Dim oldMetadata As Metas = metadata
            Dim oldTime As UInt16 = playback.currentTime

            Dim oldFilename As String 'Used if title not present

            Dim titleFound, filenameFound, artistFound, lengthFound, timeFound As Boolean
            Dim startValid, endValid As Boolean

            TCPSend("info", False) 'Send 'info' command to get information about the current stream
            TCPSend("get_length", False)
            TCPSend("get_time", False)

            Dim receivedData() As String = readTCPLines()


            If Not receivedData Is Nothing Then 'If something received
                Dim debugString As String = Nothing

                For Each line In receivedData 'For each line returned
                    line = line.Replace(">", Nothing).TrimEnd 'Remove garbage > characters and trim
                    debugString += line
                    'Debug.WriteLine(line.Trim)
                    If line.Contains("title:") Then 'If the line specifies the title
                        metadata.title = line.Split(":", 2, StringSplitOptions.None).ElementAt(1).Trim 'Return the portion of the line after :
                        titleFound = True
                    ElseIf line.Contains("filename:") Then
                        metadata.fileName = line.Split(":", 2, StringSplitOptions.None).ElementAt(1).Trim 'Set filename
                        filenameFound = True
                    ElseIf line.Contains("artist:") Then
                        metadata.artist = line.Split(":", 2, StringSplitOptions.None).ElementAt(1).Trim
                        artistFound = True
                    ElseIf IsNumeric(line.Trim) And Not lengthFound Then 'If the line is a numeric value
                        If Val(line.Trim) < 0 Then
                            metadata.timeLength = 0 'Set timeLength to its value
                            lengthFound = False
                        Else
                            metadata.timeLength = Val(line.Trim) 'Set timeLength to its value
                            lengthFound = True
                        End If
                    ElseIf IsNumeric(line.Trim) And Not timeFound Then
                        playback.currentTime = Val(line.Trim)
                        timeFound = True
                    ElseIf line.Contains("[ Meta data ]") Then 'If start of transmission received correctly
                        startValid = True
                    ElseIf line.Contains("[ end of stream info ]") Then ''If end of transmission received correctly
                        endValid = True
                    End If
                Next

                If startValid And endValid Then

                    'If a field is not found, display a substitute
                    If Not (titleFound Or filenameFound) Then
                        metadata.title = "Unspecified Title"
                        Debug.Write(debugString) 'Dump all lines read to debug console
                    ElseIf (Not titleFound) And filenameFound Then
                        metadata.title = metadata.fileName
                    End If
                    If Not artistFound Then metadata.artist = "Unspecified Artist"

                    'If the metadata has changed since the last check
                    If (Not metadata.Equals(oldMetadata)) Then 'If new file
                        RaiseEvent TrackChanged() 'Signal that the track has changed
                        System.Threading.Thread.Sleep(2)
                        sendTime()
                        Debug.WriteLine("Title: " & metadata.title & vbLf & "Artist: " & metadata.artist & vbLf _
                                        & "Length: " & metadata.timeLength) 'Print information to debug console
                    End If

                    oldFilename = metadata.fileName

                    If (Not playback.currentTime = oldTime) Or (playback.currentTime = 0) Then
                        sendTime() 'Send to arduino
                    End If


                    txtInfo.Text = metadata.title & vbCrLf & metadata.artist & vbCrLf _
                        & vbCrLf & playback.currentTime & " / " & metadata.timeLength & vbCrLf & playback.currentVolume _
                        & vbCrLf & metadata.fileName

                ElseIf (Not (titleFound Or artistFound Or filenameFound)) Then 'If no file is playing

                End If
            End If


        Catch e As Exception
            TCPTimer.Enabled = False
            applicationExit()
            Using sw As New IO.StreamWriter("log", True)
                sw.WriteLine(DateTime.Now)
                sw.WriteLine(e.Message)
            End Using
        End Try

    End Sub

    Private Function readTCPLines(Optional bufferLength As UInt16 = 16383) As String()

        If TCPStream.DataAvailable Then 'If there is data to be read
            Dim buffer(bufferLength) As Byte 'Allocate 16 KiB of buffer space
            TCPStream.Read(buffer, 0, buffer.Length) 'Read data from TCP stream into buffer
            Dim str As String = System.Text.Encoding.ASCII.GetString(buffer).TrimEnd(vbNullChar) 'Convert array of bytes to string
            Return str.Split(vbLf) 'Returns an array of strings, split by new lines
        Else
            Return Nothing
        End If
    End Function

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        SystemTrayIcon.Visible = True


        Me.Visible = False
        Me.ShowInTaskbar = False

        'If first fun
        If My.Settings.FIRSTRUN Then
            Dim menu As New Settings
            Dim response As DialogResult = menu.ShowDialog()
            If response = DialogResult.OK Then
                My.Settings.FIRSTRUN = False
                My.Settings.Save()
            Else
                Application.Exit()
            End If
        ElseIf Not IO.File.Exists(My.Settings.VLCPATH) Then
            My.Settings.FIRSTRUN = True
            My.Settings.Save()
            applicationRestart()
        End If

        Dim connected As Boolean = False
        Dim counter As Integer = 0
        While (Not connected) And (counter < 10) '10 attempts

            For Each port In My.Computer.Ports.SerialPortNames 'For each serial port connected
                'listSerialPorts.Items.Add(port)                'Add to the list
                With SerialPort1
                    Try
                        .Close() 'Close any open connection
                        .PortName = port
                        .BaudRate = baudRate
                        .Encoding = System.Text.Encoding.Default
                        .ReadTimeout = 1000
                        .Open() 'Open port

                        SerialPort1.WriteLine("HANDSHAKE") 'Send request to handshake


                        Dim received As String = SerialPort1.ReadLine()  'Read any received lines, will timeout if not present
                        If received.Contains("SHAKEN") Then connected = True 'If hand shaken, mark as connected
                        Debug.WriteLine(port & " connected")
                    Catch
                        'Do nothing, try the next port
                        Debug.WriteLine(port & " not connected")
                    End Try

                End With
            Next
            counter += 1
        End While
        If Not connected Then
            MsgBox("Remote not connected")
            applicationExit()
        Else

            Connect()
        End If
        'End If
    End Sub

    Private Sub Form1_Closing(ByVal sender As Object, ByVal e As FormClosingEventArgs) Handles MyBase.Closing
        'Me.WindowState = FormWindowState.Minimized
        Me.Visible = False
        e.Cancel = True
    End Sub

    Private Sub Connect() Handles listSerialPorts.SelectedIndexChanged

        Dim connected As Boolean = False
        While Not connected
            Try
                Dim client As New Net.Sockets.TcpClient(My.Settings.HOSTNAME, My.Settings.PORT) 'Define TCP client
                If client.Connected Then
                    TCPStream = client.GetStream 'Set the TCPStream
                    connected = True
                    Debug.WriteLine("VLC Connected")
                End If
            Catch
                'Launch VLC with required parameters
                Process.Start(My.Settings.VLCPATH, "-I qt --extraintf luarc --rc-host=""localhost:" & My.Settings.PORT.ToString & " --cli-host=""localhost:" & My.Settings.PORT & """")
            End Try
        End While


        'Enable timer to check tcp data
        TCPTimer.Interval = TCPTimerInterval
        TCPTimer.Enabled = True

        'Required as there is no way to get repeat/loop/shuffle state
        TCPSend("random off")
        TCPSend("repeat off")
        TCPSend("loop off")
        playback.repeatMode = 0
        playback.shuffle = False

        TCPSend("volume", False)
        For Each line In readTCPLines()
            line = line.Replace(">", Nothing) 'Ignore > characters
            If IsNumeric(line.Trim) Then
                Debug.WriteLine("VOL:" & vbLf & Val(line.Trim))
                SerialPort1.WriteLine("VOL:" & vbLf & Val(line.Trim))
            End If
        Next

        'Update on connection
        getInformation()
        sendParameters()
        sendMetadata()


    End Sub

    Private Sub TCPSend(ByVal command As String, Optional debugWrite As Boolean = True)
        Dim toSend As String = command + vbLf 'Forms a string to send, ending with a new line

        If debugWrite = True Then Debug.WriteLine("Sending '" & toSend.TrimEnd(vbLf) & "' ") 'Display message in debug console without trailing new line
        Dim data() As Byte = System.Text.Encoding.ASCII.GetBytes(toSend) 'Convert message into bytes, with ASCII encoding

        TCPStream.Write(data, 0, data.Length) 'Write bytes to stream, starting at the first byte
    End Sub

    Private Sub handleSerialData() Handles SerialPort1.DataReceived
        serialReceivedLines = SerialPort1.ReadExisting.Split(vbNewLine) 'Read data into array of strings

        Dim latestVolumeCommand As String = Nothing 'increments with each volume set command
        Dim volumeChanged As Boolean = False 'indicates whether or not volume should be set
        Dim playbackChanged As Boolean = False


        For Each line In serialReceivedLines 'For each line received
            'Debug.WriteLine(line.Trim) 'Write the line to the debug console

            'Detect commands
            Select Case line
                Case "NEXT" 'If line is 'NEXT'
                    TCPSend("next") 'Send 'next' command
                Case "PREV"
                    If playback.currentTime < 3 Then 'if within first three seconds of track
                        TCPSend("prev") 'Go to previous track
                    Else
                        TCPSend("seek 0") 'Go to beginning of current track
                    End If
                Case "PAUS"
                    TCPSend("pause")
                Case "SHUF"
                    playbackChanged = True
                    playback.shuffle = Not playback.shuffle 'Toggle shuffle
                    If playback.shuffle Then
                        TCPSend("random on")
                    Else
                        TCPSend("random off")
                    End If
                Case "REPT"
                    playbackChanged = True
                    If Not playback.repeatMode = 2 Then 'If not on last mode
                        playback.repeatMode += 1        'Increment
                    Else                                'Else
                        playback.repeatMode = 0         'Go to 0
                    End If
                    Select Case playback.repeatMode
                        Case 0                          'No repeat
                            TCPSend("loop off")
                            TCPSend("repeat off")
                        Case 1                          'Loop playlist
                            TCPSend("loop on")
                            TCPSend("repeat off")
                        Case 2                          'Repeat single track
                            TCPSend("loop off")
                            TCPSend("repeat on")
                    End Select
                Case Else
                    If line.Contains("VOLUME") Then
                        latestVolumeCommand = line 'Set latest volume command
                        volumeChanged = True
                    End If
            End Select
        Next

        If playbackChanged Then sendParameters()
        If volumeChanged Then TCPSend(latestVolumeCommand.ToLower.Trim)


    End Sub

    'Private Sub SystemTrayIcon_MouseDoubleClick(sender As Object, e As MouseEventArgs) Handles SystemTrayIcon.MouseDoubleClick
    '    Me.Show()
    '    Me.WindowState = FormWindowState.Normal
    '
    'End Sub

    Private Sub btnQuit_Click(sender As Object, e As EventArgs) Handles btnQuit.Click
        My.Settings.Save()
        SystemTrayIcon.Dispose()
        applicationExit()
    End Sub

    Private Sub btnSettings_Click(sender As Object, e As EventArgs) Handles btnSettings.Click
        Settings.ShowDialog()
    End Sub
End Class
