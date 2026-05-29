using System.Collections.Generic;
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
    private const float RadarWorldRange = 80f;
    private const float RadarRadius = 58f;
    private const float RadarSweepSpeed = 90f;

    private string pendingPlayerName = "Player";
    private string pendingShipId = string.Empty;
    private float localHealth = MaxStatValue;
    private float localEnergy = MaxStatValue;
    private int localScore = 0;
    private bool localIsAlive = true;
    private float localRespawnSeconds = 0f;
    private NetworkPlayerInfo[] latestWorldPlayers;

    private Image healthBarFill;
    private Image energyBarFill;
    private Text healthValueText;
    private Text energyLabelText;
    private Text energyValueText;
    private Text respawnText;
    private Text shipSelectorValueText;
    private Button shipPreviousButton;
    private Button shipNextButton;
    private GameObject crosshairRoot;
    private GameObject radarRoot;
    private RectTransform crosshairRect;
    private RectTransform hudRect;
    private RectTransform radarContentRect;
    private RectTransform radarSweepRect;
    private Camera targetCamera;
    private SpaceshipController localPlayerController;
    private Image radarStationBlip;
    private static Sprite defaultUiSprite;
    private readonly Dictionary<string, Image> radarPlayerBlips = new Dictionary<string, Image>();
    private readonly List<string> radarPlayerIdsToRemove = new List<string>();

    private void Awake()
    {
        if (networkClient == null)
        {
            networkClient = FindFirstObjectByType<NetworkClient>();
        }

        localPlayerController = FindFirstObjectByType<SpaceshipController>();
        targetCamera = Camera.main;

        if (defaultUiSprite == null)
        {
            defaultUiSprite = CreateDefaultSprite();
        }

        CreateHudIfNeeded();
        CreateShipSelectorIfNeeded();
        InitializeShipSelection();
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
        CreateShipSelectorIfNeeded();
        InitializeShipSelection();
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
        UpdateRadar();
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
            networkClient.SendJoin(pendingPlayerName, pendingShipId);
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
        networkClient.SendJoin(pendingPlayerName, pendingShipId);
        SetConnectionPanelVisible(false);
        UpdateCrosshairVisible();
        UpdateRadarVisible();
        UpdateButtons();
    }

    private void HandleDisconnected()
    {
        localRespawnSeconds = 0f;
        localIsAlive = true;
        latestWorldPlayers = null;
        SetConnectionPanelVisible(true);
        UpdateRespawnText();
        UpdateCrosshairVisible();
        ClearRadarPlayerBlips();
        UpdateRadarVisible();
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

        latestWorldPlayers = players;

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

                if (!string.IsNullOrWhiteSpace(player.shipId))
                {
                    pendingShipId = ShipLibrary.NormalizeShipId(player.shipId);
                    UpdateShipSelectorText();
                }

                UpdateStatsText();
                UpdateCrosshairVisible();
                UpdateRadarVisible();
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
        CreateRadarIfNeeded();
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
            localPlayerController = FindFirstObjectByType<SpaceshipController>();
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

    private void InitializeShipSelection()
    {
        if (!ShipLibrary.HasShips())
        {
            UpdateShipSelectorText();
            return;
        }

        if (string.IsNullOrEmpty(pendingShipId))
        {
            pendingShipId = ShipLibrary.GetDefaultShipId();
        }

        ApplyPendingShipSelection();
        UpdateShipSelectorText();
    }

    private void ApplyPendingShipSelection()
    {
        if (localPlayerController == null)
        {
            localPlayerController = FindFirstObjectByType<SpaceshipController>();
        }

        if (localPlayerController != null && !string.IsNullOrWhiteSpace(pendingShipId))
        {
            localPlayerController.SetSelectedShip(pendingShipId);
        }
    }

    private void CreateShipSelectorIfNeeded()
    {
        if (connectionPanel == null || shipSelectorValueText != null)
        {
            return;
        }

        Text template = statusText != null ? statusText : healthText;

        if (template == null)
        {
            return;
        }

        GameObject rowObject = new GameObject("Ship Selector", typeof(RectTransform));
        rowObject.transform.SetParent(connectionPanel.transform, false);

        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0.5f);
        rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.sizeDelta = new Vector2(310f, 40f);
        rowRect.anchoredPosition = GetShipSelectorPosition();

        Text label = CreateTextElement("Ship Label", rowRect, template, new Vector2(-112f, 0f), new Vector2(105f, 30f), "Vaisseau");
        label.alignment = TextAnchor.MiddleLeft;

        shipPreviousButton = CreateMenuButton("Ship Previous", rowRect, template, new Vector2(-28f, 0f), "<");
        shipNextButton = CreateMenuButton("Ship Next", rowRect, template, new Vector2(117f, 0f), ">");
        shipSelectorValueText = CreateTextElement("Ship Value", rowRect, template, new Vector2(45f, 0f), new Vector2(125f, 30f), "Vaisseau 1");
        shipSelectorValueText.alignment = TextAnchor.MiddleCenter;

        if (shipPreviousButton != null)
        {
            shipPreviousButton.onClick.AddListener(SelectPreviousShip);
        }

        if (shipNextButton != null)
        {
            shipNextButton.onClick.AddListener(SelectNextShip);
        }
    }

    private Vector2 GetShipSelectorPosition()
    {
        RectTransform playerNameRect = playerNameInput != null ? playerNameInput.transform as RectTransform : null;

        if (playerNameRect != null && connectButton != null)
        {
            RectTransform connectRect = connectButton.transform as RectTransform;

            if (connectRect != null)
            {
                return (playerNameRect.anchoredPosition + connectRect.anchoredPosition) * 0.5f + new Vector2(0f, 8f);
            }
        }

        if (playerNameRect != null)
        {
            return playerNameRect.anchoredPosition + new Vector2(0f, -50f);
        }

        return new Vector2(0f, -20f);
    }

    private void SelectPreviousShip()
    {
        CycleShipSelection(-1);
    }

    private void SelectNextShip()
    {
        CycleShipSelection(1);
    }

    private void CycleShipSelection(int direction)
    {
        int shipCount = ShipLibrary.GetShipCount();

        if (shipCount == 0)
        {
            return;
        }

        int shipIndex = ShipLibrary.GetShipIndex(pendingShipId);

        if (shipIndex < 0)
        {
            shipIndex = 0;
        }

        shipIndex = (shipIndex + direction + shipCount) % shipCount;
        pendingShipId = ShipLibrary.GetShipIdAt(shipIndex);
        ApplyPendingShipSelection();
        UpdateShipSelectorText();
    }

    private void UpdateShipSelectorText()
    {
        if (shipSelectorValueText == null)
        {
            return;
        }

        int shipCount = ShipLibrary.GetShipCount();

        if (shipCount == 0)
        {
            shipSelectorValueText.text = "Aucun modele";
        }
        else
        {
            shipSelectorValueText.text = ShipLibrary.GetDisplayName(pendingShipId);
        }

        if (shipPreviousButton != null)
        {
            shipPreviousButton.interactable = shipCount > 1;
        }

        if (shipNextButton != null)
        {
            shipNextButton.interactable = shipCount > 1;
        }
    }

    private void CreateRadarIfNeeded()
    {
        if (healthText == null || radarRoot != null)
        {
            return;
        }

        radarRoot = new GameObject("Radar", typeof(RectTransform));
        radarRoot.transform.SetParent(healthText.transform.parent, false);

        RectTransform radarRect = radarRoot.GetComponent<RectTransform>();
        radarRect.anchorMin = new Vector2(1f, 1f);
        radarRect.anchorMax = new Vector2(1f, 1f);
        radarRect.pivot = new Vector2(1f, 1f);
        radarRect.sizeDelta = new Vector2(170f, 190f);
        radarRect.anchoredPosition = new Vector2(-18f, -18f);

        Image radarBackground = radarRoot.AddComponent<Image>();
        radarBackground.sprite = defaultUiSprite;
        radarBackground.type = Image.Type.Simple;
        radarBackground.color = new Color(0.04f, 0.08f, 0.11f, 0.82f);

        Text radarLabel = CreateTextElement("Radar Label", radarRect, healthText, new Vector2(0f, -16f), new Vector2(150f, 24f), "RADAR");
        radarLabel.alignment = TextAnchor.MiddleCenter;
        radarLabel.color = new Color(0.72f, 0.95f, 1f, 1f);

        GameObject radarContentObject = new GameObject("Radar Content", typeof(RectTransform));
        radarContentObject.transform.SetParent(radarRoot.transform, false);
        radarContentRect = radarContentObject.GetComponent<RectTransform>();
        radarContentRect.anchorMin = new Vector2(0.5f, 0.5f);
        radarContentRect.anchorMax = new Vector2(0.5f, 0.5f);
        radarContentRect.pivot = new Vector2(0.5f, 0.5f);
        radarContentRect.sizeDelta = new Vector2(132f, 132f);
        radarContentRect.anchoredPosition = new Vector2(0f, -14f);

        Image radarContentBackground = radarContentObject.AddComponent<Image>();
        radarContentBackground.sprite = defaultUiSprite;
        radarContentBackground.type = Image.Type.Simple;
        radarContentBackground.color = new Color(0.02f, 0.16f, 0.15f, 0.86f);

        CreateRadarLine("Radar Horizontal", new Vector2(0f, 0f), new Vector2(118f, 2f), new Color(0.18f, 0.55f, 0.52f, 0.45f));
        CreateRadarLine("Radar Vertical", new Vector2(0f, 0f), new Vector2(2f, 118f), new Color(0.18f, 0.55f, 0.52f, 0.45f));
        CreateRadarLine("Radar Top", new Vector2(0f, 58f), new Vector2(118f, 2f), new Color(0.12f, 0.32f, 0.32f, 0.3f));
        CreateRadarLine("Radar Bottom", new Vector2(0f, -58f), new Vector2(118f, 2f), new Color(0.12f, 0.32f, 0.32f, 0.3f));
        CreateRadarLine("Radar Left", new Vector2(-58f, 0f), new Vector2(2f, 118f), new Color(0.12f, 0.32f, 0.32f, 0.3f));
        CreateRadarLine("Radar Right", new Vector2(58f, 0f), new Vector2(2f, 118f), new Color(0.12f, 0.32f, 0.32f, 0.3f));

        CreateRadarBlip("Local Blip", Color.green, new Vector2(8f, 8f), Vector2.zero, radarContentRect);
        radarStationBlip = CreateRadarBlip("Station Blip", new Color(0.22f, 0.78f, 1f, 1f), new Vector2(10f, 10f), Vector2.zero, radarContentRect);

        GameObject sweepObject = new GameObject("Radar Sweep", typeof(RectTransform));
        sweepObject.transform.SetParent(radarContentRect, false);
        radarSweepRect = sweepObject.GetComponent<RectTransform>();
        radarSweepRect.anchorMin = new Vector2(0.5f, 0.5f);
        radarSweepRect.anchorMax = new Vector2(0.5f, 0.5f);
        radarSweepRect.pivot = new Vector2(0.5f, 0f);
        radarSweepRect.sizeDelta = new Vector2(2f, RadarRadius);
        radarSweepRect.anchoredPosition = Vector2.zero;

        Image sweepImage = sweepObject.AddComponent<Image>();
        sweepImage.sprite = defaultUiSprite;
        sweepImage.type = Image.Type.Simple;
        sweepImage.color = new Color(0.48f, 1f, 0.76f, 0.45f);

        UpdateRadarVisible();
    }

    private void CreateRadarLine(string objectName, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject lineObject = new GameObject(objectName, typeof(RectTransform));
        lineObject.transform.SetParent(radarContentRect, false);

        RectTransform rectTransform = lineObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;

        Image image = lineObject.AddComponent<Image>();
        image.sprite = defaultUiSprite;
        image.type = Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
    }

    private Image CreateRadarBlip(string objectName, Color color, Vector2 size, Vector2 anchoredPosition, Transform parent)
    {
        GameObject blipObject = new GameObject(objectName, typeof(RectTransform));
        blipObject.transform.SetParent(parent, false);

        RectTransform rectTransform = blipObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;

        Image image = blipObject.AddComponent<Image>();
        image.sprite = defaultUiSprite;
        image.type = Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void UpdateRadarVisible()
    {
        if (radarRoot == null)
        {
            return;
        }

        bool isConnectionPanelVisible = connectionPanel != null && connectionPanel.activeSelf;
        bool shouldShowRadar = !isConnectionPanelVisible && networkClient != null && networkClient.IsConnected;
        radarRoot.SetActive(shouldShowRadar);
    }

    private void UpdateRadar()
    {
        UpdateRadarVisible();

        if (radarRoot == null || !radarRoot.activeSelf)
        {
            return;
        }

        if (radarSweepRect != null)
        {
            radarSweepRect.localRotation = Quaternion.Euler(0f, 0f, -Time.time * RadarSweepSpeed);
        }

        UpdateRadarStationBlip();
        UpdateRadarPlayerBlips();
    }

    private void UpdateRadarStationBlip()
    {
        if (radarStationBlip == null)
        {
            return;
        }

        if (networkClient == null || networkClient.CurrentStation == null)
        {
            radarStationBlip.gameObject.SetActive(false);
            return;
        }

        radarStationBlip.gameObject.SetActive(true);
        Vector2 radarPoint = WorldToRadarPosition(
            new Vector3(networkClient.CurrentStation.x, networkClient.CurrentStation.y, networkClient.CurrentStation.z)
        );
        radarStationBlip.rectTransform.anchoredPosition = radarPoint;
    }

    private void UpdateRadarPlayerBlips()
    {
        if (latestWorldPlayers == null || networkClient == null)
        {
            ClearRadarPlayerBlips();
            return;
        }

        radarPlayerIdsToRemove.Clear();

        foreach (string playerId in radarPlayerBlips.Keys)
        {
            radarPlayerIdsToRemove.Add(playerId);
        }

        for (int i = 0; i < latestWorldPlayers.Length; i++)
        {
            NetworkPlayerInfo player = latestWorldPlayers[i];

            if (player == null || string.IsNullOrEmpty(player.id) || player.id == networkClient.LocalPlayerId)
            {
                continue;
            }

            Image blip = GetOrCreateRadarPlayerBlip(player.id);
            Vector2 radarPoint = WorldToRadarPosition(new Vector3(player.x, player.y, player.z));
            blip.rectTransform.anchoredPosition = radarPoint;
            blip.color = player.isAlive
                ? new Color(1f, 0.46f, 0.24f, 1f)
                : new Color(0.62f, 0.62f, 0.62f, 0.75f);

            radarPlayerIdsToRemove.Remove(player.id);
        }

        for (int i = 0; i < radarPlayerIdsToRemove.Count; i++)
        {
            string playerId = radarPlayerIdsToRemove[i];

            if (radarPlayerBlips.TryGetValue(playerId, out Image blip) && blip != null)
            {
                Destroy(blip.gameObject);
            }

            radarPlayerBlips.Remove(playerId);
        }
    }

    private Image GetOrCreateRadarPlayerBlip(string playerId)
    {
        if (radarPlayerBlips.TryGetValue(playerId, out Image blip) && blip != null)
        {
            return blip;
        }

        blip = CreateRadarBlip("Radar Player " + playerId, new Color(1f, 0.46f, 0.24f, 1f), new Vector2(8f, 8f), Vector2.zero, radarContentRect);
        radarPlayerBlips[playerId] = blip;
        return blip;
    }

    private void ClearRadarPlayerBlips()
    {
        foreach (Image blip in radarPlayerBlips.Values)
        {
            if (blip != null)
            {
                Destroy(blip.gameObject);
            }
        }

        radarPlayerBlips.Clear();
    }

    private Vector2 WorldToRadarPosition(Vector3 worldPosition)
    {
        if (localPlayerController == null)
        {
            localPlayerController = FindFirstObjectByType<SpaceshipController>();
        }

        if (localPlayerController == null)
        {
            return Vector2.zero;
        }

        Transform playerTransform = localPlayerController.transform;
        Vector3 playerPosition = playerTransform.position;
        Vector3 flatForward = playerTransform.forward;
        flatForward.y = 0f;

        if (flatForward.sqrMagnitude < 0.001f)
        {
            flatForward = Vector3.forward;
        }

        flatForward.Normalize();

        Vector3 flatRight = playerTransform.right;
        flatRight.y = 0f;

        if (flatRight.sqrMagnitude < 0.001f)
        {
            flatRight = Vector3.right;
        }

        flatRight.Normalize();

        Vector3 delta = worldPosition - playerPosition;
        delta.y = 0f;

        float radarX = Vector3.Dot(delta, flatRight);
        float radarY = Vector3.Dot(delta, flatForward);
        Vector2 radarPoint = new Vector2(radarX, radarY) / RadarWorldRange * RadarRadius;

        if (radarPoint.magnitude > RadarRadius)
        {
            radarPoint = radarPoint.normalized * RadarRadius;
        }

        return radarPoint;
    }

    private Button CreateMenuButton(string objectName, RectTransform parent, Text template, Vector2 anchoredPosition, string label)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(32f, 32f);
        buttonRect.anchoredPosition = anchoredPosition;

        Image background = buttonObject.AddComponent<Image>();
        background.sprite = defaultUiSprite;
        background.type = Image.Type.Simple;
        background.color = new Color(0.12f, 0.18f, 0.24f, 0.95f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = background;

        Text labelText = CreateTextElement("Label", buttonRect, template, Vector2.zero, buttonRect.sizeDelta, label);
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = Color.white;

        return button;
    }

    private Text CreateTextElement(
        string objectName,
        RectTransform parent,
        Text template,
        Vector2 anchoredPosition,
        Vector2 size,
        string value
    )
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;

        Text text = textObject.AddComponent<Text>();
        CopyTextStyle(template, text);
        text.text = value;
        text.raycastTarget = false;
        return text;
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
        UpdateRadarVisible();
    }
}
