using UnityEngine;

public class LaserVisual : MonoBehaviour
{
    [Header("Laser Size")]
    public float lifetime = 0.28f;
    public float beamLength = 80f;
    public float coreWidth = 0.45f;
    public float glowWidth = 1.4f;

    [Header("Laser Colors")]
    public Color coreColor = new Color(1f, 1f, 0.25f, 1f);
    public Color glowColor = new Color(0.1f, 0.85f, 1f, 0.7f);

    private float age;
    private LineRenderer coreLine;
    private LineRenderer glowLine;
    private Renderer startSphereRenderer;
    private Renderer endSphereRenderer;
    private Light startLight;
    private Light endLight;

    public static LaserVisual Spawn(GameObject laserPrefab, Vector3 position, Vector3 direction)
    {
        Vector3 safeDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
        Quaternion rotation = Quaternion.LookRotation(safeDirection);
        GameObject laserObject;

        if (laserPrefab != null)
        {
            laserObject = Instantiate(laserPrefab, position, rotation);
        }
        else
        {
            laserObject = CreateBigLaser(position, rotation);
        }

        LaserVisual laserVisual = laserObject.GetComponent<LaserVisual>();

        if (laserVisual == null)
        {
            laserVisual = laserObject.AddComponent<LaserVisual>();
        }

        laserVisual.Initialize(position, safeDirection);
        return laserVisual;
    }

    public void Initialize(Vector3 position, Vector3 direction)
    {
        transform.position = position;
        transform.rotation = Quaternion.LookRotation(direction);
        age = 0f;

        FindParts();
        UpdateBeamShape();
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        age += Time.deltaTime;

        float lifeLeft = Mathf.Clamp01(1f - age / lifetime);
        float pulse = 0.85f + Mathf.Sin(Time.time * 80f) * 0.15f;
        float alpha = lifeLeft * pulse;

        SetLineAlpha(coreLine, coreColor, alpha);
        SetLineAlpha(glowLine, glowColor, alpha * 0.75f);
        SetSphereAlpha(startSphereRenderer, coreColor, alpha);
        SetSphereAlpha(endSphereRenderer, coreColor, alpha * 0.9f);

        if (startLight != null)
        {
            startLight.intensity = 8f * alpha;
        }

        if (endLight != null)
        {
            endLight.intensity = 5f * alpha;
        }
    }

    private void FindParts()
    {
        LineRenderer[] lines = GetComponentsInChildren<LineRenderer>();

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].name.Contains("Core"))
            {
                coreLine = lines[i];
            }
            else if (lines[i].name.Contains("Glow"))
            {
                glowLine = lines[i];
            }
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].name.Contains("Start"))
            {
                startSphereRenderer = renderers[i];
            }
            else if (renderers[i].name.Contains("End"))
            {
                endSphereRenderer = renderers[i];
            }
        }

        Light[] lights = GetComponentsInChildren<Light>();

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].name.Contains("Start"))
            {
                startLight = lights[i];
            }
            else if (lights[i].name.Contains("End"))
            {
                endLight = lights[i];
            }
        }
    }

    private void UpdateBeamShape()
    {
        SetupLine(coreLine, coreWidth, coreColor);
        SetupLine(glowLine, glowWidth, glowColor);

        if (endSphereRenderer != null)
        {
            endSphereRenderer.transform.localPosition = Vector3.forward * beamLength;
        }

        if (endLight != null)
        {
            endLight.transform.localPosition = Vector3.forward * beamLength;
        }
    }

    private void SetupLine(LineRenderer line, float width, Color color)
    {
        if (line == null)
        {
            return;
        }

        line.useWorldSpace = false;
        line.positionCount = 2;
        line.numCapVertices = 8;
        line.startWidth = width;
        line.endWidth = width;
        line.startColor = color;
        line.endColor = color;
        line.SetPosition(0, Vector3.zero);
        line.SetPosition(1, Vector3.forward * beamLength);
    }

    private void SetLineAlpha(LineRenderer line, Color color, float alpha)
    {
        if (line == null)
        {
            return;
        }

        line.startColor = WithAlpha(color, alpha);
        line.endColor = WithAlpha(color, alpha);
    }

    private void SetSphereAlpha(Renderer sphereRenderer, Color color, float alpha)
    {
        if (sphereRenderer == null)
        {
            return;
        }

        sphereRenderer.material.color = WithAlpha(color, alpha);
    }

    private static GameObject CreateBigLaser(Vector3 position, Quaternion rotation)
    {
        GameObject root = new GameObject("Big Laser");
        root.transform.SetPositionAndRotation(position, rotation);

        LaserVisual laserVisual = root.AddComponent<LaserVisual>();

        LineRenderer glow = CreateLine("Laser Glow", root.transform, laserVisual.glowWidth, laserVisual.glowColor);
        LineRenderer core = CreateLine("Laser Core", root.transform, laserVisual.coreWidth, laserVisual.coreColor);

        laserVisual.glowLine = glow;
        laserVisual.coreLine = core;

        Renderer startSphere = CreateSphere("Laser Start", root.transform, Vector3.zero, 1.2f, laserVisual.coreColor);
        Renderer endSphere = CreateSphere("Laser End", root.transform, Vector3.forward * laserVisual.beamLength, 1.6f, laserVisual.coreColor);

        laserVisual.startSphereRenderer = startSphere;
        laserVisual.endSphereRenderer = endSphere;

        laserVisual.startLight = CreatePointLight("Laser Start Light", root.transform, Vector3.zero, laserVisual.coreColor, 14f, 8f);
        laserVisual.endLight = CreatePointLight("Laser End Light", root.transform, Vector3.forward * laserVisual.beamLength, laserVisual.coreColor, 10f, 5f);

        return root;
    }

    private static LineRenderer CreateLine(string name, Transform parent, float width, Color color)
    {
        GameObject lineObject = new GameObject(name);
        lineObject.transform.SetParent(parent);
        lineObject.transform.localPosition = Vector3.zero;
        lineObject.transform.localRotation = Quaternion.identity;

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.material = CreateLaserMaterial(color);
        line.useWorldSpace = false;
        line.positionCount = 2;
        line.numCapVertices = 8;
        line.startWidth = width;
        line.endWidth = width;
        line.startColor = color;
        line.endColor = color;
        line.SetPosition(0, Vector3.zero);
        line.SetPosition(1, Vector3.forward * 80f);

        return line;
    }

    private static Renderer CreateSphere(string name, Transform parent, Vector3 localPosition, float size, Color color)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.SetParent(parent);
        sphere.transform.localPosition = localPosition;
        sphere.transform.localRotation = Quaternion.identity;
        sphere.transform.localScale = Vector3.one * size;

        Collider collider = sphere.GetComponent<Collider>();

        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = sphere.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.material = CreateLaserMaterial(color);
        }

        return renderer;
    }

    private static Light CreatePointLight(string name, Transform parent, Vector3 localPosition, Color color, float range, float intensity)
    {
        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent);
        lightObject.transform.localPosition = localPosition;
        lightObject.transform.localRotation = Quaternion.identity;

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = range;
        light.intensity = intensity;

        return light;
    }

    private static Material CreateLaserMaterial(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
