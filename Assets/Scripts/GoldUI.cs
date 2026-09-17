using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class GoldUI : MonoBehaviour
{
    public enum DisplayMode
    {
        CurrentRunGold, // O anki koşuda toplanan altın (Oyun içi)
        TotalGold       // Toplam birikmiş altın (Ana Menü / Mağaza)
    }

    [Header("Görünüm Ayarları")]
    [SerializeField] private DisplayMode displayMode = DisplayMode.CurrentRunGold;
    [SerializeField] private string prefix = "ALTIN: ";

    [Header("UI Referansları")]
    [SerializeField] private TextMeshProUGUI goldText;

    private int cachedGold = 0;

    private void Awake()
    {
        if (goldText == null)
        {
            goldText = GetComponent<TextMeshProUGUI>();
            if (goldText == null)
            {
                goldText = GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }
    }

    private void OnEnable()
    {
        if (displayMode == DisplayMode.CurrentRunGold)
        {
            GoldManager.OnCurrentGoldChanged += UpdateGoldUI;
        }
        else
        {
            GoldManager.OnTotalGoldChanged += UpdateGoldUI;
        }

        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        RefreshUI();
    }

    private void OnDisable()
    {
        if (displayMode == DisplayMode.CurrentRunGold)
        {
            GoldManager.OnCurrentGoldChanged -= UpdateGoldUI;
        }
        else
        {
            GoldManager.OnTotalGoldChanged -= UpdateGoldUI;
        }

        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void Start()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (GoldManager.Instance != null)
        {
            int gold = (displayMode == DisplayMode.CurrentRunGold)
                ? GoldManager.Instance.GetCurrentRunGold()
                : GoldManager.Instance.GetTotalGold();

            UpdateGoldUI(gold);
        }
    }

    private void OnLocaleChanged(Locale newLocale)
    {
        UpdateGoldUI(cachedGold);
    }

    private void UpdateGoldUI(int goldAmount)
    {
        cachedGold = goldAmount;
        if (goldText != null)
        {
            string fmt = LocalizationSettings.StringDatabase.GetLocalizedString("UIStrings", "fmt_gold");
            if (string.IsNullOrEmpty(fmt))
            {
                goldText.text = $"{prefix}{goldAmount:N0}";
            }
            else
            {
                goldText.text = string.Format(fmt, goldAmount.ToString("N0"));
            }
        }
    }
}