using UnityEngine;
using UnityEditor;

internal class AlignYolssPrefab
{
    public static void Align()
    {
        string prefabPath = "Assets/Prefabs/yolss.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError("yolss.prefab not found at " + prefabPath);
            return;
        }

        // Load prefab contents
        GameObject contentsRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            // 1. Let's inspect current local positions of children
            Transform zemin = contentsRoot.transform.Find("Zemin");
            Transform spawnPoint = contentsRoot.transform.Find("SpawnPoint");
            Transform yolTetikleyici = contentsRoot.transform.Find("YolTetikleyici");

            if (zemin != null)
            {
                // Align Zemin local position so that:
                // X center is -29.00 relative to root (so segments span exactly from X = 0 to X = -58.00)
                // Y top of the driveway models is exactly Y = 0.00
                // Z center is exactly 2.30 (to align with Yollar meshes)
                zemin.localPosition = new Vector3(-13.99f, -7.37f, 24.87f);
                Debug.Log("Aligned Zemin localPosition to " + zemin.localPosition);
            }

            if (spawnPoint != null)
            {
                // Set SpawnPoint precisely to the left end of the 58-unit road
                spawnPoint.localPosition = new Vector3(-58.00f, 0.00f, 0.00f);
                Debug.Log("Aligned SpawnPoint localPosition to " + spawnPoint.localPosition);
            }

            if (yolTetikleyici != null)
            {
                // Place YolTetikleyici near the left end
                yolTetikleyici.localPosition = new Vector3(-54.00f, 4.62f, 2.30f);
                Debug.Log("Aligned YolTetikleyici localPosition to " + yolTetikleyici.localPosition);
            }

            // Save modified prefab
            PrefabUtility.SaveAsPrefabAsset(contentsRoot, prefabPath);
            Debug.Log("Saved modified yolss.prefab successfully!");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contentsRoot);
        }
    }
}
