using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class YakinGecisUI : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private YakinGecisYoneticisi yonetici;
    [SerializeField] private TMPro.TextMeshProUGUI bildirimMetni;

    [Header("Bildirim Süresi")]
    [Tooltip("Yakın geçiş yazısının toplam ekranda kalma süresi (saniye).")]
    [Min(0f)] [SerializeField] private float gosterimSuresi = 1f;
    [Tooltip("Toplam sürenin sonundaki yumuşak kaybolma süresi.")]
    [Min(0f)] [SerializeField] private float kaybolmaSuresi = 0.25f;

    private CanvasGroup _grup;
    private KarakterKontrol _oyuncu;
    private float _kalanSure;

    private void Awake()
    {
        _grup = GetComponent<CanvasGroup>();
        _grup.interactable = false;
        _grup.blocksRaycasts = false;
        if (bildirimMetni != null)
        {
            bildirimMetni.text = "Yakın geçtin!";
            bildirimMetni.raycastTarget = false;
        }
        Gizle();
    }

    private void OnEnable()
    {
        if (yonetici != null) yonetici.YakinGecisBasarili += Bildir;
        GoldManager.OnCurrentGoldChanged += AltinDegisti;
        Gizle();
    }

    private void OnDisable()
    {
        if (yonetici != null) yonetici.YakinGecisBasarili -= Bildir;
        GoldManager.OnCurrentGoldChanged -= AltinDegisti;
        Gizle();
    }

    private void OnValidate()
    {
        gosterimSuresi = Mathf.Max(0f, gosterimSuresi);
        kaybolmaSuresi = Mathf.Clamp(kaybolmaSuresi, 0f, gosterimSuresi);
    }

    private void Bildir(KarakterKontrol player, int amount)
    {
        if (Time.timeScale <= 0f || gosterimSuresi <= 0f || bildirimMetni == null) return;
        _oyuncu = player;
        _kalanSure = gosterimSuresi;
        _grup.alpha = 1f;
    }

    private void Update()
    {
        if (_kalanSure <= 0f) return;
        if (_oyuncu == null || !_oyuncu.isActiveAndEnabled || _oyuncu.IsAlive == null || !_oyuncu.IsAlive.Value)
        {
            Gizle();
            return;
        }
        if (Time.timeScale <= 0f)
        {
            _grup.alpha = 0f;
            return;
        }
        _kalanSure = Mathf.Max(0f, _kalanSure - Time.deltaTime);
        float fade = Mathf.Min(kaybolmaSuresi, gosterimSuresi);
        _grup.alpha = fade > 0f ? Mathf.Clamp01(_kalanSure / fade) : (_kalanSure > 0f ? 1f : 0f);
        if (_kalanSure <= 0f) Gizle();
    }

    private void AltinDegisti(int amount)
    {
        if (amount == 0) Gizle();
    }

    private void Gizle()
    {
        _kalanSure = 0f;
        _oyuncu = null;
        if (_grup != null) _grup.alpha = 0f;
    }
}
