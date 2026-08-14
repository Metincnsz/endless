using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using TMPro;

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance { get; private set; }

    [Header("UI Referansı")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    private const string LanguagePrefKey = "SelectedLanguageCode";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private IEnumerator Start()
    {
        // Unity Localization başlangıcını bekle
        yield return LocalizationSettings.InitializationOperation;

        // Kaydedilmiş dili yükle (varsayılan 'tr')
        string savedCode = PlayerPrefs.GetString(LanguagePrefKey, "tr");
        SetLanguage(savedCode);

        // Dropdown hazırla
        SetupDropdown();
    }

    public void OnDropdownValueChanged(int index)
    {
        if (index == 0)
        {
            SetLanguage("tr");
        }
        else if (index == 1)
        {
            SetLanguage("en");
        }
    }

    public void SetLanguage(string localeCode)
    {
        var locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
        if (locale != null)
        {
            LocalizationSettings.SelectedLocale = locale;
            PlayerPrefs.SetString(LanguagePrefKey, localeCode);
            PlayerPrefs.Save();
        }
    }

    private void SetupDropdown()
    {
        if (languageDropdown == null) return;

        languageDropdown.onValueChanged.RemoveAllListeners();
        languageDropdown.ClearOptions();

        var options = new List<TMP_Dropdown.OptionData>
        {
            new TMP_Dropdown.OptionData("Türkçe"),
            new TMP_Dropdown.OptionData("English")
        };

        languageDropdown.AddOptions(options);

        string currentCode = PlayerPrefs.GetString(LanguagePrefKey, "tr");
        languageDropdown.value = (currentCode == "en") ? 1 : 0;
        languageDropdown.RefreshShownValue();

        languageDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }
}
