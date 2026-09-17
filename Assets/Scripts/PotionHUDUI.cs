using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PotionHUDUI : MonoBehaviour
{
    [Header("UI Elemanları")]
    [SerializeField] private GameObject hudContainer;        // Bar ana paneli
    [SerializeField] private Image iconImage;                // İksir ikonu
    [SerializeField] private Image fillBarImage;             // Kalan süre barı
    [SerializeField] private TextMeshProUGUI potionNameText; // İksir adı
    [SerializeField] private TextMeshProUGUI durationText;   // Kalan süre yazısı

    private void Awake()
    {
        if (hudContainer != null) hudContainer.SetActive(false);
    }

    private void OnEnable()
    {
        PlayerPotionController.OnPotionActivated += HandlePotionActivated;
        PlayerPotionController.OnPotionTimerUpdated += HandlePotionTimerUpdated;
        PlayerPotionController.OnPotionEnded += HandlePotionEnded;
    }

    private void OnDisable()
    {
        PlayerPotionController.OnPotionActivated -= HandlePotionActivated;
        PlayerPotionController.OnPotionTimerUpdated -= HandlePotionTimerUpdated;
        PlayerPotionController.OnPotionEnded -= HandlePotionEnded;
    }

    private void HandlePotionActivated(PotionData data, float duration)
    {
        if (hudContainer != null) hudContainer.SetActive(true);

        if (iconImage != null && data.icon != null)
        {
            iconImage.sprite = data.icon;
            iconImage.color = Color.white;
        }

        if (fillBarImage != null)
        {
            fillBarImage.color = data.themeColor;
            fillBarImage.fillAmount = 1f;
        }

        if (potionNameText != null)
        {
            potionNameText.text = data.potionName;
        }
    }

    private void HandlePotionTimerUpdated(PotionData data, float remaining, float total)
    {
        if (fillBarImage != null && total > 0f)
        {
            fillBarImage.fillAmount = Mathf.Clamp01(remaining / total);
        }

        if (durationText != null)
        {
            durationText.text = remaining.ToString("F1") + "s";
        }
    }

    private void HandlePotionEnded(PotionData data)
    {
        if (hudContainer != null)
        {
            hudContainer.SetActive(false);
        }
    }
}