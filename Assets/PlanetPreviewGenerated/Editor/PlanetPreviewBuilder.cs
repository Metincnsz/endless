using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEngine.SceneManagement;

// This builder deliberately refuses to operate on any other scene.
public static class PlanetPreviewBuilder
{
    public const string ScenePath = "Assets/Scenes/PlanetPreview.unity";
    public const string AssetFolder = "Assets/PlanetPreviewGenerated/Geometry";
    private static readonly Color[] Palette = {
        Hex("#0075BF"), Hex("#009DE5"), Hex("#00BFFF"),
        Hex("#35CFFF"), Hex("#7EE6FF"), Hex("#B7F4FF")
    };

    public static GameObject Build(Camera camera)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException("Only PlanetPreview.unity may be modified.");
        if (camera == null || camera.gameObject.scene != scene)
            throw new InvalidOperationException("A camera in PlanetPreview is required.");
        foreach (GameObject obj in scene.GetRootGameObjects())
            if (obj.name == "Cosmic Preview")
                throw new InvalidOperationException("Cosmic Preview already exists; do not create duplicates.");

        Shader planetShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/PlanetPreviewGenerated/Shaders/CosmicPlanet.shader");
        Shader glowShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/PlanetPreviewGenerated/Shaders/CosmicGlow.shader");
        if (planetShader == null || glowShader == null)
            throw new InvalidOperationException("Both Cosmic shaders are required.");
        System.IO.Directory.CreateDirectory(AssetFolder);
        AssetDatabase.Refresh();

        Material planetMaterial = new Material(planetShader);
        planetMaterial.SetColor("_BaseColor", new Color(0.001f, 0.009f, 0.024f, 1));
        planetMaterial.SetColor("_RimColor", new Color(0, 0.65f, 1.4f, 1));
        planetMaterial.SetFloat("_RimPower", 2.15f);
        planetMaterial.SetFloat("_RimStrength", 1.65f);
        SaveAsset(planetMaterial, "DarkPlanet.mat");
        Material glowMaterial = new Material(glowShader);
        glowMaterial.SetColor("_Tint", Color.white);
        glowMaterial.SetFloat("_Atmosphere", 0);
        SaveAsset(glowMaterial, "Streams.mat");
        Material atmosphereMaterial = new Material(glowShader);
        atmosphereMaterial.SetFloat("_Atmosphere", 1);
        atmosphereMaterial.SetFloat("_FresnelPower", 5);
        atmosphereMaterial.SetColor("_Tint", new Color(0, 0.65f, 1.5f, 0.28f));
        SaveAsset(atmosphereMaterial, "Atmosphere.mat");

        GameObject root = new GameObject("Cosmic Preview");
        SceneManager.MoveGameObjectToScene(root, scene);
        Vector3 center = new Vector3(-0.39f, -0.02f, 0);
        Transform planet = Group("Dark Planet", root.transform);
        planet.localPosition = center;
        MeshObject("Planet Surface", Sphere(1.53f, 96, 64), planetMaterial, planet);
        MeshObject("Blue Atmosphere", Sphere(1.56f, 96, 64), atmosphereMaterial, planet);
        Transform disk = Group("Accretion Disk", root.transform);
        disk.localPosition = center;
        disk.localRotation = Quaternion.AngleAxis(0.79f * Mathf.Rad2Deg, Vector3.forward) *
                             Quaternion.AngleAxis(-1.12f * Mathf.Rad2Deg, Vector3.right);
        System.Random random = new System.Random(5719);
        Func<float> rand = () => (float)random.NextDouble();
        Builder streams = new Builder();
        for (int j = 0; j < 105; j++)
        {
            float radius = 1.65f + j * 0.036f + rand() * 0.025f;
            float inner = Mathf.Exp(-Mathf.Pow((radius - 2.17f) / 0.63f, 2));
            Color color = inner > 0.65f ? Palette[4 + j % 2] : Palette[j % 4];
            float width = 0.007f + rand() * 0.014f;
            Ribbon(streams, radius, width, Glow(color, 0.23f + inner * 0.57f), rand() * 20,
                0.035f + (radius - 1.65f) * 0.026f, (rand() - 0.5f) * 0.07f, 0, Mathf.PI * 2, 240);
            if (j % 3 == 0)
                Ribbon(streams, radius, width * 5, Glow(Hex("#009FFF"), 0.035f + inner * 0.06f),
                    rand() * 20, 0.045f, -0.025f, 0, Mathf.PI * 2, 240);
        }
        for (int k = 0; k < 17; k++)
            Ribbon(streams, 1.8f + k * 0.18f, 0.12f, Glow(k < 5 ? Hex("#25C5FF") : Hex("#007CC4"),
                k < 5 ? 0.08f : 0.035f), 0, 0.01f, -0.065f, 0, Mathf.PI * 2, 160);
        for (int f = 0; f < 62; f++)
            Ribbon(streams, 3.3f + rand() * 2.6f, 0.006f + rand() * 0.012f,
                Glow(f % 3 == 0 ? Hex("#75E7FF") : Hex("#009BDE"), 0.25f + rand() * 0.35f),
                rand() * 20, 0.09f, -0.02f, rand() * Mathf.PI * 2, 0.08f + rand() * 0.6f, 40);
        MeshObject("Luminous Streams", streams.Build(), glowMaterial, disk);

        Builder gas = new Builder();
        Color[] gasColors = { Hex("#008BD7"), Hex("#00BAFF"), Hex("#4BDCFF"), Hex("#00679D") };
        for (int i = 0; i < 1600; i++)
        {
            float theta = rand() * Mathf.PI * 2;
            float radius = 2.85f + rand() * 2.75f;
            float turbulence = Mathf.Sin(theta * 13 + radius * 5) * 0.1f;
            Vector3 position = new Vector3(-Mathf.Cos(theta) * (radius + turbulence),
                Mathf.Sin(theta) * (radius + turbulence),
                (rand() - 0.5f) * (0.08f + (radius - 2.8f) * 0.13f));
            float size = 0.012f + Mathf.Pow(rand(), 3) * 0.09f;
            Vector3 tangent = new Vector3(Mathf.Sin(theta), Mathf.Cos(theta), 0);
            Vector3 radial = new Vector3(-Mathf.Cos(theta), Mathf.Sin(theta), 0);
            gas.Quad(position, tangent * size * (1.6f + rand() * 2), radial * size, Glow(gasColors[i % 4], 0.28f));
        }
        MeshObject("Orbital Gas", gas.Build(), glowMaterial, disk);
        Builder stars = new Builder();
        for (int i = 0; i < 130; i++)
        {
            Vector3 position = new Vector3(-(rand() - 0.5f) * 13, (rand() - 0.5f) * 12, -7 - rand() * 3);
            float size = 0.004f + Mathf.Pow(rand(), 4) * 0.016f;
            Color color = Color.Lerp(Hex("#19415B"), Hex("#698497"), rand());
            color.a = 0.85f;
            stars.Quad(position, Vector3.right * size, Vector3.up * size, color);
        }
        MeshObject("Distant Stars", stars.Build(), glowMaterial, root.transform);
        camera.transform.position = new Vector3(0, 0, 12);
        camera.transform.LookAt(Vector3.zero, Vector3.up);
        camera.orthographic = true;
        camera.orthographicSize = 4.025f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Hex("#00101E");
        camera.allowHDR = true;
        camera.allowMSAA = true;
        AssetDatabase.SaveAssets();
        return root;
    }

    private static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString(value, out Color color);
        return color;
    }
    private static Color Glow(Color color, float alpha)
    {
        return new Color(color.r * 1.2f, color.g * 1.2f, color.b * 1.2f, alpha);
    }
    private static Transform Group(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        return obj.transform;
    }
    private static void SaveAsset(UnityEngine.Object asset, string fileName)
    {
        string path = AssetFolder + "/" + fileName;
        if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            throw new InvalidOperationException("Refusing to overwrite " + path);
        AssetDatabase.CreateAsset(asset, path);
    }
    private static void MeshObject(string name, Mesh mesh, Material material, Transform parent)
    {
        mesh.name = name;
        SaveAsset(mesh, name.Replace(" ", "") + ".asset");
        GameObject obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }
    private static void Ribbon(Builder builder, float radius, float halfWidth, Color color,
        float phase, float irregularity, float elevation, float startAngle, float length, int segments)
    {
        int start = builder.Count;
        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + length * i / segments;
            float ripple = irregularity * (Mathf.Sin(angle * 7 + phase) * 0.48f +
                Mathf.Sin(angle * 17 - phase * 1.4f) * 0.23f + Mathf.Sin(angle * 31 + phase * 0.7f) * 0.13f);
            Vector3 radial = new Vector3(-Mathf.Cos(angle), Mathf.Sin(angle), 0);
            Vector3 center = radial * (radius + ripple);
            center.z = elevation + Mathf.Sin(angle * 9 + phase) * irregularity * 0.22f;
            builder.Vertex(center - radial * halfWidth, new Vector2(0.5f, 0), color, Vector3.forward);
            builder.Vertex(center + radial * halfWidth, new Vector2(0.5f, 1), color, Vector3.forward);
            if (i < segments) { int a = start + i * 2; builder.Indices(a, a + 1, a + 2, a + 3); }
        }
    }
    private static Mesh Sphere(float radius, int longitude, int latitude)
    {
        Builder builder = new Builder();
        for (int y = 0; y <= latitude; y++)
        {
            float phi = Mathf.PI * y / latitude;
            for (int x = 0; x <= longitude; x++)
            {
                float theta = Mathf.PI * 2 * x / longitude;
                Vector3 normal = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                builder.Vertex(normal * radius, new Vector2(x / (float)longitude, y / (float)latitude), Color.white, normal);
            }
        }
        int stride = longitude + 1;
        for (int y = 0; y < latitude; y++) for (int x = 0; x < longitude; x++)
        {
            int a = y * stride + x, b = a + stride, c = b + 1, d = a + 1;
            builder.Triangle(a, d, b); builder.Triangle(d, c, b);
        }
        return builder.Build();
    }
    private sealed class Builder
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<Color> colors = new List<Color>();
        private readonly List<int> triangles = new List<int>();
        public int Count => vertices.Count;
        public void Vertex(Vector3 p, Vector2 uv, Color color, Vector3 normal)
        { vertices.Add(p); normals.Add(normal); uvs.Add(uv); colors.Add(color); }
        public void Triangle(int a, int b, int c) { triangles.Add(a); triangles.Add(b); triangles.Add(c); }
        public void Indices(int a, int b, int c, int d) { Triangle(a, b, c); Triangle(b, d, c); }
        public void Quad(Vector3 center, Vector3 right, Vector3 up, Color color)
        {
            int start = Count;
            Vertex(center - right - up, new Vector2(0, 0), color, Vector3.forward);
            Vertex(center + right - up, new Vector2(1, 0), color, Vector3.forward);
            Vertex(center - right + up, new Vector2(0, 1), color, Vector3.forward);
            Vertex(center + right + up, new Vector2(1, 1), color, Vector3.forward);
            Indices(start, start + 1, start + 2, start + 3);
        }
        public Mesh Build()
        {
            Mesh mesh = new Mesh();
            if (Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uvs);
            mesh.SetColors(colors); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
