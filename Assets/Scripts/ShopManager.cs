using System;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [Header("UI Kapsayıcıları")]
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject shopCardPrefab;
    [SerializeField] private GameObject loadingIndicator;
    [SerializeField] private GameObject emptyShopText;

    [Header("Görsel Ayarları")]
    [SerializeField] private Sprite defaultProductSprite;

    [Header("Ödüllü Reklam")]
    [SerializeField] private RewardedAdManager rewardedAdManager;

    [Header("Çevrimdışı / Yedek JSON (Fallback)")]
    [TextArea(6, 12)]
    [SerializeField] private string fallbackJson = "{\"items\":[" +
        "{\"id\":\"coins_free\",\"title\":\"Günlük Hediye\",\"priceText\":\"ÜCRETSİZ\",\"isRealMoney\":false}," +
        "{\"id\":\"coins_pack_1\",\"title\":\"Başlangıç Paketi\",\"priceText\":\"₺19.99\",\"isRealMoney\":true}," +
        "{\"id\":\"coins_pack_2\",\"title\":\"Süper Paket\",\"priceText\":\"₺49.99\",\"isRealMoney\":true}" +
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
        string cachedJson = PlayerPrefs.GetString("Cached_Shop_Json", "");

        if (string.IsNullOrEmpty(cachedJson))
        {
            cachedJson = fallbackJson;
        }

        ClearAndBuildShopCards(cachedJson);
    }

    private void ClearAndBuildShopCards(string json)
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);

        if (emptyShopText != null)
            emptyShopText.SetActive(false);

        if (contentContainer != null)
        {
            for (int i = contentContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(contentContainer.GetChild(i).gameObject);
            }
        }

        BuildShopCards(json);
    }

    private void BuildShopCards(string json)
    {
        ShopListWrapper wrapper = ParseShopJson(json);

        if (wrapper == null ||
            wrapper.items == null ||
            wrapper.items.Count == 0)
        {
            wrapper = ParseShopJson(fallbackJson);
        }

        if (wrapper == null ||
            wrapper.items == null ||
            wrapper.items.Count == 0)
        {
            if (emptyShopText != null)
                emptyShopText.SetActive(true);

            return;
        }

        if (shopCardPrefab == null || contentContainer == null)
        {
            Debug.LogError(
                "[ShopManager] Kart prefabı veya Content Container atanmamış."
            );
            return;
        }

        foreach (var item in wrapper.items)
        {
            if (item == null)
                continue;

            GameObject cardObj = Instantiate(
                shopCardPrefab,
                contentContainer
            );

            cardObj.transform.localScale = Vector3.one;

            ShopCardUI cardUI = cardObj.GetComponent<ShopCardUI>();

            if (cardUI != null)
            {
                cardUI.Setup(
                    item,
                    OnBuyItemClicked,
                    defaultProductSprite
                );
            }
        }

        if (emptyShopText != null)
        {
            emptyShopText.SetActive(wrapper.items.Count == 0);
        }
    }

    private ShopListWrapper ParseShopJson(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return null;

        try
        {
            string trimmed = rawJson.Trim();

            if (trimmed.StartsWith("["))
            {
                trimmed = "{\"items\":" + trimmed + "}";
            }

            return JsonUtility.FromJson<ShopListWrapper>(trimmed);
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                $"[ShopManager] JSON parse hatası: {ex.Message}"
            );
            return null;
        }
    }

    private void OnBuyItemClicked(ShopItemData item)
    {
        if (item == null)
            return;

        Debug.Log(
            $"[ShopManager] Kart tıklandı: {item.title} (ID: {item.id})"
        );

        if (item.isRealMoney)
        {
            if (IAPManager.Instance != null &&
                IAPManager.Instance.IsInitialized)
            {
                IAPManager.Instance.BuyProduct(item.id);
            }
            else
            {
                Debug.LogWarning(
                    "[ShopManager] IAP sistemi henüz hazır değil!"
                );
            }

            return;
        }

        if (item.id == "coins_free")
        {
            RewardedAdManager manager = rewardedAdManager;

            // Ana menü tekrar yüklendiğinde kalıcı Ads objesini de bulur.
            if (manager == null)
            {
                manager = RewardedAdManager.Instance;
            }

            if (manager == null)
            {
                Debug.LogWarning(
                    "[ShopManager] RewardedAdManager sahneye eklenmemiş."
                );
                return;
            }

            // Altın burada verilmez.
            // Reklamın OnAdRewarded bildirimi altını verir.
            manager.ShowForGold();
        }
    }
}