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

    private const float MaxStatValue = 100f;
    private const float CrosshairDistance = 80f;

    private string pendingPlayerName = "Player";
    private float localHealth = MaxStatValue;
    private float localEnergy = MaxStatValue;
    private int localScore = 0;
    private bool localIsAlive = true;
    private float localRespawnSeconds = 0f;

    private Image healthBarFill;
    private Image energyBarFill;
    private Text healthValueText;
    private Text energyLabelText;
    private Text energyValueText;
    private Text respawnText;
    private GameObject crosshairRoot;
    private RectTransform crosshairRect;
    private RectTransform hudRect;
    private Camera targetCamera;
    private SpaceshipController localPlayerController;
    private static Sprite defaultUiSprite;

    private void Awake()
    {
        if (networkClient == null)
        {
            networkClient = FindObjectOfType<NetworkClient>();
        }

        localPlayerController = FindObjectOfType<SpaceshipController>();
        targetCamera = Camera.main;

        if (defaultUiSprite == null)
        {
            defaultUiSprite = CreateDefaultSprite();
        }

        CreateHudIfNeeded();
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

        CreateHudIfNeeded();
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

    private void Update()
    {
        UpdateCrosshairPosition();
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
        UpdateCrosshairVisible();
        UpdateButtons();
    }

    private void HandleDisconnected()
    {
        localRespawnSeconds = 0f;
        localIsAlive = true;
        SetConnectionPanelVisible(true);
        UpdateRespawnText();
        UpdateCrosshairVisible();
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
                localEnergy = player.energy;
                localScore = player.score;
                localIsAlive = player.isAlive;
                localRespawnSeconds = player.respawnSeconds;
                UpdateStatsText();
                UpdateCrosshairVisible();
                return;
            }
        }
    }

    private void HandleHit(HitMessage hit)
    {
        if (hit != null && networkClient != null && hit.targetId == networkClient.LocalPlayerId)
        {
            localHealth = hit.health;
            UpdateHealthBar();
        }
    }

    private void CreateHudIfNeeded()
    {
        if (healthText == null || healthBarFill != null)
        {
            return;
        }

        ConfigureLabel(healthText, "Health");
        CreateBarForLabel(healthText, "Health Bar", new Color(0.95f, 0.2f, 0.2f, 1f), out healthBarFill, out healthValueText);

        energyLabelText = CreateLabelCopy(
            "Energy",
            healthText,
            healthText.rectTransform.anchoredPosition + new Vector2(0f, -40f)
        );
        CreateBarForLabel(energyLabelText, "Energy Bar", new Color(0.2f, 0.75f, 1f, 1f), out energyBarFill, out energyValueText);

        respawnText = CreateLabelCopy(
            "Respawn Text",
            healthText,
            energyLabelText.rectTransform.anchoredPosition + new Vector2(145f, -40f)
        );
        respawnText.alignment = TextAnchor.MiddleLeft;
        respawnText.text = string.Empty;
        respawnText.gameObject.SetActive(false);

        CreateCrosshairIfNeeded();
    }

    private void ConfigureLabel(Text label, string value)
    {
        if (label == null)
        {
            return;
        }

        label.text = value;
        label.alignment = TextAnchor.MiddleLeft;
        label.raycastTarget = false;
    }

    private Text CreateLabelCopy(string objectName, Text template, Vector2 anchoredPosition)
    {
        GameObject labelObject = new GameObject(objectName, typeof(RectTransform));
        labelObject.transform.SetParent(template.transform.parent, false);

        RectTransform rectTransform = labelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = template.rectTransform.anchorMin;
        rectTransform.anchorMax = template.rectTransform.anchorMax;
        rectTransform.pivot = template.rectTransform.pivot;
        rectTransform.sizeDelta = template.rectTransform.sizeDelta;
        rectTransform.anchoredPosition = anchoredPosition;

        Text label = labelObject.AddComponent<Text>();
        CopyTextStyle(template, label);
        ConfigureLabel(label, objectName);
        return label;
    }

    private void CreateBarForLabel(Text label, string objectName, Color fillColor, out Image fillImage, out Text valueText)
    {
        GameObject barObject = new GameObject(objectName, typeof(RectTransform));
        barObject.transform.SetParent(label.transform.parent, false);

        RectTransform barRect = barObject.GetComponent<RectTransform>();
        barRect.anchorMin = label.rectTransform.anchorMin;
        barRect.anchorMax = label.rectTransform.anchorMax;
        barRect.pivot = label.rectTransform.pivot;
        barRect.sizeDelta = new Vector2(220f, 22f);
        barRect.anchoredPosition = label.rectTransform.anchoredPosition + new Vector2(145f, 0f);

        Image background = barObject.AddComponent<Image>();
        background.sprite = defaultUiSprite;
        background.type = Image.Type.Simple;
        background.color = new Color(0.08f, 0.1f, 0.15f, 0.9f);

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform));
        fillObject.transform.SetParent(barObject.transform, false);

        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);

        fillImage = fillObject.AddComponent<Image>();
        fillImage.sprite = defaultUiSprite;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.color = fillColor;

        GameObject valueObject = new GameObject("Value", typeof(RectTransform));
        valueObject.transform.SetParent(barObject.transform, false);

        RectTransform valueRect = valueObject.GetComponent<RectTransform>();
        valueRect.anchorMin = Vector2.zero;
        valueRect.anchorMax = Vector2.one;
        valueRect.offsetMin = Vector2.zero;
        valueRect.offsetMax = Vector2.zero;

        valueText = valueObject.AddComponent<Text>();
        CopyTextStyle(label, valueText);
        valueText.alignment = TextAnchor.MiddleCenter;
        valueText.color = Color.white;
        valueText.text = "100 / 100";
        valueText.raycastTarget = false;
    }

    private void CopyTextStyle(Text source, Text target)
    {
        target.font = source.font;
        target.fontSize = source.fontSize;
        target.fontStyle = source.fontStyle;
        target.color = source.color;
        target.lineSpacing = source.lineSpacing;
        target.supportRichText = source.supportRichText;
        target.horizontalOverflow = source.horizontalOverflow;
        target.verticalOverflow = source.verticalOverflow;
        target.raycastTarget = false;
    }

    private Sprite CreateDefaultSprite()
    {
        Sprite sprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f)
        );

        sprite.name = "RuntimeWhiteSprite";
        return sprite;
    }

    private void CreateCrosshairIfNeeded()
    {
        if (healthText == null || crosshairRoot != null)
        {
            return;
        }

        crosshairRoot = new GameObject("Crosshair", typeof(RectTransform));
        crosshairRoot.transform.SetParent(healthText.transform.parent, false);

        crosshairRect = crosshairRoot.GetComponent<RectTransform>();
        crosshairRect.anchorMin = new Vector2(0.5f, 0.5f);
        crosshairRect.anchorMax = new Vector2(0.5f, 0.5f);
        crosshairRect.pivot = new Vector2(0.5f, 0.5f);
        crosshairRect.sizeDelta = new Vector2(40f, 40f);
        crosshairRect.anchoredPosition = Vector2.zero;

        hudRect = healthText.transform.parent as RectTransform;

        CreateCrosshairPart("Top", new Vector2(0f, 9f), new Vector2(3f, 10f));
        CreateCrosshairPart("Bottom", new Vector2(0f, -9f), new Vector2(3f, 10f));
        CreateCrosshairPart("Left", new Vector2(-9f, 0f), new Vector2(10f, 3f));
        CreateCrosshairPart("Right", new Vector2(9f, 0f), new Vector2(10f, 3f));
        CreateCrosshairPart("Center", Vector2.zero, new Vector2(4f, 4f));

        UpdateCrosshairVisible();
    }

    private void CreateCrosshairPart(string objectName, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject partObject = new GameObject(objectName, typeof(RectTransform));
        partObject.transform.SetParent(crosshairRoot.transform, false);

        RectTransform rectTransform = partObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;

        Image image = partObject.AddComponent<Image>();
        image.sprite = defaultUiSprite;
        image.type = Image.Type.Simple;
        image.color = new Color(1f, 0.95f, 0.55f, 0.95f);
        image.raycastTarget = false;
    }

    private void UpdateCrosshairVisible()
    {
        if (crosshairRoot == null)
        {
            return;
        }

        bool isConnectionPanelVisible = connectionPanel != null && connectionPanel.activeSelf;
        bool shouldShowCrosshair = !isConnectionPanelVisible && localIsAlive;
        crosshairRoot.SetActive(shouldShowCrosshair);
    }

    private void UpdateCrosshairPosition()
    {
        if (crosshairRect == null || !crosshairRoot.activeSelf)
        {
            return;
        }

        if (localPlayerController == null)
        {
            localPlayerController = FindObjectOfType<SpaceshipController>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (localPlayerController == null || targetCamera == null || hudRect == null)
        {
            return;
        }

        Vector3 aimPoint = localPlayerController.GetLaserAimPoint(CrosshairDistance);
        Vector3 screenPoint = targetCamera.WorldToScreenPoint(aimPoint);

        if (screenPoint.z <= 0f)
        {
            return;
        }

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(hudRect, screenPoint, null, out localPoint);
        crosshairRect.anchoredPosition = localPoint;
    }

    private void UpdateStatsText()
    {
        UpdateHealthBar();
        UpdateEnergyBar();
        UpdateScoreText();
        UpdateRespawnText();
    }

    public void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = Mathf.Clamp01(localHealth / MaxStatValue);
        }

        if (healthValueText != null)
        {
            healthValueText.text = Mathf.RoundToInt(localHealth) + " / 100";
        }
    }

    public void UpdateEnergyBar()
    {
        if (energyBarFill != null)
        {
            energyBarFill.fillAmount = Mathf.Clamp01(localEnergy / MaxStatValue);
        }

        if (energyValueText != null)
        {
            energyValueText.text = Mathf.RoundToInt(localEnergy) + " / 100";
        }
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + localScore;
        }
    }

    public void UpdateRespawnText()
    {
        if (respawnText == null)
        {
            return;
        }

        bool shouldShowRespawn = !localIsAlive && localRespawnSeconds > 0f;
        respawnText.gameObject.SetActive(shouldShowRespawn);

        if (shouldShowRespawn)
        {
            respawnText.text = "Respawn dans: " + localRespawnSeconds.ToString("0.0") + "s";
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

        UpdateCrosshairVisible();
    }
}
