using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(UnityEngine.UI.ScrollRect))]
public class SwipeMapCarousel : MonoBehaviour,
    IBeginDragHandler, IEndDragHandler
{
    [Header("Veriler")]
    [SerializeField] private HaritaVeritabani haritaVeritabani;
    [SerializeField] private GameObject kartPrefab;

    [Header("Arayüz")]
    [SerializeField] private TMP_Text haritaAdiText;
    [SerializeField] private TMP_Text aciklamaText;
    [SerializeField] private UnityEngine.UI.Button secButton;
    [SerializeField] private TMP_Text secButtonText;
    [SerializeField] private UnityEngine.UI.Button solButton;
    [SerializeField] private UnityEngine.UI.Button sagButton;

    [Header("Efekt")]
    [SerializeField] private float minScale = 0.8f;
    [SerializeField] private float maxScale = 1.1f;
    [SerializeField] private float minAlpha = 0.5f;
    [SerializeField] private float lerpSpeed = 12f;

    private UnityEngine.UI.ScrollRect scrollRect;
    private RectTransform viewport;
    private RectTransform content;

    private readonly List<HaritaVerisi> haritalar =
        new List<HaritaVerisi>();

    private readonly List<RectTransform> kartlar =
        new List<RectTransform>();

    private readonly List<CanvasGroup> gruplar =
        new List<CanvasGroup>();

    private int index;
    private bool surukleniyor;
    private bool hazir;
    private Coroutine acilisRutini;

    private void Awake()
    {
        scrollRect = GetComponent<UnityEngine.UI.ScrollRect>();
        viewport = scrollRect.viewport;
        content = scrollRect.content;

        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.inertia = false;
        scrollRect.movementType =
            UnityEngine.UI.ScrollRect.MovementType.Unrestricted;

        if (secButton != null)
            secButton.onClick.AddListener(Sec);

        if (solButton != null)
            solButton.onClick.AddListener(SolaGit);

        if (sagButton != null)
            sagButton.onClick.AddListener(SagaGit);
    }

    private void OnEnable()
    {
        acilisRutini = StartCoroutine(PaneliHazirla());
    }

    private void OnDisable()
    {
        if (acilisRutini != null)
            StopCoroutine(acilisRutini);

        acilisRutini = null;
        surukleniyor = false;
        hazir = false;
    }

    private void OnDestroy()
    {
        if (secButton != null)
            secButton.onClick.RemoveListener(Sec);

        if (solButton != null)
            solButton.onClick.RemoveListener(SolaGit);

        if (sagButton != null)
            sagButton.onClick.RemoveListener(SagaGit);
    }

    private IEnumerator PaneliHazirla()
    {
        if (haritaVeritabani == null ||
            kartPrefab == null ||
            viewport == null ||
            content == null)
        {
            Debug.LogError(
                "[SwipeMapCarousel] Veritabanı, kart, Viewport " +
                "ve Content bağlantılarını kontrol et.",
                this);

            yield break;
        }

        if (kartlar.Count == 0)
            KartlariOlustur();

        if (kartlar.Count == 0)
        {
            if (haritaAdiText != null)
                haritaAdiText.text = "Kullanılabilir harita yok";

            if (secButton != null)
                secButton.interactable = false;

            yield break;
        }

        HaritaVerisi secili = haritaVeritabani.SecilenHaritayiGetir();
        index = Mathf.Max(0, haritalar.IndexOf(secili));

        // Panel açıldıktan sonra layout ölçülerinin güncellenmesini bekle.
        yield return null;

        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        var layout =
            content.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();

        if (layout != null)
        {
            float kartGenisligi = kartlar[0].rect.width;

            int bosluk = Mathf.Max(
                0,
                Mathf.RoundToInt(
                    (viewport.rect.width - kartGenisligi) * 0.5f));

            layout.padding.left = bosluk;
            layout.padding.right = bosluk;
        }

        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        Ortala(true);
        BilgileriGuncelle();
        hazir = true;
        acilisRutini = null;
    }

    private void KartlariOlustur()
    {
        haritalar.Clear();
        haritalar.AddRange(
            haritaVeritabani.KullanilabilirHaritalariGetir());

        // Content sadece bu carousel'in kartlarını barındırmalı.
        foreach (Transform child in content)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        for (int i = 0; i < haritalar.Count; i++)
        {
            HaritaVerisi harita = haritalar[i];
            int kartIndex = i;

            GameObject kart = Instantiate(kartPrefab, content);
            kart.name = "MapCard_" + harita.haritaId;
            kart.SetActive(true);

            RectTransform rect = kart.GetComponent<RectTransform>();

            if (rect == null)
            {
                Debug.LogError(
                    "Harita kart prefabında RectTransform gerekli.",
                    kart);

                Destroy(kart);
                continue;
            }

            kartlar.Add(rect);

            CanvasGroup grup = kart.GetComponent<CanvasGroup>();

            if (grup == null)
                grup = kart.AddComponent<CanvasGroup>();

            gruplar.Add(grup);

            var portrait = kart.transform.Find("Portrait")
                ?.GetComponent<UnityEngine.UI.Image>();

            if (portrait != null)
            {
                portrait.sprite = harita.kartGorseli;
                portrait.color = harita.kartGorseli != null
                    ? Color.white
                    : new Color(0.25f, 0.25f, 0.25f, 1f);

                portrait.raycastTarget = true;
            }

            var isim = kart.transform.Find("Name")
                ?.GetComponent<TMP_Text>();

            if (isim != null)
                isim.text = harita.haritaAdi;

            var buton = kart.GetComponent<UnityEngine.UI.Button>();

            if (buton == null)
                buton = kart.AddComponent<UnityEngine.UI.Button>();

            if (portrait != null)
                buton.targetGraphic = portrait;

            buton.transition =
                UnityEngine.UI.Selectable.Transition.None;

            buton.onClick.AddListener(() => KartOdakla(kartIndex));
        }
    }

    private void LateUpdate()
    {
        if (!hazir || kartlar.Count == 0)
            return;

        if (!surukleniyor)
        {
            scrollRect.StopMovement();
            Ortala(false);
        }

        Vector3 merkez = viewport.TransformPoint(viewport.rect.center);
        float yariGenislik = Mathf.Max(1f, viewport.rect.width * 0.5f);

        for (int i = 0; i < kartlar.Count; i++)
        {
            Vector3 kartMerkez =
                kartlar[i].TransformPoint(kartlar[i].rect.center);

            float uzaklik = Mathf.Abs(
                viewport.InverseTransformPoint(kartMerkez).x -
                viewport.InverseTransformPoint(merkez).x);

            float t = Mathf.Clamp01(uzaklik / yariGenislik);
            float scale = Mathf.Lerp(maxScale, minScale, t);

            kartlar[i].localScale = new Vector3(scale, scale, 1f);
            gruplar[i].alpha = Mathf.Lerp(1f, minAlpha, t);
        }
    }

    private void Ortala(bool aninda)
    {
        if (index < 0 || index >= kartlar.Count)
            return;

        Vector3 hedef = viewport.TransformPoint(viewport.rect.center);
        Vector3 mevcut =
            kartlar[index].TransformPoint(kartlar[index].rect.center);

        // Dünya koordinatını UI yerel koordinatına dönüştür.
        Vector3 fark =
            content.parent.InverseTransformVector(hedef - mevcut);

        float t = aninda
            ? 1f
            : 1f - Mathf.Exp(-lerpSpeed * Time.unscaledDeltaTime);

        content.anchoredPosition += new Vector2(fark.x * t, 0f);
    }

    private void KartOdakla(int yeniIndex)
    {
        if (!hazir || yeniIndex < 0 || yeniIndex >= kartlar.Count)
            return;

        index = yeniIndex;
        BilgileriGuncelle();
    }

    public void SolaGit()
    {
        KartOdakla(Mathf.Max(0, index - 1));
    }

    public void SagaGit()
    {
        KartOdakla(Mathf.Min(kartlar.Count - 1, index + 1));
    }

    public void Sec()
    {
        if (!hazir || index < 0 || index >= haritalar.Count)
            return;

        haritaVeritabani.HaritaSec(haritalar[index]);
        BilgileriGuncelle();
    }

    private void BilgileriGuncelle()
    {
        if (index < 0 || index >= haritalar.Count)
            return;

        HaritaVerisi harita = haritalar[index];

        if (haritaAdiText != null)
            haritaAdiText.text = harita.haritaAdi;

        if (aciklamaText != null)
            aciklamaText.text = harita.aciklama;

        bool secili =
            haritaVeritabani.SecilenHaritayiGetir() == harita;

        if (secButtonText != null)
            secButtonText.text = secili ? "SEÇİLDİ" : "SEÇ";

        if (secButton != null)
            secButton.interactable = !secili;

        if (solButton != null)
            solButton.interactable = index > 0;

        if (sagButton != null)
            sagButton.interactable = index < kartlar.Count - 1;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        surukleniyor = true;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        surukleniyor = false;

        if (!hazir)
            return;

        scrollRect.StopMovement();

        Vector3 merkez = viewport.TransformPoint(viewport.rect.center);
        float enYakin = float.MaxValue;

        for (int i = 0; i < kartlar.Count; i++)
        {
            Vector3 kartMerkez =
                kartlar[i].TransformPoint(kartlar[i].rect.center);

            float uzaklik = Mathf.Abs(
                viewport.InverseTransformPoint(kartMerkez).x -
                viewport.InverseTransformPoint(merkez).x);

            if (uzaklik < enYakin)
            {
                enYakin = uzaklik;
                index = i;
            }
        }

        BilgileriGuncelle();
    }
}