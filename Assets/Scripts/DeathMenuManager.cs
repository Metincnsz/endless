using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class DeathMenuManager : MonoBehaviour
{
    public static DeathMenuManager Instance { get; private set; }

    [Header("UI Panelleri")]
    [Tooltip("Ölüm ekranı panelini buraya sürükleyin.")]
    [SerializeField] private GameObject deathMenuPanel;

    [Header("Reklamla Devam")]
    [SerializeField] private UnityEngine.UI.Button continueButton;
    [SerializeField] private UnityEngine.UI.Button restartButton;
    [SerializeField] private UnityEngine.UI.Button mainMenuButton;
    [SerializeField] private TMPro.TMP_Text continueStatusText;
    private KarakterKontrol deadPlayer;
    private bool adRequestActive;
    private bool rewardEarned;
    private bool adClosed;
    private int requestVersion;
    public bool IsOpen => deathMenuPanel != null && deathMenuPanel.activeSelf;

    [Header("Sahne Ayarları")]
    [SerializeField] private string gameplaySceneName = "SampleScene";
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this); 
            return;
        }

        // Oyun başlarken ölüm ekranının kapalı olduğundan emin olalım
        if (deathMenuPanel != null)
        {
            deathMenuPanel.SetActive(false);
        }
    }

    // Ölüm ekranını açan fonksiyon
    public void ShowDeathMenu()
    {
        deadPlayer = null;
        foreach (var player in KarakterKontrol.AktifKarakterler)
        {
            if (player != null && !player.IsAlive.Value && (!player.IsSpawned || player.IsOwner))
            {
                deadPlayer = player;
                break;
            }
        }
        requestVersion++;
        adRequestActive = false;
        rewardEarned = false;
        adClosed = false;
        bool canContinue = deadPlayer != null && deadPlayer.ReklamlaDevamEdebilir && Application.isEditor;
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(canContinue);
            continueButton.interactable = canContinue;
        }
        SetButtonsEnabled(true);
        if (continueStatusText != null) continueStatusText.text = "";
        if (deathMenuPanel != null) deathMenuPanel.SetActive(true);
        if (canContinue)
        {
            Time.timeScale = 0f;
            if (RewardedAdManager.Instance != null) RewardedAdManager.Instance.LoadAd();
        }
    }

    public void WatchAdAndContinue()
    {
        if (!IsOpen || adRequestActive || deadPlayer == null || !deadPlayer.ReklamlaDevamEdebilir)
            return;
        var manager = RewardedAdManager.Instance;
        if (manager == null)
        {
            if (continueStatusText != null) continueStatusText.text = "Reklam için oyunu ana menüden başlatın.";
            return;
        }
        int version = ++requestVersion;
        adRequestActive = true;
        rewardEarned = false;
        adClosed = false;
        SetButtonsEnabled(false);
        if (continueStatusText != null) continueStatusText.text = "";
        // Editor SDK sayacı ölçekli zamanı kullanır; karakter IsAlive=false kaldığı için koşu durur.
        Time.timeScale = 1f;
        bool shown = manager.ShowForContinue(
            () =>
            {
                if (this == null || version != requestVersion || !IsOpen) return;
                rewardEarned = true;
                TryContinue();
            },
            () =>
            {
                if (this == null || version != requestVersion || !IsOpen) return;
                adClosed = true;
                adRequestActive = false;
                Time.timeScale = 0f;
                SetButtonsEnabled(true);
                if (continueStatusText != null && !rewardEarned)
                    continueStatusText.text = "Devam etmek için reklamı tamamlayın.";
                TryContinue();
            });
        if (!shown)
        {
            adRequestActive = false;
            Time.timeScale = 0f;
            SetButtonsEnabled(true);
            if (continueStatusText != null) continueStatusText.text = "Reklam henüz hazır değil. Tekrar deneyin.";
        }
    }

    private void TryContinue()
    {
        if (!rewardEarned || !adClosed || deadPlayer == null) return;
        if (!deadPlayer.ReklamSonrasiDevamEt()) return;
        requestVersion++;
        adRequestActive = false;
        deathMenuPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    private void SetButtonsEnabled(bool enabled)
    {
        if (continueButton != null) continueButton.interactable = enabled;
        if (restartButton != null) restartButton.interactable = enabled;
        if (mainMenuButton != null) mainMenuButton.interactable = enabled;
    }

    private void FinishRun()
    {
        requestVersion++;
        if (deadPlayer != null && deadPlayer.ReklamlaDevamEdebilir) deadPlayer.KosuyuKaydet();
    }

    private void OnDestroy()
    {
        requestVersion++;
        if (Instance == this) Instance = null;
    }

    // "Tekrar Başla" butonuna basılınca çağrılacak metot
    public void RestartGame()
    {
        if (adRequestActive) return;
        FinishRun();
        // Zaman akışını normale döndür
        Time.timeScale = 1f;

        // Aktif ağ bağlantısı varsa temiz bir şekilde kapatıp NetworkManager nesnesini imha ediyoruz
        if (NetworkManager.Singleton != null)
        {
            var netGO = NetworkManager.Singleton.gameObject;
            NetworkManager.Singleton.Shutdown();
            Destroy(netGO);
        }

        // Sahneyi sıfırdan baştan yüklüyoruz
        SceneManager.LoadScene(gameplaySceneName);
    }

    // "Ana Menü" butonuna basılınca çağrılacak metot
    public void GoToMainMenu()
    {
        if (adRequestActive) return;
        FinishRun();
        Time.timeScale = 1f;

        // Ağ bağlantısını temiz şekilde sonlandırıyoruz
        if (NetworkManager.Singleton != null)
        {
            var netGO = NetworkManager.Singleton.gameObject;
            NetworkManager.Singleton.Shutdown();
            Destroy(netGO);
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}