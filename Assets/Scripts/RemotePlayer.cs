using UnityEngine;
using UnityEngine.Rendering;

public class RemotePlayer : MonoBehaviour
{
    public string playerId;
    public string playerName;
    public string shipId;
    public int health = 100;
    public float energy = 100f;
    public int score = 0;
    public float smoothingSpeed = 10f;

    [Header("Name Label")]
    public bool showNameLabel = true;
    public float nameLabelHeight = 2.2f;
    public float nameLabelCharacterSize = 0.08f;
    public Color nameLabelColor = new Color(0.95f, 0.98f, 1f, 1f);

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool hasTarget;
    private bool isAlive = true;
    private bool hasAliveState;
    private Renderer[] shipRenderers;
    private Transform nameLabelRoot;
    private TextMesh nameLabelText;
    private MeshRenderer nameLabelRenderer;
    private Camera mainCamera;
    private Transform shipVisualRoot;
    private string currentShipVisualId;

    private void Awake()
    {
        RefreshShipRenderers();
        CreateNameLabelIfNeeded();
        mainCamera = Camera.main;
    }

    public void ApplyWorldState(NetworkPlayerInfo playerInfo, Vector3 position, Quaternion rotation)
    {
        bool wasAlive = isAlive;

        playerName = playerInfo.name;
        shipId = playerInfo.shipId;
        health = playerInfo.health;
        energy = playerInfo.energy;
        score = playerInfo.score;
        isAlive = playerInfo.isAlive;
        UpdateNameLabelText();

        if (!hasAliveState)
        {
            transform.SetPositionAndRotation(position, rotation);
            targetPosition = position;
            targetRotation = rotation;
            hasTarget = true;
            hasAliveState = true;
            SetShipVisible(isAlive);
            return;
        }

        if (wasAlive && !isAlive)
        {
            transform.SetPositionAndRotation(position, rotation);
            targetPosition = position;
            targetRotation = rotation;
            hasTarget = true;
            PlayExplosion();
            SetShipVisible(false);
            return;
        }

        if (!wasAlive && isAlive)
        {
            transform.SetPositionAndRotation(position, rotation);
            targetPosition = position;
            targetRotation = rotation;
            hasTarget = true;
            SetShipVisible(true);
            return;
        }

        if (!isAlive)
        {
            transform.SetPositionAndRotation(position, rotation);
            targetPosition = position;
            targetRotation = rotation;
            hasTarget = true;
            return;
        }

        SetTarget(position, rotation);
    }

    public void SetTarget(Vector3 position, Quaternion rotation)
    {
        targetPosition = position;
        targetRotation = rotation;

        if (!hasTarget)
        {
            transform.SetPositionAndRotation(targetPosition, targetRotation);
            hasTarget = true;
        }
    }

    public void SetShipVisible(bool isVisible)
    {
        if (shipRenderers == null)
        {
            return;
        }

        for (int i = 0; i < shipRenderers.Length; i++)
        {
            if (shipRenderers[i] != null)
            {
                shipRenderers[i].enabled = isVisible;
            }
        }

        if (nameLabelRoot != null)
        {
            nameLabelRoot.gameObject.SetActive(isVisible && showNameLabel);
        }
    }

    public void PlayExplosion()
    {
        ExplosionVisual.Spawn(transform.position);
    }

    public void SetShipVisual(string newShipId, GameObject shipPrefab, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
    {
        string normalizedShipId = ShipLibrary.NormalizeShipId(newShipId);

        if (shipVisualRoot != null && currentShipVisualId == normalizedShipId)
        {
            return;
        }

        RemoveShipVisual();

        if (shipPrefab == null)
        {
            EnsureFallbackVisual();
            return;
        }

        GameObject model = Instantiate(shipPrefab, transform);
        model.name = "ShipModel";
        model.transform.localPosition = localPosition;
        model.transform.localRotation = Quaternion.Euler(localEulerAngles);
        model.transform.localScale = localScale;

        shipVisualRoot = model.transform;
        currentShipVisualId = normalizedShipId;
        RefreshShipRenderers();
        SetShipVisible(isAlive);
    }

    public void EnsureFallbackVisual()
    {
        if (shipVisualRoot != null)
        {
            return;
        }

        GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        fallback.name = "ShipModel";
        fallback.transform.SetParent(transform, false);
        fallback.transform.localPosition = Vector3.zero;
        fallback.transform.localRotation = Quaternion.identity;
        fallback.transform.localScale = new Vector3(1f, 0.5f, 2f);

        Renderer renderer = fallback.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.material.color = Color.cyan;
        }

        shipVisualRoot = fallback.transform;
        currentShipVisualId = "fallback";
        RefreshShipRenderers();
        SetShipVisible(isAlive);
    }

    private void Update()
    {
        if (!hasTarget || !isAlive)
        {
            return;
        }

        // Smooth remote movement so other ships do not snap between server updates.
        float amount = Mathf.Clamp01(smoothingSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, amount);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, amount);
    }

    private void LateUpdate()
    {
        UpdateNameLabelTransform();
    }

    private void CreateNameLabelIfNeeded()
    {
        if (!showNameLabel || nameLabelRoot != null)
        {
            return;
        }

        GameObject labelObject = new GameObject("Name Label");
        labelObject.transform.SetParent(transform, false);

        nameLabelRoot = labelObject.transform;
        nameLabelText = labelObject.AddComponent<TextMesh>();
        nameLabelRenderer = labelObject.GetComponent<MeshRenderer>();

        Font builtInFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (builtInFont == null)
        {
            builtInFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        if (builtInFont != null)
        {
            nameLabelText.font = builtInFont;
            nameLabelRenderer.material = builtInFont.material;
        }

        nameLabelText.text = "Player";
        nameLabelText.anchor = TextAnchor.MiddleCenter;
        nameLabelText.alignment = TextAlignment.Center;
        nameLabelText.fontSize = 48;
        nameLabelText.characterSize = nameLabelCharacterSize;
        nameLabelText.color = nameLabelColor;

        if (nameLabelRenderer != null)
        {
            nameLabelRenderer.shadowCastingMode = ShadowCastingMode.Off;
            nameLabelRenderer.receiveShadows = false;
        }

        UpdateNameLabelText();
        UpdateNameLabelTransform();
    }

    private void UpdateNameLabelText()
    {
        if (nameLabelText == null)
        {
            return;
        }

        nameLabelText.text = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName;
    }

    private void UpdateNameLabelTransform()
    {
        if (nameLabelRoot == null || !nameLabelRoot.gameObject.activeSelf)
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        nameLabelRoot.position = transform.position + Vector3.up * nameLabelHeight;

        if (mainCamera == null)
        {
            return;
        }

        Vector3 lookDirection = nameLabelRoot.position - mainCamera.transform.position;

        if (lookDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        nameLabelRoot.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
    }

    private void RefreshShipRenderers()
    {
        shipRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void RemoveShipVisual()
    {
        Transform existingVisual = FindShipVisual();

        if (existingVisual == null)
        {
            currentShipVisualId = string.Empty;
            return;
        }

        existingVisual.gameObject.SetActive(false);

        if (Application.isPlaying)
        {
            Destroy(existingVisual.gameObject);
        }
        else
        {
            DestroyImmediate(existingVisual.gameObject);
        }

        shipVisualRoot = null;
        currentShipVisualId = string.Empty;
        RefreshShipRenderers();
    }

    private Transform FindShipVisual()
    {
        if (shipVisualRoot != null)
        {
            return shipVisualRoot;
        }

        Transform existingVisual = transform.Find("ShipModel");

        if (existingVisual != null)
        {
            shipVisualRoot = existingVisual;
        }

        return shipVisualRoot;
    }
}
