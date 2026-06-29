using UnityEngine;
using System;
using System.Collections.Generic;
using SocketIOClient;
using SocketIOClient.Transport;
using SocketIOClient.Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Threading.Tasks;

public enum ConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Error
}

public class SocketManager : MonoBehaviour
{
    private SocketIOUnity socket;
    public string roomId = string.Empty;
    public bool isConnected = false;
    public bool errConnecting = false;

    private async void Start()
    {
        await Connect();
        await TryCreateRoom();
    }

    private async Task Connect()
    {
        var uri = new Uri($"http://localhost:5757/tv");
        isConnected = false;
        errConnecting = false;

        socket = new(uri, new SocketIOOptions
        {
            Reconnection = true,
            ReconnectionAttempts = int.MaxValue,
            ReconnectionDelay = 1000,
            Query = new Dictionary<string, string>
            {
                { "token", "UNITY" }
            },
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        });
        socket.JsonSerializer = new NewtonsoftJsonSerializer();

        socket.OnConnected += (_, __) => Debug.Log("Connected");
        socket.OnDisconnected += (_, reason) => Debug.Log("Disconnected: " + reason);

        try
        {
            await socket.ConnectAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError("Connect failed: " + ex.Message);
        }
    }

    public async Task TryCreateRoom()
    {
        try
        {
            CreateRoomAck Ack = await RoomSocket.CreateRoomAsync(socket);
            if(!Ack.ok)
            {
                Debug.Log("Room Creation Failure");
                return;
            }

            Debug.Log($"Room-Created {Ack.roomId}");
            roomId = Ack.roomId;

        }catch (Exception ex)
        {
            Debug.LogError("Failed to create room");
        }
    }
}

