using UnityEngine;
using Unity.Netcode;

public class PotionSpawner : MonoBehaviour
{
    [Header("İksir Prefabları & Havuzu")]
    [Tooltip("Parkurda çıkabilecek iksir prefabları")]
    [SerializeField] private GameObject[] potionPrefabs;

    [Header("Spawn Aralıkları")]
    [SerializeField] private float spawnIntervalDistance = 65f; // İki iksir arasındaki mesafe
    [SerializeField] private float spawnAheadDistance = 120f;
    [SerializeField] private float yOffset = 0.8f;

    [Header("Şerit Koordinatları (Z)")]
    [SerializeField] private float solSeritZ = -6.45f;
    [SerializeField] private float ortaSeritZ = 2.23f;
    [SerializeField] private float sagSeritZ = 11.11f;

    private ZeminKontrol zeminKontrol;
    private float nextSpawnX;
    private bool isInitialized = false;

    private bool IsServerOrOffline
    {
        get
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                return true;
            return NetworkManager.Singleton.IsServer;
        }
    }

    private void Start()
    {
        zeminKontrol = ZeminKontrol.Instance;
        TryInitializeSpawnX();
    }

    private void TryInitializeSpawnX()
    {
        var leader = KarakterKontrol.GetLeadingPlayer();
        if (leader != null)
        {
            nextSpawnX = leader.transform.position.x - 45f;
            isInitialized = true;
        }
    }

    private void Update()
    {
        if (!IsServerOrOffline) return;
        if (potionPrefabs == null || potionPrefabs.Length == 0) return;

        if (zeminKontrol == null) zeminKontrol = ZeminKontrol.Instance;

        var leader = KarakterKontrol.GetLeadingPlayer();
        if (leader == null) return;

        if (!isInitialized)
        {
            nextSpawnX = leader.transform.position.x - 45f;
            isInitialized = true;
        }

        float spawnThresholdX = leader.transform.position.x - spawnAheadDistance;

        while (spawnThresholdX <= nextSpawnX)
        {
            SpawnRandomPotion(nextSpawnX);
            nextSpawnX -= spawnIntervalDistance;
        }
    }

    private void SpawnRandomPotion(float spawnX)
    {
        int targetLane = Random.Range(0, 3);
        GameObject selectedPrefab = potionPrefabs[Random.Range(0, potionPrefabs.Length)];

        Vector3 spawnPos = Vector3.zero;
        YolBileseni activeRoad = (zeminKontrol != null) ? zeminKontrol.GetRoadAtX(spawnX) : null;

        if (activeRoad != null)
        {
            spawnPos = activeRoad.GetLanePosition(targetLane, spawnX);
            spawnPos.y += yOffset;
        }
        else
        {
            float baseHeight = (zeminKontrol != null) ? zeminKontrol.zeminTabanY : 0f;
            float secilenZ = ortaSeritZ;
            if (targetLane == 0) secilenZ = solSeritZ;
            else if (targetLane == 2) secilenZ = sagSeritZ;

            spawnPos = new Vector3(spawnX, baseHeight + yOffset, secilenZ);
        }

        GameObject spawned = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
        {
            var netObj = spawned.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn(true);
            }
        }
    }
}