using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Seviyeye Bağlı Ortam Verileri")]
    public OrtamVerisi[] ortamlar;

    [Header("Seçilebilir Zemin Haritaları")]
    [SerializeField] private HaritaVeritabani haritaVeritabani;

    [Header("Gelişim Ayarları")]
    public int seviyeBasinaGerekenSkor = 500;

    private const string SeviyeKey = "OyuncuSeviyesi";
    private const string ToplamXpKey = "ToplamXp";

    public int OyuncuSeviyesi { get; private set; }
    public OrtamVerisi AktifOrtam { get; private set; }

    private OrtamVerisi temelOrtam;
    private OrtamVerisi oyunOrtamKopyasi;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Application.targetFrameRate = 60;
        OyuncuSeviyesi = PlayerPrefs.GetInt(SeviyeKey, 1);

        temelOrtam = OrtamiBelirle(OyuncuSeviyesi);
        AktifOrtam = temelOrtam;

        SecilenHaritaninYollariniUygula();
    }

    private void Start()
    {
        // Mevcut davranış korunur:
        // atmosfer oyuncunun seviyesine bağlıdır.
        AtmosferiUygula(temelOrtam);
    }

    private HaritaVerisi OyunHaritasiniGetir()
    {
        MultiplayerRoomManager room =
            MultiplayerRoomManager.Instance;

        if (room != null && room.ActiveSession != null)
        {
            // Multiplayer: kurucunun oda oluştururken seçtiği harita.
            return MultiplayerRoomManager.GetRoomMap(
                haritaVeritabani,
                room.ActiveSession);
        }

        // Tek oyunculu: Eşyalarım panelindeki kişisel seçim.
        return haritaVeritabani != null
            ? haritaVeritabani.SecilenHaritayiGetir()
            : null;
    }

    private void SecilenHaritaninYollariniUygula()
    {
        if (haritaVeritabani == null)
        {
            Debug.LogError(
                "[LevelManager] Harita veritabanını Inspector'dan bağla.",
                this);
            return;
        }

        HaritaVerisi harita = OyunHaritasiniGetir();

        if (harita == null)
        {
            Debug.LogWarning(
                "[LevelManager] Kullanılabilir harita bulunamadı.",
                this);
            return;
        }

        var yollar = new List<GameObject>();

        foreach (GameObject prefab in harita.yolPrefablari)
        {
            if (prefab != null)
                yollar.Add(prefab);
        }

        if (yollar.Count == 0)
            return;

        if (temelOrtam == null)
        {
            Debug.LogError(
                "[LevelManager] Ortamlar listesinde geçerli " +
                "bir OrtamVerisi gerekli.",
                this);
            return;
        }

        oyunOrtamKopyasi = Instantiate(temelOrtam);
        oyunOrtamKopyasi.name =
            temelOrtam.name + "_Harita_" + harita.haritaId;

        oyunOrtamKopyasi.yolPrefablari = yollar.ToArray();
        AktifOrtam = oyunOrtamKopyasi;

        Debug.Log(
            "[LevelManager] Oyun haritası: " + harita.haritaAdi +
            ", ID: " + harita.haritaId +
            ", Yol çeşidi: " + yollar.Count,
            this);
    }

    private OrtamVerisi OrtamiBelirle(int level)
    {
        if (ortamlar == null || ortamlar.Length == 0)
            return null;

        OrtamVerisi ilkGecerli = null;
        OrtamVerisi secilen = null;

        foreach (OrtamVerisi ortam in ortamlar)
        {
            if (ortam == null)
                continue;

            if (ilkGecerli == null)
                ilkGecerli = ortam;

            if (level >= ortam.minOyuncuSeviyesi &&
                (secilen == null ||
                 ortam.minOyuncuSeviyesi >= secilen.minOyuncuSeviyesi))
            {
                secilen = ortam;
            }
        }

        return secilen != null ? secilen : ilkGecerli;
    }

    private void AtmosferiUygula(OrtamVerisi ortam)
    {
        if (ortam == null)
            return;

        RenderSettings.skybox = ortam.skyboxMaterial;
        RenderSettings.fog = ortam.sisAktif;
        RenderSettings.fogColor = ortam.sisRengi;
        RenderSettings.fogDensity = ortam.sisYogunlugu;
        RenderSettings.ambientLight = ortam.ortamIsigiRengi;

        DynamicGI.UpdateEnvironment();
    }

    public void RunTamamlandi(int kazanilanSkor)
    {
        int eskiSeviye = OyuncuSeviyesi;

        int toplamXp =
            PlayerPrefs.GetInt(ToplamXpKey, 0) +
            Mathf.Max(0, kazanilanSkor);

        PlayerPrefs.SetInt(ToplamXpKey, toplamXp);

        int gerekenSkor = Mathf.Max(1, seviyeBasinaGerekenSkor);
        int yeniSeviye = 1 + toplamXp / gerekenSkor;

        if (yeniSeviye > eskiSeviye)
        {
            OyuncuSeviyesi = yeniSeviye;
            PlayerPrefs.SetInt(SeviyeKey, OyuncuSeviyesi);

            Debug.Log(
                "TEBRİKLER! Yeni Seviye: " + OyuncuSeviyesi);

            OrtamVerisi yeniOrtam = OrtamiBelirle(yeniSeviye);

            if (yeniOrtam != temelOrtam && yeniOrtam != null)
            {
                Debug.Log(
                    "Yeni seviye ortamı: " + yeniOrtam.name +
                    ". Bir sonraki oyunda uygulanacak; " +
                    "seçilen zemin haritası korunacak.");
            }
        }

        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (oyunOrtamKopyasi != null)
            Destroy(oyunOrtamKopyasi);
    }
}