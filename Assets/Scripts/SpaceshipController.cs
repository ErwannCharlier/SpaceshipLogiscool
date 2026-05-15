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

    [Header("Networking")]
    public NetworkClient networkClient;
    public float stateMessagesPerSecond = 10f;

    private float nextFireTime;
    private float stateTimer;
    private float yaw;
    private float pitch;
    private float roll;
    private bool lockedCursorThisFrame;

    private void Start()
    {
        if (networkClient == null)
        {
            networkClient = FindObjectOfType<NetworkClient>();
        }

        yaw = transform.eulerAngles.y;
        pitch = 0f;
        roll = 0f;
        transform.rotation = Quaternion.Euler(pitch, yaw, roll);
        ClampAltitude();
    }

    private void Update()
    {
        lockedCursorThisFrame = false;
        UpdateCursorLock();
        RotateShip();
        MoveShip();
        TryShoot();
        SendStateSometimes();
    }

    private void UpdateCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (!lockCursorOnClick || IsPointerOverUI())
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

        Vector3 position = shootPoint != null ? shootPoint.position : transform.position + transform.forward;
        Vector3 direction = transform.forward;

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
}
