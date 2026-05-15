using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("References")]
    public NetworkClient networkClient;

    [Header("Connection UI")]
    public InputField playerNameInput;
    public InputField serverUrlInput;
    public Button connectButton;
    public Button disconnectButton;
    public Text statusText;
    public GameObject connectionPanel;

    [Header("Optional Game UI")]
    public Text healthText;
    public Text scoreText;

    private string pendingPlayerName = "Player";
    private int localHealth = 100;
    private int localScore = 0;

    private void Awake()
    {
        if (networkClient == null)
        {
            networkClient = FindObjectOfType<NetworkClient>();
        }
    }

    private void OnEnable()
    {
        if (networkClient != null)
        {
            networkClient.Connected += HandleConnected;
            networkClient.Disconnected += HandleDisconnected;
            networkClient.StatusChanged += HandleStatusChanged;
            networkClient.WorldReceived += HandleWorld;
            networkClient.HitReceived += HandleHit;
        }

        if (connectButton != null)
        {
            connectButton.onClick.AddListener(ConnectButtonClicked);
        }

        if (disconnectButton != null)
        {
            disconnectButton.onClick.AddListener(DisconnectButtonClicked);
        }
    }

    private void Start()
    {
        if (serverUrlInput != null && networkClient != null)
        {
            serverUrlInput.text = networkClient.serverUrl;
        }

        HandleStatusChanged(networkClient != null ? networkClient.LastStatus : "No NetworkClient");
        UpdateStatsText();
        UpdateButtons();
    }

    private void OnDisable()
    {
        if (networkClient != null)
        {
            networkClient.Connected -= HandleConnected;
            networkClient.Disconnected -= HandleDisconnected;
            networkClient.StatusChanged -= HandleStatusChanged;
            networkClient.WorldReceived -= HandleWorld;
            networkClient.HitReceived -= HandleHit;
        }

        if (connectButton != null)
        {
            connectButton.onClick.RemoveListener(ConnectButtonClicked);
        }

        if (disconnectButton != null)
        {
            disconnectButton.onClick.RemoveListener(DisconnectButtonClicked);
        }
    }

    private void ConnectButtonClicked()
    {
        if (networkClient == null)
        {
            return;
        }

        if (serverUrlInput != null && !string.IsNullOrWhiteSpace(serverUrlInput.text))
        {
            networkClient.serverUrl = serverUrlInput.text.Trim();
        }

        pendingPlayerName = "Player";

        if (playerNameInput != null && !string.IsNullOrWhiteSpace(playerNameInput.text))
        {
            pendingPlayerName = playerNameInput.text.Trim();
        }

        if (networkClient.IsConnected)
        {
            networkClient.SendJoin(pendingPlayerName);
        }
        else
        {
            networkClient.Connect();
        }

        UpdateButtons();
    }

    private void DisconnectButtonClicked()
    {
        if (networkClient != null)
        {
            networkClient.Disconnect();
        }

        UpdateButtons();
    }

    private void HandleConnected()
    {
        networkClient.SendJoin(pendingPlayerName);
        SetConnectionPanelVisible(false);
        UpdateButtons();
    }

    private void HandleDisconnected()
    {
        SetConnectionPanelVisible(true);
        UpdateButtons();
    }

    private void HandleStatusChanged(string status)
    {
        if (statusText != null)
        {
            statusText.text = "Status: " + status;
        }

        UpdateButtons();
    }

    private void HandleWorld(NetworkPlayerInfo[] players)
    {
        if (players == null || networkClient == null)
        {
            return;
        }

        for (int i = 0; i < players.Length; i++)
        {
            NetworkPlayerInfo player = players[i];

            if (player != null && player.id == networkClient.LocalPlayerId)
            {
                localHealth = player.health;
                localScore = player.score;
                UpdateStatsText();
                return;
            }
        }
    }

    private void HandleHit(HitMessage hit)
    {
        if (hit != null && networkClient != null && hit.targetId == networkClient.LocalPlayerId)
        {
            localHealth = hit.health;
            UpdateStatsText();
        }
    }

    private void UpdateStatsText()
    {
        if (healthText != null)
        {
            healthText.text = "Health: " + localHealth;
        }

        if (scoreText != null)
        {
            scoreText.text = "Score: " + localScore;
        }
    }

    private void UpdateButtons()
    {
        bool isConnected = networkClient != null && networkClient.IsConnected;
        bool isConnecting = networkClient != null && networkClient.IsConnecting;

        if (connectButton != null)
        {
            connectButton.interactable = !isConnected && !isConnecting;
        }

        if (disconnectButton != null)
        {
            disconnectButton.interactable = isConnected || isConnecting;
        }
    }

    private void SetConnectionPanelVisible(bool isVisible)
    {
        if (connectionPanel != null)
        {
            connectionPanel.SetActive(isVisible);
        }
    }
}
