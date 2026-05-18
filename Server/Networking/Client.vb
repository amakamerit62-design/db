Imports System.IO
Imports System.Net.Sockets

Public Class Client

    Public ClientSocket As Socket = Nothing
    Public ServerSocket As Server = Nothing
    Public IsConnected As Boolean = False
    Public BufferLength As Long = Nothing
    Public BufferLengthReceived As Boolean = False
    Public Buffer() As Byte = Nothing
    Public MS As MemoryStream = Nothing
    Public IP As String = Nothing
    Public LV As ListViewItem = Nothing

    Sub New(ByVal CL As Socket, SR As Server)

        ClientSocket = CL
        ServerSocket = SR
        ClientSocket.ReceiveBufferSize = 50 * 1000
        ClientSocket.SendBufferSize = 50 * 1000
        IsConnected = True
        BufferLength = 0
        Buffer = New Byte(49999) {}  ' Initialize to proper size to receive protocol headers
        MS = New MemoryStream
        IP = CL.RemoteEndPoint.ToString

        If ServerSocket.Blocked.Contains(IP.Split(":")(0)) Then
            isDisconnected()
            Return
        Else
            Settings.Online.Add(Me)
            ClientSocket.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, New AsyncCallback(AddressOf ListenForClientData), Nothing)
        End If

    End Sub

    ' Alternative constructor for WebSocket connections
    Sub New(Optional isWebSocket As Boolean = False)
        ' Initialize properties for WebSocket mode
        ClientSocket = Nothing
        ServerSocket = Nothing
        IsConnected = True
        BufferLength = 0
        Buffer = New Byte(49999) {}
        MS = New MemoryStream
        IP = "websocket"
        LV = Nothing
        If isWebSocket Then
            Settings.Online.Add(Me)
        End If
    End Sub

    Async Sub ListenForClientData(ByVal ar As IAsyncResult)
        If IsConnected = False OrElse Not ClientSocket.Connected Then
            isDisconnected()
            Exit Sub
        End If
        Try
            Dim Received As Integer = ClientSocket.EndReceive(ar)
            Debug.WriteLine($"[CLIENT-RECV] Received {Received} bytes from {IP}, BufferLengthReceived={BufferLengthReceived}, MS.Length={MS.Length}")
            
            If Received > 0 Then
                If BufferLengthReceived = False Then
                    ' First message - should contain length header
                    Await MS.WriteAsync(Buffer, 0, Received)
                    
                    ' Try to parse the length from the data we received
                    Dim ReceivedData As Byte() = MS.ToArray
                    Dim NullIndex = Array.IndexOf(ReceivedData, CByte(0))
                    
                    If NullIndex > 0 Then
                        ' Found null terminator, extract length string
                        Dim LengthBytes As Byte() = New Byte(NullIndex - 1) {}
                        Array.Copy(ReceivedData, 0, LengthBytes, 0, NullIndex)
                        Try
                            Dim LengthStr As String = Text.Encoding.UTF8.GetString(LengthBytes)
                            If Long.TryParse(LengthStr, BufferLength) Then
                                Debug.WriteLine($"[CLIENT-RECV] Parsed length: {BufferLength}")
                                MS.Dispose()
                                MS = New MemoryStream
                                If Received > NullIndex + 1 Then
                                    ' There's encrypted data after the length header
                                    Await MS.WriteAsync(ReceivedData, NullIndex + 1, Received - NullIndex - 1)
                                End If
                                BufferLengthReceived = True
                                
                                ' CHECK IMMEDIATELY if message is complete
                                If MS.Length >= BufferLength Then
                                    Debug.WriteLine($"[CLIENT-RECV] Complete message in first packet: {MS.Length} bytes")
                                    Dim ClientReq As New Incoming_Requests(Me, MS.ToArray)
                                    Pending.Req_In.Add(ClientReq)
                                    MS.Dispose()
                                    MS = New MemoryStream
                                    Buffer = New Byte(49999) {}
                                    BufferLength = 0
                                    BufferLengthReceived = False
                                Else
                                    Buffer = New Byte(BufferLength - MS.Length) {}
                                End If
                            End If
                        Catch ex As Exception
                            Debug.WriteLine($"[CLIENT-RECV] Error parsing length: {ex.Message}")
                        End Try
                    End If
                Else
                    ' Receiving continuation data
                    Await MS.WriteAsync(Buffer, 0, Received)
                    Debug.WriteLine($"[CLIENT-RECV] Data fragment: MS.Length now {MS.Length}, need {BufferLength}")
                    
                    If (MS.Length >= BufferLength) Then
                        Debug.WriteLine($"[CLIENT-RECV] Complete message received: {MS.Length} bytes")
                        Dim ClientReq As New Incoming_Requests(Me, MS.ToArray)
                        Pending.Req_In.Add(ClientReq)
                        MS.Dispose()
                        MS = New MemoryStream
                        Buffer = New Byte(49999) {}
                        BufferLength = 0
                        BufferLengthReceived = False
                    Else
                        Buffer = New Byte(BufferLength - MS.Length) {}
                    End If
                End If
            Else
                Debug.WriteLine($"[CLIENT-RECV] Connection closed by {IP} (Received=0)")
                isDisconnected()
                Exit Sub
            End If
            ClientSocket.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, New AsyncCallback(AddressOf ListenForClientData), Nothing)
        Catch ex As Exception
            Debug.WriteLine($"[CLIENT-RECV] Exception from {IP}: {ex.Message} - {ex.GetType.Name}")
            isDisconnected()
            Exit Sub
        End Try
    End Sub

    Async Sub BeginSend(ParamArray Msgs As Object())
        If IsConnected OrElse ClientSocket.Connected Then
            Try
                Dim Packer As New Pack
                Dim Data As Byte() = Packer.Serialize(Msgs)

                Debug.WriteLine("BeginSend: Sending packet type " + Msgs(0).ToString + " to " + IP + " with " + Msgs.Length.ToString + " items")

                Using MS As New MemoryStream
                    Dim b As Byte() = AES_Encryptor(Data)
                    Dim L As Byte() = SB(b.Length & CChar(vbNullChar))
                    Await MS.WriteAsync(L, 0, L.Length)
                    Await MS.WriteAsync(b, 0, b.Length)

                    ClientSocket.Poll(-1, SelectMode.SelectWrite)
                    ClientSocket.BeginSend(MS.ToArray, 0, MS.Length, SocketFlags.None, New AsyncCallback(AddressOf EndSend), Nothing)
                    Debug.WriteLine("BeginSend: Sent " + MS.Length.ToString + " bytes")
                End Using
            Catch ex As Exception
                Debug.WriteLine("BeginSend " + ex.Message)
                isDisconnected()
            End Try
        End If
    End Sub

    Sub EndSend(ByVal ar As IAsyncResult)
        Try
            ClientSocket.EndSend(ar)
        Catch ex As Exception
            Debug.WriteLine("EndSend " + ex.Message)
            isDisconnected()
        End Try
    End Sub

    Delegate Sub _isDisconnected()
    Sub isDisconnected()

        IsConnected = False
        Settings.Online.Remove(Me)

        Try
            If LV IsNot Nothing Then
                If Messages.F.InvokeRequired Then
                    Messages.F.Invoke(New _isDisconnected(AddressOf isDisconnected))
                    Exit Sub
                Else
                    LV.Remove()
                    Messages.ClinetLog(Me, "Disconnected", Color.Red)
                End If
            End If
        Catch ex As Exception
            Debug.WriteLine("L.Remove " + ex.Message)
        End Try

        Try
            ClientSocket.Close()
            ClientSocket.Dispose()
        Catch ex As Exception
            Debug.WriteLine("C.Close " + ex.Message)
        End Try

        Try
            MS.Close()
            MS.Dispose()
        Catch ex As Exception
            Debug.WriteLine("MS.Dispose " + ex.Message)
        End Try

    End Sub

End Class
