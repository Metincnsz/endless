using UnityEngine;
using UnityEditor;

public class StandardizeYolss
{
    public static void Run()
    {
        string prefabPath = "Assets/Prefabs/yolss.prefab";
        GameObject contentsRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Transform zemin = contentsRoot.transform.Find("Zemin");
            Transform spawnPoint = contentsRoot.transform.Find("SpawnPoint");
            Transform yolTetikleyici = contentsRoot.transform.Find("YolTetikleyici");

            if (zemin != null)
            {
                zemin.localPosition = new Vector3(-15.25f, -7.23f, 24.87f);
                Debug.Log("Zemin localPosition set to: " + zemin.localPosition);
            }

            if (spawnPoint != null)
            {
                spawnPoint.localPosition = new Vector3(-58.00f, 0.00f, 0.00f);
                Debug.Log("SpawnPoint localPosition set to: " + spawnPoint.localPosition);
            }

            if (yolTetikleyici != null)
            {
                yolTetikleyici.localPosition = new Vector3(-54.00f, 4.62f, 2.30f);
                Debug.Log("YolTetikleyici localPosition set to: " + yolTetikleyici.localPosition);
            }

            PrefabUtility.SaveAsPrefabAsset(contentsRoot, prefabPath);
            Debug.Log("Standardized yolss.prefab successfully on disk!");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contentsRoot);
        }
    }
}

