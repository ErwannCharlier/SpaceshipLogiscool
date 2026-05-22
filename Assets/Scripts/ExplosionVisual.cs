using System.Collections.Generic;
using UnityEngine;

public class ExplosionVisual : MonoBehaviour
{
    [Header("Explosion")]
    public float lifetime = 0.9f;
    public int fireFragmentCount = 12;
    public int smokeFragmentCount = 7;
    public float fireSpeed = 11f;
    public float smokeSpeed = 4f;

    [Header("Colors")]
    public Color fireColor = new Color(1f, 0.55f, 0.15f, 1f);
    public Color smokeColor = new Color(0.35f, 0.38f, 0.45f, 0.7f);

    private float age;
    private Transform flashTransform;
    private Renderer flashRenderer;
    private Light flashLight;
    private readonly List<ExplosionPart> fireParts = new List<ExplosionPart>();
    private readonly List<ExplosionPart> smokeParts = new List<ExplosionPart>();

    private class ExplosionPart
    {
        public Transform transform;
        public Renderer renderer;
        public Vector3 direction;
        public float speed;
        public float startSize;
        public float endSize;
        public Color color;
    }

    public static ExplosionVisual Spawn(Vector3 position, GameObject explosionPrefab = null)
    {
        GameObject explosionObject;

        if (explosionPrefab != null)
        {
            explosionObject = Instantiate(explosionPrefab, position, Quaternion.identity);
        }
        else
        {
            explosionObject = new GameObject("Explosion");
            explosionObject.transform.position = position;
        }

        ExplosionVisual explosion = explosionObject.GetComponent<ExplosionVisual>();

        if (explosion == null)
        {
            explosion = explosionObject.AddComponent<ExplosionVisual>();
        }

        explosion.Initialize(position);
        return explosion;
    }

    public void Initialize(Vector3 position)
    {
        transform.position = position;
        age = 0f;

        if (fireParts.Count == 0 && smokeParts.Count == 0)
        {
            BuildDefaultExplosion();
        }

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        age += Time.deltaTime;
        float normalizedAge = Mathf.Clamp01(age / lifetime);

        UpdateFlash(normalizedAge);
        UpdateParts(fireParts, normalizedAge, 1f - normalizedAge);
        UpdateParts(smokeParts, normalizedAge, (1f - normalizedAge) * 0.65f);
    }

    private void BuildDefaultExplosion()
    {
        CreateFlash();
        CreateExplosionParts(fireParts, fireFragmentCount, fireSpeed, 0.45f, 1.35f, fireColor);
        CreateExplosionParts(smokeParts, smokeFragmentCount, smokeSpeed, 0.8f, 2.4f, smokeColor);
    }

    private void CreateFlash()
    {
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "Explosion Flash";
        flash.transform.SetParent(transform, false);
        flash.transform.localPosition = Vector3.zero;
        flash.transform.localScale = Vector3.one * 1.2f;

        Collider flashCollider = flash.GetComponent<Collider>();

        if (flashCollider != null)
        {
            Destroy(flashCollider);
        }

        flashTransform = flash.transform;
        flashRenderer = flash.GetComponent<Renderer>();

        if (flashRenderer != null)
        {
            flashRenderer.material = CreateExplosionMaterial(fireColor);
        }

        GameObject lightObject = new GameObject("Explosion Light");
        lightObject.transform.SetParent(transform, false);
        lightObject.transform.localPosition = Vector3.zero;

        flashLight = lightObject.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.range = 18f;
        flashLight.intensity = 7f;
        flashLight.color = fireColor;
    }

    private void CreateExplosionParts(
        List<ExplosionPart> parts,
        int count,
        float speed,
        float minSize,
        float maxSize,
        Color color
    )
    {
        for (int i = 0; i < count; i++)
        {
            GameObject fragment = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fragment.name = "Explosion Part " + i;
            fragment.transform.SetParent(transform, false);
            fragment.transform.localPosition = Random.insideUnitSphere * 0.2f;

            Collider fragmentCollider = fragment.GetComponent<Collider>();

            if (fragmentCollider != null)
            {
                Destroy(fragmentCollider);
            }

            Renderer renderer = fragment.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.material = CreateExplosionMaterial(color);
            }

            float startSize = Random.Range(minSize, maxSize);
            fragment.transform.localScale = Vector3.one * startSize;

            ExplosionPart part = new ExplosionPart
            {
                transform = fragment.transform,
                renderer = renderer,
                direction = Random.onUnitSphere,
                speed = speed * Random.Range(0.75f, 1.2f),
                startSize = startSize,
                endSize = startSize * Random.Range(0.35f, 0.8f),
                color = color
            };

            parts.Add(part);
        }
    }

    private void UpdateFlash(float normalizedAge)
    {
        if (flashTransform != null)
        {
            float flashScale = Mathf.Lerp(1.2f, 4.5f, normalizedAge);
            flashTransform.localScale = Vector3.one * flashScale;
        }

        if (flashRenderer != null)
        {
            float flashAlpha = Mathf.Clamp01(1f - normalizedAge * 1.8f);
            flashRenderer.material.color = WithAlpha(fireColor, flashAlpha);
        }

        if (flashLight != null)
        {
            flashLight.intensity = Mathf.Lerp(7f, 0f, normalizedAge);
        }
    }

    private void UpdateParts(List<ExplosionPart> parts, float normalizedAge, float alpha)
    {
        for (int i = 0; i < parts.Count; i++)
        {
            ExplosionPart part = parts[i];

            if (part.transform != null)
            {
                part.transform.localPosition += part.direction * part.speed * Time.deltaTime;
                float size = Mathf.Lerp(part.startSize, part.endSize, normalizedAge);
                part.transform.localScale = Vector3.one * size;
            }

            if (part.renderer != null)
            {
                part.renderer.material.color = WithAlpha(part.color, alpha);
            }
        }
    }

    private static Material CreateExplosionMaterial(Color color)
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
