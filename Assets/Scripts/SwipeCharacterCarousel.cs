using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(ScrollRect))]
public class SwipeCharacterCarousel : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [Header("Karakter Veritabanı")]
    [Tooltip("Assets/CharacterDatabase.asset dosyasını buraya sürükleyin")]
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private GameObject itemCardPrefab;

    [Header("Arayüz Elemanları (Opsiyonel)")]
    [SerializeField] private TextMeshProUGUI selectedNameText;
    [SerializeField] private TextMeshProUGUI selectedClassText;
    [SerializeField] private TextMeshProUGUI selectedDescriptionText;
    [SerializeField] private Button actionButton; // Seç / Satın Al Butonu
    [SerializeField] private TextMeshProUGUI actionButtonText;
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;

    [Header("Carousel Efekt Ayarları")]
    [SerializeField] private float minScale = 0.8f;
    [SerializeField] private float maxScale = 1.1f;
    [SerializeField] private float minAlpha = 0.5f;
    [SerializeField] private float maxAlpha = 1.0f;
    [SerializeField] private float lerpSpeed = 12f;

    private ScrollRect scrollRect;
    private RectTransform viewportRect;
    private RectTransform contentRect;
    private HorizontalLayoutGroup layoutGroup;

    private readonly List<RectTransform> cards = new List<RectTransform>();
    private readonly List<CanvasGroup> cardCanvasGroups = new List<CanvasGroup>();
    private readonly List<Canvas> cardCanvases = new List<Canvas>();

    private int currentItemIndex = 0;
    private bool isDragging = false;
    private float cardWidth = 300f;
    private bool isInitialized = false;

    public int Count => (characterDatabase != null && characterDatabase.characters != null) ? characterDatabase.characters.Count : 0;
    public CharacterDefinition SelectedCharacter => (characterDatabase != null && currentItemIndex >= 0 && currentItemIndex < Count) 
        ? characterDatabase.GetByIndex(currentItemIndex) 
        : null;

    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        if (scrollRect != null)
        {
            viewportRect = scrollRect.viewport;
            contentRect = scrollRect.content;
            scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
        }

        if (contentRect != null)
        {
            layoutGroup = contentRect.GetComponent<HorizontalLayoutGroup>();
            contentRect.pivot = new Vector2(0f, 0.5f);
        }

        // Otomatik veritabanı ve prefab bulma koruması
        EnsureDatabaseAndPrefab();
    }

    private void EnsureDatabaseAndPrefab()
    {
        if (characterDatabase == null)
        {
            characterDatabase = Resources.Load<CharacterDatabase>("CharacterDatabase");
            #if UNITY_EDITOR
            if (characterDatabase == null)
            {
                var guids = UnityEditor.AssetDatabase.FindAssets("t:CharacterDatabase");
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    characterDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterDatabase>(path);
                }
            }
            #endif
        }

        if (itemCardPrefab == null)
        {
            #if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("ItemCard t:Prefab");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                itemCardPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            #endif
        }
    }

    private void OnEnable()
    {
        InitializeCards();

        if (cards.Count > 0)
        {
            SnapImmediate(currentItemIndex);
            UpdateSelectedUIInfo();
        }

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

    private void Start()
    {
        InitializeCards();

        if (leftArrowButton != null) leftArrowButton.onClick.AddListener(ScrollLeft);
        if (rightArrowButton != null) rightArrowButton.onClick.AddListener(ScrollRight);
        if (actionButton != null) actionButton.onClick.AddListener(OnActionButtonPressed);

        UpdateSelectedUIInfo();
    }

    public void InitializeCards()
    {
        EnsureDatabaseAndPrefab();

        if (isInitialized || characterDatabase == null || contentRect == null) return;

        // Eski objeleri temizle
        foreach (Transform child in contentRect)
        {
            Destroy(child.gameObject);
        }

        SpawnCharacterCards();

        if (cards.Count > 0)
        {
            // Kayıtlı seçili karakter varsa o karaktere odaklan
            string selectedId = CharacterSelection.GetSelectedId(characterDatabase);
            for (int i = 0; i < characterDatabase.Count; i++)
            {
                var def = characterDatabase.GetByIndex(i);
                if (def != null && def.characterId == selectedId)
                {
                    currentItemIndex = i;
                    break;
                }
            }

            ApplyDynamicPadding();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            isInitialized = true;
        }
    }

    private void ApplyDynamicPadding()
    {
        if (layoutGroup == null || cards.Count == 0 || viewportRect == null) return;

        cardWidth = cards[0].rect.width > 0 ? cards[0].rect.width : 250f;
        int dynamicPad = Mathf.RoundToInt((viewportRect.rect.width - cardWidth) / 2f);

        layoutGroup.padding.left = Mathf.Max(0, dynamicPad);
        layoutGroup.padding.right = Mathf.Max(0, dynamicPad);

        layoutGroup.CalculateLayoutInputHorizontal();
        layoutGroup.SetLayoutHorizontal();
    }

    private void SpawnCharacterCards()
    {
        if (itemCardPrefab == null)
        {
            Debug.LogError("[SwipeCharacterCarousel] Lütfen Inspector üzerinden 'Item Card Prefab' atayın!", this);
            return;
        }

        cards.Clear();
        cardCanvasGroups.Clear();
        cardCanvases.Clear();

        for (int i = 0; i < characterDatabase.Count; i++)
        {
            CharacterDefinition def = characterDatabase.GetByIndex(i);
            if (def == null) continue;

            int cardIndex = i;
            GameObject cardObj = Instantiate(itemCardPrefab, contentRect);
            cardObj.name = $"Card_{def.characterId}";

            RectTransform rt = cardObj.GetComponent<RectTransform>();
            cards.Add(rt);

            // CanvasGroup ekle/al
            CanvasGroup cg = cardObj.GetComponent<CanvasGroup>();
            if (cg == null) cg = cardObj.AddComponent<CanvasGroup>();
            cardCanvasGroups.Add(cg);

            // Canvas ekle/al
            Canvas cv = cardObj.GetComponent<Canvas>();
            if (cv == null) cv = cardObj.AddComponent<Canvas>();
            cv.overrideSorting = true;
            cardCanvases.Add(cv);

            // GraphicRaycaster ekle/al
            if (cardObj.GetComponent<GraphicRaycaster>() == null)
                cardObj.AddComponent<GraphicRaycaster>();

            // 1. Karakter Görseli (Portrait)
            var portrait = cardObj.transform.Find("Portrait")?.GetComponent<Image>();
            if (portrait != null && def.portrait != null)
            {
                portrait.sprite = def.portrait;
                portrait.color = Color.white;
            }

            // 2. Karakter İsmi ve Sınıfı (Prefab içinde varsa)
            var nameTxt = cardObj.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            if (nameTxt != null) nameTxt.text = def.displayName;

            var classTxt = cardObj.transform.Find("Class")?.GetComponent<TextMeshProUGUI>();
            if (classTxt == null) classTxt = cardObj.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
            if (classTxt != null) classTxt.text = def.characterClass;

            // Karta tıklandığında odaklanma
            Button cardBtn = cardObj.GetComponent<Button>();
            if (cardBtn == null) cardBtn = cardObj.AddComponent<Button>();
            cardBtn.transition = Selectable.Transition.None;
            cardBtn.onClick.RemoveAllListeners();
            cardBtn.onClick.AddListener(() => OnCardClicked(cardIndex));
        }
    }

    private void OnCardClicked(int index)
    {
        currentItemIndex = index;
        UpdateSelectedUIInfo();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        if (scrollRect != null) scrollRect.movementType = ScrollRect.MovementType.Elastic;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        if (scrollRect != null)
        {
            scrollRect.velocity = Vector2.zero;
            scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
        }
        currentItemIndex = GetClosestCardIndex();
        UpdateSelectedUIInfo();
    }

    private void Update()
    {
        if (cards.Count == 0 || viewportRect == null || contentRect == null) return;

        if (!isDragging && currentItemIndex >= 0 && currentItemIndex < cards.Count)
        {
            float delta = viewportRect.position.x - cards[currentItemIndex].position.x;
            Vector2 pos = contentRect.anchoredPosition;
            pos.x += delta * Mathf.Clamp01(Time.deltaTime * lerpSpeed);
            contentRect.anchoredPosition = pos;
            if (scrollRect != null) scrollRect.velocity = Vector2.zero;
        }

        UpdateCarouselEffects();
    }

    private void UpdateCarouselEffects()
    {
        if (viewportRect == null) return;

        float viewportCenterX = viewportRect.position.x;
        float maxDistance = viewportRect.rect.width * 0.5f;
        if (maxDistance <= 0) maxDistance = 300f;

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] == null) continue;

            float distance = Mathf.Abs(viewportCenterX - cards[i].position.x);
            float t = Mathf.Clamp01(distance / maxDistance);

            float scale = Mathf.Lerp(maxScale, minScale, t);
            cards[i].localScale = new Vector3(scale, scale, 1f);

            if (i < cardCanvasGroups.Count && cardCanvasGroups[i] != null)
                cardCanvasGroups[i].alpha = Mathf.Lerp(maxAlpha, minAlpha, t);

            if (i < cardCanvases.Count && cardCanvases[i] != null)
                cardCanvases[i].sortingOrder = Mathf.RoundToInt((1f - t) * 100);
        }
    }

    private int GetClosestCardIndex()
    {
        if (viewportRect == null) return 0;
        float viewportCenterX = viewportRect.position.x;
        int closest = 0;
        float minDist = float.MaxValue;

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] == null) continue;
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
        if (cards.Count == 0 || index < 0 || index >= cards.Count || viewportRect == null) return;

        float delta = viewportRect.position.x - cards[index].position.x;
        contentRect.anchoredPosition += new Vector2(delta, 0f);
        if (scrollRect != null) scrollRect.velocity = Vector2.zero;
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

    private void UpdateSelectedUIInfo()
    {
        CharacterDefinition def = SelectedCharacter;
        if (def == null) return;

        if (selectedNameText != null) selectedNameText.text = def.displayName;
        if (selectedClassText != null) selectedClassText.text = def.characterClass;
        if (selectedDescriptionText != null) selectedDescriptionText.text = def.characterClass;

        bool isUnlocked = CharacterSelection.IsUnlocked(def);
        string currentSelectedId = CharacterSelection.GetSelectedId(characterDatabase);
        bool isEquipped = (currentSelectedId == def.characterId);

        if (actionButtonText != null)
        {
            if (!isUnlocked)
            {
                actionButtonText.text = $"SATIN AL ({def.goldCost} ALTIN)";
            }
            else if (isEquipped)
            {
                actionButtonText.text = "SEÇİLDİ";
            }
            else
            {
                actionButtonText.text = "SEÇ";
            }
        }
    }

    private void OnActionButtonPressed()
    {
        CharacterDefinition def = SelectedCharacter;
        if (def == null) return;

        bool isUnlocked = CharacterSelection.IsUnlocked(def);

        if (!isUnlocked)
        {
            // Altın harcayarak kilidi aç
            if (GoldManager.Instance != null && GoldManager.Instance.SpendGold(def.goldCost))
            {
                CharacterSelection.Unlock(def);
                CharacterSelection.SetSelected(def.characterId);
            }
        }
        else
        {
            // Karakteri kuşan / seç
            CharacterSelection.SetSelected(def.characterId);
        }

        UpdateSelectedUIInfo();
    }
}