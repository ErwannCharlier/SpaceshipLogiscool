using System.Collections.Generic;
using UnityEngine;

public class RemotePlayerManager : MonoBehaviour
{
    [Header("References")]
    public NetworkClient networkClient;
    public GameObject remoteShipPrefab;
    public GameObject remoteLaserPrefab;
    public Transform remotePlayersParent;

    [Header("Remote Ship Model Fix")]
    public Vector3 remoteModelLocalPosition = Vector3.zero;
    public Vector3 remoteModelLocalEulerAngles = new Vector3(-90f, 0f, 0f);
    public Vector3 remoteModelLocalScale = new Vector3(100f, 100f, 100f);

    private readonly Dictionary<string, RemotePlayer> remotePlayers = new Dictionary<string, RemotePlayer>();
    private readonly List<string> playersToRemove = new List<string>();

    private void Awake()
    {
        if (networkClient == null)
        {
            networkClient = FindObjectOfType<NetworkClient>();
        }

        if (remotePlayersParent == null)
        {
            remotePlayersParent = transform;
        }
    }

    private void OnEnable()
    {
        if (networkClient == null)
        {
            return;
        }

        networkClient.WorldReceived += HandleWorld;
        networkClient.ShootEventReceived += HandleShootEvent;
        networkClient.PlayerDisconnected += RemovePlayer;
        networkClient.Disconnected += RemoveAllPlayers;
    }

    private void OnDisable()
    {
        if (networkClient == null)
        {
            return;
        }

        networkClient.WorldReceived -= HandleWorld;
        networkClient.ShootEventReceived -= HandleShootEvent;
        networkClient.PlayerDisconnected -= RemovePlayer;
        networkClient.Disconnected -= RemoveAllPlayers;
    }

    private void HandleWorld(NetworkPlayerInfo[] players)
    {
        // TODO COURS 1 - Exercice 5:
        // Le serveur envoie la liste complete des joueurs.
        // 1. Ignorer le joueur local avec networkClient.LocalPlayerId.
        // 2. Pour chaque autre joueur, appeler GetOrCreateRemotePlayer.
        // 3. Construire position et rotation avec x/y/z et pitch/yaw/roll.
        // 4. Appeler SetTarget sur le RemotePlayer.
        // 5. Mettre a jour name, health et score.
        // 6. Supprimer les joueurs qui ne sont plus dans la liste.
    }

    private RemotePlayer GetOrCreateRemotePlayer(NetworkPlayerInfo playerInfo)
    {
        // TODO COURS 1 - Exercice 5:
        // Si le joueur existe deja dans le dictionnaire, le retourner.
        // Sinon, creer son GameObject avec CreateShipObject, ajouter/trouver RemotePlayer,
        // remplir playerId/playerName, l'ajouter au dictionnaire, puis le retourner.
        return null;
    }

    private GameObject CreateShipObject(NetworkPlayerInfo playerInfo)
    {
        Vector3 position = new Vector3(playerInfo.x, playerInfo.y, playerInfo.z);
        Quaternion rotation = Quaternion.Euler(0f, playerInfo.yaw, 0f);
        GameObject ship;

        if (remoteShipPrefab != null)
        {
            ship = new GameObject("Remote Ship - " + playerInfo.id);
            ship.transform.SetParent(remotePlayersParent);
            ship.transform.SetPositionAndRotation(position, rotation);

            GameObject model = Instantiate(remoteShipPrefab, ship.transform);
            model.name = "ShipModel";
            model.transform.localPosition = remoteModelLocalPosition;
            model.transform.localRotation = Quaternion.Euler(remoteModelLocalEulerAngles);
            model.transform.localScale = remoteModelLocalScale;
        }
        else
        {
            ship = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            ship.transform.SetParent(remotePlayersParent);
            ship.transform.SetPositionAndRotation(position, rotation);
            ship.transform.localScale = new Vector3(1f, 0.5f, 2f);

            Renderer renderer = ship.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.material.color = Color.cyan;
            }
        }

        ship.name = "Remote Ship - " + playerInfo.id;
        return ship;
    }

    private void HandleShootEvent(ShootEventMessage shootEvent)
    {
        // TODO COURS 1 - Exercice 5:
        // Si le tir vient du joueur local, l'ignorer car il a deja affiche son laser.
        // Sinon, construire position et direction depuis le message,
        // puis appeler LaserVisual.Spawn pour afficher le laser distant.
    }

    private void RemovePlayer(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            return;
        }

        if (remotePlayers.TryGetValue(playerId, out RemotePlayer remotePlayer))
        {
            Destroy(remotePlayer.gameObject);
            remotePlayers.Remove(playerId);
        }
    }

    private void RemoveAllPlayers()
    {
        foreach (RemotePlayer remotePlayer in remotePlayers.Values)
        {
            if (remotePlayer != null)
            {
                Destroy(remotePlayer.gameObject);
            }
        }

        remotePlayers.Clear();
    }
}
