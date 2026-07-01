using UnityEngine;
using System;
using System.Collections.Generic;
using SocketIOClient;
using SocketIOClient.Transport;
using SocketIOClient.Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
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
public class RemotePlayerStateSyncPayload
{
    public string roomId;
    public string playerId;
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

[Serializable]
public class PhoneHandLifecyclePayload
{
    public string roomId;
    public string eventName;
    public string eventValue;
    public int handId;
    public string message;
}

[Serializable]
public class PhoneTurnStatePayload
{
    public string roomId;
    public string playerId;
    public bool isPlayerTurn;
    public string currentPlayerId;
    public string currentPlayerName;
    public int balance;
    public int pot;
    public int currentBet;
    public int playerBet;
    public int amountToCall;
    public int minRaiseTo;
    public bool canFold;
    public bool canCheck;
    public bool canCall;
    public bool canBet;
    public bool canRaise;
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

    private string GetLocalIPv4()
    {
        try
        {
            string interfaceAddress = GetBestNetworkInterfaceIPv4();
            if (!string.IsNullOrEmpty(interfaceAddress))
                return interfaceAddress;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Could not scan network interfaces for phone URL IP: {ex.Message}");
        }

        try
        {
            IPAddress fallback = Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .FirstOrDefault(IsUsableLocalIPv4);

            if (fallback != null)
                return fallback.ToString();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Could not use DNS fallback for phone URL IP: {ex.Message}");
        }

        return "localhost";
    }

    private string GetBestNetworkInterfaceIPv4()
    {
        List<NetworkInterface> interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(IsUsableNetworkInterface)
            .OrderBy(InterfacePriority)
            .ToList();

        foreach (NetworkInterface networkInterface in interfaces.Where(HasGateway))
        {
            string address = GetFirstAddress(networkInterface, true);
            if (!string.IsNullOrEmpty(address))
                return address;
        }

        foreach (NetworkInterface networkInterface in interfaces)
        {
            string address = GetFirstAddress(networkInterface, true);
            if (!string.IsNullOrEmpty(address))
                return address;
        }

        foreach (NetworkInterface networkInterface in interfaces)
        {
            string address = GetFirstAddress(networkInterface, false);
            if (!string.IsNullOrEmpty(address))
                return address;
        }

        return string.Empty;
    }

    private bool IsUsableNetworkInterface(NetworkInterface networkInterface)
    {
        if (networkInterface == null)
            return false;

        if (networkInterface.OperationalStatus != OperationalStatus.Up)
            return false;

        if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
            networkInterface.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
            return false;

        string name = (networkInterface.Name ?? string.Empty).ToLowerInvariant();
        string description = (networkInterface.Description ?? string.Empty).ToLowerInvariant();
        string combined = name + " " + description;

        return !combined.Contains("virtual") &&
               !combined.Contains("vmware") &&
               !combined.Contains("virtualbox") &&
               !combined.Contains("hyper-v") &&
               !combined.Contains("wsl") &&
               !combined.Contains("docker") &&
               !combined.Contains("bluetooth");
    }

    private int InterfacePriority(NetworkInterface networkInterface)
    {
        switch (networkInterface.NetworkInterfaceType)
        {
            case NetworkInterfaceType.Wireless80211:
                return 0;
            case NetworkInterfaceType.Ethernet:
                return 1;
            case NetworkInterfaceType.GigabitEthernet:
                return 2;
            default:
                return 3;
        }
    }

    private bool HasGateway(NetworkInterface networkInterface)
    {
        try
        {
            return networkInterface.GetIPProperties()
                .GatewayAddresses
                .Any(g => g?.Address != null && g.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(g.Address));
        }
        catch
        {
            return false;
        }
    }

    private string GetFirstAddress(NetworkInterface networkInterface, bool privateOnly)
    {
        foreach (UnicastIPAddressInformation unicast in networkInterface.GetIPProperties().UnicastAddresses)
        {
            IPAddress address = unicast.Address;
            if (!IsUsableLocalIPv4(address))
                continue;

            if (privateOnly && !IsPrivateIPv4(address))
                continue;

            return address.ToString();
        }

        return string.Empty;
    }

    private bool IsUsableLocalIPv4(IPAddress address)
    {
        if (address == null || address.AddressFamily != AddressFamily.InterNetwork)
            return false;

        byte[] bytes = address.GetAddressBytes();

        if (IPAddress.IsLoopback(address))
            return false;

        if (bytes[0] == 0 || bytes[0] == 127)
            return false;

        // 169.254.x.x is link-local/APIPA and usually means the adapter did not get a real router IP.
        if (bytes[0] == 169 && bytes[1] == 254)
            return false;

        if (bytes[0] >= 224)
            return false;

        return true;
    }

    private bool IsPrivateIPv4(IPAddress address)
    {
        byte[] bytes = address.GetAddressBytes();

        return bytes[0] == 10 ||
               (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
               (bytes[0] == 192 && bytes[1] == 168);
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

        socket.OnUnityThread("request-phone-state-sync", response =>
        {
            var payload = response.GetValue<RemotePlayerStateSyncPayload>();
            Debug.Log($"Phone state sync requested for {payload.playerId}");
            PhoneTurnStateReporter reporter = FindObjectOfType<PhoneTurnStateReporter>();
            reporter?.ForceSendTurnStates();
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

    public async void SendHandLifecycleToPhones(string eventName, int handId, string message)
    {
        if (socket == null || !isConnected || string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(eventName))
            return;

        PhoneHandLifecyclePayload payload = new PhoneHandLifecyclePayload
        {
            roomId = roomId,
            eventName = eventName,
            eventValue = eventName,
            handId = handId,
            message = message
        };

        try
        {
            await socket.EmitAsync("phone-hand-lifecycle", payload);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to send hand lifecycle to phone: {ex.Message}");
        }
    }

    public async void SendTurnStateToPhone(PhoneTurnStatePayload payload)
    {
        if (socket == null || !isConnected || string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(payload.playerId))
            return;

        payload.roomId = roomId;

        try
        {
            await socket.EmitAsync("phone-turn-state", payload);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to send turn state to phone: {ex.Message}");
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
