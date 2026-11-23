using System.Collections;
using UnityEngine;
using Unity.Services.LevelPlay;

public class RewardedAdManager : MonoBehaviour
{
    public static RewardedAdManager Instance { get; private set; }

    [Header("Ödüllü Reklam")]
    [SerializeField] private string adUnitId = "editor";

    private LevelPlayRewardedAd rewardedAd;

    private bool initSubscribed;
    private bool isLoading;
    private bool isShowing;
    private bool rewardPending;
    private bool rewardGranted;
    private bool continueReward;
    private System.Action continueRewardCallback;
    private System.Action continueClosedCallback;
    private float adStartRealtime;
    // SDK Editor reklamı 5 saniyeliktir; erken Close da ödül bildirimi gönderir.
    private const float EditorRewardedDuration = 5f;

    public bool IsShowing => isShowing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!Application.isEditor)
            return;

        LevelPlay.OnInitSuccess += OnSDKInitialized;
        initSubscribed = true;
    }

    private IEnumerator Start()
    {
        if (!Application.isEditor)
        {
            Debug.LogWarning(
                "[RewardedAd] Bu sürüm yalnızca Editor testi içindir."
            );
            yield break;
        }

        // Initializer'ın Start/Init çağrısının tamamlanmasına izin ver.
        yield return null;

        // Editor mock reklamı başlatma bildirimi kaçırılmış olsa da
        // oluşturulabilir. Bu yedek yol yalnızca Editor içindir.
        CreateEditorAdIfNeeded();
        LoadAd();
    }

    private void OnSDKInitialized(LevelPlayConfiguration configuration)
    {
        CreateEditorAdIfNeeded();
    }

    private void CreateEditorAdIfNeeded()
    {
        if (!Application.isEditor || rewardedAd != null)
            return;

        if (string.IsNullOrWhiteSpace(adUnitId))
        {
            Debug.LogError("[RewardedAd] Ad Unit ID alanı boş.");
            return;
        }

        rewardedAd = new LevelPlayRewardedAd(adUnitId);

        rewardedAd.OnAdLoaded += OnAdLoaded;
        rewardedAd.OnAdLoadFailed += OnAdLoadFailed;
        rewardedAd.OnAdDisplayed += OnAdDisplayed;
        rewardedAd.OnAdDisplayFailed += OnAdDisplayFailed;
        rewardedAd.OnAdRewarded += OnAdRewarded;
        rewardedAd.OnAdClosed += OnAdClosed;

        Debug.Log("[RewardedAd] Editor reklam nesnesi oluşturuldu.");
    }

    public bool IsAdReady()
    {
        return rewardedAd != null
            && !isShowing
            && rewardedAd.IsAdReady();
    }

    public void LoadAd()
    {
        if (!Application.isEditor)
            return;

        CreateEditorAdIfNeeded();

        if (rewardedAd == null || isShowing)
            return;

        if (rewardedAd.IsAdReady())
        {
            isLoading = false;
            return;
        }

        if (isLoading)
            return;

        CancelInvoke(nameof(LoadAd));

        isLoading = true;
        Debug.Log("[RewardedAd] Reklam yükleniyor...");
        rewardedAd.LoadAd();
    }

    public void ShowForGold()
    {
        TryShow(false, null, null);
    }

    public bool ShowForContinue(System.Action onReward, System.Action onClosed)
    {
        return TryShow(true, onReward, onClosed);
    }

    private bool TryShow(bool forContinue, System.Action onReward, System.Action onClosed)
    {
        if (!Application.isEditor || isShowing)
            return false;

        if (!IsAdReady())
        {
            LoadAd();
            if (!IsAdReady())
            {
                Debug.LogWarning("[RewardedAd] Reklam henüz hazır değil.");
                return false;
            }
        }

        isLoading = false;
        isShowing = true;
        rewardPending = true;
        rewardGranted = false;
        continueReward = forContinue;
        continueRewardCallback = onReward;
        continueClosedCallback = onClosed;

        Debug.Log(forContinue
            ? "[RewardedAd] Devam etmek için reklam açılıyor."
            : "[RewardedAd] 100 altın için reklam açılıyor.");
        rewardedAd.ShowAd();
        return true;
    }

    private void OnAdLoaded(LevelPlayAdInfo adInfo)
    {
        isLoading = false;
        Debug.Log("[RewardedAd] Reklam hazır.");
    }

    private void OnAdLoadFailed(LevelPlayAdError error)
    {
        isLoading = false;

        Debug.LogWarning(
            $"[RewardedAd] Yükleme başarısız: " +
            $"{error.ErrorCode} - {error.ErrorMessage}"
        );

        CancelInvoke(nameof(LoadAd));
        Invoke(nameof(LoadAd), 10f);
    }

    private void OnAdDisplayed(LevelPlayAdInfo adInfo)
    {
        adStartRealtime = Time.realtimeSinceStartup;
        Debug.Log("[RewardedAd] Reklam gösterildi.");
    }

    private void OnAdDisplayFailed(
        LevelPlayAdInfo adInfo,
        LevelPlayAdError error)
    {
        isShowing = false;
        rewardPending = false;
        continueRewardCallback = null;
        var closedCallback = continueClosedCallback;
        continueClosedCallback = null;
        closedCallback?.Invoke();

        Debug.LogWarning(
            $"[RewardedAd] Gösterim başarısız: " +
            $"{error.ErrorCode} - {error.ErrorMessage}"
        );

        LoadAd();
    }

    private void OnAdRewarded(
        LevelPlayAdInfo adInfo,
        LevelPlayReward reward)
    {
        if (!rewardPending || rewardGranted)
            return;

        if (continueReward && Application.isEditor
            && Time.realtimeSinceStartup - adStartRealtime < EditorRewardedDuration)
        {
            rewardPending = false;
            continueRewardCallback = null;
            Debug.Log("[RewardedAd] Test reklamı erken kapatıldı; devam hakkı verilmedi.");
            return;
        }

        rewardGranted = true;
        rewardPending = false;

        if (continueReward)
        {
            var callback = continueRewardCallback;
            continueRewardCallback = null;
            callback?.Invoke();
            Debug.Log("[RewardedAd] Devam etme hakkı kazanıldı.");
            return;
        }

        GoldManager.Instance.AddGold(100);
        GoldManager.Instance.SaveRunGold();

        Debug.Log("[RewardedAd] Ödül kazanıldı: +100 altın, kaydedildi.");
    }

    private void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        isShowing = false;

        // Ödül burada verilmez ve bekleyen ödül bilgisi silinmez.
        Debug.Log("[RewardedAd] Reklam kapandı.");

        var callback = continueClosedCallback;
        continueClosedCallback = null;
        callback?.Invoke();
        StartCoroutine(ReloadAfterClose());
    }

    private IEnumerator ReloadAfterClose()
    {
        // Kapanıştan sonra gelen ödül bildiriminin işlenmesine izin ver.
        yield return null;
        LoadAd();
    }

    private void OnDestroy()
    {
        CancelInvoke();
        StopAllCoroutines();

        if (initSubscribed)
        {
            LevelPlay.OnInitSuccess -= OnSDKInitialized;
        }

        if (rewardedAd != null)
        {
            rewardedAd.OnAdLoaded -= OnAdLoaded;
            rewardedAd.OnAdLoadFailed -= OnAdLoadFailed;
            rewardedAd.OnAdDisplayed -= OnAdDisplayed;
            rewardedAd.OnAdDisplayFailed -= OnAdDisplayFailed;
            rewardedAd.OnAdRewarded -= OnAdRewarded;
            rewardedAd.OnAdClosed -= OnAdClosed;

            rewardedAd.DestroyAd();
            rewardedAd = null;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }
}