using UnityEngine;

public class RemotePlayer : MonoBehaviour
{
    public string playerId;
    public string playerName;
    public int health = 100;
    public int score = 0;
    public float smoothingSpeed = 10f;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool hasTarget;

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

    private void Update()
    {
        if (!hasTarget)
        {
            return;
        }

        // Smooth remote movement so other ships do not snap between server updates.
        float amount = Mathf.Clamp01(smoothingSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, amount);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, amount);
    }
}
