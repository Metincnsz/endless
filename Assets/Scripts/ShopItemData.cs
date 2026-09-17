using System;
using System.Collections.Generic;

[Serializable]
public class ShopItemData
{
    public string id;              // Ürün ID (örn: "coins_free", "pack_starter")
    public string title;           // Kart Başlığı (örn: "Günlük Hediye", "Altın Paketi")
    public string priceText;       // Buton Metni (örn: "ÜCRETSİZ", "AL", "₺29.99")
    public bool isRealMoney;       // Gerçek para ile mi (IAP) yoksa ücretsiz mi?
}

[Serializable]
public class ShopListWrapper
{
    public List<ShopItemData> items;
}