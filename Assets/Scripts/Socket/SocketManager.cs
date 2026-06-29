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

[Serializable]
public class CardPayload
{
    public string rank;
    public string suit;
    public string code;
}

[Serializable]
public class DealPlayerCardsPayload
{
    public string roomId;
    public string playerId;
    public List<CardPayload> cards;
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

        bool connected = await Connect();
        if (!connected)
        {
            Debug.LogError($"TV socket could not connect to http://localhost:{ServerPort}/tv. Start/build the server first, or add LocalServerLauncher to the scene.");
            return;
        }

        await TryCreateRoom();
    }

    string GetLocalIPv4()
    {
        return Dns.GetHostEntry(Dns.GetHostName())
                  .AddressList
                  .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)?
                  .ToString() ?? "localhost";
    }

    private async Task<bool> Connect()
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
            return true;
        }
        catch (Exception ex)
        {
            errConnecting = true;
            Debug.LogError("Connect failed: " + ex.Message);
            return false;
        }
    }

    public async Task TryCreateRoom()
    {
        if (socket == null || !isConnected)
        {
            Debug.LogError("Cannot create room because the TV socket is not connected.");
            return;
        }

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

    public async void SendHoleCardsToPhone(string playerId, List<Card> cards)
    {
        if (socket == null || !isConnected || string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(playerId))
            return;

        DealPlayerCardsPayload payload = new DealPlayerCardsPayload
        {
            roomId = roomId,
            playerId = playerId,
            cards = cards.Select(ToPayload).ToList()
        };

        try
        {
            await socket.EmitAsync("deal-player-cards", payload);
            Debug.Log($"Sent hole cards to {playerId}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to send hole cards to phone: {ex.Message}");
        }
    }

    private CardPayload ToPayload(Card card)
    {
        string rank = RankCode(card.rank);
        string suit = SuitCode(card.suit);
        return new CardPayload
        {
            rank = rank,
            suit = suit,
            code = $"{rank}{suit}"
        };
    }

    private string RankCode(Card.Rank rank)
    {
        switch (rank)
        {
            case Card.Rank.Ace: return "A";
            case Card.Rank.King: return "K";
            case Card.Rank.Queen: return "Q";
            case Card.Rank.Jack: return "J";
            case Card.Rank.Ten: return "T";
            default: return ((int)rank).ToString();
        }
    }

    private string SuitCode(Card.Suit suit)
    {
        switch (suit)
        {
            case Card.Suit.Clubs: return "C";
            case Card.Suit.Diamonds: return "D";
            case Card.Suit.Hearts: return "H";
            case Card.Suit.Spades: return "S";
            default: return "?";
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