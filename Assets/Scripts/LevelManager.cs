using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Ortam Verileri (9 Adet)")]
    public OrtamVerisi[] ortamlar;

    [Header("Gelişim Ayarları")]
    public int seviyeBasinaGerekenSkor = 500; // Örn: Her 500 puanlık koşu 1 seviye atlatır

    // Kalıcı kayıt anahtarları
    private const string SeviyeKey = "OyuncuSeviyesi";
    private const string ToplamXpKey = "ToplamXp";

    public int OyuncuSeviyesi { get; private set; }
    public OrtamVerisi AktifOrtam { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // MOBİL OPTİMİZASYONU: Kare hızını 60 FPS'e sabitliyoruz.
        // Doğrudan bu sahneden teste başlandığında da sınırlandırma devreye girer.
        Application.targetFrameRate = 60;

        // Kayıtlı verileri yükle (İlk kez açılıyorsa 1. seviyeden başlar)
        OyuncuSeviyesi = PlayerPrefs.GetInt(SeviyeKey, 1);
        
        // Oyun başlamadan önce ortamı belirle (Çok Önemli: Sahne yüklenirken kurulur)
        AktifOrtam = OrtamiBelirle(OyuncuSeviyesi);
    }

    private void Start()
    {
        // Belirlenen ortamın atmosferini uygula
        AtmosferiUygula(AktifOrtam);
    }

    // Oyuncu seviyesine göre 9 ortamdan uygun olanı seçer
    private OrtamVerisi OrtamiBelirle(int level)
    {
        OrtamVerisi secilen = ortamlar[0]; // Varsayılan 1. ortam
        
        for (int i = 0; i < ortamlar.Length; i++)
        {
            if (level >= ortamlar[i].minOyuncuSeviyesi)
            {
                secilen = ortamlar[i];
            }
        }
        return secilen;
    }

    private void AtmosferiUygula(OrtamVerisi o)
    {
        if (o == null) return;

        RenderSettings.skybox = o.skyboxMaterial;
        RenderSettings.fog = o.sisAktif;
        RenderSettings.fogColor = o.sisRengi;
        RenderSettings.fogDensity = o.sisYogunlugu;
        RenderSettings.ambientLight = o.ortamIsigiRengi;
        DynamicGI.UpdateEnvironment(); // Gölgeleri ve yansımaları güncelle
    }

    // Oyun bittiğinde/tamamlandığında çağrılacak metot
    public void RunTamamlandi(int kazanilanSkor)
    {
        int eskiSeviye = OyuncuSeviyesi;
        
        // Yeni kazanılan skoru/deneyimi ekle
        int toplamXp = PlayerPrefs.GetInt(ToplamXpKey, 0) + kazanilanSkor;
        PlayerPrefs.SetInt(ToplamXpKey, toplamXp);

        // Seviyeyi hesapla (Örn: Seviye = ToplamXP / 500)
        int yeniSeviye = 1 + (toplamXp / seviyeBasinaGerekenSkor);
        
        if (yeniSeviye > eskiSeviye)
        {
            OyuncuSeviyesi = yeniSeviye;
            PlayerPrefs.SetInt(SeviyeKey, OyuncuSeviyesi);
            PlayerPrefs.Save();

            Debug.Log($"TEBRİKLER! Seviye Atladınız. Yeni Seviye: {yeniSeviye}");

            // Yeni seviye ile ortam değişti mi kontrolü
            OrtamVerisi yeniOrtam = OrtamiBelirle(yeniSeviye);
            if (yeniOrtam != AktifOrtam)
            {
                Debug.Log($"YENİ ORTAM AÇILDI: {yeniOrtam.name}! Bir sonraki oyunda aktif olacak.");
            }
        }
    }
}