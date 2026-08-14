using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI Panelleri")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject pauseButton;

    [Header("Sahne Ayarları")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    private bool isPaused = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Oyun başlarken her zaman zaman akışının normal olduğundan emin olalım
        Time.timeScale = 1f;
    }

    private void Start()
    {
        // Başlangıçta menüyü kapatıp duraklatma butonunu açalım
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        if (pauseButton != null)
        {
            pauseButton.SetActive(true);
        }
    }

    // Oyunu duraklatır
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }

        if (pauseButton != null)
        {
            pauseButton.SetActive(false);
        }
    }

    // Oyunu devam ettirir
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        if (pauseButton != null)
        {
            pauseButton.SetActive(true);
        }
    }

    // Ana menüye döner
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;

        // Duraklatma menüsünden ana menüye geçerken de ağ oturumunu sıfırlıyoruz
        if (NetworkManager.Singleton != null)
        {
            var netGO = NetworkManager.Singleton.gameObject;
            NetworkManager.Singleton.Shutdown();
            Destroy(netGO);
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }
}