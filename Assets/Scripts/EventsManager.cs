using System;
using System.Globalization;
using UnityEngine;

public class EventsManager : MonoBehaviour
{
    [Header("UI Kapsayıcıları")]
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject eventCardPrefab;
    [SerializeField] private GameObject loadingIndicator;
    [SerializeField] private GameObject noEventsText;

    [Header("Çevrimdışı / Yedek JSON")]
    [TextArea(6, 12)]
    [SerializeField] private string fallbackJson = "{\"events\":[" +
        "{\"id\":\"gold_rush_101\",\"title\":\"Büyük Altın Avı\",\"description\":\"500 adet altın topla ve ödülü kap!\",\"targetAmount\":500,\"rewardAmount\":1500,\"startTimeUtc\":\"2025-01-01T00:00:00Z\",\"endTimeUtc\":\"2030-12-31T23:59:59Z\"}," +
        "{\"id\":\"distance_runner_102\",\"title\":\"Hızlı Koşucu\",\"description\":\"Toplam 2500 metre koş!\",\"targetAmount\":2500,\"rewardAmount\":2000,\"startTimeUtc\":\"2025-01-01T00:00:00Z\",\"endTimeUtc\":\"2030-12-31T23:59:59Z\"}" +
        "]}";

    private void OnEnable()
    {
        UnityServicesInitializer.OnRemoteConfigFetched += OnRemoteDataUpdated;
        RefreshFromCacheOrFallback();

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
        string cachedJson = PlayerPrefs.GetString("Cached_Events_Json", "");
        if (string.IsNullOrEmpty(cachedJson))
        {
            cachedJson = fallbackJson;
        }

        ClearAndBuildCards(cachedJson);
    }

    private void ClearAndBuildCards(string json)
    {
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        if (noEventsText != null) noEventsText.SetActive(false);

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
        EventListWrapper wrapper = ParseEventsJson(json);

        if (wrapper == null || wrapper.events == null || wrapper.events.Count == 0)
        {
            wrapper = ParseEventsJson(fallbackJson);
        }

        if (wrapper == null || wrapper.events == null || wrapper.events.Count == 0)
        {
            if (noEventsText != null) noEventsText.SetActive(true);
            return;
        }

        int activeCount = 0;
        DateTime nowUtc = DateTime.UtcNow;

        foreach (var ev in wrapper.events)
        {
            if (ev == null) continue;

            bool isTimeValid = true;

            if (!string.IsNullOrEmpty(ev.startTimeUtc) && !string.IsNullOrEmpty(ev.endTimeUtc))
            {
                if (DateTime.TryParse(ev.startTimeUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime start) &&
                    DateTime.TryParse(ev.endTimeUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime end))
                {
                    isTimeValid = (nowUtc >= start && nowUtc <= end);
                }
            }

            if (isTimeValid)
            {
                GameObject cardObj = Instantiate(eventCardPrefab, contentContainer);
                cardObj.transform.localScale = Vector3.one;

                EventCardUI cardUI = cardObj.GetComponent<EventCardUI>();
                if (cardUI != null)
                {
                    int currentProgress = PlayerPrefs.GetInt($"Event_{ev.id}_Progress", 0);
                    bool isClaimed = PlayerPrefs.GetInt($"Event_{ev.id}_Claimed", 0) == 1;

                    cardUI.Setup(ev, currentProgress, isClaimed);
                    activeCount++;
                }
            }
        }

        if (noEventsText != null) noEventsText.SetActive(activeCount == 0);
    }

    private EventListWrapper ParseEventsJson(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson)) return null;

        try
        {
            string trimmed = rawJson.Trim();
            if (trimmed.StartsWith("["))
            {
                trimmed = "{\"events\":" + trimmed + "}";
            }

            return JsonUtility.FromJson<EventListWrapper>(trimmed);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[EventsManager] JSON parse hatası: {ex.Message}");
            return null;
        }
    }
}