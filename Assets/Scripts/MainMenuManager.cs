using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Menü Panelleri")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject createRoomPanel; // YENİ: Oda Kur Paneli
    [SerializeField] private GameObject joinRoomPanel;   // YENİ: Odaya Katıl Paneli
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject eventsPanel;
    [SerializeField] private GameObject myItemsPanel;
    [SerializeField] private GameObject taskPanel;

    [Header("Sahne Ayarları")]
    [SerializeField] private string gameplaySceneName = "SampleScene";

    public static string SecilenMod = "Offline";

    private void Start()
    {
        Application.targetFrameRate = 60;
        ShowPanel(mainMenuPanel);
    }

    // Tek Oyunculu
    public void StartSingleplayerGame()
    {
        SecilenMod = "Offline";
        SceneManager.LoadScene(gameplaySceneName);
    }

    // Panelleri Açma
    public void OpenCreateRoomPanel() => ShowPanel(createRoomPanel);
    public void OpenJoinRoomPanel() => ShowPanel(joinRoomPanel);
    public void OpenSettings() => ShowPanel(settingsPanel);
    public void OpenShop() => ShowPanel(shopPanel);
    public void OpenEvents() => ShowPanel(eventsPanel);
    public void OpenMyItems() => ShowPanel(myItemsPanel);
    public void OpenTasks() => ShowPanel(taskPanel);
    public void BackToMainMenu() => ShowPanel(mainMenuPanel);

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    private void ShowPanel(GameObject panelToActive)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(mainMenuPanel == panelToActive);
        if (createRoomPanel != null) createRoomPanel.SetActive(createRoomPanel == panelToActive);
        if (joinRoomPanel != null) joinRoomPanel.SetActive(joinRoomPanel == panelToActive);
        if (settingsPanel != null) settingsPanel.SetActive(settingsPanel == panelToActive);
        if (shopPanel != null) shopPanel.SetActive(shopPanel == panelToActive);
        if (eventsPanel != null) eventsPanel.SetActive(eventsPanel == panelToActive);
        if (myItemsPanel != null) myItemsPanel.SetActive(myItemsPanel == panelToActive);
        if (taskPanel != null) taskPanel.SetActive(taskPanel == panelToActive);
    }
}