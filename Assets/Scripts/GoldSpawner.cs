using UnityEngine;
using Unity.Netcode;

public class GoldSpawner : MonoBehaviour
{
    [Header("Altın Ayarları")]
    [SerializeField] private GameObject goldPrefab;
    [SerializeField] private int rowCount = 3;             // Bir grupta peş peşe dizilecek altın sayısı
    [SerializeField] private float goldSpacing = 3.5f;     // Peş peşe altınlar arası mesafe (X ekseni)
    [SerializeField] private float yOffset = 0.5f;         // Zeminden yükseklik ofseti

    [Header("Spawn Zamanlama Ayarları")]
    [SerializeField] private float spawnIntervalDistance = 30f; // İki altın grubu arasındaki mesafe
    [SerializeField] private float spawnAheadDistance = 120f;   // Karakterin ne kadar ilerisine spawn edilsin

    [Header("Yedek Şerit Koordinatları (Z)")]
    [SerializeField] private float solSeritZ = -6.45f;
    [SerializeField] private float ortaSeritZ = 2.23f;
    [SerializeField] private float sagSeritZ = 11.11f;

    private ZeminKontrol zeminKontrol;
    private float nextSpawnX;
    private bool isInitialized = false;

    // Hem Tek Oyunculu (Offline) hem de Multiplayer Host/Server modunu destekleyen kontrol
    private bool IsServerOrOffline
    {
        get
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                return true; // Çevrimdışı Tek Oyunculu Mod
            return NetworkManager.Singleton.IsServer; // Multiplayer Sunucu/Host
        }
    }

    void Start()
    {
        zeminKontrol = ZeminKontrol.Instance;
        TryInitializeSpawnX();
    }

    private void TryInitializeSpawnX()
    {
        var leader = KarakterKontrol.GetLeadingPlayer();
        if (leader != null)
        {
            // İlk altın grubunu karakterin 25 birim önünden başlat
            nextSpawnX = leader.transform.position.x - 25f;
            isInitialized = true;
        }
    }

    void Update()
    {
        // Client modundaki oyuncular altın üretmez (Sadece Host veya Tek Oyunculu üretir)
        if (!IsServerOrOffline) return;

        if (goldPrefab == null) return;

        if (zeminKontrol == null)
        {
            zeminKontrol = ZeminKontrol.Instance;
        }

        var leader = KarakterKontrol.GetLeadingPlayer();
        if (leader == null) return;

        Transform playerTransform = leader.transform;

        if (!isInitialized)
        {
            nextSpawnX = playerTransform.position.x - 25f;
            isInitialized = true;
        }

        // Karakter -X yönünde koştuğu için eşik değer hesabı
        float spawnThresholdX = playerTransform.position.x - spawnAheadDistance;

        // Karakter ilerledikçe önündeki mesafeye altın grupları üretilir
        while (spawnThresholdX <= nextSpawnX)
        {
            SpawnGoldGroup(nextSpawnX);
            nextSpawnX -= spawnIntervalDistance;
        }
    }

    private void SpawnGoldGroup(float groupStartX)
    {
        // 0: Sol, 1: Orta, 2: Sağ şerit rastgele seçilir
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
                float baseHeight = (zeminKontrol != null) ? zeminKontrol.zeminTabanY : 0f;
                float secilenZ = ortaSeritZ;
                if (targetLane == 0) secilenZ = solSeritZ;
                else if (targetLane == 2) secilenZ = sagSeritZ;

                spawnPos = new Vector3(targetX, baseHeight + yOffset, secilenZ);
            }

            // Altın nesnesini oluştur
            GameObject yeniAltin = Instantiate(goldPrefab, spawnPos, goldPrefab.transform.rotation);

            // Eğer oyun Multiplayer modundaysa ve Host isek altını ağda yayınla
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
            {
                var netObj = yeniAltin.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    netObj.Spawn(true);
                }
            }
        }
    }
}