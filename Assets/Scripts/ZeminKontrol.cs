using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode; // 1. Netcode kütüphanesini ekledik

public class ZeminKontrol : NetworkBehaviour // 2. Sınıfı NetworkBehaviour yaptık
{
    [Header("Yol / Zemin Ayarları")]
    [Tooltip("Varsayılan yol prefabı (Eğer ortam verisinde yol atanmamışsa kullanılır).")]
    public GameObject yolPrefab;

    [Tooltip("Sahnedeki ilk yolun en ucuna koyduğunuz boş nesneyi (SpawnPoint) buraya sürükleyin.")]
    public Transform ilkSpawnNoktasi;

    [Tooltip("Oyun başında peş peşe kaç adet yol dizilsin?")]
    public int baslangicYolSayisi = 5;

    [Header("Otomatik Hizalama (Pivot/SpawnPoint Bağımsız)")]
    public bool otomatikHizalama = true;
    public bool rayiOtomatikYakala = true;
    public float zeminTabanY = 0f;
    public float seritMerkeziZ = 0f;
    public float yolBindirmesi = 0f;

    private List<GameObject> aktifYollar = new List<GameObject>();
    private float frontierX;
    private Vector3 sonrakiSpawnPozisyonu;

    // 3. Oyunun online mı offline mı olduğunu kontrol eden yardımcı özellik
    private bool IsServerOrOffline => (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) || IsServer;

        void Start()
    {
        // 1. Önce referans kontrolü yapalım (Güvenlik)
        if (ilkSpawnNoktasi == null)
        {
            Debug.LogError("Hata: Lütfen sahnedeki ilk yolun içindeki SpawnPoint nesnesini ZeminKontrol scriptine sürükleyin!");
            return;
        }

        // 2. Değişkeni dış kapsamda tek bir kez tanımlayıp referansımızı alıyoruz
        GameObject eskiYolGO = ilkSpawnNoktasi.parent != null ? ilkSpawnNoktasi.parent.gameObject : ilkSpawnNoktasi.root.gameObject;

        // 3. Eğer lobi kurulmuşsa ve biz CLIENT isek: Yol üretimini durdur. 
        // Sunucu (Host) yolları üretip ekranımıza otomatik spawn edecektir.
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !IsServer)
        {
            // Client sadece sahnedeki editör tasarımı olan ilk yolu yok eder, böylece yerini ağ yolları alır.
            Destroy(eskiYolGO);
            return;
        }

        // 4. Sunucu (Host) veya Çevrimdışı (Singleplayer) ise haritayı kurmaya başla
        bool baslangicSinirOkundu = SinirlariHesapla(eskiYolGO, out Bounds baslangicSinir);

        if (otomatikHizalama)
        {
            if (baslangicSinirOkundu)
            {
                frontierX = baslangicSinir.max.x;

                if (rayiOtomatikYakala)
                {
                    zeminTabanY = baslangicSinir.min.y;
                    seritMerkeziZ = baslangicSinir.center.z;
                }
            }
            else
            {
                frontierX = ilkSpawnNoktasi.position.x;
            }
        }
        else
        {
            sonrakiSpawnPozisyonu = ilkSpawnNoktasi.position;
        }

        // 5. İlk yolu temizleyip sıfırdan üretmeye başla
        Destroy(eskiYolGO);

        if (otomatikHizalama)
        {
            YolSpawnEt();
        }

        for (int i = 0; i < baslangicYolSayisi; i++)
        {
            YolSpawnEt();
        }
    }

    public void YolSpawnEt()
    {
        // Sadece Sunucu veya Çevrimdışı Mod yol üretebilir
        if (!IsServerOrOffline) return;

        GameObject secilenYolPrefab = SecilenYoluBul();
        if (secilenYolPrefab == null)
        {
            Debug.LogError("Hata: Üretilecek bir yol prefabı bulunamadı!");
            return;
        }

        GameObject yeniYol;

        if (otomatikHizalama)
        {
            yeniYol = OtomatikHizalayarakUret(secilenYolPrefab);
        }
        else
        {
            yeniYol = SpawnPointIleUret(secilenYolPrefab);
        }

        if (yeniYol != null)
        {
            aktifYollar.Add(yeniYol);

            // 4. MULTIPLAYER AKTİFSE: Yeni üretilen yolu ağ üzerinden istemcilere kopyala
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && IsServer)
            {
                var netObj = yeniYol.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    netObj.Spawn(true); // Ağ üzerinde spawn et (Sahibi sunucudur)
                }
                else
                {
                    Debug.LogWarning($"Uyarı: Üretilen '{yeniYol.name}' üzerinde NetworkObject bileşeni yok!");
                }
            }
        }

        if (aktifYollar.Count > baslangicYolSayisi + 2)
        {
            YolSil();
        }
    }

    private GameObject SecilenYoluBul()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.AktifOrtam != null)
        {
            GameObject[] yolHavuzu = LevelManager.Instance.AktifOrtam.yolPrefablari;
            if (yolHavuzu != null && yolHavuzu.Length > 0)
            {
                return yolHavuzu[Random.Range(0, yolHavuzu.Length)];
            }
        }
        return yolPrefab;
    }

    private GameObject OtomatikHizalayarakUret(GameObject prefab)
    {
        GameObject yeniYol = Instantiate(prefab, Vector3.zero, Quaternion.identity);

        if (!SinirlariHesapla(yeniYol, out Bounds sinir))
        {
            Destroy(yeniYol);
            return SpawnPointIleUret(prefab);
        }

        Vector3 pivot = yeniYol.transform.position;

        float pivottanOnKenara = sinir.max.x - pivot.x;
        float pivottanTabana   = sinir.min.y - pivot.y;
        float pivottanMerkezeZ = sinir.center.z - pivot.z;

        float px = frontierX - pivottanOnKenara;
        float py = zeminTabanY - pivottanTabana;
        float pz = seritMerkeziZ - pivottanMerkezeZ;

        yeniYol.transform.position = new Vector3(px, py, pz);

        float uzunlukX = sinir.size.x;
        frontierX = (frontierX - uzunlukX) + yolBindirmesi;

        return yeniYol;
    }

    private GameObject SpawnPointIleUret(GameObject prefab)
    {
        GameObject yeniYol = Instantiate(prefab, sonrakiSpawnPozisyonu, Quaternion.identity);

        Transform yeniSpawnPoint = yeniYol.transform.Find("SpawnPoint");
        if (yeniSpawnPoint != null)
        {
            sonrakiSpawnPozisyonu = yeniSpawnPoint.position;
        }
        return yeniYol;
    }

    private bool SinirlariHesapla(GameObject go, out Bounds bounds)
    {
        Renderer[] rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length > 0)
        {
            bounds = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++)
            {
                bounds.Encapsulate(rends[i].bounds);
            }
            return true;
        }

        Collider[] cols = go.GetComponentsInChildren<Collider>();
        if (cols.Length > 0)
        {
            bounds = cols[0].bounds;
            for (int i = 1; i < cols.Length; i++)
            {
                bounds.Encapsulate(cols[i].bounds);
            }
            return true;
        }

        bounds = new Bounds(go.transform.position, Vector3.zero);
        return false;
    }

    void YolSil()
    {
        GameObject eskiYol = aktifYollar[0];
        aktifYollar.RemoveAt(0);
        
        if (eskiYol != null)
        {
            // 5. MULTIPLAYER AKTİFSE: Ağdan sil (Bu komut diğer oyuncuların ekranından da yolu otomatik kaldırır)
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && IsServer)
            {
                var netObj = eskiYol.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                {
                    netObj.Despawn(true); // Despawn komutu objeyi ağda yok eder ve hafızadan siler
                    return;
                }
            }
            Destroy(eskiYol);
        }
    }

    public YolBileseni GetRoadAtX(float xCoordinate)
    {
        if (aktifYollar == null || aktifYollar.Count == 0) return null;

        GameObject closestYol = null;
        float minDistance = float.MaxValue;

        foreach (var yol in aktifYollar)
        {
            if (yol == null) continue;

            float distance = Mathf.Abs(yol.transform.position.x - xCoordinate);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestYol = yol;
            }
        }

        return closestYol != null ? closestYol.GetComponent<YolBileseni>() : null;
    }
}