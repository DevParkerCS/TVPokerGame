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

[Serializable]
public class RemotePlayerColor
{
    public int r;
    public int g;
    public int b;
}

[Serializable]
public class RemotePlayerPayload
{
    public string roomId;
    public string playerId;
    public string name;
    public string spriteCode;
    public RemotePlayerColor color;
    public int balance;
}

[Serializable]
public class RemotePlayerActionPayload
{
    public string playerId;
    public string action;
    public int amount;
}

public class SocketManager : MonoBehaviour
{
    private SocketIOUnity socket;
    [SerializeField] private LocalServerLauncher servLauncher;
    [SerializeField] private GameManager gameManager;
    public string roomId = string.Empty;
    public bool isConnected = false;
    public bool errConnecting = false;

    private int ServerPort => servLauncher != null ? servLauncher.port : 5757;

    private async void Start()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        await Connect();
        await TryCreateRoom();
    }

    string GetLocalIPv4()
    {
        return Dns.GetHostEntry(Dns.GetHostName())
                  .AddressList
                  .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)?
                  .ToString() ?? "localhost";
    }

    private async Task Connect()
    {
        var uri = new Uri($"http://localhost:{ServerPort}/tv");
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

        socket.OnConnected += (_, __) =>
        {
            isConnected = true;
            Debug.Log("Connected");
        };
        socket.OnDisconnected += (_, reason) =>
        {
            isConnected = false;
            Debug.Log("Disconnected: " + reason);
        };

        socket.OnUnityThread("player-joined", response =>
        {
            var payload = response.GetValue<RemotePlayerPayload>();
            gameManager?.AddRemotePlayer(payload);
        });

        socket.OnUnityThread("player-action", response =>
        {
            var payload = response.GetValue<RemotePlayerActionPayload>();
            gameManager?.HandleRemotePlayerAction(payload.playerId, payload.action, payload.amount);
        });

        try
        {
            await socket.ConnectAsync();
        }
        catch (Exception ex)
        {
            errConnecting = true;
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
            Debug.LogError("Failed to create room: " + ex.Message);
        }
    }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(roomId)) return;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 32,
            alignment = TextAnchor.UpperLeft
        };
        style.normal.textColor = Color.white;

        string phoneUrl = $"http://{GetLocalIPv4()}:3000";
        GUI.Label(new Rect(24, 24, 900, 120), $"Room Code: {roomId}\nPhone: {phoneUrl}", style);
    }
}