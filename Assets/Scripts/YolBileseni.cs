using UnityEngine;

public class YolBileseni : MonoBehaviour
{
    [Header("Şerit Spawn Noktaları")]
    public Transform solSpawnNoktasi;
    public Transform ortaSpawnNoktasi;
    public Transform sagSpawnNoktasi;

    /// <summary>
    /// Belirtilen şeridin (0: Sol, 1: Orta, 2: Sağ) o anki X koordinatındaki tam dünya pozisyonunu hesaplar.
    /// </summary>
    public Vector3 GetLanePosition(int laneIndex, float xCoordinate)
    {
        Transform targetPoint = null;
        if (laneIndex == 0) targetPoint = solSpawnNoktasi;
        else if (laneIndex == 1) targetPoint = ortaSpawnNoktasi;
        else if (laneIndex == 2) targetPoint = sagSpawnNoktasi;

        if (targetPoint != null)
        {
            // Şeridin güncel Dünya Y ve Z değerlerini koruyarak istenen X değerini birleştirir
            return new Vector3(xCoordinate, targetPoint.position.y, targetPoint.position.z);
        }

        // Eğer prefabda şerit noktaları atanmamışsa (Yedek Plan - Geriye dönük uyumluluk):
        float fallbackY = transform.position.y + 0.5f; 
        float fallbackZ = transform.position.z;
        if (laneIndex == 0) fallbackZ -= 8.68f;
        else if (laneIndex == 2) fallbackZ += 8.68f;

        return new Vector3(xCoordinate, fallbackY, fallbackZ);
    }
}