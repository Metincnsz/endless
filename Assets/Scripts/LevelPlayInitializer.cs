using UnityEngine;
using Unity.Services.LevelPlay;

public class LevelPlayInitializer : MonoBehaviour
{
    [SerializeField] private string appKey = "editor";

    private static LevelPlayInitializer instance;
    private bool callbacksRegistered;

    private void Awake()
    {
        // Ana menüye tekrar dönüldüğünde ikinci kez başlatmayı engeller.
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Bu sürüm yalnızca Unity Editor testi içindir.
        // Telefonda gerçek reklam SDK'sını başlatmaz.
        if (!Application.isEditor)
        {
            Debug.LogWarning(
                "[LevelPlay] Bu başlatma kodu yalnızca Editor testi içindir."
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(appKey))
        {
            Debug.LogError("[LevelPlay] App Key alanı boş.");
            return;
        }

        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;
        callbacksRegistered = true;

        // Kullanıcı tarafından belirtilen çocuklara yönelik uygulama ayarı.
        LevelPlayPrivacySettings.SetCOPPA(true);

        Debug.Log("[LevelPlay] Editor test başlatması gönderiliyor.");
        LevelPlay.Init(appKey);
    }

    private void OnInitSuccess(LevelPlayConfiguration configuration)
    {
        Debug.Log("LevelPlay SDK initialized successfully");
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogError(
            $"[LevelPlay] Başlatma başarısız: " +
            $"{error.ErrorCode} - {error.ErrorMessage}"
        );
    }

    private void OnDestroy()
    {
        if (callbacksRegistered)
        {
            LevelPlay.OnInitSuccess -= OnInitSuccess;
            LevelPlay.OnInitFailed -= OnInitFailed;
        }

        if (instance == this)
        {
            instance = null;
        }
    }
}