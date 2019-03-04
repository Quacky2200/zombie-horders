Imports System
Imports System.Text
Imports System.Diagnostics
Imports System.Net.Sockets
Imports System.Net
Module GeneralFunctions
    ''' <summary>
    ''' Get the angle from 2 points
    ''' </summary>
    Public Function GetAngleOfPoints(ByVal Point1 As Point, ByVal Point2 As Point) As Double
        Dim diffX As Single = CSng(Point1.X - Point2.X)
        Dim diffY As Single = CSng(Point1.Y - Point2.Y)
        Dim Angle As Double = Math.Atan2(Point2.Y - Point1.Y, Point2.X - Point1.X) * 180 / Math.PI
        Return If(Angle < 0, 360 + Angle, Angle)
    End Function
    Public Function MovebyAngle(ByVal Point As Point, ByVal Distance As Integer, ByVal Angle As Double) As Point
        Point.X = (Math.Sin(Angle * (Math.PI / 180)) * Distance + Point.X)
        Point.Y = (Math.Cos(Angle * (Math.PI / 180)) * Distance + Point.Y)
        Return Point
    End Function
    Public Function ConvertWritableBitmapToBitmapImage(wbm As WriteableBitmap) As BitmapImage
        Dim bmimage As New BitmapImage
        Using stream As New IO.MemoryStream
            Dim encoder As New PngBitmapEncoder
            encoder.Frames.Add(BitmapFrame.Create(wbm))
            encoder.Save(stream)
            bmimage.BeginInit()
            bmimage.CacheOption = BitmapCacheOption.OnLoad
            bmimage.StreamSource = stream
            bmimage.EndInit()
            bmimage.Freeze()
        End Using
        Return bmimage
    End Function
    'Public Function GetDistanceOfPoints(ByVal Point1 As Point, Point2 As Point) As Double
    '    If Point1.X - Point2.X >= 0 Then
    '        If Point1.Y - Point2.Y >= 0 Then
    '            Return ((Point1.X - Point2.X) + (Point1.Y - Point2.Y)) / 2
    '        Else
    '            Return ((Point1.X - Point2.X) + (Point2.Y - Point1.Y)) / 2
    '        End If
    '    Else
    '        If Point1.Y - Point2.Y >= 0 Then
    '            Return ((Point2.Y - Point1.Y) + (Point1.Y - Point2.Y)) / 2
    '        Else
    '            Return ((Point2.Y - Point1.Y) + (Point2.Y - Point1.Y)) / 2
    '        End If
    '    End If
    'End Function
    ''' <summary>
    ''' Detect if a location is within the rectangle
    ''' </summary>
    ''' <param name="R"></param>
    ''' <param name="P"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function PointInRectangle(R As Rect, P As Point) As Boolean
        If P.X >= R.Left AndAlso P.X <= R.Right AndAlso P.Y >= R.Top AndAlso P.Y <= R.Bottom Then : Return True
        Else : Return False
        End If
    End Function
    Public Function LocalHost() As String
        Dim strHostName = System.Net.Dns.GetHostName()
        Return System.Net.Dns.GetHostEntry(strHostName).AddressList(1).ToString()
    End Function
    Public Function GetDistanceFromPointsXY(ByRef Point1 As Point, Point2 As Point) As Double
        Dim x1 = Point1.X
        Dim y1 = Point1.Y
        Dim x2 = Point2.X
        Dim y2 = Point2.Y
        Return Math.Sqrt(((x1 - x2) * (x1 - x2)) + ((y1 - y2) * (y1 - y2)))
    End Function
End Module
Public Class UPNP
    Sub New()

    End Sub
    Public Sub OpenFirewallPort(port As Integer)
        Dim nics As System.Net.NetworkInformation.NetworkInterface() = NetworkInformation.NetworkInterface.GetAllNetworkInterfaces
        For Each nic As NetworkInformation.NetworkInterface In nics
            Try
                Dim machineIP As String = nic.GetIPProperties().UnicastAddresses(0).Address.ToString
                For Each gwInfo As NetworkInformation.GatewayIPAddressInformation In nic.GetIPProperties().GatewayAddresses
                    Try
                        OpenFirewallPort(machineIP, gwInfo.Address.ToString(), port)
                    Catch ex As Exception

                    End Try
                Next
            Catch ex As Exception
            End Try
        Next
    End Sub
    Public Sub OpenFirewallPort(machineIP As String, firewallIP As String, openPort As Integer)
        Dim svc As String = getServicesFromDevice(firewallIP)
        openPortFromService(svc, "urn:schemas-upnp.org:service:WANIPConnection:1", machineIP, firewallIP, 80, openPort)
        openPortFromService(svc, "urn:schemas-upnp.org:service:WANPPPConnection:1", machineIP, firewallIP, 80, openPort)
    End Sub
    Private Function getServicesFromDevice(firewallIP As String) As String
        Dim queryResponse As String = "239.255.255.250"
        Try
            Dim query As String = ""
            Dim client As New Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            Dim endPoint As New IPEndPoint(IPAddress.Parse(firewallIP), 1900)
            client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1500)
            Dim q As Byte() = Encoding.ASCII.GetBytes(query)
            client.SendTo(q, q.Length, SocketFlags.None, endPoint)
            Dim sender As New IPEndPoint(IPAddress.Any, 0)
            Dim senderEP As EndPoint = CType(sender, EndPoint)

            Dim data(1024) As Byte
            Dim recv As Integer = client.ReceiveFrom(data, senderEP)
            queryResponse = Encoding.ASCII.GetString(data)
        Catch ex As Exception
        End Try
        If queryResponse.Length = 0 Then
            Return ""
        End If
        Dim location As String = ""
        Dim parts() As String = queryResponse.Split(New String() {System.Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries)
        For Each part As String In parts
            If part.ToLower().StartsWith("location") Then
                location = part.Substring(part.IndexOf(":") + 1)
                Exit For
            End If
        Next
        If location.Length = 0 Then
            Return ""
        End If
        Dim webClient As New WebClient
        Try
            Dim ret As String = webClient.DownloadString(location)
            Debug.Print(ret)
            Return ret
        Catch ex As Exception
            Debug.Print(ex.Message)
        Finally
            webClient.Dispose()
        End Try
        Return ""
    End Function
    Private Sub openPortFromService(services As String, serviceType As String, machineIP As String, firewallIP As String, gatewayport As Integer, portToForward As Integer)
        If services.Length = 0 Then Return
        Dim svcIndex As Integer = services.IndexOf(serviceType)
        If svcIndex = -1 Then Return
        Dim controlUrl As String = services.Substring(svcIndex)
        Dim tag1 As String = "<controlURL>"
        Dim tag2 As String = "</controlURL>"
        controlUrl = controlUrl.Substring(controlUrl.IndexOf(tag1) + tag1.Length)
        controlUrl = controlUrl.Substring(0, controlUrl.IndexOf(tag2))
        Dim soapBody As String = "<s:Envelope " &
 "xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/ \""" & _
 "s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/ \"" > " & _
  "<s:Body>" & _
    "<u:AddPortMapping xmlns:u=""" & serviceType & " \ "" > " & _
        "<NewRemoteHost></NewRemoteHost>" & _
        "<NewExternalPort>" & portToForward.ToString() & "</NewExternalPort>" & _
        "<NewProtocol>TCP</NewProtocol>" & _
        "<NewInternalPort>" & portToForward.ToString() & "</NewInternalPort>" & _
        "<NewInternalClient>" & machineIP & "</NewInternalClient>" & _
        "<NewEnabled>1</NewEnabled>" & _
        "<NewPortMappingDescription>Zombie Game</NewPortMappingDescription>" & _
        "<NewLeaseDuration>0</NewLeaseDuration>" & _
    "</u:AddPortMapping>" & _
  "</s:Body>" & _
 "</s:Envelope>"
        Dim body As Byte() = UTF8Encoding.ASCII.GetBytes(soapBody)
        Dim url As String = "http://" & firewallIP & ":" & gatewayport.ToString & controlUrl
        Dim wr As WebRequest = WebRequest.Create(url)
        wr.Method = "POST"
        wr.Headers.Add("SOAPAction", """" & serviceType & "#AddPortMapping""")
        wr.ContentType = "text/xml;charset=""utf-8"""
        wr.ContentLength = body.Length
        Dim stream As System.IO.Stream = wr.GetRequestStream
        stream.Write(body, 0, body.Length)
        stream.Flush()
        stream.Close()

        Dim wres As WebResponse = wr.GetResponse
        Dim sr As New IO.StreamReader(wres.GetResponseStream())
        Dim ret As String = sr.ReadToEnd
        sr.Close()

        Debug.Print("Setting port forwarding: " & portToForward.ToString & " " & ret)
    End Sub
End Class
