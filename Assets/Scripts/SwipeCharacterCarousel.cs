using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(UnityEngine.UI.ScrollRect))]
public class SwipeCharacterCarousel : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [System.Serializable]
    public class CharacterData
    {
        public string characterName;
        public string characterClass;
        public int goldCost; // Elmas maliyeti yerine Altın maliyeti
        public Sprite characterSprite;
        public bool isUnlocked;
    }

    [Header("Karakter Verileri")]
    [SerializeField] private List<CharacterData> characters = new List<CharacterData>();
    [SerializeField] private GameObject characterCardPrefab;

    [Header("Arayüz Elemanları (Seçim Alanı)")]
    [SerializeField] private TextMeshProUGUI selectedNameText;
    [SerializeField] private TextMeshProUGUI selectedCostText;
    [SerializeField] private Button unlockButton;
    [SerializeField] private TextMeshProUGUI unlockButtonText;
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;

    [Header("Carousel Efekt Ayarları")]
    [SerializeField] private float minScale = 0.75f;
    [SerializeField] private float maxScale = 1.15f;
    [SerializeField] private float minAlpha = 0.4f;
    [SerializeField] private float maxAlpha = 1.0f;
    [SerializeField] private float lerpSpeed = 10f; 

    private ScrollRect scrollRect;
    private RectTransform viewportRect;
    private RectTransform contentRect;
    private HorizontalLayoutGroup layoutGroup;

    private List<RectTransform> cards = new List<RectTransform>();
    private List<CanvasGroup> cardCanvasGroups = new List<CanvasGroup>();
    private List<Canvas> cardCanvases = new List<Canvas>();

    private int currentItemIndex = 0;
    private bool isDragging = false;
    private float cardWidth = 300f;

    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        viewportRect = scrollRect.viewport;
        contentRect = scrollRect.content;
        layoutGroup = contentRect.GetComponent<HorizontalLayoutGroup>();

        scrollRect.movementType = ScrollRect.MovementType.Unrestricted;

        if (contentRect != null)
        {
            contentRect.pivot = new Vector2(0f, 0.5f);
        }
    }

    private void Start()
    {
        // Kayıtlı karakter kilit durumlarını yükle
        for (int i = 0; i < characters.Count; i++)
        {
            if (i == 0)
            {
                characters[i].isUnlocked = true; // İlk karakter varsayılan olarak açık
            }
            else
            {
                // Kayıt anahtarı: CharacterUnlocked_KarakterAdı
                characters[i].isUnlocked = PlayerPrefs.GetInt("CharacterUnlocked_" + characters[i].characterName, 0) == 1;
            }
        }

        foreach (Transform child in contentRect)
        {
            Destroy(child.gameObject);
        }

        SpawnCharacterCards();

        if (cards.Count == 0) return;

        currentItemIndex = characters.Count / 2;

        ApplyDynamicPadding();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        SnapImmediate(currentItemIndex);

        if (leftArrowButton != null) leftArrowButton.onClick.AddListener(ScrollLeft);
        if (rightArrowButton != null) rightArrowButton.onClick.AddListener(ScrollRight);
        if (unlockButton != null) unlockButton.onClick.AddListener(OnUnlockPressed);

        UpdateSelectedUIInfo();
    }

    private void ApplyDynamicPadding()
    {
        if (layoutGroup == null || cards.Count == 0) return;

        cardWidth = cards[0].rect.width; 
        int dynamicPad = Mathf.RoundToInt((viewportRect.rect.width - cardWidth) / 2f);
        
        layoutGroup.padding.left = dynamicPad;
        layoutGroup.padding.right = dynamicPad;
        
        layoutGroup.CalculateLayoutInputHorizontal();
        layoutGroup.SetLayoutHorizontal();
    }

    private void SpawnCharacterCards()
    {
        if (characterCardPrefab == null)
        {
            Debug.LogError("Lütfen Character Carousel üzerindeki 'Character Card Prefab' alanını doldurun!", this);
            return;
        }

        cards.Clear();
        cardCanvasGroups.Clear();
        cardCanvases.Clear();

        for (int i = 0; i < characters.Count; i++)
        {
            GameObject cardObj = Instantiate(characterCardPrefab, contentRect);
            cardObj.name = "Card_" + characters[i].characterName;

            cards.Add(cardObj.GetComponent<RectTransform>());

            CanvasGroup cg = cardObj.GetComponent<CanvasGroup>();
            if (cg == null) cg = cardObj.AddComponent<CanvasGroup>();
            cardCanvasGroups.Add(cg);

            Canvas cv = cardObj.GetComponent<Canvas>();
            if (cv == null) cv = cardObj.AddComponent<Canvas>();
            cv.overrideSorting = true;
            cardCanvases.Add(cv);

            if (cardObj.GetComponent<GraphicRaycaster>() == null)
                cardObj.AddComponent<GraphicRaycaster>();

            var portrait = cardObj.transform.Find("Portrait")?.GetComponent<UnityEngine.UI.Image>();
            if (portrait != null) portrait.sprite = characters[i].characterSprite;

            var nameTxt = cardObj.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            if (nameTxt != null) nameTxt.text = characters[i].characterName;

            var classTxt = cardObj.transform.Find("Class")?.GetComponent<TextMeshProUGUI>();
            if (classTxt != null) classTxt.text = characters[i].characterClass;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        scrollRect.velocity = Vector2.zero;
        scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
        currentItemIndex = GetClosestCardIndex();
        UpdateSelectedUIInfo();
    }

    private void Update()
    {
        if (cards.Count == 0) return;

        if (!isDragging)
        {
            float delta = viewportRect.position.x - cards[currentItemIndex].position.x;
            Vector2 pos = contentRect.anchoredPosition;
            pos.x += delta * Mathf.Clamp01(Time.deltaTime * lerpSpeed);
            contentRect.anchoredPosition = pos;
            scrollRect.velocity = Vector2.zero;
        }

        UpdateCarouselEffects();
    }

    private void UpdateCarouselEffects()
    {
        float viewportCenterX = viewportRect.position.x;
        float maxDistance = viewportRect.rect.width * 0.5f;

        for (int i = 0; i < cards.Count; i++)
        {
            float distance = Mathf.Abs(viewportCenterX - cards[i].position.x);
            float t = Mathf.Clamp01(distance / maxDistance);

            float scale = Mathf.Lerp(maxScale, minScale, t);
            cards[i].localScale = new Vector3(scale, scale, 1f);

            if (cardCanvasGroups[i] != null)
                cardCanvasGroups[i].alpha = Mathf.Lerp(maxAlpha, minAlpha, t);

            if (cardCanvases[i] != null)
                cardCanvases[i].sortingOrder = Mathf.RoundToInt((1f - t) * 100);
        }
    }

    private int GetClosestCardIndex()
    {
        float viewportCenterX = viewportRect.position.x;
        int closest = 0;
        float minDist = float.MaxValue;
        
        for (int i = 0; i < cards.Count; i++)
        {
            float d = Mathf.Abs(viewportCenterX - cards[i].position.x);
            if (d < minDist) 
            { 
                minDist = d; 
                closest = i; 
            }
        }
        return closest;
    }

    private void SnapImmediate(int index)
    {
        if (cards.Count == 0 || index < 0 || index >= cards.Count) return;
        
        float delta = viewportRect.position.x - cards[index].position.x;
        contentRect.anchoredPosition += new Vector2(delta, 0f);
        scrollRect.velocity = Vector2.zero;
        UpdateCarouselEffects();
    }

    public void ScrollLeft()
    {
        if (currentItemIndex > 0) 
        { 
            currentItemIndex--; 
            UpdateSelectedUIInfo(); 
        }
    }

    public void ScrollRight()
    {
        if (currentItemIndex < cards.Count - 1) 
        { 
            currentItemIndex++; 
            UpdateSelectedUIInfo(); 
        }
    }

    private void OnEnable()
    {
        UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(UnityEngine.Localization.Locale newLocale)
    {
        UpdateSelectedUIInfo();
    }

    private void UpdateSelectedUIInfo()
    {
        if (currentItemIndex < 0 || currentItemIndex >= characters.Count) return;
        CharacterData c = characters[currentItemIndex];

        if (selectedNameText != null) selectedNameText.text = c.characterName;
        if (selectedCostText != null)
        {
            if (c.isUnlocked)
            {
                selectedCostText.text = UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase.GetLocalizedString("UIStrings", "str_owned");
            }
            else
            {
                string fmt = UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase.GetLocalizedString("UIStrings", "fmt_gold_cost");
                if (string.IsNullOrEmpty(fmt)) fmt = "{0} ALTIN";
                selectedCostText.text = string.Format(fmt, c.goldCost.ToString("N0"));
            }
        }
        if (unlockButtonText != null)
        {
            if (c.isUnlocked)
            {
                unlockButtonText.text = UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase.GetLocalizedString("UIStrings", "str_select");
            }
            else
            {
                unlockButtonText.text = UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase.GetLocalizedString("UIStrings", "str_unlock");
            }
        }
    }

    private void OnUnlockPressed()
    {
        if (currentItemIndex < 0 || currentItemIndex >= characters.Count) return;
        CharacterData c = characters[currentItemIndex];
        
        if (!c.isUnlocked) 
        { 
            // Altın harcayarak satın almayı dene
            if (GoldManager.Instance != null && GoldManager.Instance.SpendGold(c.goldCost))
            {
                c.isUnlocked = true; 
                PlayerPrefs.SetInt("CharacterUnlocked_" + c.characterName, 1);
                PlayerPrefs.Save();
                Debug.Log(c.characterName + " Kilidi Açıldı!"); 
            }
            else
            {
                Debug.Log("Satın alma başarısız! Yetersiz altın.");
            }
        }
        else 
        {
            // Seçilen karakteri kaydet
            PlayerPrefs.SetString("SelectedCharacter", c.characterName);
            PlayerPrefs.Save();
            Debug.Log(c.characterName + " Seçildi!"); 
        }
        UpdateSelectedUIInfo();
    }
}