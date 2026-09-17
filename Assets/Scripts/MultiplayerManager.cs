using UnityEngine;
using Unity.Netcode;

public class MultiplayerManager : MonoBehaviour
{
    [Header("UI Elemanları")]
    [Tooltip("Çok oyunculu modlarda açılacak olan bağlantı paneli")]
    [SerializeField] private GameObject baglantiPaneli;

    [Header("Karakter Veritabanı")]
    [SerializeField] private CharacterDatabase database;

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
            // 1. Ana menüden gelen ConnectionApproval onay kilidini çevrimdışı mod için sıfırla
            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = false;
            NetworkManager.Singleton.ConnectionApprovalCallback = null;

            // 2. Seçili karakteri veritabanından al ve PlayerPrefab olarak ata
            string selectedId = CharacterSelection.GetSelectedId(database);
            CharacterDefinition def = database != null ? database.GetById(selectedId) : null;

            if (def != null && def.playerPrefab != null)
            {
                NetworkManager.Singleton.NetworkConfig.PlayerPrefab = def.playerPrefab;
            }
            else
            {
                Debug.LogWarning("[Offline] Seçili karakter prefabı bulunamadı, varsayılan kullanılacak.");
            }

            // 3. Tek oyunculu oturumu başlat (Karakter ve kamerası sahnede otomatik doğacaktır)
            NetworkManager.Singleton.StartHost();
            Debug.Log($"[MultiplayerManager] Tek oyunculu (Offline) mod başlatıldı! Seçili Karakter: {selectedId}");
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