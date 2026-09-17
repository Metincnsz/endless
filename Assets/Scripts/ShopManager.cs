using System;
using System.Collections.Generic;
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
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        if (emptyShopText != null) emptyShopText.SetActive(false);

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

        if (wrapper == null || wrapper.items == null || wrapper.items.Count == 0)
        {
            wrapper = ParseShopJson(fallbackJson);
        }

        if (wrapper == null || wrapper.items == null || wrapper.items.Count == 0)
        {
            if (emptyShopText != null) emptyShopText.SetActive(true);
            return;
        }

        foreach (var item in wrapper.items)
        {
            if (item == null) continue;

            GameObject cardObj = Instantiate(shopCardPrefab, contentContainer);
            cardObj.transform.localScale = Vector3.one;

            ShopCardUI cardUI = cardObj.GetComponent<ShopCardUI>();
            if (cardUI != null)
            {
                cardUI.Setup(item, OnBuyItemClicked, defaultProductSprite);
            }
        }

        if (emptyShopText != null) emptyShopText.SetActive(wrapper.items.Count == 0);
    }

    private ShopListWrapper ParseShopJson(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson)) return null;

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
            Debug.LogWarning($"[ShopManager] JSON parse hatası: {ex.Message}");
            return null;
        }
    }

        private void OnBuyItemClicked(ShopItemData item)
    {
        Debug.Log($"[ShopManager] Satın alma tıklandı: {item.title} (ID: {item.id})");

        if (item.isRealMoney)
        {
            // Gerçek para ile satın alım (IAP / Fake Store tetiklenir)
            if (IAPManager.Instance != null && IAPManager.Instance.IsInitialized)
            {
                IAPManager.Instance.BuyProduct(item.id);
            }
            else
            {
                Debug.LogWarning("[ShopManager] IAP sistemi henüz hazır değil!");
            }
        }
        else
        {
            // Ücretsiz / Oyun içi hediyeler
            if (item.id == "coins_free")
            {
                GoldManager.Instance.AddGold(100);
                GoldManager.Instance.SaveRunGold();
                Debug.Log("[ShopManager] Günlük ücretsiz altın verildi: +100 Altın");
            }
        }
    }
}