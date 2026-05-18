Imports System.Net.WebSockets
Imports System.Text
Imports System.Threading

Public Class WebSocketClient
    Private WebSocket As ClientWebSocket
    Private Uri As Uri
    Public IsConnected As Boolean = False

    Public Sub New(hostname As String, port As Integer)
        Uri = New Uri($"ws://{hostname}:{port}/")
    End Sub

    Public Async Function Connect() As Task
        Try
            WebSocket = New ClientWebSocket()
            Await WebSocket.ConnectAsync(Uri, CancellationToken.None)
            IsConnected = True
            LogToFile($"[CLIENT] WebSocket connected to {Uri}")
        Catch ex As Exception
            LogToFile($"[CLIENT] WebSocket connection error: {ex.Message}")
            IsConnected = False
            Throw
        End Try
    End Function

    Public Function ReceiveAsync() As Task(Of Byte())
        Return Task.Run(Function()
            Try
                Dim buffer(49999) As Byte
                Dim result = WebSocket.ReceiveAsync(New ArraySegment(Of Byte)(buffer), CancellationToken.None).Result
                
                If result.Count > 0 Then
                    Dim data As Byte() = New Byte(result.Count - 1) {}
                    Array.Copy(buffer, 0, data, 0, result.Count)
                    Return data
                End If
                
                If result.MessageType = WebSocketMessageType.Close Then
                    CloseAsync().Wait()
                End If
                
                Return Nothing
            Catch ex As Exception
                LogToFile($"[CLIENT] Receive error: {ex.Message}")
                Return Nothing
            End Try
        End Function)
    End Function

    Public Async Function SendAsync(data As Byte()) As Task
        Try
            If WebSocket.State = WebSocketState.Open Then
                Await WebSocket.SendAsync(New ArraySegment(Of Byte)(data), WebSocketMessageType.Binary, True, CancellationToken.None)
            End If
        Catch ex As Exception
            LogToFile($"[CLIENT] Send error: {ex.Message}")
        End Try
    End Function

    Public Async Function CloseAsync() As Task
        Try
            If WebSocket IsNot Nothing AndAlso WebSocket.State = WebSocketState.Open Then
                Await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None)
            End If
            IsConnected = False
        Catch ex As Exception
            LogToFile($"[CLIENT] Close error: {ex.Message}")
        End Try
    End Function

    Private Sub LogToFile(message As String)
        Try
            Dim logPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\client_debug.log"
            Dim timestamp = DateTime.Now.ToString("HH:mm:ss.fff")
            Dim logMessage = $"{timestamp} {message}"
            System.IO.File.AppendAllText(logPath, logMessage & Environment.NewLine, Encoding.UTF8)
        Catch
        End Try
    End Sub
End Class
