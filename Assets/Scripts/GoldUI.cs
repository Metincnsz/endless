using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class GoldUI : MonoBehaviour
{
    [Header("UI Referansları")]
    [SerializeField] private TextMeshProUGUI goldText;

    private int cachedGold = 0;

    private void OnEnable()
    {
        GoldManager.OnCurrentGoldChanged += UpdateGoldUI;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        GoldManager.OnCurrentGoldChanged -= UpdateGoldUI;
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void Start()
    {
        if (GoldManager.Instance != null)
        {
            UpdateGoldUI(GoldManager.Instance.GetCurrentRunGold());
        }
    }

    private void OnLocaleChanged(Locale newLocale)
    {
        UpdateGoldUI(cachedGold);
    }

    private void UpdateGoldUI(int currentGold)
    {
        cachedGold = currentGold;
        if (goldText != null)
        {
            string fmt = LocalizationSettings.StringDatabase.GetLocalizedString("UIStrings", "fmt_gold");
            if (string.IsNullOrEmpty(fmt)) fmt = "ALTIN: {0}";
            goldText.text = string.Format(fmt, currentGold.ToString("N0"));
        }
    }
}