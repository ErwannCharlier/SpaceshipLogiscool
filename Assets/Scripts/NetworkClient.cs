using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NativeWebSocket;
using UnityEngine;

public class NetworkClient : MonoBehaviour
{
    [Header("Connection")]
    public string serverUrl = "wss://unity.erwann.xyz";
    public bool connectOnStart = false;

    public string LocalPlayerId { get; private set; }
    public string LastStatus { get; private set; } = "Disconnected";
    public bool IsConnected => socket != null && socket.State == WebSocketState.Open;
    public bool IsConnecting { get; private set; }
    public StationInfo CurrentStation { get; private set; }

    public event Action Connected;
    public event Action Disconnected;
    public event Action<string> StatusChanged;
    public event Action<string> WelcomeReceived;
    public event Action<NetworkPlayerInfo[]> WorldReceived;
    public event Action<StationInfo> StationReceived;
    public event Action<ShootEventMessage> ShootEventReceived;
    public event Action<string> PlayerDisconnected;
    public event Action<HitMessage> HitReceived;

    private WebSocket socket;
    private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);
    private readonly ConcurrentQueue<string> receivedJson = new ConcurrentQueue<string>();
    private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

    private void Awake()
    {
        EnsureStationVisualManager();
    }

    private void Start()
    {
        if (connectOnStart)
        {
            Connect();
        }
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        socket?.DispatchMessageQueue();
#endif

        RunMainThreadActions();
        ReadReceivedMessages();
    }

    private void OnDestroy()
    {
        Disconnect();
    }

    public async void Connect()
    {
        if (IsConnected || IsConnecting)
        {
            QueueStatus("Already connected or connecting");
            return;
        }

        await ConnectAsync();
    }

    public async void Disconnect()
    {
        await DisconnectAsync();
    }

    public void SendJoin(string playerName)
    {
        SendJoin(playerName, string.Empty);
    }

    public void SendJoin(string playerName, string shipId)
    {
        JoinMessage message = new JoinMessage
        {
            type = "join",
            name = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName.Trim(),
            shipId = string.IsNullOrWhiteSpace(shipId) ? ShipLibrary.GetDefaultShipId() : ShipLibrary.NormalizeShipId(shipId)
        };

        SendJson(JsonUtility.ToJson(message));
    }

    public void SendPlayerState(Vector3 position, float yaw)
    {
        SendPlayerState(position, yaw, 0f, 0f);
    }

    public void SendPlayerState(Vector3 position, float yaw, float pitch)
    {
        SendPlayerState(position, yaw, pitch, 0f);
    }

    public void SendPlayerState(Vector3 position, float yaw, float pitch, float roll)
    {
        PlayerStateMessage message = new PlayerStateMessage
        {
            type = "state",
            x = position.x,
            y = position.y,
            z = position.z,
            yaw = yaw,
            pitch = pitch,
            roll = roll
        };

        SendJson(JsonUtility.ToJson(message));
    }

    public void SendShoot(Vector3 position, Vector3 direction)
    {
        Vector3 safeDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;

        ShootMessage message = new ShootMessage
        {
            type = "shoot",
            x = position.x,
            y = position.y,
            z = position.z,
            dx = safeDirection.x,
            dy = safeDirection.y,
            dz = safeDirection.z
        };

        SendJson(JsonUtility.ToJson(message));
    }

    public async void SendJson(string json)
    {
        await SendJsonAsync(json);
    }

    private void EnsureStationVisualManager()
    {
        StationVisualManager stationVisualManager = GetComponent<StationVisualManager>();

        if (stationVisualManager == null)
        {
            stationVisualManager = gameObject.AddComponent<StationVisualManager>();
        }

        stationVisualManager.networkClient = this;
    }

    private async Task ConnectAsync()
    {
        await DisconnectAsync(false);

        serverUrl = serverUrl.Trim();

        if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out Uri uri))
        {
            QueueStatus("Invalid server URL");
            return;
        }

        try
        {
            IsConnecting = true;
            QueueStatus("Connecting...");

            Debug.Log("Connecting to [" + uri + "]");
            Debug.Log("Platform = " + Application.platform);

            socket = new WebSocket(uri.ToString());

            socket.OnOpen += () =>
            {
                Debug.Log("WebSocket opened");

                IsConnecting = false;
                QueueConnected();
            };

            socket.OnError += (error) =>
            {
                Debug.LogError("WebSocket error: " + error);

                IsConnecting = false;
                QueueStatus("Connection failed: " + error);
                QueueDisconnected();
            };

            socket.OnClose += (code) =>
            {
                Debug.Log("WebSocket closed: " + code);

                IsConnecting = false;
                QueueDisconnected();
            };

            socket.OnMessage += (bytes) =>
            {
                string json = Encoding.UTF8.GetString(bytes);
                receivedJson.Enqueue(json);
            };

            await socket.Connect();
        }
        catch (Exception exception)
        {
            IsConnecting = false;
            CleanupSocket();
            QueueStatus("Connection failed: " + exception.Message);
            QueueDisconnected();
        }
    }

    private async Task DisconnectAsync(bool showStatus = true)
    {
        if (showStatus && socket != null)
        {
            QueueStatus("Disconnecting...");
        }

        try
        {
            IsConnecting = false;

            if (socket != null)
            {
                await socket.Close();
            }
        }
        catch
        {
            // Closing can fail if the server already disappeared.
        }
        finally
        {
            CleanupSocket();

            if (showStatus)
            {
                QueueDisconnected();
            }
        }
    }

    private async Task SendJsonAsync(string json)
    {
        if (!IsConnected)
        {
            Debug.LogWarning("Cannot send because socket is not connected: " + json);
            return;
        }

        await sendLock.WaitAsync();

        try
        {
            if (IsConnected)
            {
                await socket.SendText(json);
            }
        }
        catch (Exception exception)
        {
            QueueStatus("Send failed: " + exception.Message);
        }
        finally
        {
            sendLock.Release();
        }
    }

    private void ReadReceivedMessages()
    {
        while (receivedJson.TryDequeue(out string json))
        {
            HandleServerMessage(json);
        }
    }

    private void HandleServerMessage(string json)
    {
        try
        {
            MessageTypeOnly messageType = JsonUtility.FromJson<MessageTypeOnly>(json);

            switch (messageType.type)
            {
                case "welcome":
                    WelcomeMessage welcome = JsonUtility.FromJson<WelcomeMessage>(json);
                    LocalPlayerId = welcome.id;
                    WelcomeReceived?.Invoke(welcome.id);
                    break;

                case "world":
                    WorldMessage world = JsonUtility.FromJson<WorldMessage>(json);
                    CurrentStation = world.station;
                    WorldReceived?.Invoke(world.players);
                    StationReceived?.Invoke(world.station);
                    break;

                case "shoot_event":
                    ShootEventMessage shootEvent = JsonUtility.FromJson<ShootEventMessage>(json);
                    ShootEventReceived?.Invoke(shootEvent);
                    break;

                case "disconnect":
                    DisconnectMessage disconnect = JsonUtility.FromJson<DisconnectMessage>(json);
                    PlayerDisconnected?.Invoke(disconnect.id);
                    break;

                case "hit":
                    HitMessage hit = JsonUtility.FromJson<HitMessage>(json);
                    HitReceived?.Invoke(hit);
                    break;

                default:
                    QueueStatus("Unknown message type: " + messageType.type);
                    break;
            }
        }
        catch (Exception exception)
        {
            QueueStatus("Bad JSON message: " + exception.Message);
        }
    }

    private void RunMainThreadActions()
    {
        while (mainThreadActions.TryDequeue(out Action action))
        {
            action?.Invoke();
        }
    }

    private void QueueStatus(string status)
    {
        mainThreadActions.Enqueue(() =>
        {
            LastStatus = status;
            StatusChanged?.Invoke(status);
        });
    }

    private void QueueConnected()
    {
        mainThreadActions.Enqueue(() =>
        {
            LastStatus = "Connected";
            StatusChanged?.Invoke(LastStatus);
            Connected?.Invoke();
        });
    }

    private void QueueDisconnected()
    {
        mainThreadActions.Enqueue(() =>
        {
            IsConnecting = false;
            LocalPlayerId = null;
            CurrentStation = null;
            LastStatus = "Disconnected";
            StatusChanged?.Invoke(LastStatus);
            StationReceived?.Invoke(null);
            Disconnected?.Invoke();
        });
    }

    private void CleanupSocket()
    {
        socket = null;
    }
}

[Serializable]
public class MessageTypeOnly
{
    public string type;
}

[Serializable]
public class JoinMessage
{
    public string type;
    public string name;
    public string shipId;
}

[Serializable]
public class PlayerStateMessage
{
    public string type;
    public float x;
    public float y;
    public float z;
    public float yaw;
    public float pitch;
    public float roll;
}

[Serializable]
public class ShootMessage
{
    public string type;
    public float x;
    public float y;
    public float z;
    public float dx;
    public float dy;
    public float dz;
}

[Serializable]
public class WelcomeMessage
{
    public string type;
    public string id;
}

[Serializable]
public class WorldMessage
{
    public string type;
    public NetworkPlayerInfo[] players;
    public StationInfo station;
}

[Serializable]
public class NetworkPlayerInfo
{
    public string id;
    public string name;
    public string shipId;
    public float x;
    public float y;
    public float z;
    public float yaw;
    public float pitch;
    public float roll;
    public int health;
    public float energy;
    public int score;
    public bool isAlive;
    public float respawnSeconds;
}

[Serializable]
public class StationInfo
{
    public float x;
    public float y;
    public float z;
    public float size;
}

[Serializable]
public class ShootEventMessage
{
    public string type;
    public string id;
    public float x;
    public float y;
    public float z;
    public float dx;
    public float dy;
    public float dz;
}

[Serializable]
public class DisconnectMessage
{
    public string type;
    public string id;
}

[Serializable]
public class HitMessage
{
    public string type;
    public string targetId;
    public int health;
}