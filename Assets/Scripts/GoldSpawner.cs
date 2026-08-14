using UnityEngine;
using Unity.Netcode; // Netcode eklendi

public class GoldSpawner : NetworkBehaviour // NetworkBehaviour yapıldı
{
    [Header("Altın Ayarları")]
    [SerializeField] private GameObject goldPrefab;
    [SerializeField] private int rowCount = 3;
    [SerializeField] private float goldSpacing = 3.5f;
    [SerializeField] private float yOffset = 0.1f; 

    [Header("Spawn Zamanlama Ayarları")]
    [SerializeField] private float spawnIntervalDistance = 25f;
    [SerializeField] private float spawnAheadDistance = 120f;

    private Transform playerTransform;
    private ZeminKontrol zeminKontrol;
    private float nextSpawnX;

    void Start()
    {
        zeminKontrol = Object.FindFirstObjectByType<ZeminKontrol>();

        KarakterKontrol karakter = Object.FindFirstObjectByType<KarakterKontrol>();
        if (karakter != null)
        {
            playerTransform = karakter.transform;
            nextSpawnX = playerTransform.position.x - 40f; 
        }
    }

    void Update()
    {
        // Sadece Sunucu (Host) altın üretebilir, Client'lar otomatik eşitlenir
        if (!IsServer) return; 

        if (goldPrefab == null) return;

        if (playerTransform == null)
        {
            KarakterKontrol karakter = Object.FindFirstObjectByType<KarakterKontrol>();
            if (karakter != null)
            {
                playerTransform = karakter.transform;
                nextSpawnX = playerTransform.position.x - 40f;
            }
            else
            {
                return;
            }
        }

        float spawnThresholdX = playerTransform.position.x - spawnAheadDistance;

        if (spawnThresholdX <= nextSpawnX)
        {
            SpawnGoldGroup(nextSpawnX);
            nextSpawnX -= spawnIntervalDistance;
        }
    }

    private void SpawnGoldGroup(float groupStartX)
    {
        int targetLane = Random.Range(0, 3);

        for (int i = 0; i < rowCount; i++)
        {
            float targetX = groupStartX - (i * goldSpacing);
            Vector3 spawnPos = Vector3.zero;

            YolBileseni activeRoad = null;
            if (zeminKontrol != null)
            {
                activeRoad = zeminKontrol.GetRoadAtX(targetX);
            }

            if (activeRoad != null)
            {
                spawnPos = activeRoad.GetLanePosition(targetLane, targetX);
                spawnPos.y += yOffset; 
            }
            else
            {
                float baseHeight = zeminKontrol != null ? zeminKontrol.zeminTabanY : 0f;
                float fallbackZ = 0f;
                if (targetLane == 0) fallbackZ = -8.68f;
                else if (targetLane == 2) fallbackZ = 8.68f;

                spawnPos = new Vector3(targetX, baseHeight + yOffset, fallbackZ);
            }

            // Altını oluşturup ağda yayınlıyoruz
            GameObject yeniAltin = Instantiate(goldPrefab, spawnPos, goldPrefab.transform.rotation);
            var netObj = yeniAltin.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn(true);
            }
        }
    }
}