using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public float lookHeight = 1.5f;

    [Header("Camera")]
    public float distance = 12f;
    public float height = 4f;
    public float followSpeed = 10f;
    public float rotationSpeed = 12f;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 lookPoint = target.position + target.up * lookHeight;
        Vector3 wantedPosition = target.position - target.forward * distance + target.up * height;

        transform.position = Vector3.Lerp(transform.position, wantedPosition, followSpeed * Time.deltaTime);

        Quaternion wantedRotation = Quaternion.LookRotation(lookPoint - transform.position, target.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, wantedRotation, rotationSpeed * Time.deltaTime);
    }
}
