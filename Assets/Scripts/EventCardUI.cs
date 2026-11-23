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
    [SerializeField] private Button claimRewardButton;
    [SerializeField] private TMP_Text claimButtonText;

    private EventItemData eventData;
    private DateTime endUtc;
    private bool isClaimed;

    public void Setup(EventItemData data, int currentProgress, bool claimed)
    {
        eventData = data;
        isClaimed = claimed;
        expired = false;
        lastDisplayedMinutes = -1;if (titleText != null) titleText.text = data.title;
        if (descriptionText != null) descriptionText.text = data.description;

        // Yönetici ve kart aynı yayın zamanını kullanır; açılışta süre başlamaz.
        if (!data.TryGetTimeWindow(out DateTime startUtc, out endUtc))
        {
            endUtc = DateTime.MinValue;
        }

        isCompleted = currentProgress >= data.targetAmount;
        UpdateButtonState(isCompleted, isClaimed);
        UpdateCountdown();

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
        if (eventData == null || isClaimed || !isCompleted || DateTime.UtcNow >= endUtc) return;

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
        UpdateCountdown();
    }

    private bool isCompleted;
    private long lastDisplayedMinutes = -1;
    private bool expired;

    private void UpdateCountdown()
    {
        if (eventData == null) return;
        TimeSpan remaining = endUtc - DateTime.UtcNow;
        if (remaining.TotalSeconds <= 0)
        {
            if (!expired)
            {
                expired = true;
                if (timerText != null) timerText.text = "00 GÜN / 00 SAAT / 00 DAKİKA\nSüre Bitti";
                if (claimRewardButton != null) claimRewardButton.interactable = false;
                if (claimButtonText != null && !isClaimed) claimButtonText.text = "Süre Bitti";
            }
            return;
        }

        long totalMinutes = (long)Math.Ceiling(remaining.TotalMinutes);
        if (totalMinutes == lastDisplayedMinutes) return;
        lastDisplayedMinutes = totalMinutes;
        if (timerText != null)
            timerText.text = $"{totalMinutes / 1440:D2} GÜN / {(totalMinutes / 60) % 24:D2} SAAT / {totalMinutes % 60:D2} DAKİKA";
    }
}