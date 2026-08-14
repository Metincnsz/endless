using UnityEngine;
using Unity.Netcode;

public class MultiplayerManager : MonoBehaviour
{
    [Header("UI Elemanları")]
    [Tooltip("Çok oyunculu modlarda açılacak olan bağlantı paneli")]
    [SerializeField] private GameObject baglantiPaneli;

    private void Start()
    {
        // Eğer zaten bir bağlantı aktifse (MultiplayerRoomManager ile oda/lobi açılmış ve oyuna geçilmişse)
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            Debug.Log("[MultiplayerManager] NetworkManager zaten aktif ve dinliyor. Yeni bağlantı başlatılmadı.");
            if (baglantiPaneli != null) baglantiPaneli.SetActive(false);
            return;
        }

        string mod = MainMenuManager.SecilenMod;
        Debug.Log($"[MultiplayerManager] Aktif Oyun Modu: {mod}");

        if (mod == "Offline")
        {
            if (baglantiPaneli != null) baglantiPaneli.SetActive(false);
            StartOfflineGame();
        }
        else if (mod == "Host")
        {
            if (baglantiPaneli != null) baglantiPaneli.SetActive(false);
            StartHostGame();
        }
        else if (mod == "Client")
        {
            if (baglantiPaneli != null) baglantiPaneli.SetActive(false);
            StartClientGame();
        }
        else
        {
            if (baglantiPaneli != null) baglantiPaneli.SetActive(true);
        }
    }

    private void StartOfflineGame()
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.StartHost();
            Debug.Log("[MultiplayerManager] Tek oyunculu (Offline) mod başlatıldı!");
        }
    }

    private void StartHostGame()
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.StartHost();
            Debug.Log("[MultiplayerManager] Çok oyunculu HOST mod otomatik başlatıldı!");
        }
    }

    private void StartClientGame()
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.StartClient();
            Debug.Log("[MultiplayerManager] Çok oyunculu CLIENT mod otomatik başlatıldı!");
        }
    }
}