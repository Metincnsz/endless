using System;
using System.Collections.Generic;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    [Header("UI Kapsayıcıları")]
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject taskCardPrefab;
    [SerializeField] private GameObject loadingIndicator;
    [SerializeField] private GameObject noTasksText;

    [Header("Görev Türü İkonları")]
    [SerializeField] private Sprite defaultGoldTaskIcon;
    [SerializeField] private Sprite defaultDistanceTaskIcon;
    [SerializeField] private Sprite defaultPotionTaskIcon;
    [SerializeField] private Sprite defaultPlayTimeTaskIcon;
    [SerializeField] private Sprite defaultRewardIcon;

    [Header("Yedek / Çevrimdışı JSON (Fallback)")]
    [TextArea(6, 12)]
    [SerializeField] private string fallbackJson = "{\"tasks\":[" +
        "{\"id\":\"task_gold_500\",\"title\":\"500 Altın Topla\",\"description\":\"Koşuda 500 altın topla\",\"taskType\":\"gold\",\"targetAmount\":500,\"rewardAmount\":200}," +
        "{\"id\":\"task_dist_1000\",\"title\":\"1000 Metre Koş\",\"description\":\"Toplam 1000 metre mesafe katet\",\"taskType\":\"distance\",\"targetAmount\":1000,\"rewardAmount\":300}," +
        "{\"id\":\"task_potion_5\",\"title\":\"5 İksir Topla\",\"description\":\"5 adet güç iksiri bul\",\"taskType\":\"potion\",\"targetAmount\":5,\"rewardAmount\":150}," +
        "{\"id\":\"task_time_10\",\"title\":\"10 Dakika Oyna\",\"description\":\"Toplam 10 dakika hayatta kal\",\"taskType\":\"play_time\",\"targetAmount\":10,\"rewardAmount\":250}" +
        "]}";

    private void OnEnable()
    {
        UnityServicesInitializer.OnRemoteConfigFetched += OnRemoteDataUpdated;
        RefreshFromCacheOrFallback();

        // Panel açıldığında UGS hazırsa tekrar yenileme isteği at
        if (UnityServicesInitializer.IsInitialized)
        {
            UnityServicesInitializer.FetchRemoteConfigData();
        }
    }

    private void OnDisable()
    {
        UnityServicesInitializer.OnRemoteConfigFetched -= OnRemoteDataUpdated;
    }

    private void OnRemoteDataUpdated()
    {
        RefreshFromCacheOrFallback();
    }

    public void RefreshFromCacheOrFallback()
    {
        string cachedJson = PlayerPrefs.GetString("Cached_Tasks_Json", "");
        if (string.IsNullOrEmpty(cachedJson))
        {
            cachedJson = fallbackJson;
        }

        ClearAndBuildCards(cachedJson);
    }

    private void ClearAndBuildCards(string json)
    {
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        if (noTasksText != null) noTasksText.SetActive(false);

        if (contentContainer != null)
        {
            for (int i = contentContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(contentContainer.GetChild(i).gameObject);
            }
        }

        BuildCards(json);
    }

    private void BuildCards(string json)
    {
        TaskListWrapper wrapper = ParseTasksJson(json);

        if (wrapper == null || wrapper.tasks == null || wrapper.tasks.Count == 0)
        {
            wrapper = ParseTasksJson(fallbackJson);
        }

        if (wrapper == null || wrapper.tasks == null || wrapper.tasks.Count == 0)
        {
            if (noTasksText != null) noTasksText.SetActive(true);
            return;
        }

        foreach (var task in wrapper.tasks)
        {
            if (task == null) continue;

            GameObject cardObj = Instantiate(taskCardPrefab, contentContainer);
            cardObj.transform.localScale = Vector3.one;

            TaskCardUI cardUI = cardObj.GetComponent<TaskCardUI>();
            if (cardUI != null)
            {
                int currentProgress = PlayerPrefs.GetInt($"Task_{task.id}_Progress", 0);
                bool isClaimed = PlayerPrefs.GetInt($"Task_{task.id}_Claimed", 0) == 1;

                Sprite taskIcon = defaultGoldTaskIcon;
                switch (task.taskType?.ToLower())
                {
                    case "distance":
                        taskIcon = defaultDistanceTaskIcon != null ? defaultDistanceTaskIcon : defaultGoldTaskIcon;
                        break;
                    case "potion":
                        taskIcon = defaultPotionTaskIcon != null ? defaultPotionTaskIcon : defaultGoldTaskIcon;
                        break;
                    case "play_time":
                    case "time":
                        taskIcon = defaultPlayTimeTaskIcon != null ? defaultPlayTimeTaskIcon : defaultGoldTaskIcon;
                        break;
                    default:
                        taskIcon = defaultGoldTaskIcon;
                        break;
                }

                cardUI.Setup(task, currentProgress, isClaimed, taskIcon, defaultRewardIcon);
            }
        }

        if (noTasksText != null) noTasksText.SetActive(wrapper.tasks.Count == 0);
    }

    private TaskListWrapper ParseTasksJson(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson)) return null;

        try
        {
            string trimmed = rawJson.Trim();
            if (trimmed.StartsWith("["))
            {
                trimmed = "{\"tasks\":" + trimmed + "}";
            }

            return JsonUtility.FromJson<TaskListWrapper>(trimmed);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[TaskManager] JSON parse hatası: {ex.Message}");
            return null;
        }
    }

    public static void AddProgressToTasks(string taskType, int amount)
    {
        if (amount <= 0) return;

        string json = PlayerPrefs.GetString("Cached_Tasks_Json", "");
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            string trimmed = json.Trim();
            if (trimmed.StartsWith("["))
            {
                trimmed = "{\"tasks\":" + trimmed + "}";
            }

            TaskListWrapper wrapper = JsonUtility.FromJson<TaskListWrapper>(trimmed);
            if (wrapper == null || wrapper.tasks == null) return;

            foreach (var task in wrapper.tasks)
            {
                if (string.Equals(task.taskType, taskType, StringComparison.OrdinalIgnoreCase))
                {
                    int current = PlayerPrefs.GetInt($"Task_{task.id}_Progress", 0);
                    if (current < task.targetAmount)
                    {
                        current = Mathf.Min(current + amount, task.targetAmount);
                        PlayerPrefs.SetInt($"Task_{task.id}_Progress", current);
                    }
                }
            }
            PlayerPrefs.Save();
        }
        catch (Exception) { }
    }
}