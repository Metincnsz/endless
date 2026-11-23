using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "HaritaVeritabani",
    menuName = "Harita Sistemi/Harita Veritabani")]
public class HaritaVeritabani : ScriptableObject
{
    private const string SecimKey = "SecilenHaritaId";

    public HaritaVerisi[] haritalar;

    public List<HaritaVerisi> KullanilabilirHaritalariGetir()
    {
        var sonuc = new List<HaritaVerisi>();
        var kimlikler = new HashSet<string>();

        if (haritalar == null)
            return sonuc;

        foreach (HaritaVerisi harita in haritalar)
        {
            if (harita == null || !harita.Kullanilabilir)
                continue;

            // Aynı kimliğin iki ayrı karta dönüşmesini önle.
            if (kimlikler.Add(harita.haritaId))
                sonuc.Add(harita);
        }

        return sonuc;
    }

    public HaritaVerisi SecilenHaritayiGetir()
    {
        List<HaritaVerisi> liste = KullanilabilirHaritalariGetir();

        if (liste.Count == 0)
            return null;

        string kayitliId = PlayerPrefs.GetString(SecimKey, "");

        foreach (HaritaVerisi harita in liste)
        {
            if (harita.haritaId == kayitliId)
                return harita;
        }

        // İlk açılışta veya kayıtlı harita kaldırılmışsa:
        return liste[0];
    }

    public void HaritaSec(HaritaVerisi harita)
    {
        if (harita == null)
            return;

        foreach (HaritaVerisi aday in KullanilabilirHaritalariGetir())
        {
            if (aday == harita)
            {
                PlayerPrefs.SetString(SecimKey, harita.haritaId);
                PlayerPrefs.Save();
                return;
            }
        }
    }

    private void OnValidate()
    {
        if (haritalar == null)
            return;

        var kimlikler = new HashSet<string>();

        foreach (HaritaVerisi harita in haritalar)
        {
            if (harita == null)
                continue;

            if (string.IsNullOrWhiteSpace(harita.haritaId))
            {
                Debug.LogWarning(
                    $"{harita.name}: Harita ID boş.",
                    harita);
            }
            else if (!kimlikler.Add(harita.haritaId))
            {
                Debug.LogWarning(
                    $"Tekrarlanan Harita ID: {harita.haritaId}",
                    this);
            }
        }
    }
}