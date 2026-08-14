using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MyItemsTabManager : MonoBehaviour
{
    [System.Serializable]
    public class Tab
    {
        public string name;
        public Button button;
        public GameObject panel;
        public TextMeshProUGUI buttonText; // Aktif/Pasif renklendirmesi için
    }

    [Header("Sekme Tanımlamaları")]
    [SerializeField] private Tab[] tabs;

    [Header("Görünüm Ayarları")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(0.6f, 0.6f, 0.6f, 1f); // Pasif buton metni rengi (Gri)

    private void Start()
    {
        // 1. Buton dinleyicilerini (onClick olaylarını) bağla
        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i; // Lambda closure hatasını önlemek için lokal kopyası
            if (tabs[i].button != null)
            {
                tabs[i].button.onClick.AddListener(() => SelectTab(index));
            }
        }

        // 2. Başlangıçta varsayılan olarak Karakterler sekmesini (İndeks 1) aç
        // (Ortamlar = 0, Karakterler = 1, İtemler = 2)
        SelectTab(1);
    }

    public void SelectTab(int selectedIndex)
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            bool isActive = (i == selectedIndex);

            // Alt paneli aç veya kapat
            if (tabs[i].panel != null)
            {
                tabs[i].panel.SetActive(isActive);
            }

            // Buton metni rengini değiştirerek aktif olduğunu belli et
            if (tabs[i].buttonText != null)
            {
                tabs[i].buttonText.color = isActive ? activeColor : inactiveColor;
            }

            // Butonun arka plan görselini hafif şeffaflaştır/belirginleştir
            if (tabs[i].button != null)
            {
                Image btnImage = tabs[i].button.GetComponent<Image>();
                if (btnImage != null)
                {
                    btnImage.color = isActive ? activeColor : new Color(activeColor.r, activeColor.g, activeColor.b, 0.4f);
                }
            }
        }
    }
}