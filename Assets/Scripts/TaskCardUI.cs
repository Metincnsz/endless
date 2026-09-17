using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskCardUI : MonoBehaviour
{
    [Header("Sol Taraf (Görev Görseli)")]
    [SerializeField] private Image taskIconImage;

    [Header("Orta Kısım (Başlık ve İlerleme Barı)")]
    [SerializeField] private TMP_Text taskTitleText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TMP_Text progressRatioText; // "10/20", "80/100" vb.

    [Header("Sağ Taraf (Ödül / Buton)")]
    [SerializeField] private Image rewardIconImage;
    [SerializeField] private Button claimRewardButton;
    [SerializeField] private TMP_Text rewardAmountText;
    [SerializeField] private TMP_Text claimButtonStatusText;

    private TaskItemData taskData;
    private bool isClaimed;

    public void Setup(TaskItemData data, int currentProgress, bool claimed, Sprite taskIcon = null, Sprite rewardIcon = null)
    {
        taskData = data;
        isClaimed = claimed;

        // 1. Sol İkon
        if (taskIconImage != null && taskIcon != null)
        {
            taskIconImage.sprite = taskIcon;
        }

        // 2. Orta Başlık
        if (taskTitleText != null)
        {
            taskTitleText.text = data.title;
        }

        // 3. Orta İlerleme Barı ve Yazısı
        int clampedProgress = Mathf.Min(currentProgress, data.targetAmount);
        if (progressSlider != null)
        {
            progressSlider.minValue = 0;
            progressSlider.maxValue = data.targetAmount;
            progressSlider.value = clampedProgress;
        }

        if (progressRatioText != null)
        {
            progressRatioText.text = $"{clampedProgress} / {data.targetAmount}";
        }

        // 4. Sağ Taraf (Ödül Görseli ve Miktarı)
        if (rewardIconImage != null && rewardIcon != null)
        {
            rewardIconImage.sprite = rewardIcon;
        }

        if (rewardAmountText != null)
        {
            rewardAmountText.text = $"+{data.rewardAmount}";
        }

        // 5. Buton / Durum Ayarı
        bool isCompleted = currentProgress >= data.targetAmount;
        UpdateButtonState(isCompleted, isClaimed);

        if (claimRewardButton != null)
        {
            claimRewardButton.onClick.RemoveAllListeners();
            claimRewardButton.onClick.AddListener(OnClaimButtonClicked);
        }
    }

    private void UpdateButtonState(bool isCompleted, bool claimed)
    {
        if (claimRewardButton == null) return;

        if (claimed)
        {
            claimRewardButton.interactable = false;
            if (claimButtonStatusText != null) claimButtonStatusText.text = "Alındı";
        }
        else if (isCompleted)
        {
            claimRewardButton.interactable = true;
            if (claimButtonStatusText != null) claimButtonStatusText.text = "Al";
        }
        else
        {
            claimRewardButton.interactable = false;
            if (claimButtonStatusText != null) claimButtonStatusText.text = "Devam Ediyor";
        }
    }

    private void OnClaimButtonClicked()
    {
        if (isClaimed) return;

        isClaimed = true;
        PlayerPrefs.SetInt($"Task_{taskData.id}_Claimed", 1);
        PlayerPrefs.Save();

        // Ödülü GoldManager'a ekle
        if (GoldManager.Instance != null)
        {
            GoldManager.Instance.AddGold(taskData.rewardAmount);
            GoldManager.Instance.SaveRunGold();
        }

        UpdateButtonState(true, true);
    }
}