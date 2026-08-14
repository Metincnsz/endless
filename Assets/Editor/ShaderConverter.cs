using UnityEngine;
using UnityEditor;
using System.IO;

public class ShaderConverter : EditorWindow
{
    private string excludeFolderPath = "Assets/Character"; // Muaf tutulacak klasör
    private bool convertToSimpleLit = true;

    [MenuItem("Mobil Optimasyon/Shader Dönüştürücü")]
    public static void ShowWindow()
    {
        GetWindow<ShaderConverter>("Shader Converter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Mobil Shader Optimizasyon Paneli", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Bu araç, mobil performansı artırmak için ağır 'URP/Lit' shader'ını kullanan materyalleri " +
            "optimize edilmiş 'URP/Simple Lit' shader'ına dönüştürür.", MessageType.Info);

        EditorGUILayout.Space();

        excludeFolderPath = EditorGUILayout.TextField("Muaf Klasör (Örn: Karakter):", excludeFolderPath);
        convertToSimpleLit = EditorGUILayout.Toggle("Simple Lit'e Dönüştür", convertToSimpleLit);

        if (!convertToSimpleLit)
        {
            GUILayout.Label("(İşaretlenmezse 'URP/Unlit' shader'ına dönüştürülür - Tamamen Işıksız)", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Materyalleri Mobil İçin Dönüştür", GUILayout.Height(40)))
        {
            ConvertShaders();
        }
    }

    private void ConvertShaders()
    {
        string targetShaderName = convertToSimpleLit ? "Universal Render Pipeline/Simple Lit" : "Universal Render Pipeline/Unlit";
        Shader targetShader = Shader.Find(targetShaderName);

        if (targetShader == null)
        {
            EditorUtility.DisplayDialog("Hata!", $"Hedef Shader bulunamadı: {targetShaderName}. Projenizin URP kullandığından emin olun.", "Tamam");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        int total = guids.Length;
        int convertedCount = 0;

        try
        {
            for (int i = 0; i < total; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                // Muaf tutulacak klasör kontrolü (Örn: Karakter materyalleri yüksek kalitede kalsın)
                if (!string.IsNullOrEmpty(excludeFolderPath) && path.StartsWith(excludeFolderPath))
                    continue;

                // Editör veya paket materyallerini atla
                if (path.Contains("packages") || path.Contains("Editor"))
                    continue;

                float progress = (float)i / total;
                if (EditorUtility.DisplayCancelableProgressBar("Shader'lar Dönüştürülüyor", $"İşleniyor ({i}/{total}): {Path.GetFileName(path)}", progress))
                {
                    Debug.Log("İşlem kullanıcı tarafından iptal edildi.");
                    break;
                }

                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null && mat.shader != null && mat.shader.name == "Universal Render Pipeline/Lit")
                {
                    // Eski materyal değerlerini yedekle (Kayıp yaşamamak için)
                    Color baseColor = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white;
                    Texture baseMap = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null;
                    Vector2 baseMapScale = mat.HasProperty("_BaseMap") ? mat.GetTextureScale("_BaseMap") : Vector2.one;
                    Vector2 baseMapOffset = mat.HasProperty("_BaseMap") ? mat.GetTextureOffset("_BaseMap") : Vector2.zero;
                    
                    Texture bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
                    float bumpScale = mat.HasProperty("_BumpScale") ? mat.GetFloat("_BumpScale") : 1.0f;

                    // Shader'ı değiştir
                    mat.shader = targetShader;

                    // Yedeklenen değerleri yeni shader'a güvenle aktar
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
                    if (mat.HasProperty("_BaseMap") && baseMap != null)
                    {
                        mat.SetTexture("_BaseMap", baseMap);
                        mat.SetTextureScale("_BaseMap", baseMapScale);
                        mat.SetTextureOffset("_BaseMap", baseMapOffset);
                    }
                    if (mat.HasProperty("_BumpMap") && bumpMap != null)
                    {
                        mat.SetTexture("_BumpMap", bumpMap);
                        mat.SetFloat("_BumpScale", bumpScale);
                        mat.EnableKeyword("_NORMALMAP"); // Normal map özelliğini aktif et
                    }

                    EditorUtility.SetDirty(mat);
                    convertedCount++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("İşlem Başarıyla Tamamlandı!", 
                $"{convertedCount} adet ağır URP/Lit materyali başarıyla hafif '{targetShaderName}' shader'ına dönüştürüldü.", "Harika!");
        }
    }
}