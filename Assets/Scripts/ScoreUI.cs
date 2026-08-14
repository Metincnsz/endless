using UnityEngine;
using TMPro; // TextMeshPro namespace'i
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class ScoreUI : MonoBehaviour
{
    [Header("UI Referansları")]
    [Tooltip("Mevcut skoru gösterecek TextMeshProUGUI bileşeni")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Tooltip("En yüksek skoru gösterecek TextMeshProUGUI bileşeni")]
    [SerializeField] private TextMeshProUGUI highScoreText;

    private int cachedScore = 0;
    private int cachedHighScore = 0;

    private void OnEnable()
    {
        // ScoreManager event'ine abone ol
        ScoreManager.OnScoreChanged += UpdateUI;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        // Bellek sızıntılarını önlemek için abonelikten çık
        ScoreManager.OnScoreChanged -= UpdateUI;
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale newLocale)
    {
        UpdateUI(cachedScore, cachedHighScore);
    }

    private void UpdateUI(int currentScore, int highScore)
    {
        cachedScore = currentScore;
        cachedHighScore = highScore;

        if (scoreText != null)
        {
            string fmtScore = LocalizationSettings.StringDatabase.GetLocalizedString("UIStrings", "fmt_score");
            if (string.IsNullOrEmpty(fmtScore)) fmtScore = "SKOR: {0} m";
            scoreText.text = string.Format(fmtScore, currentScore.ToString("N0"));
        }

        if (highScoreText != null)
        {
            string fmtRekor = LocalizationSettings.StringDatabase.GetLocalizedString("UIStrings", "fmt_rekor");
            if (string.IsNullOrEmpty(fmtRekor)) fmtRekor = "REKOR: {0} m";
            highScoreText.text = string.Format(fmtRekor, highScore.ToString("N0"));
        }
    }
}