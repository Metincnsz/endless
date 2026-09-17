using UnityEngine;
using Unity.Netcode;

public class EngelYoneticisi : NetworkBehaviour
{
    [Header("Varsayılan Referanslar (Yedek)")]
    public GameObject engelPrefab;
    public GameObject bariyerPrefab; 
    public Transform karakterTransform;

    [Header("Merkezi Engel Hız Ayarı ⚡")]
    public float engelHareketHizi = 15f;

    [Header("Mesafe Tabanlı Spawn Ayarları")]
    public float spawnUzakligi = 150f;
    public float engelAraligiMesafe = 35f;

    [Header("Yedek Şerit Koordinatları")]
    public float solSeritZ = -6.45f;
    public float ortaSeritZ = 2.23f;
    public float sagSeritZ = 11.11f;
    public float spawnYuksekligi = 0.5f;

    private ZeminKontrol zeminKontrol;
    private float sonrakiSpawnX;

    void Start()
    {
        zeminKontrol = ZeminKontrol.Instance;

        if (LevelManager.Instance != null && LevelManager.Instance.AktifOrtam != null)
        {
            OrtamVerisi ortam = LevelManager.Instance.AktifOrtam;
            engelHareketHizi = ortam.engelHizi;
            engelAraligiMesafe = ortam.spawnAraligi;
        }

        Transform lider = GetLeadingPlayerTransform();
        if (lider != null)
        {
            float ilkHedefX = lider.position.x - spawnUzakligi;
            sonrakiSpawnX = Mathf.Floor(ilkHedefX / engelAraligiMesafe) * engelAraligiMesafe;
        }
    }

    void Update()
    {
        // Eşleşme aktifse engelleri sadece SUNUCU üretir
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !IsServer)
            return;

        Transform lider = GetLeadingPlayerTransform();
        // Oyuncu öldüyse veya lider bulunamadıysa engel üretimi durur
        if (lider == null) return;

        float spawnPenceresiUcuX = lider.position.x - spawnUzakligi;

        if (spawnPenceresiUcuX <= sonrakiSpawnX)
        {
            SpawnTekEngel(sonrakiSpawnX);
            sonrakiSpawnX -= engelAraligiMesafe;
        }
    }

    // Sadece HAYATTA olan oyuncuları dikkate alan lider bulma metodu
    private Transform GetLeadingPlayerTransform()
    {
        var leader = KarakterKontrol.GetLeadingPlayer();
        return leader != null ? leader.transform : null;
    }

    void SpawnTekEngel(float tamKoordinatX)
    {
        int rastgeleSerit = Random.Range(0, 3);
        Vector3 spawnPos = Vector3.zero;

        YolBileseni aktifYol = null;
        if (zeminKontrol != null)
        {
            aktifYol = zeminKontrol.GetRoadAtX(tamKoordinatX);
        }

        if (aktifYol != null)
        {
            spawnPos = aktifYol.GetLanePosition(rastgeleSerit, tamKoordinatX);
        }
        else
        {
            float secilenZ = ortaSeritZ;
            if (rastgeleSerit == 0) secilenZ = solSeritZ;
            else if (rastgeleSerit == 2) secilenZ = sagSeritZ;

            spawnPos = new Vector3(tamKoordinatX, spawnYuksekligi, secilenZ);
        }

        GameObject secilenPrefab = null;

        if (LevelManager.Instance != null && LevelManager.Instance.AktifOrtam != null)
        {
            GameObject[] engelHavuzu = LevelManager.Instance.AktifOrtam.engelPrefablari;
            if (engelHavuzu != null && engelHavuzu.Length > 0)
            {
                secilenPrefab = engelHavuzu[Random.Range(0, engelHavuzu.Length)];
            }
        }

        if (secilenPrefab == null)
        {
            if (engelPrefab != null && bariyerPrefab != null)
            {
                secilenPrefab = Random.value > 0.5f ? engelPrefab : bariyerPrefab;
            }
            else if (engelPrefab != null)
            {
                secilenPrefab = engelPrefab;
            }
            else if (bariyerPrefab != null)
            {
                secilenPrefab = bariyerPrefab;
            }
        }

        if (secilenPrefab == null) return;

        GameObject yeniEngel = Instantiate(secilenPrefab, spawnPos, secilenPrefab.transform.rotation);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && IsServer)
        {
            var netObj = yeniEngel.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn(true);
            }
        }

        EngelKontrol dikenliTopScript = yeniEngel.GetComponent<EngelKontrol>();
        if (dikenliTopScript != null)
        {
            dikenliTopScript.EngelAyarlariniYap(engelHareketHizi);
            return;
        }

        BariyerKontrol bariyerScript = yeniEngel.GetComponent<BariyerKontrol>();
        if (bariyerScript != null)
        {
            bariyerScript.EngelAyarlariniYap(engelHareketHizi);
            return;
        }
    }
}