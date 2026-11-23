using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(UnityEngine.UI.ScrollRect))]
public class SwipeCharacterCarousel : MonoBehaviour,
    IBeginDragHandler, IEndDragHandler
{
    [Header("Karakter Veritabanı")]
    [Tooltip("Assets/CharacterDatabase.asset dosyasını buraya sürükleyin.")]
    [SerializeField] private CharacterDatabase characterDatabase;

    [SerializeField] private GameObject itemCardPrefab;

    [Header("Arayüz Elemanları (Opsiyonel)")]
    [SerializeField] private TextMeshProUGUI selectedNameText;
    [SerializeField] private TextMeshProUGUI selectedClassText;
    [SerializeField] private TextMeshProUGUI selectedDescriptionText;

    [SerializeField] private UnityEngine.UI.Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonText;

    [SerializeField] private UnityEngine.UI.Button leftArrowButton;
    [SerializeField] private UnityEngine.UI.Button rightArrowButton;

    [Header("Carousel Efekt Ayarları")]
    [SerializeField] private float minScale = 0.8f;
    [SerializeField] private float maxScale = 1.1f;
    [SerializeField] private float minAlpha = 0.5f;
    [SerializeField] private float maxAlpha = 1f;
    [SerializeField] private float lerpSpeed = 12f;

    private UnityEngine.UI.ScrollRect scrollRect;
    private RectTransform viewportRect;
    private RectTransform contentRect;
    private UnityEngine.UI.HorizontalLayoutGroup layoutGroup;

    private readonly List<RectTransform> cards =
        new List<RectTransform>();

    private readonly List<CanvasGroup> cardCanvasGroups =
        new List<CanvasGroup>();

    private readonly List<Canvas> cardCanvases =
        new List<Canvas>();

    // Oluşturulan kart ile karakter verisini aynı sırada tutar.
    private readonly List<CharacterDefinition> cardDefinitions =
        new List<CharacterDefinition>();

    private int currentItemIndex;
    private bool isDragging;
    private bool isInitialized;

    public int Count =>
        characterDatabase != null ? characterDatabase.Count : 0;

    // Bu özellik, odaklanan kartın karakterini döndürür.
    // Kalıcı seçim CharacterSelection içinde tutulur.
    public CharacterDefinition SelectedCharacter
    {
        get
        {
            if (currentItemIndex < 0 ||
                currentItemIndex >= cardDefinitions.Count)
            {
                return null;
            }

            return cardDefinitions[currentItemIndex];
        }
    }

    private void Awake()
    {
        scrollRect = GetComponent<UnityEngine.UI.ScrollRect>();
        viewportRect = scrollRect.viewport;
        contentRect = scrollRect.content;

        scrollRect.movementType =
            UnityEngine.UI.ScrollRect.MovementType.Unrestricted;

        if (contentRect != null)
        {
            layoutGroup =
                contentRect.GetComponent<
                    UnityEngine.UI.HorizontalLayoutGroup>();

            contentRect.pivot = new Vector2(0f, 0.5f);
        }

        EnsureDatabaseAndPrefab();

        if (leftArrowButton != null)
            leftArrowButton.onClick.AddListener(ScrollLeft);

        if (rightArrowButton != null)
            rightArrowButton.onClick.AddListener(ScrollRight);

        if (actionButton != null)
            actionButton.onClick.AddListener(OnActionButtonPressed);
    }

    private void EnsureDatabaseAndPrefab()
    {
        if (characterDatabase == null)
        {
            characterDatabase =
                Resources.Load<CharacterDatabase>("CharacterDatabase");

#if UNITY_EDITOR
            if (characterDatabase == null)
            {
                string[] guids =
                    UnityEditor.AssetDatabase.FindAssets(
                        "t:CharacterDatabase");

                if (guids.Length > 0)
                {
                    string path =
                        UnityEditor.AssetDatabase.GUIDToAssetPath(
                            guids[0]);

                    characterDatabase =
                        UnityEditor.AssetDatabase
                            .LoadAssetAtPath<CharacterDatabase>(path);
                }
            }
#endif
        }

        // Build'de prefabın Inspector'dan atanmış olması gerekir.
#if UNITY_EDITOR
        if (itemCardPrefab == null)
        {
            string[] guids =
                UnityEditor.AssetDatabase.FindAssets(
                    "CharacterCard t:Prefab");

            if (guids.Length > 0)
            {
                string path =
                    UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);

                itemCardPrefab =
                    UnityEditor.AssetDatabase
                        .LoadAssetAtPath<GameObject>(path);
            }
        }
#endif
    }

    private void OnEnable()
    {
        InitializeCards();

        if (cards.Count > 0)
        {
            FocusSavedCharacter();
            Canvas.ForceUpdateCanvases();

            UnityEngine.UI.LayoutRebuilder
                .ForceRebuildLayoutImmediate(contentRect);

            SnapImmediate(currentItemIndex);
            UpdateSelectedUIInfo();
        }

        UnityEngine.Localization.Settings.LocalizationSettings
            .SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        isDragging = false;

        UnityEngine.Localization.Settings.LocalizationSettings
            .SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnDestroy()
    {
        if (leftArrowButton != null)
            leftArrowButton.onClick.RemoveListener(ScrollLeft);

        if (rightArrowButton != null)
            rightArrowButton.onClick.RemoveListener(ScrollRight);

        if (actionButton != null)
            actionButton.onClick.RemoveListener(OnActionButtonPressed);
    }

    private void OnLocaleChanged(
        UnityEngine.Localization.Locale newLocale)
    {
        UpdateSelectedUIInfo();
    }

    public void InitializeCards()
    {
        if (isInitialized)
            return;

        EnsureDatabaseAndPrefab();

        if (characterDatabase == null ||
            contentRect == null ||
            viewportRect == null ||
            itemCardPrefab == null)
        {
            Debug.LogError(
                "[SwipeCharacterCarousel] Character Database, " +
                "Item Card Prefab, Content ve Viewport " +
                "bağlantılarını kontrol edin.",
                this);

            return;
        }

        if (itemCardPrefab.GetComponent<RectTransform>() == null)
        {
            Debug.LogError(
                "[SwipeCharacterCarousel] Kart prefabında " +
                "RectTransform bulunmalı.",
                this);

            return;
        }

        foreach (Transform child in contentRect)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        cards.Clear();
        cardCanvasGroups.Clear();
        cardCanvases.Clear();
        cardDefinitions.Clear();

        for (int i = 0; i < characterDatabase.Count; i++)
        {
            CharacterDefinition def =
                characterDatabase.GetByIndex(i);

            if (def == null)
                continue;

            GameObject cardObj =
                Instantiate(itemCardPrefab, contentRect);

            cardObj.name = $"Card_{def.characterId}";

            RectTransform rt =
                cardObj.GetComponent<RectTransform>();

            int cardIndex = cards.Count;

            cards.Add(rt);
            cardDefinitions.Add(def);

            CanvasGroup cg = cardObj.GetComponent<CanvasGroup>();

            if (cg == null)
                cg = cardObj.AddComponent<CanvasGroup>();

            cardCanvasGroups.Add(cg);

            Canvas cv = cardObj.GetComponent<Canvas>();

            if (cv == null)
                cv = cardObj.AddComponent<Canvas>();

            cv.overrideSorting = true;
            cardCanvases.Add(cv);

            if (cardObj.GetComponent<
                    UnityEngine.UI.GraphicRaycaster>() == null)
            {
                cardObj.AddComponent<
                    UnityEngine.UI.GraphicRaycaster>();
            }

            UnityEngine.UI.Image portrait =
                cardObj.transform.Find("Portrait")
                    ?.GetComponent<UnityEngine.UI.Image>();

            if (portrait != null)
            {
                if (def.portrait != null)
                {
                    portrait.sprite = def.portrait;
                    portrait.color = Color.white;
                }

                // Kartın tıklanabilir yüzeyi.
                portrait.raycastTarget = true;
            }

            TextMeshProUGUI nameText =
                cardObj.transform.Find("Name")
                    ?.GetComponent<TextMeshProUGUI>();

            if (nameText != null)
                nameText.text = def.displayName;

            TextMeshProUGUI classText =
                cardObj.transform.Find("Class")
                    ?.GetComponent<TextMeshProUGUI>();

            if (classText == null)
            {
                classText =
                    cardObj.transform.Find("Description")
                        ?.GetComponent<TextMeshProUGUI>();
            }

            if (classText != null)
                classText.text = def.characterClass;

            UnityEngine.UI.Button cardButton =
                cardObj.GetComponent<UnityEngine.UI.Button>();

            if (cardButton == null)
            {
                cardButton =
                    cardObj.AddComponent<UnityEngine.UI.Button>();
            }

            cardButton.transition =
                UnityEngine.UI.Selectable.Transition.None;

            if (portrait != null)
                cardButton.targetGraphic = portrait;

            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(
                () => OnCardClicked(cardIndex));
        }

        if (cards.Count == 0)
            return;

        Canvas.ForceUpdateCanvases();

        UnityEngine.UI.LayoutRebuilder
            .ForceRebuildLayoutImmediate(contentRect);

        ApplyDynamicPadding();

        UnityEngine.UI.LayoutRebuilder
            .ForceRebuildLayoutImmediate(contentRect);

        FocusSavedCharacter();
        isInitialized = true;
    }

    private void FocusSavedCharacter()
    {
        string savedId =
            CharacterSelection.GetSelectedId(characterDatabase);

        for (int i = 0; i < cardDefinitions.Count; i++)
        {
            if (cardDefinitions[i].characterId == savedId)
            {
                currentItemIndex = i;
                return;
            }
        }

        currentItemIndex = 0;
    }

    private void ApplyDynamicPadding()
    {
        if (layoutGroup == null || cards.Count == 0)
            return;

        float width = cards[0].rect.width;

        if (width <= 0f)
            width = 250f;

        int padding = Mathf.Max(
            0,
            Mathf.RoundToInt(
                (viewportRect.rect.width - width) / 2f));

        layoutGroup.padding.left = padding;
        layoutGroup.padding.right = padding;
    }

    private void OnCardClicked(int index)
    {
        if (index < 0 || index >= cardDefinitions.Count)
            return;

        currentItemIndex = index;

        CharacterDefinition def = SelectedCharacter;

        // DÜZELTME:
        // Açık karakterin kartına basınca seçim hemen kaydedilir.
        if (CharacterSelection.IsUnlocked(def))
        {
            SaveCharacterSelection(def);
        }
        else
        {
            Debug.Log(
                $"[Karakter Seçimi] {def.displayName} kilitli. " +
                "Önce satın alınması gerekiyor.",
                this);
        }

        UpdateSelectedUIInfo();
    }

    private void SaveCharacterSelection(CharacterDefinition def)
    {
        if (def == null)
            return;

        if (string.IsNullOrWhiteSpace(def.characterId))
        {
            Debug.LogError(
                "[Karakter Seçimi] Character Id boş olamaz.",
                def);

            return;
        }

        if (def.playerPrefab == null)
        {
            Debug.LogError(
                $"[Karakter Seçimi] {def.displayName} için " +
                "Player Prefab atanmamış.",
                def);

            return;
        }

        CharacterSelection.SetSelected(def.characterId);

        Debug.Log(
            $"[Karakter Seçimi] Kaydedildi: {def.characterId} " +
            $"| Prefab: {def.playerPrefab.name}",
            this);
    }

    private void OnActionButtonPressed()
    {
        CharacterDefinition def = SelectedCharacter;

        if (def == null)
            return;

        if (!CharacterSelection.IsUnlocked(def))
        {
            if (GoldManager.Instance == null)
            {
                Debug.LogError(
                    "[Karakter Seçimi] GoldManager bulunamadı.",
                    this);

                return;
            }

            if (!GoldManager.Instance.SpendGold(def.goldCost))
            {
                Debug.Log(
                    "[Karakter Seçimi] Yeterli altın yok.",
                    this);

                return;
            }

            CharacterSelection.Unlock(def);
        }

        SaveCharacterSelection(def);
        UpdateSelectedUIInfo();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;

        scrollRect.movementType =
            UnityEngine.UI.ScrollRect.MovementType.Elastic;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        scrollRect.velocity = Vector2.zero;

        scrollRect.movementType =
            UnityEngine.UI.ScrollRect.MovementType.Unrestricted;

        currentItemIndex = GetClosestCardIndex();
        UpdateSelectedUIInfo();
    }

    private void Update()
    {
        if (cards.Count == 0 ||
            viewportRect == null ||
            contentRect == null)
        {
            return;
        }

        if (!isDragging &&
            currentItemIndex >= 0 &&
            currentItemIndex < cards.Count)
        {
            float delta =
                GetCenterOffsetInContentParent(
                    cards[currentItemIndex]);

            Vector2 position = contentRect.anchoredPosition;

            position.x += delta * Mathf.Clamp01(
                Time.unscaledDeltaTime * lerpSpeed);

            contentRect.anchoredPosition = position;
            scrollRect.velocity = Vector2.zero;
        }

        UpdateCarouselEffects();
    }

    private float GetCenterOffsetInContentParent(RectTransform card)
    {
        Vector3 viewportCenter = viewportRect.position;

        Vector3 cardCenter = card.position;

        Transform parent = contentRect.parent;

        return parent.InverseTransformPoint(viewportCenter).x
             - parent.InverseTransformPoint(cardCenter).x;
    }

    private float GetCardDistanceFromCenter(RectTransform card)
    {
        Vector3 cardCenter = card.position;

        float localX =
            viewportRect.InverseTransformPoint(cardCenter).x;

        return Mathf.Abs(localX);
    }

    private void UpdateCarouselEffects()
    {
        float maxDistance =
            Mathf.Max(1f, viewportRect.rect.width * 0.5f);

        for (int i = 0; i < cards.Count; i++)
        {
            float distance =
                GetCardDistanceFromCenter(cards[i]);

            float t = Mathf.Clamp01(distance / maxDistance);
            float scale = Mathf.Lerp(maxScale, minScale, t);

            cards[i].localScale =
                new Vector3(scale, scale, 1f);

            cardCanvasGroups[i].alpha =
                Mathf.Lerp(maxAlpha, minAlpha, t);

            cardCanvases[i].sortingOrder =
                Mathf.RoundToInt((1f - t) * 100f);
        }
    }

    private int GetClosestCardIndex()
    {
        int closest = 0;
        float minDistance = float.MaxValue;

        for (int i = 0; i < cards.Count; i++)
        {
            float distance =
                GetCardDistanceFromCenter(cards[i]);

            if (distance < minDistance)
            {
                minDistance = distance;
                closest = i;
            }
        }

        return closest;
    }

    private void SnapImmediate(int index)
    {
        if (index < 0 || index >= cards.Count)
            return;

        contentRect.anchoredPosition += new Vector2(
            GetCenterOffsetInContentParent(cards[index]),
            0f);

        scrollRect.velocity = Vector2.zero;
        UpdateCarouselEffects();
    }

    public void ScrollLeft()
    {
        if (currentItemIndex <= 0)
            return;

        currentItemIndex--;
        UpdateSelectedUIInfo();
    }

    public void ScrollRight()
    {
        if (currentItemIndex >= cards.Count - 1)
            return;

        currentItemIndex++;
        UpdateSelectedUIInfo();
    }

    private void UpdateSelectedUIInfo()
    {
        CharacterDefinition def = SelectedCharacter;

        if (def == null)
            return;

        // Bu panelde ayrı bir Seç butonu yoksa odaklanan açık kart seçimdir.
        // Kilitli kartlar ve satın alma akışı bundan etkilenmez.
        if (actionButton == null && CharacterSelection.IsUnlocked(def)
            && def.playerPrefab != null
            && !string.IsNullOrWhiteSpace(def.characterId)
            && CharacterSelection.GetSelectedId(characterDatabase) != def.characterId)
        {
            SaveCharacterSelection(def);
        }

        if (selectedNameText != null)
            selectedNameText.text = def.displayName;

        if (selectedClassText != null)
            selectedClassText.text = def.characterClass;

        if (selectedDescriptionText != null)
            selectedDescriptionText.text = def.characterClass;

        bool unlocked = CharacterSelection.IsUnlocked(def);

        bool equipped =
            CharacterSelection.GetSelectedId(characterDatabase)
            == def.characterId;

        if (actionButtonText != null)
        {
            if (!unlocked)
            {
                actionButtonText.text =
                    $"SATIN AL ({def.goldCost} ALTIN)";
            }
            else
            {
                actionButtonText.text =
                    equipped ? "SEÇİLDİ" : "SEÇ";
            }
        }
    }
}