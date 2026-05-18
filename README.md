# VB.NET WebSocket RAT (Remote Administration Tool)

A VB.NET Windows Forms Remote Administration Tool with WebSocket support for cloud deployment.

## Project Status

### ✅ Completed Features
- **Cursor Control**: Server can control client mouse with cursor invisibility
- **Input Control**: Freeze/unfreeze client keyboard and mouse via Ctrl+F toggle  
- **Remote Desktop**: Stream and control client desktop
- **AES-256 Encryption**: All communications encrypted with 256-bit AES
- **WebSocket Protocol**: HTTP-friendly WebSocket transport (ws://)
- **Async Architecture**: Non-blocking async/await patterns for scalability

### 🏗️ In Progress
- Cloud deployment (Railway.app/Azure recommended)
- Client URL configuration for deployed servers
- End-to-end testing with cloud connection

### 📋 Architecture

**Server** (VB.NET Windows Forms)
- GUI displays connected clients and remote control interface
- WebSocket server on port 9823
- Receives encrypted client messages and sends commands
- Supports: Remote Desktop, Mouse Control, Keyboard Control, Input Freeze

**Client** (VB.NET injected executable)
- Invisible injected process on target machine
- Connects to server via WebSocket
- Executes server commands in real-time
- Sends system state (screenshot, input events)

**Protocol**
- Transport: WebSocket (ws://)
- Encryption: AES-256 ECB mode
- Message Format: [length]\x00[encrypted_data]
- Packets: Identification, Ping, Mouse, Keyboard, RemoteDesktop, Freeze, etc.

## Quick Start

### Build
```bash
dotnet build Server/Server.vbproj -c Debug
dotnet build Client/Client.vbproj -c Debug
```

### Run Locally
1. Start Server: `Server\bin\Debug\AsyncRAT.exe`
2. Start Client: `Client\bin\Debug\Client.exe` (or inject into target process)
3. Server connects to client on `ws://localhost:9823`

### Deploy to Cloud
See [DEPLOYMENT.md](DEPLOYMENT.md) for step-by-step cloud deployment on:
- Railway.app (recommended for .NET Framework)
- Azure App Service (production)
- AWS EC2 Windows

## Repository Structure

```
├── Server/              # Server executable with UI
│   ├── Forms/          # WinForms GUI (RemoteDesktop, TaskForm, etc.)
│   ├── Networking/     # WebSocket server + Client handler
│   ├── Requests/       # Message processing
│   └── Resources/      # Stub.vb (embedded client code)
├── Client/             # Client executable
│   ├── Program.vb      # Main client with WebSocket
│   └── WebSocketWrapper.vb  # Socket→WebSocket adapter
└── DEPLOYMENT.md       # Cloud deployment guide
```

## WebSocket Details

### Connection Flow
1. Client initiates ws://server:9823
2. Server accepts WebSocket connection
3. Client sends encrypted identification packet
4. Server adds client to active list
5. Server can send commands; client executes and responds

### Message Format
```
[UTF8 Length String]\x00[Encrypted Binary Data]
```
Example:
```
"256\x00[256 bytes of AES-encrypted data]"
```

### Packet Types (PacketHeader Enum)
- `0` - Identification (initial client info)
- `1` - RemoteDesktopOpen
- `2` - RemoteDesktopSend  
- `3` - ErrorMessages
- `9` - Ping (keepalive)
- `10` - MouseInput
- `11` - KeyboardInput
- `12` - FreezeInput (lock client keyboard/mouse)

## Usage Examples

### Remote Mouse Control
```vb
' Server sends packet: PacketHeader.MouseInput
' Payload: Normalized X (0-65535), Y (0-65535), button flags
' Client: Moves mouse to normalized position, executes button click
```

### Freeze Input
```vb
' Server sends: PacketHeader.FreezeInput with boolean flag
' Client: Sets Program.IsFrozen = true
' When frozen: Ignores all mouse and keyboard events
' Toggle: Ctrl+F in RemoteDesktop form
```

### Keep-Alive
```vb
' Every 30 seconds, client pings server
' Server responds to maintain connection
' Prevents network timeouts
```

## Configuration

### Server Settings (Settings.vb)
- `KEY` - AES encryption key (default: "K9@xR#2mL$vN8pQ&wT4bF!sD5cG7jH*3")
- `ServerPort` - Default 9823
- `Blocked` - List of IPs to reject

### Client Settings
- Hardcoded for testing: `localhost:9823`
- Post-deployment: Update with server URL in Program.vb lines 50-52

## Security Notes

⚠️ **Development/Testing Only**
- Unencrypted WebSocket (ws://)
- No mutual TLS authentication
- Hardcoded encryption key

🔒 **For Production**
- Enable TLS: wss:// protocol
- Use certificate authentication
- Randomize encryption key per session
- Rate limiting on server
- Connection logging and auditing

## Known Limitations

- **Windows-Only Forms**: Requires Windows OS for server UI
- **.NET Framework 4.8**: Limits cloud deployment options (see DEPLOYMENT.md)
- **Injected Process**: Requires access to target system
- **No Anti-Virus Bypass**: Will be detected by modern security software

## Future Improvements

1. **Convert to .NET 8**: Remove Windows dependency, improve cloud compatibility
2. **Plugin System**: Extensible command architecture
3. **Web UI**: Browser-based control interface (Blazor)
4. **Multi-User**: Concurrent admin sessions
5. **C2 Server**: Full command-and-control backend

## License
For educational/authorized use only. Unauthorized access is illegal.

## Repository
https://github.com/amakamerit62-design/db
