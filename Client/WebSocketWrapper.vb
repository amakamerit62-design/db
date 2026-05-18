Imports System.Net.WebSockets
Imports System.Text
Imports System.Threading

''' <summary>
''' Wrapper that makes WebSocketClient compatible with Socket-like interface
''' </summary>
Public Class WebSocketWrapper
    Private WebSocket As ClientWebSocket
    Private Uri As Uri
    Public IsConnected As Boolean = False
    Private ReceiveCallback As AsyncCallback
    Private ReceiveBuffer() As Byte

    Public Sub New(hostname As String, port As Integer)
        Uri = New Uri($"ws://{hostname}:{port}/")
    End Sub

    Public Sub Connect()
        Try
            WebSocket = New ClientWebSocket()
            WebSocket.ConnectAsync(Uri, CancellationToken.None).Wait()
            IsConnected = True
        Catch ex As Exception
            IsConnected = False
            Throw
        End Try
    End Sub

    Public Sub BeginReceive(buffer() As Byte, offset As Integer, count As Integer, flags As Integer, callback As AsyncCallback, state As Object)
        ReceiveBuffer = buffer
        ReceiveCallback = callback
        
        Task.Run(Sub()
            Try
                Dim result = WebSocket.ReceiveAsync(New ArraySegment(Of Byte)(buffer, offset, count), CancellationToken.None).Result
                ' Create IAsyncResult-like result
                Dim asyncResult = New WebSocketAsyncResult(result, buffer, offset, result.Count, state)
                callback?.Invoke(asyncResult)
            Catch ex As Exception
                Dim asyncResult = New WebSocketAsyncResult(Nothing, buffer, offset, 0, state)
                asyncResult.Exception = ex
                callback?.Invoke(asyncResult)
            End Try
        End Sub)
    End Sub

    Public Function EndReceive(ar As IAsyncResult) As Integer
        If TypeOf ar Is WebSocketAsyncResult Then
            Dim result = CType(ar, WebSocketAsyncResult)
            If result.Exception IsNot Nothing Then
                Throw result.Exception
            End If
            Return result.BytesReceived
        End If
        Return 0
    End Function

    Public Sub Send(buffer() As Byte, offset As Integer, count As Integer, flags As Integer)
        Try
            Dim data As Byte() = New Byte(count - 1) {}
            Array.Copy(buffer, offset, data, 0, count)
            WebSocket.SendAsync(New ArraySegment(Of Byte)(data), WebSocketMessageType.Binary, True, CancellationToken.None).Wait()
        Catch ex As Exception
            Throw
        End Try
    End Sub

    Public Sub BeginSend(buffer() As Byte, offset As Integer, count As Integer, flags As Integer, callback As AsyncCallback, state As Object)
        Task.Run(Sub()
            Try
                Dim data As Byte() = New Byte(count - 1) {}
                Array.Copy(buffer, offset, data, 0, count)
                WebSocket.SendAsync(New ArraySegment(Of Byte)(data), WebSocketMessageType.Binary, True, CancellationToken.None).Wait()
                Dim asyncResult = New WebSocketAsyncResult(Nothing, buffer, offset, count, state)
                callback?.Invoke(asyncResult)
            Catch ex As Exception
                Dim asyncResult = New WebSocketAsyncResult(Nothing, buffer, offset, 0, state)
                asyncResult.Exception = ex
                callback?.Invoke(asyncResult)
            End Try
        End Sub)
    End Sub

    Public Function EndSend(ar As IAsyncResult) As Integer
        If TypeOf ar Is WebSocketAsyncResult Then
            Dim result = CType(ar, WebSocketAsyncResult)
            If result.Exception IsNot Nothing Then
                Throw result.Exception
            End If
            Return result.BytesReceived
        End If
        Return 0
    End Function
End Class

''' <summary>
''' Simulates IAsyncResult for WebSocket operations
''' </summary>
Public Class WebSocketAsyncResult
    Implements IAsyncResult

    Public Property BytesReceived As Integer
    Public Property AsyncState As Object Implements IAsyncResult.AsyncState
    Public Property AsyncWaitHandle As Threading.WaitHandle Implements IAsyncResult.AsyncWaitHandle
    Public Property CompletedSynchronously As Boolean Implements IAsyncResult.CompletedSynchronously
    Public Property IsCompleted As Boolean Implements IAsyncResult.IsCompleted
    Public Property Exception As Exception

    Public Sub New(result As System.Net.WebSockets.WebSocketReceiveResult, buffer() As Byte, offset As Integer, bytesReceived As Integer, state As Object)
        Me.BytesReceived = bytesReceived
        Me.AsyncState = state
        Me.IsCompleted = True
        Me.CompletedSynchronously = True
    End Sub
End Class
