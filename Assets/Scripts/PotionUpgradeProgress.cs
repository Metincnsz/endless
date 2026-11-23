using System;
using UnityEngine;

public static class PotionUpgradeProgress
{
    public const int MaxLevel = 5;
    public const int UpgradeCost = 1000;
    public const float SecondsPerLevel = 2f;
    private const string LevelKeyPrefix = "PotionLevel_";
    private static bool _isPurchasing;

    public static event Action<PotionData> OnLevelChanged;

    public static int GetLevel(PotionData potion)
    {
        if (potion == null || string.IsNullOrWhiteSpace(potion.potionId)) return 1;
        return Mathf.Clamp(PlayerPrefs.GetInt(LevelKeyPrefix + potion.potionId, 1), 1, MaxLevel);
    }

    public static float GetDuration(PotionData potion)
    {
        if (potion == null) return 0f;
        // Can Hakkı remains permanent until used; its upgrade increases charges, not the HUD timer.
        if (potion is ExtraLifePotionData) return potion.duration;
        return potion.duration + (GetLevel(potion) - 1) * SecondsPerLevel;
    }

    public static int GetExtraLifeAmount(ExtraLifePotionData potion)
    {
        return potion == null ? 0 : potion.canHakkiMiktari + GetLevel(potion) - 1;
    }

    public static bool TryUpgrade(PotionData potion)
    {
        if (_isPurchasing || potion == null || string.IsNullOrWhiteSpace(potion.potionId)) return false;
        int level = GetLevel(potion);
        if (level >= MaxLevel) return false;
        _isPurchasing = true;
        try
        {
            if (!GoldManager.Instance.SpendGold(UpgradeCost)) return false;
            PlayerPrefs.SetInt(LevelKeyPrefix + potion.potionId, level + 1);
            PlayerPrefs.Save();
            OnLevelChanged?.Invoke(potion);
            return true;
        }
        finally
        {
            _isPurchasing = false;
        }
    }
}
