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
        if (players == null)
        {
            return;
        }

        // The world message is treated as the latest full list of players from the server.
        playersToRemove.Clear();

        foreach (string playerId in remotePlayers.Keys)
        {
            playersToRemove.Add(playerId);
        }

        foreach (NetworkPlayerInfo playerInfo in players)
        {
            if (playerInfo == null || string.IsNullOrEmpty(playerInfo.id))
            {
                continue;
            }

            if (playerInfo.id == networkClient.LocalPlayerId)
            {
                continue;
            }

            RemotePlayer remotePlayer = GetOrCreateRemotePlayer(playerInfo);
            Vector3 position = new Vector3(playerInfo.x, playerInfo.y, playerInfo.z);
            Quaternion rotation = Quaternion.Euler(playerInfo.pitch, playerInfo.yaw, playerInfo.roll);

            remotePlayer.ApplyWorldState(playerInfo, position, rotation);
            playersToRemove.Remove(playerInfo.id);
        }

        for (int i = 0; i < playersToRemove.Count; i++)
        {
            RemovePlayer(playersToRemove[i]);
        }
    }

    private RemotePlayer GetOrCreateRemotePlayer(NetworkPlayerInfo playerInfo)
    {
        if (remotePlayers.TryGetValue(playerInfo.id, out RemotePlayer remotePlayer))
        {
            return remotePlayer;
        }

        GameObject ship = CreateShipObject(playerInfo);
        remotePlayer = ship.GetComponent<RemotePlayer>();

        if (remotePlayer == null)
        {
            remotePlayer = ship.AddComponent<RemotePlayer>();
        }

        remotePlayer.playerId = playerInfo.id;
        remotePlayer.playerName = playerInfo.name;
        remotePlayers.Add(playerInfo.id, remotePlayer);

        return remotePlayer;
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
        if (shootEvent == null || shootEvent.id == networkClient.LocalPlayerId)
        {
            return;
        }

        Vector3 position = new Vector3(shootEvent.x, shootEvent.y, shootEvent.z);
        Vector3 direction = new Vector3(shootEvent.dx, shootEvent.dy, shootEvent.dz);
        LaserVisual.Spawn(remoteLaserPrefab, position, direction);
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
