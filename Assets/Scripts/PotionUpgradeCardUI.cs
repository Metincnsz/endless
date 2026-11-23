using UnityEngine;

public class PotionUpgradeCardUI : MonoBehaviour
{
    [Header("Kart Referansları")]
    [SerializeField] private TMPro.TextMeshProUGUI nameText;
    [SerializeField] private TMPro.TextMeshProUGUI descriptionText;
    [SerializeField] private TMPro.TextMeshProUGUI levelText;
    [SerializeField] private TMPro.TextMeshProUGUI durationText;
    [SerializeField] private TMPro.TextMeshProUGUI previewText;
    [SerializeField] private TMPro.TextMeshProUGUI buttonText;
    [SerializeField] private TMPro.TextMeshProUGUI symbolText;
    [SerializeField] private UnityEngine.UI.Image portrait;
    [SerializeField] private UnityEngine.UI.Image[] levelSegments;
    [SerializeField] private UnityEngine.UI.Button upgradeButton;

    private PotionData potion;
    public PotionData Potion => potion;

    private void Awake()
    {
        if (upgradeButton != null) upgradeButton.onClick.AddListener(Upgrade);
    }

    private void OnEnable()
    {
        GoldManager.OnTotalGoldChanged += OnGoldChanged;
        PotionUpgradeProgress.OnLevelChanged += OnLevelChanged;
        Refresh();
    }

    private void OnDisable()
    {
        GoldManager.OnTotalGoldChanged -= OnGoldChanged;
        PotionUpgradeProgress.OnLevelChanged -= OnLevelChanged;
    }

    private void OnDestroy()
    {
        if (upgradeButton != null) upgradeButton.onClick.RemoveListener(Upgrade);
    }

    public void Bind(PotionData data)
    {
        potion = data;
        if (potion == null) return;
        nameText.text = potion.potionName;
        descriptionText.text = GetDescription(potion);
        portrait.sprite = potion.icon;
        portrait.enabled = potion.icon != null;
        symbolText.gameObject.SetActive(potion.icon == null);
        symbolText.text = GetSymbol(potion);
        Refresh();
    }

    private void OnGoldChanged(int gold) => Refresh();
    private void OnLevelChanged(PotionData data)
    {
        if (data == potion) Refresh();
    }

    private void Upgrade()
    {
        if (!PotionUpgradeProgress.TryUpgrade(potion)) Refresh();
    }

    private void Refresh()
    {
        if (potion == null) return;
        int level = PotionUpgradeProgress.GetLevel(potion);
        bool maxed = level >= PotionUpgradeProgress.MaxLevel;
        bool affordable = GoldManager.Instance.GetTotalGold() >= PotionUpgradeProgress.UpgradeCost;
        levelText.text = "SEVİYE " + level + " / " + PotionUpgradeProgress.MaxLevel;
        for (int i = 0; i < levelSegments.Length; i++)
            levelSegments[i].color = i < level ? new Color(0.3f, 0.9f, 0.85f) : new Color(0.15f, 0.23f, 0.33f);

        var life = potion as ExtraLifePotionData;
        if (life != null)
        {
            int amount = PotionUpgradeProgress.GetExtraLifeAmount(life);
            durationText.text = amount + " CAN HAKKI";
            previewText.text = maxed ? "Kullanılana kadar kalır" : "Sonraki seviye: " + (amount + 1) + " can";
        }
        else
        {
            float duration = PotionUpgradeProgress.GetDuration(potion);
            durationText.text = "SÜRE: " + duration.ToString("0.#") + " sn";
            previewText.text = maxed ? "En yüksek süreye ulaşıldı" : "Sonraki seviye: " + (duration + PotionUpgradeProgress.SecondsPerLevel).ToString("0.#") + " sn";
        }
        upgradeButton.interactable = !maxed && affordable;
        buttonText.text = maxed ? "MAKSİMUM SEVİYE" : affordable ? "YÜKSELT\n1.000 ALTIN" : "YETERSİZ ALTIN\n1.000 ALTIN";
    }

    private static string GetSymbol(PotionData data)
    {
        if (data is SpeedPotionData) return "HIZ";
        if (data is MagnetPotionData) return "M";
        if (data is ExtraLifePotionData) return "+1";
        if (data is GlidePotionData) return "S";
        if (data is InvulnerabilityPotionData) return "K";
        if (data is BouncePotionData) return "Z";
        return "x2";
    }

    private static string GetDescription(PotionData data)
    {
        if (data is SpeedPotionData) return "Koşu hızını artırır.";
        if (data is MagnetPotionData) return "Yakındaki altınları sana çeker.";
        if (data is ExtraLifePotionData) return "Çarpışmada seni kurtaran ek can verir. Seviye arttıkça can sayısı artar.";
        if (data is GlidePotionData) return "Yükselerek engellerin üzerinde süzülmeni sağlar.";
        if (data is InvulnerabilityPotionData) return "Engellere karşı geçici koruma sağlar.";
        if (data is BouncePotionData) return "Daha yükseğe sıçramanı sağlar.";
        return "Topladığın altınların değerini katlar.";
    }
}
