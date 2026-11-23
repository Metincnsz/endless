using UnityEngine;

[DisallowMultipleComponent]
public class ProceduralPlanet : MonoBehaviour
{
    [Header("Planet")]
    [SerializeField, Min(0.1f)]
    private float radius = 2f;

    [SerializeField]
    private int seed = 42;

    [SerializeField]
    private float rotationSpeed = 8f;

    [Header("Surface")]
    [SerializeField]
    private Material surfaceMaterial;

    [SerializeField, Range(128, 1024)]
    private int textureWidth = 512;

    [SerializeField, Range(0.25f, 5f)]
    private float continentScale = 1.8f;

    [SerializeField, Range(0.3f, 0.7f)]
    private float seaLevel = 0.5f;

    [SerializeField, Range(0.65f, 0.98f)]
    private float polarStart = 0.85f;

    [Header("Colors")]
    [SerializeField]
    private Color deepOcean = new Color(0.015f, 0.055f, 0.18f);

    [SerializeField]
    private Color shallowOcean = new Color(0.04f, 0.38f, 0.58f);

    [SerializeField]
    private Color beach = new Color(0.76f, 0.69f, 0.43f);

    [SerializeField]
    private Color grass = new Color(0.16f, 0.43f, 0.19f);

    [SerializeField]
    private Color forest = new Color(0.055f, 0.22f, 0.10f);

    [SerializeField]
    private Color mountain = new Color(0.38f, 0.34f, 0.29f);

    [SerializeField]
    private Color ice = new Color(0.88f, 0.95f, 1f);

    private GameObject planetObject;
    private Material generatedMaterial;
    private Texture2D generatedTexture;

    private void Start()
    {
        if (surfaceMaterial == null)
        {
            Debug.LogError(
                "ProceduralPlanet: Surface Material alanina " +
                "PlanetSurface materyalini bagla.",
                this
            );

            enabled = false;
            return;
        }

        CreatePlanet();
    }

    private void Update()
    {
        if (planetObject == null)
            return;

        planetObject.transform.Rotate(
            Vector3.up,
            rotationSpeed * Time.deltaTime,
            Space.Self
        );
    }

    private void CreatePlanet()
    {
        // Unity'nin yerlesik kure geometrisi kullanilir.
        // Harici bir model dosyasi gerekmez.
        planetObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        planetObject.name = "Generated Planet";
        planetObject.layer = gameObject.layer;

        planetObject.transform.SetParent(transform, false);
        planetObject.transform.localPosition = Vector3.zero;
        planetObject.transform.localRotation = Quaternion.identity;

        // Yerlesik kurenin capi 1 birimdir.
        float diameter = Mathf.Max(0.1f, radius) * 2f;
        planetObject.transform.localScale = Vector3.one * diameter;

        // Bu onizlemede fizik/carpisma kullanmiyoruz.
        Collider planetCollider = planetObject.GetComponent<Collider>();

        if (planetCollider != null)
        {
            planetCollider.enabled = false;
            Destroy(planetCollider);
        }

        // Kaynak materyali degistirmemek icin kopyasini olustur.
        generatedMaterial = new Material(surfaceMaterial);
        generatedMaterial.name = "Generated Planet Material";

        generatedTexture = CreateSurfaceTexture();

        generatedMaterial.SetTexture("_BaseMap", generatedTexture);
        generatedMaterial.SetColor("_BaseColor", Color.white);
        generatedMaterial.SetFloat("_Metallic", 0f);
        generatedMaterial.SetFloat("_Smoothness", 0.3f);

        MeshRenderer meshRenderer =
            planetObject.GetComponent<MeshRenderer>();

        meshRenderer.sharedMaterial = generatedMaterial;
    }

    private Texture2D CreateSurfaceTexture()
    {
        int width = Mathf.Clamp(textureWidth, 128, 1024);
        int height = width / 2;

        Texture2D texture = new Texture2D(
            width,
            height,
            TextureFormat.RGBA32,
            true
        );

        texture.name = "Generated Planet Surface";
        texture.wrapModeU = TextureWrapMode.Repeat;
        texture.wrapModeV = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Trilinear;
        texture.anisoLevel = 2;

        Color32[] pixels = new Color32[width * height];

        // Unity'nin global Random durumunu degistirmiyoruz.
        System.Random random = new System.Random(seed);

        Vector3 offset = new Vector3(
            (float)random.NextDouble() * 100f,
            (float)random.NextDouble() * 100f,
            (float)random.NextDouble() * 100f
        );

        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);
            float latitude = (v - 0.5f) * Mathf.PI;

            float cosLatitude = Mathf.Cos(latitude);
            float sinLatitude = Mathf.Sin(latitude);

            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);
                float longitude = u * Mathf.PI * 2f;

                // Gurultuyu duz bir resimde degil, kure uzerinde
                // ornekleyerek yatay birlesme izini azalt.
                Vector3 direction = new Vector3(
                    cosLatitude * Mathf.Cos(longitude),
                    sinLatitude,
                    cosLatitude * Mathf.Sin(longitude)
                );

                Vector3 samplePosition =
                    direction * continentScale + offset;

                float terrain = FractalNoise(samplePosition);

                Color surfaceColor = GetSurfaceColor(
                    terrain,
                    Mathf.Abs(sinLatitude)
                );

                pixels[y * width + x] = surfaceColor;
            }
        }

        texture.SetPixels32(pixels);

        // Mipmap olustur, CPU tarafindaki piksel kopyasini serbest birak.
        texture.Apply(true, true);

        return texture;
    }

    private Color GetSurfaceColor(float terrain, float polarAmount)
    {
        Color color;

        if (terrain < seaLevel)
        {
            float oceanBlend = Mathf.InverseLerp(
                seaLevel - 0.18f,
                seaLevel,
                terrain
            );

            color = Color.Lerp(
                deepOcean,
                shallowOcean,
                oceanBlend
            );
        }
        else if (terrain < seaLevel + 0.025f)
        {
            float beachBlend = Mathf.InverseLerp(
                seaLevel,
                seaLevel + 0.025f,
                terrain
            );

            color = Color.Lerp(beach, grass, beachBlend);
        }
        else if (terrain < seaLevel + 0.12f)
        {
            float landBlend = Mathf.InverseLerp(
                seaLevel + 0.025f,
                seaLevel + 0.12f,
                terrain
            );

            color = Color.Lerp(grass, forest, landBlend);
        }
        else
        {
            float mountainBlend = Mathf.InverseLerp(
                seaLevel + 0.12f,
                seaLevel + 0.25f,
                terrain
            );

            color = Color.Lerp(forest, mountain, mountainBlend);
        }

        float iceBlend = Mathf.SmoothStep(
            0f,
            1f,
            Mathf.InverseLerp(polarStart, 1f, polarAmount)
        );

        return Color.Lerp(color, ice, iceBlend);
    }

    private static float FractalNoise(Vector3 position)
    {
        float total = 0f;
        float totalAmplitude = 0f;
        float amplitude = 1f;
        float frequency = 1f;

        for (int octave = 0; octave < 4; octave++)
        {
            total += SampleNoise(position * frequency) * amplitude;
            totalAmplitude += amplitude;

            amplitude *= 0.5f;
            frequency *= 2f;
        }

        return total / totalAmplitude;
    }

    private static float SampleNoise(Vector3 p)
    {
        // Unity PerlinNoise 2D'dir.
        // Uc duzlemin ortalamasi ile 3D-benzeri bir ornek elde edilir.
        float xy = Mathf.PerlinNoise(p.x, p.y);
        float yz = Mathf.PerlinNoise(p.y, p.z);
        float zx = Mathf.PerlinNoise(p.z, p.x);

        return (xy + yz + zx) / 3f;
    }

    private void OnDestroy()
    {
        if (planetObject != null)
            Destroy(planetObject);

        if (generatedMaterial != null)
            Destroy(generatedMaterial);

        if (generatedTexture != null)
            Destroy(generatedTexture);
    }
}