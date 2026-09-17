using UnityEngine;

[CreateAssetMenu(fileName = "Data_BonusGold", menuName = "Oyun/İksirler/Bonus Altın")]
public class BonusGoldPotionData : PotionData
{
    [Header("Bonus Altın Ayarları 🪙")]
    [Tooltip("İksir aktifken toplanan altınların kaç katı alınacağı (Varsayılan: 2)")]
    public int goldMultiplier = 2;

    public override int GoldMultiplier => goldMultiplier;

    public override void OnStart(PlayerPotionController controller)
    {
        Debug.Log($"[İksir] Bonus Altın (x{goldMultiplier}) aktif edildi! Süre: {duration}s");
    }

    public override void OnEnd(PlayerPotionController controller)
    {
        Debug.Log("[İksir] Bonus Altın süresi sona erdi.");
    }
}