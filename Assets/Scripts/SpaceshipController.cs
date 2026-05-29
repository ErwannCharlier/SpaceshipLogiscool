using UnityEngine;
using UnityEngine.EventSystems;

public class SpaceshipController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float turnSpeed = 120f;
    public float verticalSpeed = 6f;
    public float minAltitude = 1f;
    public float maxAltitude = 20f;

    [Header("Mouse Aim")]
    public bool useMouseAim = true;
    public bool lockCursorOnClick = true;
    public float mouseSensitivity = 3f;
    public float minPitch = -45f;
    public float maxPitch = 45f;
    public bool invertMouseY = false;

    [Header("Roll")]
    public float rollSpeed = 120f;
    public float maxRoll = 55f;
    public float rollReturnSpeed = 90f;

    [Header("Shooting")]
    public float fireCooldown = 0.4f;
    public Transform shootPoint;
    public GameObject laserPrefab;

    [Header("Effects")]
    public GameObject explosionPrefab;

    [Header("Ship Model")]
    public Vector3 shipModelLocalPosition = Vector3.zero;
    public Vector3 shipModelLocalEulerAngles = new Vector3(-90f, 0f, 0f);
    public Vector3 shipModelLocalScale = new Vector3(100f, 100f, 100f);

    [Header("Networking")]
    public NetworkClient networkClient;
    public float stateMessagesPerSecond = 10f;

    private float nextFireTime;
    private float stateTimer;
    private float yaw;
    private float pitch;
    private float roll;
    private bool lockedCursorThisFrame;
    private bool isAlive = true;
    private bool hasLocalWorldState;
    private Renderer[] shipRenderers;
    private Transform shipModelRoot;
    private string currentShipVisualId;

    private void Awake()
    {
        if (networkClient == null)
        {
            networkClient = FindFirstObjectByType<NetworkClient>();
        }

        RefreshShipRenderers();
    }

    private void OnEnable()
    {
        if (networkClient != null)
        {
            networkClient.WorldReceived += HandleWorld;
            networkClient.Disconnected += HandleDisconnected;
        }
    }

    private void Start()
    {
        if (ShipLibrary.HasShips())
        {
            SetSelectedShip(ShipLibrary.GetDefaultShipId());
        }

        yaw = transform.eulerAngles.y;
        pitch = 0f;
        roll = 0f;
        transform.rotation = Quaternion.Euler(pitch, yaw, roll);
        ClampAltitude();
        SetShipVisible(true);
    }

    private void OnDisable()
    {
        if (networkClient != null)
        {
            networkClient.WorldReceived -= HandleWorld;
            networkClient.Disconnected -= HandleDisconnected;
        }
    }

    private void Update()
    {
        lockedCursorThisFrame = false;
        UpdateCursorLock();

        if (!CanControlShip())
        {
            return;
        }

        RotateShip();
        MoveShip();
        TryShoot();
        SendStateSometimes();
    }

    public bool CanControlShip()
    {
        return isAlive;
    }

    public Vector3 GetShootOrigin()
    {
        if (shootPoint != null)
        {
            return shootPoint.position;
        }

        return transform.position + transform.forward;
    }

    public Vector3 GetShootDirection()
    {
        return transform.forward;
    }

    public Vector3 GetLaserAimPoint(float distance)
    {
        return GetShootOrigin() + GetShootDirection() * distance;
    }

    public void HandleLocalPlayerWorldState(NetworkPlayerInfo playerInfo)
    {
        if (playerInfo == null)
        {
            return;
        }

        bool wasAlive = isAlive;
        isAlive = playerInfo.isAlive;

        if (!string.IsNullOrWhiteSpace(playerInfo.shipId))
        {
            SetSelectedShip(playerInfo.shipId);
        }

        if (!hasLocalWorldState)
        {
            ApplyServerTransform(playerInfo);
            hasLocalWorldState = true;
            SetShipVisible(isAlive);
            return;
        }

        if (wasAlive && !isAlive)
        {
            ApplyServerTransform(playerInfo);
            PlayExplosion();
            SetShipVisible(false);
            UnlockCursor();
            return;
        }

        if (!wasAlive && isAlive)
        {
            ApplyServerTransform(playerInfo);
            SetShipVisible(true);
            return;
        }

        if (!isAlive)
        {
            ApplyServerTransform(playerInfo);
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
    }

    public void SetSelectedShip(string shipId)
    {
        // TODO cours3: charger le bon prefab de vaisseau, remplacer l'ancien modele et afficher le nouveau.
    }

    public void PlayExplosion()
    {
        ExplosionVisual.Spawn(transform.position, explosionPrefab);
    }

    private void HandleWorld(NetworkPlayerInfo[] players)
    {
        if (players == null || networkClient == null || string.IsNullOrEmpty(networkClient.LocalPlayerId))
        {
            return;
        }

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].id == networkClient.LocalPlayerId)
            {
                HandleLocalPlayerWorldState(players[i]);
                return;
            }
        }
    }

    private void HandleDisconnected()
    {
        isAlive = true;
        hasLocalWorldState = false;
        SetShipVisible(true);
        UnlockCursor();
    }

    private void ApplyServerTransform(NetworkPlayerInfo playerInfo)
    {
        yaw = playerInfo.yaw;
        pitch = playerInfo.pitch;
        roll = playerInfo.roll;
        transform.SetPositionAndRotation(
            new Vector3(playerInfo.x, playerInfo.y, playerInfo.z),
            Quaternion.Euler(pitch, yaw, roll)
        );
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void UpdateCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
        }

        if (!CanControlShip() || !lockCursorOnClick || IsPointerOverUI())
        {
            return;
        }

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            lockedCursorThisFrame = true;
        }
    }

    private void RotateShip()
    {
        float keyboardTurn = 0f;
        float rollInput = 0f;

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            keyboardTurn -= 1f;
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            keyboardTurn += 1f;
        }

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.Q))
        {
            rollInput += 1f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            rollInput -= 1f;
        }

        yaw += keyboardTurn * turnSpeed * Time.deltaTime;
        UpdateRoll(rollInput);

        if (useMouseAim && Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            yaw += mouseX;

            if (invertMouseY)
            {
                pitch += mouseY;
            }
            else
            {
                pitch -= mouseY;
            }

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        transform.rotation = Quaternion.Euler(pitch, yaw, roll);
    }

    private void UpdateRoll(float rollInput)
    {
        if (Mathf.Abs(rollInput) > 0.01f)
        {
            roll += rollInput * rollSpeed * Time.deltaTime;
        }
        else
        {
            roll = Mathf.MoveTowards(roll, 0f, rollReturnSpeed * Time.deltaTime);
        }

        roll = Mathf.Clamp(roll, -maxRoll, maxRoll);
    }

    private void MoveShip()
    {
        // W/Z both work, so the course is comfortable on QWERTY and AZERTY keyboards.
        float forwardInput = 0f;
        float verticalInput = 0f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.UpArrow))
        {
            forwardInput += 1f;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            forwardInput -= 1f;
        }

        if (Input.GetKey(KeyCode.Space))
        {
            verticalInput += 1f;
        }

        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftShift))
        {
            verticalInput -= 1f;
        }

        Vector3 movement = transform.forward * forwardInput * moveSpeed;
        movement += Vector3.up * verticalInput * verticalSpeed;

        transform.position += movement * Time.deltaTime;
        ClampAltitude();
    }

    private void ClampAltitude()
    {
        Vector3 position = transform.position;
        position.y = Mathf.Clamp(position.y, minAltitude, maxAltitude);
        transform.position = position;
    }

    private void TryShoot()
    {
        if (IsPointerOverUI())
        {
            return;
        }

        bool wantsToShoot = !lockedCursorThisFrame && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.F));

        if (!wantsToShoot || Time.time < nextFireTime)
        {
            return;
        }

        if (lockCursorOnClick && Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        nextFireTime = Time.time + fireCooldown;

        Vector3 position = GetShootOrigin();
        Vector3 direction = GetShootDirection();

        LaserVisual.Spawn(laserPrefab, position, direction);

        if (networkClient != null)
        {
            networkClient.SendShoot(position, direction);
        }
    }

    private void SendStateSometimes()
    {
        if (networkClient == null || !networkClient.IsConnected || stateMessagesPerSecond <= 0f)
        {
            return;
        }

        stateTimer += Time.deltaTime;
        float sendInterval = 1f / stateMessagesPerSecond;

        if (stateTimer >= sendInterval)
        {
            stateTimer = 0f;
            networkClient.SendPlayerState(transform.position, yaw, pitch, roll);
        }
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void RefreshShipRenderers()
    {
        shipRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void RemoveExistingShipModel()
    {
        Transform existingShipModel = FindShipModel();

        if (existingShipModel == null)
        {
            currentShipVisualId = string.Empty;
            return;
        }

        existingShipModel.gameObject.SetActive(false);

        if (Application.isPlaying)
        {
            Destroy(existingShipModel.gameObject);
        }
        else
        {
            DestroyImmediate(existingShipModel.gameObject);
        }

        shipModelRoot = null;
        currentShipVisualId = string.Empty;
        RefreshShipRenderers();
    }

    private Transform FindShipModel()
    {
        if (shipModelRoot != null)
        {
            return shipModelRoot;
        }

        Transform existingShipModel = transform.Find("ShipModel");

        if (existingShipModel != null)
        {
            shipModelRoot = existingShipModel;
        }

        return shipModelRoot;
    }
}
