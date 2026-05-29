using System.Collections.Generic;
using UnityEngine;

public static class ShipLibrary
{
    public struct ShipOption
    {
        public string id;
        public string displayName;
        public GameObject prefab;
    }

    private static ShipOption[] cachedShips;

    public static ShipOption[] GetShips()
    {
        EnsureLoaded();
        return cachedShips;
    }

    public static bool HasShips()
    {
        return GetShips().Length > 0;
    }

    public static string GetDefaultShipId()
    {
        ShipOption[] ships = GetShips();
        return ships.Length > 0 ? ships[0].id : string.Empty;
    }

    public static int GetShipCount()
    {
        return GetShips().Length;
    }

    public static int GetShipIndex(string shipId)
    {
        ShipOption[] ships = GetShips();
        string normalizedId = NormalizeShipId(shipId);

        for (int i = 0; i < ships.Length; i++)
        {
            if (ships[i].id == normalizedId)
            {
                return i;
            }
        }

        return ships.Length > 0 ? 0 : -1;
    }

    public static string GetShipIdAt(int index)
    {
        ShipOption[] ships = GetShips();

        if (ships.Length == 0)
        {
            return string.Empty;
        }

        int clampedIndex = Mathf.Clamp(index, 0, ships.Length - 1);
        return ships[clampedIndex].id;
    }

    public static string GetDisplayName(string shipId)
    {
        ShipOption[] ships = GetShips();
        string normalizedId = NormalizeShipId(shipId);

        for (int i = 0; i < ships.Length; i++)
        {
            if (ships[i].id == normalizedId)
            {
                return ships[i].displayName;
            }
        }

        return ships.Length > 0 ? ships[0].displayName : "Vaisseau";
    }

    public static string NormalizeShipId(string shipId)
    {
        if (string.IsNullOrWhiteSpace(shipId))
        {
            return string.Empty;
        }

        string value = shipId.Trim().ToLowerInvariant();
        char[] buffer = new char[value.Length];
        int count = 0;

        for (int i = 0; i < value.Length; i++)
        {
            if (char.IsLetterOrDigit(value[i]))
            {
                buffer[count] = value[i];
                count++;
            }
        }

        return new string(buffer, 0, count);
    }

    public static GameObject GetShipPrefab(string shipId)
    {
        ShipOption[] ships = GetShips();

        if (ships.Length == 0)
        {
            return null;
        }

        string normalizedId = NormalizeShipId(shipId);

        for (int i = 0; i < ships.Length; i++)
        {
            if (ships[i].id == normalizedId)
            {
                return ships[i].prefab;
            }
        }

        return ships[0].prefab;
    }

    public static GameObject InstantiateShip(
        string shipId,
        Transform parent,
        Vector3 localPosition,
        Vector3 localEulerAngles,
        Vector3 localScale,
        string objectName
    )
    {
        GameObject prefab = GetShipPrefab(shipId);

        if (prefab == null)
        {
            return null;
        }

        GameObject instance = Object.Instantiate(prefab, parent);
        instance.name = objectName;
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = Quaternion.Euler(localEulerAngles);
        instance.transform.localScale = localScale;
        return instance;
    }

    private static void EnsureLoaded()
    {
        if (cachedShips != null)
        {
            return;
        }

        GameObject[] prefabs = Resources.LoadAll<GameObject>("Ships");

        if (prefabs == null || prefabs.Length == 0)
        {
            cachedShips = new ShipOption[0];
            return;
        }

        List<GameObject> shipPrefabs = new List<GameObject>();

        for (int i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i] != null)
            {
                shipPrefabs.Add(prefabs[i]);
            }
        }

        shipPrefabs.Sort((left, right) => string.CompareOrdinal(left.name, right.name));

        cachedShips = new ShipOption[shipPrefabs.Count];

        for (int i = 0; i < shipPrefabs.Count; i++)
        {
            string shipId = NormalizeShipId(shipPrefabs[i].name);

            if (string.IsNullOrEmpty(shipId))
            {
                shipId = "ship" + (i + 1);
            }

            cachedShips[i] = new ShipOption
            {
                id = shipId,
                displayName = "Vaisseau " + (i + 1),
                prefab = shipPrefabs[i]
            };
        }
    }
}
