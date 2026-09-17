using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EventCardUI : MonoBehaviour
{
    [Header("UI Elemanları")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Button claimRewardButton;
    [SerializeField] private TMP_Text claimButtonText;

    private EventItemData eventData;
    private DateTime endUtc;
    private bool isClaimed;

    public void Setup(EventItemData data, int currentProgress, bool claimed)
    {
        eventData = data;
        isClaimed = claimed;

        if (titleText != null) titleText.text = data.title;
        if (descriptionText != null) descriptionText.text = data.description;

        // Bitiş saatini UTC olarak ayrıştır
        if (!DateTime.TryParse(data.endTimeUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out endUtc))
        {
            endUtc = DateTime.UtcNow.AddHours(24);
        }

        // İlerleme çubuğunu ayarla
        if (progressSlider != null)
        {
            progressSlider.minValue = 0;
            progressSlider.maxValue = data.targetAmount;
            progressSlider.value = Mathf.Min(currentProgress, data.targetAmount);
        }

        if (progressText != null)
        {
            progressText.text = $"{Mathf.Min(currentProgress, data.targetAmount)} / {data.targetAmount}";
        }

        // Buton durumu
        bool isCompleted = currentProgress >= data.targetAmount;
        UpdateButtonState(isCompleted, isClaimed);

        if (claimRewardButton != null)
        {
            claimRewardButton.onClick.RemoveAllListeners();
            claimRewardButton.onClick.AddListener(OnClaimClicked);
        }
    }

    private void UpdateButtonState(bool isCompleted, bool claimed)
    {
        if (claimRewardButton == null) return;

        if (claimed)
        {
            claimRewardButton.interactable = false;
            if (claimButtonText != null) claimButtonText.text = "Alındı";
        }
        else if (isCompleted)
        {
            claimRewardButton.interactable = true;
            if (claimButtonText != null) claimButtonText.text = "Ödülü Al";
        }
        else
        {
            claimRewardButton.interactable = false;
            if (claimButtonText != null) claimButtonText.text = "Devam Ediyor";
        }
    }

    private void OnClaimClicked()
    {
        if (isClaimed) return;

        isClaimed = true;
        PlayerPrefs.SetInt($"Event_{eventData.id}_Claimed", 1);
        PlayerPrefs.Save();

        // Ödülü mevcut GoldManager sistemine ver
        if (GoldManager.Instance != null)
        {
            GoldManager.Instance.AddGold(eventData.rewardAmount);
            GoldManager.Instance.SaveRunGold();
        }

        UpdateButtonState(true, true);
    }

    private void Update()
    {
        if (timerText == null) return;

        TimeSpan remaining = endUtc - DateTime.UtcNow;
        if (remaining.TotalSeconds > 0)
        {
            timerText.text = $"Kalan: {(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }
        else
        {
            timerText.text = "Süre Bitti";
            if (claimRewardButton != null) claimRewardButton.interactable = false;
        }
    }
}