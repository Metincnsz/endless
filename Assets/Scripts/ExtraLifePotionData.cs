using UnityEngine;

[CreateAssetMenu(fileName = "Data_ExtraLife", menuName = "Oyun/İksirler/Can Hakkı", order = 2)]
public class ExtraLifePotionData : PotionData
{
    [Header("Can Hakkı Ayarları ❤️")]
    [Tooltip("Yerden alındığında karaktere kaç can hakkı ekleneceği (Varsayılan: 1)")]
    public int canHakkiMiktari = 1;

    public override void OnStart(PlayerPotionController controller)
    {
        if (controller != null)
        {
            controller.CanHakkiEkle(canHakkiMiktari);
            Debug.Log($"[İksir] +{canHakkiMiktari} Can Hakkı eklendi! Toplam Can: {controller.CanHakki}");
        }
    }
}