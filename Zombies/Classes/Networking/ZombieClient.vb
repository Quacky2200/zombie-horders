Option Strict Off

Imports System.Net
Imports System.Net.Sockets
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.Runtime.Serialization
Imports Zombies.Shared.Serialization

Public Class ZombieClient
    Public Event OnStatusMessage(Str As String)
    Public Event OnUserStatusMessage(Str As String)
    Public Event OnMessage(Obj As Object, Address As IPEndPoint)
    Public Event OnBytePacketTimeoutLockout(Address As IPEndPoint)
    Public Event OnBytePacketTransmissionFinished()
    Private RecieveClient As UdpClient
    Private SendClientPort As Integer
    Private RecieveClientPort As Integer
    Private RecieveThread As New System.Threading.Thread(AddressOf WaitForMessage)
    Private ThreadShutdown As Boolean = False
    Private BytePacketAwaitResponse As Boolean = False
    Private BytePacketTimeout As Integer = 2500
    Private BytePacketTimeoutAchieved As Boolean = False
    Private BytePacketTimeoutMaxCount As Integer = 2
    Private BytePacketTimeoutCount As Integer = 0
    Private BytePacketDisableTimeoutEPs As New List(Of IPEndPoint)
    Private WithEvents BytePacketTimeoutTimer As New Timers.Timer(BytePacketTimeout)
    Private WasBytePacket As Boolean = False
    Public Connected As Boolean = False
    Sub New(RecievePort As Integer, SendPort As Integer)
        RecieveClient = New UdpClient(RecievePort)
        RecieveClient.Client.ReceiveBufferSize = 65527
        RecieveClientPort = RecievePort
        SendClientPort = SendPort
        RecieveThread = New System.Threading.Thread(AddressOf WaitForMessage)
        RecieveThread.Start()
        RecieveClient.EnableBroadcast = True
        RaiseEvent OnStatusMessage("Port Check Successful")
    End Sub
    Sub BytePacketTimeout_Elapsed() Handles BytePacketTimeoutTimer.Elapsed
        BytePacketTimeoutAchieved = True
        BytePacketTimeoutTimer.Stop()
    End Sub
    Sub Send(Of T)(ByRef EP As IPEndPoint, ByRef Attachment As T)
        Dim sStream As New IO.MemoryStream()
        Dim bfTemp As New BinaryFormatter
        bfTemp.AssemblyFormat = Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
        Dim isAlreadySerialized As Boolean = Attachment.GetType Is GetType(MetaData)
        bfTemp.Serialize(sStream, If(isAlreadySerialized, Attachment, New MetaData(Attachment)))
        Dim SendingClient = New UdpClient(EP.Address.ToString, EP.Port) 'We assign the location of the computer with an IP and Port thats used in an endpoint variable
        SendingClient.EnableBroadcast = True 'Allow the server to produce broadcast messages
        SendingClient.Send(sStream.ToArray(), sStream.ToArray().Length)
        SendingClient.Close()
        If WasBytePacket Then
            BytePacketTimeoutTimer.Start()
            Do Until BytePacketAwaitResponse Or BytePacketTimeoutAchieved
            Loop
            BytePacketTimeoutTimer.Stop()
            If BytePacketTimeoutAchieved And BytePacketTimeoutCount <> BytePacketTimeoutMaxCount Then
                Send(EP, Attachment)
                BytePacketTimeoutCount += 1
            ElseIf BytePacketTimeoutAchieved And BytePacketTimeoutCount = BytePacketTimeoutMaxCount Then
                BytePacketDisableTimeoutEPs.Add(EP)
                RaiseEvent OnBytePacketTimeoutLockout(EP)
            ElseIf BytePacketAwaitResponse Then
                WasBytePacket = False
                BytePacketAwaitResponse = False
            End If
            BytePacketTimeoutAchieved = False
        End If
    End Sub
    Sub Send(Of T)(ByRef EP() As IPEndPoint, ByRef Attachment As T)
        Dim EPS = EP
        Dim Obj = Attachment
        Dim SendThread As New System.Threading.Thread(Sub()
                                                          Dim SC As New UdpClient
                                                          SC.Client.SendBufferSize = 65527
                                                          SC.EnableBroadcast = True 'Allow the server to produce broadcast messages
                                                          Dim sStream As New IO.MemoryStream()
                                                          Dim bfTemp As New BinaryFormatter
                                                          bfTemp.AssemblyFormat = Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
                                                          bfTemp.Serialize(sStream, Obj)
                                                          If sStream.ToArray.Length > 64000 Then 'We're checking to see if the data is too big for a datagram
                                                              Dim Identity As String = Guid.NewGuid.ToString ' We set an identity to split different bits of data 
                                                              Dim StepBuffer(63999) As Byte 'This step buffer is used to temporarily hold the bytes inside so that we can hold 64000 (it increases to 64000 when it's written to)
                                                              sStream.Position = 0 'always set the position to 0 so we're not reading the end of the stream
                                                              Do Until sStream.Position = sStream.Length 'We'll keep reading until we get to the end of the stream
                                                                  Dim newStream As New IO.MemoryStream ' Make a new memory stream so the first one is not effected
                                                                  Dim newBF As New BinaryFormatter 'Make a new binaryformatter
                                                                  sStream.Read(StepBuffer, 0, StepBuffer.Length) 'Read the data into the buffer
                                                                  For Each EndPoint As IPEndPoint In EPS 'Now we're going to send it to each client connected
                                                                      If Not BytePacketDisableTimeoutEPs.Contains(EndPoint) Then
                                                                          WasBytePacket = True
                                                                          Send(EndPoint, New BytePacket(StepBuffer, sStream.ToArray.Length, Identity)) 'Send the data with the identity and buffer
                                                                      End If
                                                                  Next
                                                              Loop
                                                              RaiseEvent OnBytePacketTransmissionFinished()
                                                          Else
                                                              For Each EndPoint As IPEndPoint In EPS
                                                                  SC.Send(sStream.ToArray(), sStream.ToArray.Length, EndPoint)
                                                              Next
                                                          End If
                                                      End Sub)
        SendThread.IsBackground = True
        SendThread.Start()
    End Sub
    Sub StopClient()
        ThreadShutdown = True
        RecieveThread.Abort()
        'RecieveThread = Nothing
        RecieveClient.Close()
        ' RecieveClient = Nothing
        ThreadShutdown = False
    End Sub
    Sub WaitForMessage()
        While ThreadShutdown = False
            Dim endPoint As IPEndPoint = New IPEndPoint(IPAddress.Parse("127.0.0.1"), RecieveClientPort)
            Dim data() As Byte = RecieveClient.Receive(endPoint)
            endPoint.Port = SendClientPort
            Dim bfTemp As New BinaryFormatter
            Dim sStream As New IO.MemoryStream(data)
            Try
                Dim Obj As Object = bfTemp.UnsafeDeserialize(sStream, Nothing)
                If Obj.GetType.UnderlyingSystemType.ToString.Contains("BytePacketRecieved") Then
                    BytePacketAwaitResponse = True
                Else
                    Debug.Print(Obj.GetType.Name)
                    RaiseEvent OnMessage(Obj, endPoint)
                End If

            Catch ex As SerializationException
                Debug.Print("Failed to deserialize. Reason: " & ex.Message)
                Throw
            Finally
                sStream.Close()
            End Try

        End While
    End Sub
End Class
'Public Enum NetworkUpdateFunction As Short
'    GameStart = 0
'    Position = 1
'    Shoot = 2
'    Death = 3
'    Healed = 4
'    GameEnd = 5
'End Enum