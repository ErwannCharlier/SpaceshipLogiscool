using UnityEngine;

public class RemotePlayer : MonoBehaviour
{
    public string playerId;
    public string playerName;
    public int health = 100;
    public float energy = 100f;
    public int score = 0;
    public float smoothingSpeed = 10f;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool hasTarget;
    private bool isAlive = true;
    private bool hasAliveState;
    private Renderer[] shipRenderers;

    private void Awake()
    {
        shipRenderers = GetComponentsInChildren<Renderer>(true);
    }

    public void ApplyWorldState(NetworkPlayerInfo playerInfo, Vector3 position, Quaternion rotation)
    {
        bool wasAlive = isAlive;

        playerName = playerInfo.name;
        health = playerInfo.health;
        energy = playerInfo.energy;
        score = playerInfo.score;
        isAlive = playerInfo.isAlive;

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
    }

    public void PlayExplosion()
    {
        ExplosionVisual.Spawn(transform.position);
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
}
