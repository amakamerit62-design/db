Imports System.Net
Imports System.Net.WebSockets
Imports System.Text
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
        Dim wsContext = context.AcceptWebSocketAsync(Nothing).Result
        Dim ws = wsContext.WebSocket
        Debug.WriteLine($"[WebSocket Server] New connection accepted from {context.Request.RemoteEndPoint}")
        
        ' Create client handler for this connection
        Dim clientHandler = New WebSocketClientHandler(ws)
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
    Private Encryption As New Pack.AesEncryption()

    Public Sub New(ws As WebSocket)
        Me.WebSocket = ws
        ' Start listening for messages
        Dim t As New Thread(AddressOf ListenForMessages)
        t.IsBackground = True
        t.Start()
    End Sub

    Private Sub ListenForMessages()
        Try
            Dim buffer(49999) As Byte
            While WebSocket.State = WebSocketState.Open
                Dim result = WebSocket.ReceiveAsync(New ArraySegment(Of Byte)(buffer), CancellationToken.None).Result
                
                If result.MessageType = WebSocketMessageType.Binary AndAlso result.Count > 0 Then
                    ' Decrypt the message
                    Dim encryptedData As Byte() = New Byte(result.Count - 1) {}
                    Array.Copy(buffer, 0, encryptedData, 0, result.Count)
                    
                    Try
                        Dim decrypted As String = Encryption.DecryptData(encryptedData)
                        Debug.WriteLine($"[WebSocket] Received: {decrypted}")
                        
                        ' Process the message (mouse, keyboard, etc.)
                        Messages.Read(encryptedData)
                    Catch ex As Exception
                        Debug.WriteLine($"[WebSocket] Decryption error: {ex.Message}")
                    End Try
                End If
                
                If result.MessageType = WebSocketMessageType.Close Then
                    WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).Wait()
                    Debug.WriteLine("[WebSocket] Connection closed")
                End If
            End While
        Catch ex As Exception
            Debug.WriteLine($"[WebSocket] Error in listen loop: {ex.Message}")
        Finally
            If WebSocket IsNot Nothing Then
                WebSocket.Dispose()
            End If
        End Try
    End Sub

    Public Sub SendEncryptedMessage(data As Byte())
        Try
            If WebSocket.State = WebSocketState.Open Then
                WebSocket.SendAsync(New ArraySegment(Of Byte)(data), WebSocketMessageType.Binary, True, CancellationToken.None).Wait()
            End If
        Catch ex As Exception
            Debug.WriteLine($"[WebSocket] Send error: {ex.Message}")
        End Try
    End Sub
End Class
