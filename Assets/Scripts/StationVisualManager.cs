using UnityEngine;
using UnityEngine.Rendering;

public class StationVisualManager : MonoBehaviour
{
    [Header("References")]
    public NetworkClient networkClient;

    [Header("Visual")]
    public Color stationColor = new Color(0.15f, 0.65f, 1f, 0.28f);

    private GameObject stationObject;
    private Renderer stationRenderer;

    private void Awake()
    {
        if (networkClient == null)
        {
            networkClient = GetComponent<NetworkClient>();
        }

        if (networkClient == null)
        {
            networkClient = FindObjectOfType<NetworkClient>();
        }
    }

    private void OnEnable()
    {
        if (networkClient == null)
        {
            return;
        }

        networkClient.StationReceived += UpdateStationVisual;
        networkClient.Disconnected += HideStation;

        if (networkClient.CurrentStation != null)
        {
            UpdateStationVisual(networkClient.CurrentStation);
        }
    }

    private void OnDisable()
    {
        if (networkClient == null)
        {
            return;
        }

        networkClient.StationReceived -= UpdateStationVisual;
        networkClient.Disconnected -= HideStation;
    }

    public void CreateStationVisual()
    {
        // TODO Cours 2:
        // 1. Cree un cube avec GameObject.CreatePrimitive.
        // 2. Donne-lui le nom "Energy Station".
        // 3. Retire son collider.
        // 4. Applique un materiau transparent bleu.
    }

    public void UpdateStationVisual(StationInfo stationInfo)
    {
        if (stationInfo == null)
        {
            HideStation();
            return;
        }

        CreateStationVisual();

        if (stationObject == null)
        {
            return;
        }

        stationObject.SetActive(true);
        stationObject.transform.position = new Vector3(stationInfo.x, stationInfo.y, stationInfo.z);
        stationObject.transform.localScale = new Vector3(stationInfo.size, stationInfo.size, stationInfo.size);
    }

    private void HideStation()
    {
        if (stationObject != null)
        {
            stationObject.SetActive(false);
        }
    }

    private Material CreateStationMaterial(Color color)
    {
        Shader shader = Shader.Find("Standard");

        if (shader == null)
        {
            shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
        }

        Material material = new Material(shader);
        material.color = color;

        if (material.shader != null && material.shader.name == "Standard")
        {
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }

        return material;
    }
}
