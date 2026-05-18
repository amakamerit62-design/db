Imports System.Net.Sockets
Imports System.Net

Public Class Server
    Public S As Socket
    Public Blocked As List(Of String)
    Public allDone As New Threading.ManualResetEvent(False)

    Sub Start(ByVal Port As Integer)
        Try
            Blocked = New List(Of String)

            S = New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            ' Bind to all network interfaces (0.0.0.0) so it accepts the public IP
            Dim IpEndPoint As IPEndPoint = New IPEndPoint(IPAddress.Any, Port)

            S.ReceiveBufferSize = 50 * 1000
            S.SendBufferSize = 50 * 1000
            S.Bind(IpEndPoint)
            S.Listen(20)
            
            ' DuckDNS disabled - using Cloudflare Tunnel for public access
            ' UpdateDuckDNS()
            
            ' Background DuckDNS thread disabled - using Cloudflare Tunnel
            'Dim duckDNSThread As New Threading.Thread(Sub()
            '    While True
            '        Try
            '            Threading.Thread.Sleep(5 * 60 * 1000) ' 5 minutes
            '            UpdateDuckDNS()
            '        Catch
            '        End Try
            '    End While
            'End Sub)
            'duckDNSThread.IsBackground = True
            'duckDNSThread.Start()

            While True
                allDone.Reset()
                S.BeginAccept(New AsyncCallback(AddressOf EndAccept), Nothing)
                allDone.WaitOne()
            End While

        Catch ex As Exception
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1)
            Environment.Exit(0)
        End Try
    End Sub

    Sub EndAccept(ByVal ar As IAsyncResult)
        Try
            Dim IncomingSocket As Socket = S.EndAccept(ar)
            Debug.WriteLine("[SERVER] New connection accepted")
            Dim C As Client = New Client(IncomingSocket, Me)
        Catch ex As Exception
            Debug.WriteLine("EndAccept " + ex.Message)
        Finally
            allDone.Set()
        End Try
    End Sub

End Class
