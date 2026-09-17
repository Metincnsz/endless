using UnityEngine;

[CreateAssetMenu(fileName = "Data_Speed", menuName = "Oyun/İksirler/Hızlanma", order = 1)]
public class SpeedPotionData : PotionData
{
    [Header("Hız Ayarları ⚡")]
    [Range(1f, 100f)]
    [Tooltip("Karakter hızının yüzde kaç artırılacağı. Örn: 25 girilirse hız %25 artar.")]
    public float hizArtisYuzdesi = 25f;

    // Çalışma anında eklenen hız miktarını saklamak için
    private float eklenenHizMiktari = 0f;

    public override void OnStart(PlayerPotionController controller)
    {
        if (controller != null && controller.Karakter != null)
        {
            // %25 hız artışı hesaplama
            eklenenHizMiktari = controller.Karakter.ileriKosmaHizi * (hizArtisYuzdesi / 100f);
            controller.Karakter.ileriKosmaHizi += eklenenHizMiktari;

            Debug.Log($"[İksir] Hızlanma aktif! +%{hizArtisYuzdesi} artış sağlandı. Yeni Hız: {controller.Karakter.ileriKosmaHizi} (Süre: {duration}s)");
        }
    }

    public override void OnEnd(PlayerPotionController controller)
    {
        if (controller != null && controller.Karakter != null && eklenenHizMiktari > 0f)
        {
            // Eklenen bonus hızı düşürerek eski hıza dön
            controller.Karakter.ileriKosmaHizi = Mathf.Max(0f, controller.Karakter.ileriKosmaHizi - eklenenHizMiktari);
            Debug.Log($"[İksir] Hızlanma sona erdi. Normal Hız: {controller.Karakter.ileriKosmaHizi}");

            eklenenHizMiktari = 0f;
        }
    }
}