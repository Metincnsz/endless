using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

public class IAPManager : MonoBehaviour
{
    public static IAPManager Instance { get; private set; }

    // Tanımladığınız IAP Ürün ID'leri
    public const string PACK_STARTER = "coins_pack_1";
    public const string PACK_SUPER = "coins_pack_2";

    private StoreController storeController;
    public bool IsInitialized => storeController != null;

    public event Action OnStoreInitialized;
    public event Action<string, bool> OnPurchaseResult; // productId, success

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeIAP();
        }
        else
        {
            Destroy(gameObject);
        }
    }

        private async void InitializeIAP()
    {
        storeController = UnityIAPServices.StoreController();

        // Olay abonelikleri
        storeController.OnStoreConnected += OnStoreConnected;
        storeController.OnStoreDisconnected += (desc) => Debug.LogWarning($"[IAP] Bağlantı kesildi: {desc.Message}");
        storeController.OnProductsFetched += OnProductsFetched;
        storeController.OnProductsFetchFailed += (desc) => Debug.LogError($"[IAP] Ürünler çekilemedi: {desc.FailureReason}");
        storeController.OnPurchasePending += OnPurchasePending;
        storeController.OnPurchaseConfirmed += OnPurchaseConfirmed; // <-- EKLENDİ
        storeController.OnPurchaseDeferred += (deferred) => Debug.Log("[IAP] Satın alma beklemede (Ask-to-Buy)");
        storeController.OnPurchaseFailed += (failedOrder) => {
            Debug.LogError($"[IAP] Satın alma başarısız: {failedOrder.FailureReason} - {failedOrder.Details}");
            string failedId = failedOrder.CartOrdered?.Items().FirstOrDefault()?.Product.definition.id;
            OnPurchaseResult?.Invoke(failedId, false);
        };

        // Mağazaya bağlan
        await storeController.Connect();
    }
    private void OnPurchaseConfirmed(Order order)
    {
        if (order is ConfirmedOrder confirmedOrder)
        {
            var product = confirmedOrder.CartOrdered?.Items().FirstOrDefault()?.Product;
            Debug.Log($"[IAP] Satın alma başarıyla onaylandı: {product?.definition.id}");
        }
    }
    private void OnStoreConnected()
    {
        Debug.Log("[IAP] Mağazaya bağlandı, ürünler çekiliyor...");
        
        var products = new List<ProductDefinition>
        {
            new ProductDefinition(PACK_STARTER, ProductType.Consumable),
            new ProductDefinition(PACK_SUPER, ProductType.Consumable)
        };

        storeController.FetchProducts(products);
    }

    private void OnProductsFetched(List<Product> products)
    {
        Debug.Log($"[IAP] {products.Count} adet ürün başarıyla yüklendi.");
        OnStoreInitialized?.Invoke();
    }

    public void BuyProduct(string productId)
    {
        if (!IsInitialized)
        {
            Debug.LogError("[IAP] Mağaza henüz hazır değil!");
            return;
        }

        var product = storeController.GetProductById(productId);
        if (product != null && product.availableToPurchase)
        {
            storeController.PurchaseProduct(product);
        }
    }

    private void OnPurchasePending(PendingOrder pendingOrder)
    {
        var product = pendingOrder.CartOrdered.Items().FirstOrDefault()?.Product;
        if (product == null) return;

        string productId = product.definition.id;

        // Ödülü Oyuncuya Ver
        if (productId == PACK_STARTER)
        {
            GoldManager.Instance.AddGold(500);
            GoldManager.Instance.SaveRunGold();
        }
        else if (productId == PACK_SUPER)
        {
            GoldManager.Instance.AddGold(1500);
            GoldManager.Instance.SaveRunGold();
        }

        // Satın alımı mağazaya onayla (Zorunludur)
        storeController.ConfirmPurchase(pendingOrder);
        OnPurchaseResult?.Invoke(productId, true);
    }

    public string GetLocalizedPrice(string productId)
    {
        if (!IsInitialized) return "";
        var product = storeController.GetProductById(productId);
        return product != null ? product.metadata.localizedPriceString : "";
    }
}