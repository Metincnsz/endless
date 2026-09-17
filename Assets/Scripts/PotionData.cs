using UnityEngine;

public abstract class PotionData : ScriptableObject
{
    [Header("Temel İksir Bilgileri")]
    public string potionId = "iksir_id";
    public string potionName = "İksir Adı";
    [TextArea] public string description;

    [Header("Süre Ayarı")]
    [Tooltip("İksirin aktif kalma süresi (Saniye)")]
    public float duration = 8f;

    [Header("Görsel & Ses Efektleri")]
    public Sprite icon;
    public Color themeColor = Color.cyan;
    public GameObject collectVFX;
    public GameObject auraVFXPrefab;

    // Özel Durum Bayrakları (İhtiyaç duyan iksirler 'override' eder)
    public virtual bool IsInvulnerable => false;
    public virtual bool IsGliding => false;
    public virtual int GoldMultiplier => 1;

    /// <summary>
    /// İksir yerden alındığı an 1 kez çalışır.
    /// </summary>
    public virtual void OnStart(PlayerPotionController controller) { }

    /// <summary>
    /// İksir aktif olduğu sürece her Update karesinde çalışır.
    /// </summary>
    public virtual void OnUpdate(PlayerPotionController controller) { }

    /// <summary>
    /// İksir aktif olduğu sürece her FixedUpdate adımında fizik hesapları için çalışır.
    /// </summary>
    public virtual void OnFixedUpdate(PlayerPotionController controller) { }

    /// <summary>
    /// İksirin süresi bittiğinde temizlik ve geri alma işlemleri için çalışır.
    /// </summary>
    public virtual void OnEnd(PlayerPotionController controller) { }
}