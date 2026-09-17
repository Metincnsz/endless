using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.RemoteConfig;
using UnityEngine;

public class UnityServicesInitializer : MonoBehaviour
{
    public static UnityServicesInitializer Instance { get; private set; }
    public static bool IsInitialized { get; private set; }
    public static bool IsFetching { get; private set; }
    
    public static event Action OnRemoteConfigFetched;

    [Header("Canlı Servis (UGS) Ayarları")]
    [Tooltip("Dashboard'da kullandığınız ortam adı (Varsayılan: production)")]
    [SerializeField] private string environmentName = "production";

    public struct UserAttributes { }
    public struct AppAttributes { }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private async void Start()
    {
        await InitializeServicesAsync(environmentName);
    }

    public static async Task InitializeServicesAsync(string environment = "production")
    {
        if (IsInitialized)
        {
            FetchRemoteConfigData();
            return;
        }

        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                var options = new InitializationOptions();
                if (!string.IsNullOrEmpty(environment))
                {
                    options.SetEnvironmentName(environment);
                }

                await UnityServices.InitializeAsync(options);
                Debug.Log($"<color=#55FF55>[UGS]</color> Unity Services başlatıldı (Ortam: {environment}).");
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"<color=#55FF55>[UGS]</color> Anonim Giriş Başarılı! Oyuncu ID: {AuthenticationService.Instance.PlayerId}");
            }

            IsInitialized = true;
            FetchRemoteConfigData();
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=#FF5555>[UGS]</color> Başlatma Hatası: {ex.Message}");
        }
    }

    public static void FetchRemoteConfigData()
    {
        if (IsFetching)
        {
            Debug.Log("<color=#FFFF55>[UGS]</color> Remote Config isteği zaten devam ediyor.");
            return;
        }

        if (RemoteConfigService.Instance == null)
        {
            Debug.LogWarning("<color=#FF5555>[UGS]</color> RemoteConfigService henüz hazır değil.");
            return;
        }

        IsFetching = true;
        RemoteConfigService.Instance.FetchCompleted -= OnFetchCompleted;
        RemoteConfigService.Instance.FetchCompleted += OnFetchCompleted;
        RemoteConfigService.Instance.FetchConfigs(new UserAttributes(), new AppAttributes());
        Debug.Log("<color=#55FFFF>[UGS]</color> Remote Config verileri sunucudan talep ediliyor...");
    }

    private static void OnFetchCompleted(ConfigResponse response)
    {
        IsFetching = false;
        RemoteConfigService.Instance.FetchCompleted -= OnFetchCompleted;

        Debug.Log($"<color=#55FF55>[UGS]</color> Remote Config Yanıtı: {response.status} (Origin: {response.requestOrigin})");

        if (response.status == ConfigRequestStatus.Success)
        {
            // Dashboard'dan gelen tüm anahtarları konsola yazdır (Hata ayıklama için çok önemlidir)
            ListKeysDebug();

            // 1. Görevler (game_tasks / tasks)
            string tasksJson = ExtractConfigJson("game_tasks", "tasks", "Tasks", "gameTasks");
            if (IsValidJsonData(tasksJson))
            {
                PlayerPrefs.SetString("Cached_Tasks_Json", tasksJson);
                Debug.Log($"<color=#55FF55>[UGS]</color> 'game_tasks' verisi güncellendi: {tasksJson}");
            }

            // 2. Etkinlikler (game_events / events)
            string eventsJson = ExtractConfigJson("game_events", "events", "Events", "gameEvents");
            if (IsValidJsonData(eventsJson))
            {
                PlayerPrefs.SetString("Cached_Events_Json", eventsJson);
                Debug.Log($"<color=#55FF55>[UGS]</color> 'game_events' verisi güncellendi: {eventsJson}");
            }

            // 3. Mağaza (game_shop / shop)
            string shopJson = ExtractConfigJson("game_shop", "shop", "Shop", "gameShop", "shop_items");
            if (IsValidJsonData(shopJson))
            {
                PlayerPrefs.SetString("Cached_Shop_Json", shopJson);
                Debug.Log($"<color=#55FF55>[UGS]</color> 'game_shop' verisi güncellendi: {shopJson}");
            }

            PlayerPrefs.Save();
            OnRemoteConfigFetched?.Invoke();
        }
        else
        {
            Debug.LogWarning($"<color=#FF5555>[UGS]</color> Remote Config çekilemedi. Durum: {response.status}");
        }
    }

    private static void ListKeysDebug()
    {
        try
        {
            if (RemoteConfigService.Instance.appConfig != null)
            {
                string[] keys = RemoteConfigService.Instance.appConfig.GetKeys();
                Debug.Log($"<color=#55FFFF>[UGS Dashboard'dan Dönen Key Sayısı: {keys.Length}]</color> -> Anahtarlar: [{string.Join(", ", keys)}]");
            }
        }
        catch (Exception) { }
    }

    /// <summary>
    /// Verilen anahtar alternatiflerini hem JSON hem String tipinde dener.
    /// </summary>
    public static string ExtractConfigJson(params string[] keyAliases)
    {
        if (RemoteConfigService.Instance.appConfig == null) return string.Empty;

        foreach (string key in keyAliases)
        {
            if (RemoteConfigService.Instance.appConfig.HasKey(key))
            {
                // 1. JSON olarak oku
                try
                {
                    string jsonVal = RemoteConfigService.Instance.appConfig.GetJson(key, string.Empty);
                    if (IsValidJsonData(jsonVal)) return jsonVal.Trim();
                }
                catch { }

                // 2. String olarak oku
                try
                {
                    string strVal = RemoteConfigService.Instance.appConfig.GetString(key, string.Empty);
                    if (IsValidJsonData(strVal)) return strVal.Trim();
                }
                catch { }
            }
        }

        return string.Empty;
    }

    private static bool IsValidJsonData(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        string t = json.Trim();
        if (t == "{}" || t == "[]" || t == "null") return false;
        return t.StartsWith("{") || t.StartsWith("[");
    }
}