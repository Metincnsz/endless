using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class DeathMenuManager : MonoBehaviour
{
    public static DeathMenuManager Instance { get; private set; }

    [Header("UI Panelleri")]
    [Tooltip("Ölüm ekranı panelini buraya sürükleyin.")]
    [SerializeField] private GameObject deathMenuPanel;

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
        if (deathMenuPanel != null)
        {
            deathMenuPanel.SetActive(true);
        }
    }

    // "Tekrar Başla" butonuna basılınca çağrılacak metot
    public void RestartGame()
    {
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