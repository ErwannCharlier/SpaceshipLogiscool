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
        // TODO COURS 1 - Exercice 3:
        // 1. Utiliser la souris pour changer yaw et pitch quand le curseur est lock.
        // 2. Utiliser A/Q et D pour calculer le roll.
        // 3. Appeler UpdateRoll.
        // 4. Appliquer la rotation finale avec Quaternion.Euler(pitch, yaw, roll).
    }

    private void UpdateRoll(float rollInput)
    {
        // TODO COURS 1 - Exercice 3:
        // Si rollInput est different de 0, modifier roll avec rollSpeed.
        // Sinon, ramener roll doucement vers 0 avec Mathf.MoveTowards.
        // Toujours limiter roll entre -maxRoll et +maxRoll.
    }

    private void MoveShip()
    {
        // TODO COURS 1 - Exercice 3:
        // 1. Lire W/Z/UpArrow pour avancer.
        // 2. Lire S/DownArrow pour reculer.
        // 3. Lire Space pour monter et LeftControl/LeftShift pour descendre.
        // 4. Construire un Vector3 movement avec transform.forward et Vector3.up.
        // 5. Ajouter movement * Time.deltaTime a transform.position.
        // 6. Appeler ClampAltitude.
    }

    private void ClampAltitude()
    {
        Vector3 position = transform.position;
        position.y = Mathf.Clamp(position.y, minAltitude, maxAltitude);
        transform.position = position;
    }

    private void TryShoot()
    {
        // TODO COURS 1 - Exercice 4:
        // 1. Ne rien faire si la souris est sur l'UI.
        // 2. Detecter clic gauche ou touche F.
        // 3. Respecter fireCooldown avec Time.time et nextFireTime.
        // 4. Choisir la position de depart: shootPoint si possible.
        // 5. Afficher le laser avec LaserVisual.Spawn.
        // 6. Envoyer le tir au serveur avec networkClient.SendShoot.
    }

    private void SendStateSometimes()
    {
        // TODO COURS 1 - Exercice 3:
        // 1. Verifier que networkClient existe et qu'il est connecte.
        // 2. Utiliser stateTimer pour envoyer seulement quelques messages par seconde.
        // 3. Appeler SendPlayerState avec position, yaw, pitch et roll.
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
