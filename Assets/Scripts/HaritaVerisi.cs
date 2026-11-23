using UnityEngine;

[CreateAssetMenu(
    fileName = "Harita_",
    menuName = "Harita Sistemi/Harita Verisi")]
public class HaritaVerisi : ScriptableObject
{
    [Header("Kimlik")]
    [Tooltip("Benzersiz ve sonradan değiştirilmeyen kimlik. Örnek: orman")]
    public string haritaId;

    public string haritaAdi;

    [TextArea]
    public string aciklama;

    [Header("Kart")]
    public Sprite kartGorseli;

    [Header("Zemin")]
    [Tooltip("Bu harita seçildiğinde üretilecek yol prefabları.")]
    public GameObject[] yolPrefablari;

    public bool Kullanilabilir
    {
        get
        {
            if (string.IsNullOrWhiteSpace(haritaId) ||
                yolPrefablari == null)
            {
                return false;
            }

            foreach (GameObject prefab in yolPrefablari)
            {
                if (prefab != null)
                    return true;
            }

            return false;
        }
    }
}