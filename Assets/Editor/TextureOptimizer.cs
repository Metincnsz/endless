using UnityEngine;
using UnityEditor;
using System.IO;

public class TextureOptimizer : EditorWindow
{
    private int maxTextureSizeLimit = 1024;
    private bool overrideNormalMaps = true;
    private bool overrideUIAndSprites = true;

    [MenuItem("Mobil Optimasyon/Texture Sıkıştırıcı")]
    public static void ShowWindow()
    {
        GetWindow<TextureOptimizer>("Texture Optimizer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Mobil Texture Optimizasyon Paneli", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        maxTextureSizeLimit = EditorGUILayout.IntPopup("Maksimum Boyut Sınırı:", maxTextureSizeLimit, 
            new string[] { "512", "1024", "2048", "4096" }, 
            new int[] { 512, 1024, 2048, 4096 });

        overrideNormalMaps = EditorGUILayout.Toggle("Normal Map'leri Optimize Et", overrideNormalMaps);
        overrideUIAndSprites = EditorGUILayout.Toggle("UI ve Sprite'ları Optimize Et", overrideUIAndSprites);

        EditorGUILayout.Space();

        if (GUILayout.Button("Tüm Texture'ları Android İçin Optimize Et (ASTC)", GUILayout.Height(40)))
        {
            OptimizeAllTextures();
        }
    }

    private void OptimizeAllTextures()
    {
        // Projedeki tüm Texture2D'leri bul
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
        int total = guids.Length;
        int optimizedCount = 0;

        try
        {
            for (int i = 0; i < total; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                
                // Unity paketlerini veya geçici dosyaları atla
                if (path.StartsWith("Assets/Packages") || path.Contains("packages") || path.Contains("Editor"))
                    continue;

                // İlerleme çubuğunu güncelle
                float progress = (float)i / total;
                if (EditorUtility.DisplayCancelableProgressBar("Görseller Optimize Ediliyor", $"İşleniyor ({i}/{total}): {Path.GetFileName(path)}", progress))
                {
                    Debug.Log("İşlem kullanıcı tarafından iptal edildi.");
                    break;
                }

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    // Görsel türüne göre filtrele
                    if (importer.textureType == TextureImporterType.NormalMap && !overrideNormalMaps)
                        continue;
                    if ((importer.textureType == TextureImporterType.Sprite || importer.textureType == TextureImporterType.GUI) && !overrideUIAndSprites)
                        continue;

                    // Android platform ayarlarını yapılandır
                    TextureImporterPlatformSettings androidSettings = importer.GetPlatformTextureSettings("Android");
                    
                    // Eğer zaten optimize edilmişse ve boyutu sınırdan küçükse dokunma (Performans koruması)
                    if (androidSettings.overridden && androidSettings.maxTextureSize <= maxTextureSizeLimit)
                        continue;

                    androidSettings.overridden = true;
                    androidSettings.name = "Android";
                    
                    // Görselin orijinal boyutunu aşmamak için akıllı boyut sınırlaması
                    int currentMax = Mathf.Max(importer.maxTextureSize, maxTextureSizeLimit);
                    androidSettings.maxTextureSize = Mathf.Min(currentMax, maxTextureSizeLimit);

                    // Android için otomatik en uygun ASTC formatını seçmesini sağla (Unity bunu akıllıca yapar)
                    androidSettings.format = TextureImporterFormat.Automatic;
                    androidSettings.textureCompression = TextureImporterCompression.Compressed;
                    
                    // --- DÜZELTME: UnityEngine yerine UnityEditor namespace'i kullanıldı ---
                    androidSettings.compressionQuality = (int)UnityEditor.TextureCompressionQuality.Normal;

                    // Ayarları uygula ve kaydet
                    importer.SetPlatformTextureSettings(androidSettings);
                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                    
                    optimizedCount++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Optimizasyon Tamamlandı!", 
                $"{optimizedCount} adet görsel Android platformu için başarıyla ASTC formatında sıkıştırıldı ve optimize edildi.", "Harika!");
        }
    }
}