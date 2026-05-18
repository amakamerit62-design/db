Imports System.Net
Imports System.Net.WebSockets
Imports System.IO
Imports System.Threading

Public Class WebSocketServer
    Private Listener As HttpListener
    Private Running As Boolean = False
    Public Port As Integer = 9823

    Public Sub Start()
        Listener = New HttpListener()
        Listener.Prefixes.Add($"http://*:{Port}/")
        Listener.Start()
        Running = True
        Debug.WriteLine($"[WebSocket Server] Listening on ws://localhost:{Port}")
        
        ' Accept connections in background thread
        Dim t As New Thread(AddressOf AcceptConnections)
        t.IsBackground = True
        t.Start()
    End Sub

    Private Sub AcceptConnections()
        While Running
            Try
                Dim context = Listener.GetContext()
                If context.Request.IsWebSocketRequest Then
                    ProcessWebSocketRequest(context)
                Else
                    context.Response.StatusCode = 400
                    context.Response.Close()
                End If
            Catch ex As Exception
                Debug.WriteLine($"[WebSocket Server] Error: {ex.Message}")
            End Try
        End While
    End Sub

    Private Sub ProcessWebSocketRequest(context As HttpListenerContext)
        Try
            Dim wsContext = context.AcceptWebSocketAsync(Nothing).Result
            Dim ws = wsContext.WebSocket
            Debug.WriteLine($"[WebSocket Server] New connection accepted from {context.Request.RemoteEndPoint}")
            
            ' Create client handler for this connection
            Dim clientHandler = New WebSocketClientHandler(ws)
        Catch ex As Exception
            Debug.WriteLine($"[WebSocket Server] WebSocket error: {ex.Message}")
            context.Response.StatusCode = 500
            context.Response.Close()
        End Try
    End Sub

    Public Sub [Stop]()
        Running = False
        If Listener IsNot Nothing Then
            Listener.Stop()
            Listener.Close()
        End If
    End Sub
End Class

Public Class WebSocketClientHandler
    Private WebSocket As WebSocket
    Private MS As MemoryStream
    Private BufferLength As Long = 0
    Private BufferLengthReceived As Boolean = False

    Public Sub New(ws As WebSocket)
        Me.WebSocket = ws
        MS = New MemoryStream()
        ' Start listening for messages
        Dim t As New Thread(AddressOf ListenForMessages)
        t.IsBackground = True
        t.Start()
    End Sub

    Private Sub ListenForMessages()
        Try
            Dim buffer(49999) As Byte
            While WebSocket.State = WebSocketState.Open
                Try
                    Dim result = WebSocket.ReceiveAsync(New ArraySegment(Of Byte)(buffer), CancellationToken.None).Result
                    
                    If result.MessageType = WebSocketMessageType.Binary AndAlso result.Count > 0 Then
                        Debug.WriteLine($"[WebSocket Handler] Received {result.Count} bytes")
                        
                        ' Write to memory stream for protocol parsing
                        MS.Write(buffer, 0, result.Count)
                        
                        ' Try to parse complete messages
                        While ParseMessage()
                            ' Continue parsing if there are more complete messages
                        End While
                    End If
                    
                    If result.MessageType = WebSocketMessageType.Close Then
                        WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).Wait()
                        Debug.WriteLine("[WebSocket Handler] Connection closed by client")
                    End If
                Catch ex As Exception
                    Debug.WriteLine($"[WebSocket Handler] Receive error: {ex.Message}")
                    Exit While
                End Try
            End While
        Catch ex As Exception
            Debug.WriteLine($"[WebSocket Handler] Fatal error in listen loop: {ex.Message}")
        Finally
            If WebSocket IsNot Nothing Then
                WebSocket.Dispose()
            End If
        End Try
    End Sub

    Private Function ParseMessage() As Boolean
        Try
            Dim ReceivedData As Byte() = MS.ToArray
            
            If Not BufferLengthReceived Then
                ' Look for length header (ends with 0x00)
                Dim NullIndex = Array.IndexOf(ReceivedData, CByte(0))
                
                If NullIndex > 0 Then
                    ' Extract length string
                    Dim LengthBytes As Byte() = New Byte(NullIndex - 1) {}
                    Array.Copy(ReceivedData, 0, LengthBytes, 0, NullIndex)
                    
                    If Long.TryParse(System.Text.Encoding.UTF8.GetString(LengthBytes), BufferLength) Then
                        BufferLengthReceived = True
                        
                        ' Check if complete message arrived
                        If ReceivedData.Length >= NullIndex + 1 + BufferLength Then
                            ' We have complete encrypted data
                            Dim EncryptedData As Byte() = New Byte(BufferLength - 1) {}
                            Array.Copy(ReceivedData, NullIndex + 1, EncryptedData, 0, BufferLength)
                            
                            Debug.WriteLine($"[WebSocket Handler] Complete message: {EncryptedData.Length} encrypted bytes")
                            
                            ' Process the message
                            ThreadPool.QueueUserWorkItem(New WaitCallback(AddressOf ProcessEncryptedMessage), EncryptedData)
                            
                            ' Prepare for next message
                            Dim RemainingBytes = ReceivedData.Length - (NullIndex + 1 + BufferLength)
                            MS = New MemoryStream()
                            If RemainingBytes > 0 Then
                                MS.Write(ReceivedData, NullIndex + 1 + CInt(BufferLength), RemainingBytes)
                            End If
                            BufferLength = 0
                            BufferLengthReceived = False
                            Return True ' Continue parsing
                        End If
                    End If
                End If
            End If
        Catch ex As Exception
            Debug.WriteLine($"[WebSocket Handler] Parse error: {ex.Message}")
        End Try
        Return False ' No complete message
    End Function

    Private Sub ProcessEncryptedMessage(state As Object)
        Try
            Dim EncryptedData As Byte() = CType(state, Byte())
            ' Create a Client instance for WebSocket mode
            Dim wsClient As New Client(True)
            Messages.Read(wsClient, EncryptedData)
        Catch ex As Exception
            Debug.WriteLine($"[WebSocket Handler] Message processing error: {ex.Message}")
        End Try
    End Sub

    Public Sub SendEncryptedMessage(data As Byte())
        Try
            If WebSocket.State = WebSocketState.Open Then
                WebSocket.SendAsync(New ArraySegment(Of Byte)(data), WebSocketMessageType.Binary, True, CancellationToken.None).Wait()
            End If
        Catch ex As Exception
            Debug.WriteLine($"[WebSocket Handler] Send error: {ex.Message}")
        End Try
    End Sub
End Class
