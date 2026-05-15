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
        // TODO COURS 1 - Exercice 6:
        // Sauvegarder la position et la rotation recues du serveur.
        // La premiere fois, placer directement le vaisseau sur cette cible.
    }

    private void Update()
    {
        if (!hasTarget)
        {
            return;
        }

        // TODO COURS 1 - Exercice 6:
        // Calculer amount avec smoothingSpeed et Time.deltaTime.
        // Interpoler position avec Vector3.Lerp.
        // Interpoler rotation avec Quaternion.Slerp.
    }
}
