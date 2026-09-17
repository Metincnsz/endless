using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopCardUI : MonoBehaviour
{
    [Header("UI Bileşenleri")]
    [SerializeField] private TMP_Text titleText;       // Kart Başlığı ("Günlük Hediye" vb.)
    [SerializeField] private TMP_Text priceButtonText; // Buton Metni ("ÜCRETSİZ", "₺29.99" vb.)
    [SerializeField] private Image productMainImage;   // Ortadaki ürün görseli
    [SerializeField] private Button buyButton;         // Satın alma / Al butonu

    private ShopItemData itemData;
    private Action<ShopItemData> onBuyCallback;

    public void Setup(ShopItemData data, Action<ShopItemData> onBuyClicked, Sprite productSprite = null)
    {
        itemData = data;
        onBuyCallback = onBuyClicked;

        // Kart Başlığı ("300 Para" yerine Remote Config'den gelen title yazılır)
        if (titleText != null)
        {
            titleText.text = !string.IsNullOrEmpty(data.title) ? data.title : "Ürün";
        }

        // Buton Metni ("AL" yerine Remote Config'den gelen priceText yazılır)
        if (priceButtonText != null)
        {
            priceButtonText.text = !string.IsNullOrEmpty(data.priceText) ? data.priceText : "AL";
        }

        // Ürün Ana Görseli
        if (productMainImage != null && productSprite != null)
        {
            productMainImage.sprite = productSprite;
        }

        // Buton Tıklama Olayı
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnButtonClicked);
        }
    }

    private void OnButtonClicked()
    {
        onBuyCallback?.Invoke(itemData);
    }
}