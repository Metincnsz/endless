using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MultiplayerRoomManager : MonoBehaviour
{
    public static MultiplayerRoomManager Instance { get; private set; }

    [Header("Oda Kur Paneli Elemanları")]
    [SerializeField] private TMP_InputField createPasswordInput; // İsteğe bağlı, girilirse en az 8 karakter
    [SerializeField] private TMP_Dropdown mapDropdown;          // Harita seçimi
    [SerializeField] private TMP_Dropdown playerCountDropdown;   // Oyuncu sayısı seçimi
    [SerializeField] private Button createRoomButton;
    [SerializeField] private TMP_Text createStatusText;

    [Header("Odaya Katıl Paneli Elemanları")]
    [SerializeField] private TMP_InputField joinCodeInput;      // Oda Kurulduğunda Verilen 6 Haneli Kod
    [SerializeField] private TMP_InputField joinPasswordInput;  // Odanın Şifresi (Varsa)
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private TMP_Text joinStatusText;

    [Header("Oda Bilgi / Bekleme Paneli")]
    [SerializeField] private GameObject roomInfoPanel;
    [SerializeField] private TMP_Text roomCodeText;
    [SerializeField] private Button startGameButton;
    [SerializeField] private UnityEngine.UI.Image roomMapImage;          // Harita resmi alanı
    [SerializeField] private Transform playerCardsContainer;           // Oyuncu kartları konteyneri

    [Header("Ayar Parametreleri")]
    [SerializeField] private string defaultMapSceneName = "SampleScene";
    [SerializeField] private int maxPlayers = 5; // Dropdown seçilmediğinde varsayılan değer

    [Header("Netcode Yapılandırması")]
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private GameObject fallbackPlayerPrefab;

    public ISession ActiveSession { get; private set; }

    private readonly Dictionary<ulong, string> _clientCharacterIds = new Dictionary<ulong, string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        EnsureNetworkManager();
    }

    private void Start()
    {
        if (createRoomButton != null) createRoomButton.onClick.AddListener(OnCreateRoomClicked);
        if (joinRoomButton != null) joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
        if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGameClicked);

        if (roomInfoPanel != null) roomInfoPanel.SetActive(false);

        InitializePlayerCountDropdown();
    }

    private void OnDestroy()
    {
        UnsubscribeSessionEvents(ActiveSession);
    }

    /// <summary>
    /// Oyuncu sayısı Dropdown'ını 2, 3, 4, 5 seçenekleriyle doldurur.
    /// </summary>
    private void InitializePlayerCountDropdown()
    {
        if (playerCountDropdown != null)
        {
            playerCountDropdown.ClearOptions();
            List<string> options = new List<string> { "2 Oyuncu", "3 Oyuncu", "4 Oyuncu", "5 Oyuncu" };
            playerCountDropdown.AddOptions(options);

            // Varsayılan olarak 5 Oyuncu (Index 3) seçili gelsin
            playerCountDropdown.value = 3;
            playerCountDropdown.RefreshShownValue();
        }
    }

    /// <summary>
    /// Sahnede NetworkManager yoksa otomatik olarak oluşturur.
    /// </summary>
    private void EnsureNetworkManager()
    {
        if (NetworkManager.Singleton == null)
        {
            GameObject nmGo = new GameObject("NetworkManager");
            var transport = nmGo.AddComponent<UnityTransport>();
            var nm = nmGo.AddComponent<NetworkManager>();
            nm.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                EnableSceneManagement = true
            };
            DontDestroyOnLoad(nmGo);
            Debug.Log("[Multiplayer] NetworkManager ve UnityTransport otomatik olarak oluşturuldu.");
        }

        var networkManager = NetworkManager.Singleton;
        if (networkManager != null)
        {
            networkManager.NetworkConfig.ConnectionApproval = true;
            networkManager.ConnectionApprovalCallback = ApprovalCheck;
        }

        ConfigureNetworkConfig(networkManager);
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
                              NetworkManager.ConnectionApprovalResponse response)
    {
        string charId = (request.Payload != null && request.Payload.Length > 0)
            ? System.Text.Encoding.UTF8.GetString(request.Payload)
            : "";

        _clientCharacterIds[request.ClientNetworkId] = charId;

        response.Approved = true;
        response.CreatePlayerObject = false; // Karakteri OnGameSceneLoadCompleted içinde biz üretiyoruz
        response.Pending = false;
    }

    private void ConfigureNetworkConfig(NetworkManager nm)
    {
        if (nm == null || nm.NetworkConfig == null) return;

        nm.NetworkConfig.EnableSceneManagement = true;
        nm.NetworkConfig.PlayerPrefab = null;

        if (characterDatabase != null)
        {
            foreach (var def in characterDatabase.characters)
            {
                if (def == null || def.playerPrefab == null) continue;

                bool alreadyAdded = false;
                foreach (var registered in nm.NetworkConfig.Prefabs.Prefabs)
                {
                    if (registered.Prefab == def.playerPrefab)
                    {
                        alreadyAdded = true;
                        break;
                    }
                }

                if (!alreadyAdded)
                {
                    nm.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = def.playerPrefab });
                }
            }
        }

        if (fallbackPlayerPrefab != null)
        {
            bool alreadyAdded = false;
            foreach (var registered in nm.NetworkConfig.Prefabs.Prefabs)
            {
                if (registered.Prefab == fallbackPlayerPrefab)
                {
                    alreadyAdded = true;
                    break;
                }
            }

            if (!alreadyAdded)
            {
                nm.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = fallbackPlayerPrefab });
            }
        }
    }

    private void SetActiveSession(ISession session)
    {
        if (ActiveSession != null)
        {
            UnsubscribeSessionEvents(ActiveSession);
        }

        ActiveSession = session;

        if (ActiveSession != null)
        {
            SubscribeSessionEvents(ActiveSession);
        }
    }

    private void SubscribeSessionEvents(ISession session)
    {
        if (session == null) return;
        session.PlayerJoined += OnPlayerJoinedSession;
        session.PlayerLeaving += OnPlayerLeftSession;
        session.PlayerHasLeft += OnPlayerLeftSession;
        session.Changed += OnSessionChanged;
    }

    private void UnsubscribeSessionEvents(ISession session)
    {
        if (session == null) return;
        session.PlayerJoined -= OnPlayerJoinedSession;
        session.PlayerLeaving -= OnPlayerLeftSession;
        session.PlayerHasLeft -= OnPlayerLeftSession;
        session.Changed -= OnSessionChanged;
    }

    private void OnPlayerJoinedSession(string playerId)
    {
        Debug.Log($"[Multiplayer] Yeni oyuncu katıldı: {playerId}");
        RefreshPlayerCards(ActiveSession != null && ActiveSession.IsHost);
    }

    private void OnPlayerLeftSession(string playerId)
    {
        Debug.Log($"[Multiplayer] Oyuncu ayrıldı: {playerId}");
        RefreshPlayerCards(ActiveSession != null && ActiveSession.IsHost);
    }

    private void OnSessionChanged()
    {
        RefreshPlayerCards(ActiveSession != null && ActiveSession.IsHost);
    }

    // --- 1. ODA OLUŞTURMA (HOST) ---
    public async void OnCreateRoomClicked()
    {
        EnsureNetworkManager();

        SetStatus(createStatusText, "Servislere bağlanılıyor...");
        await UnityServicesInitializer.InitializeServicesAsync();

        if (!UnityServicesInitializer.IsInitialized)
        {
            SetStatus(createStatusText, "Hata: İnternet / UGS bağlantısı sağlanamadı!");
            return;
        }

        string password = createPasswordInput != null ? createPasswordInput.text.Trim() : "";

        if (!string.IsNullOrEmpty(password) && password.Length < 8)
        {
            SetStatus(createStatusText, "Şifre en az 8 karakter olmalıdır!");
            return;
        }

        int selectedMaxPlayers = GetSelectedPlayerCount();
        string selectedMapName = GetSelectedMapSceneName();

        SetStatus(createStatusText, "Oda oluşturuluyor...");
        if (createRoomButton != null) createRoomButton.interactable = false;

        try
        {
            string myCharId = CharacterSelection.GetSelectedId(characterDatabase);
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.NetworkConfig != null)
            {
                NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(myCharId);
            }

            var sessionOptions = new SessionOptions
            {
                Name = $"Oda_{UnityEngine.Random.Range(1000, 9999)}",
                MaxPlayers = selectedMaxPlayers,
                Password = string.IsNullOrEmpty(password) ? null : password,
                IsPrivate = true,
                SessionProperties = new Dictionary<string, SessionProperty>
                {
                    { "map", new SessionProperty(selectedMapName) }
                }
            }.WithRelayNetwork();

            IHostSession hostSession = await MultiplayerService.Instance.CreateSessionAsync(sessionOptions);
            SetActiveSession(hostSession);

            // .WithRelayNetwork() kullanıldığında Netcode (NetworkManager) session tarafından
            // otomatik host olarak başlatılır. Nadiren başlatılmadıysa güvenlik ağı olarak elle başlat.
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
            {
                Debug.LogWarning("[Multiplayer] Session Netcode'u otomatik başlatmadı, StartHost() elle çağrılıyor.");
                NetworkManager.Singleton.StartHost();
            }
            Debug.Log($"[Multiplayer] Host durumu -> IsListening: {NetworkManager.Singleton?.IsListening}, IsServer: {NetworkManager.Singleton?.IsServer}");

            Debug.Log($"[Multiplayer] Oda Oluştu! Oda Kodu: {hostSession.Code}, Max Oyuncu: {selectedMaxPlayers}");
            SetStatus(createStatusText, "Oda başarıyla kuruldu!");

            ShowRoomInfoPanel(hostSession.Code, true);
        }
        catch (SessionException ex)
        {
            Debug.LogError($"[Multiplayer] Oda Kurma Hatası: {ex.Message}");
            SetStatus(createStatusText, $"Oda Kurulamadı: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Multiplayer] Hata: {ex.Message}");
            SetStatus(createStatusText, $"Hata: {ex.Message}");
        }
        finally
        {
            if (createRoomButton != null) createRoomButton.interactable = true;
        }
    }

    private int GetSelectedPlayerCount()
    {
        if (playerCountDropdown != null)
        {
            return playerCountDropdown.value + 2;
        }
        return maxPlayers;
    }

    private string GetSelectedMapSceneName()
    {
        if (mapDropdown != null && mapDropdown.options.Count > 0)
        {
            string rawText = mapDropdown.options[mapDropdown.value].text;
            if (rawText.Contains(" "))
            {
                return rawText.Split(' ')[0].Trim();
            }
            return rawText.Trim();
        }
        return defaultMapSceneName;
    }

    // --- 2. ODAYA KATILMA (CLIENT) ---
    public async void OnJoinRoomClicked()
    {
        EnsureNetworkManager();

        string code = joinCodeInput != null ? joinCodeInput.text.Trim().ToUpper() : "";
        string password = joinPasswordInput != null ? joinPasswordInput.text.Trim() : "";

        if (string.IsNullOrEmpty(code))
        {
            SetStatus(joinStatusText, "Lütfen oda kodunu girin!");
            return;
        }

        SetStatus(joinStatusText, "Servislere bağlanılıyor...");
        await UnityServicesInitializer.InitializeServicesAsync();

        if (!UnityServicesInitializer.IsInitialized)
        {
            SetStatus(joinStatusText, "Hata: İnternet / UGS bağlantısı sağlanamadı!");
            return;
        }

        SetStatus(joinStatusText, "Odaya katılınıyor...");
        if (joinRoomButton != null) joinRoomButton.interactable = false;

        try
        {
            string myCharId = CharacterSelection.GetSelectedId(characterDatabase);
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.NetworkConfig != null)
            {
                NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(myCharId);
            }

            var joinOptions = new JoinSessionOptions
            {
                Password = string.IsNullOrEmpty(password) ? null : password
            };

            ISession joinedSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(code, joinOptions);
            SetActiveSession(joinedSession);

            // Katılımda Netcode (NetworkManager) session tarafından otomatik client olarak başlatılır.
            // Nadiren başlatılmadıysa güvenlik ağı olarak elle başlat.
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
            {
                Debug.LogWarning("[Multiplayer] Session Netcode'u otomatik başlatmadı, StartClient() elle çağrılıyor.");
                NetworkManager.Singleton.StartClient();
            }
            Debug.Log($"[Multiplayer] Client durumu -> IsListening: {NetworkManager.Singleton?.IsListening}, IsConnectedClient: {NetworkManager.Singleton?.IsConnectedClient}");

            Debug.Log($"[Multiplayer] Odaya katılım başarılı! Session ID: {ActiveSession.Id}");
            SetStatus(joinStatusText, "Odaya katılındı! Kurucunun oyunu başlatması bekleniyor...");

            ShowRoomInfoPanel(code, false);
        }
        catch (SessionException ex)
        {
            Debug.LogError($"[Multiplayer] Katılma Hatası: {ex.Message}");
            SetStatus(joinStatusText, "Katılma başarısız! Kodu veya şifreyi kontrol edin.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Multiplayer] Hata: {ex.Message}");
            SetStatus(joinStatusText, $"Hata: {ex.Message}");
        }
        finally
        {
            if (joinRoomButton != null) joinRoomButton.interactable = true;
        }
    }

    // --- 3. OYUNU BAŞLATMA ---
    public void OnStartGameClicked()
    {
        // Sadece oda kurucusu (Host) oyunu başlatabilir.
        if (ActiveSession == null || !ActiveSession.IsHost)
        {
            Debug.LogWarning("[Multiplayer] Oyunu yalnızca oda kurucusu başlatabilir.");
            return;
        }

        // Karakter spawn'ını tamamen kendimiz (OnGameSceneLoadCompleted) yöneteceğimiz için
        // Netcode'un otomatik player spawn mekanizmasını devre dışı bırakıyoruz. Böylece
        // çift karakter oluşmaz ve spawn pozisyonlarını (üst üste binmeyi önleyerek) kontrol ederiz.
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.NetworkConfig != null)
        {
            NetworkManager.Singleton.NetworkConfig.PlayerPrefab = null;
            NetworkManager.Singleton.NetworkConfig.AutoSpawnPlayerPrefabClientSide = false;
        }

        string targetMap = GetSelectedMapSceneName();

        // Netcode'un ağ sahne yöneticisi ile sahne yüklenir. Bu sayede bağlı olan
        // TÜM istemciler otomatik olarak aynı sahneye senkronize geçer.
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
        {
            Debug.Log($"[Multiplayer] Host oyunu başlatıyor. Sahne yükleniyor: {targetMap}");

            // Sahne yüklemesi tamamlandığında (tüm istemciler dahil) karakterleri spawn et.
            // Oyuncular lobide zaten bağlandığından, sahne değişimi tek başına player spawn'ı
            // tetiklemez; bu yüzden sahne yüklendikten sonra elle spawn ediyoruz.
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnGameSceneLoadCompleted;

            if (startGameButton != null) startGameButton.interactable = false;

            NetworkManager.Singleton.SceneManager.LoadScene(targetMap, LoadSceneMode.Single);
        }
        else
        {
            // Netcode aktif değilse (beklenmeyen durum) en azından offline devam et.
            Debug.LogWarning("[Multiplayer] NetworkManager dinlemiyor! Sahne normal yöntemle yükleniyor.");
            SceneManager.LoadScene(targetMap);
        }
    }

        /// <summary>
    /// Oyun sahnesi tüm istemcilerde yüklendikten sonra sunucu tarafından çağrılır.
    /// Bağlı olan her oyuncu için seçtiği karakter prefabını ağ üzerinde spawn eder.
    /// </summary>
    private void OnGameSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode,
        List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        // Bu olay birden fazla sahne için tetiklenebilir; sadece bir kez çalışsın.
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnGameSceneLoadCompleted;

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            var client = NetworkManager.Singleton.ConnectedClients[clientId];

            // Bu oyuncunun zaten bir karakteri varsa tekrar spawn etme.
            if (client.PlayerObject != null)
                continue;

            // Oyuncunun bağlandığında gönderdiği seçili karakter ID'sini al
            string charId = _clientCharacterIds.TryGetValue(clientId, out var id) ? id : "";
            CharacterDefinition charDef = characterDatabase != null ? characterDatabase.GetById(charId) : null;
            
            // Eğer seçilen karakter bulunamazsa varsayılan (fallback) prefabı kullan
            GameObject prefabToSpawn = (charDef != null && charDef.playerPrefab != null) ? charDef.playerPrefab : fallbackPlayerPrefab;

            if (prefabToSpawn == null)
            {
                Debug.LogError($"[Multiplayer] Oyuncu {clientId} için spawn edilecek karakter prefabı bulunamadı!");
                continue;
            }

            GameObject playerInstance = Instantiate(prefabToSpawn);
            var netObj = playerInstance.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                // Karakteri ilgili oyuncunun sahibi (owner) olacak şekilde ağda türet.
                netObj.SpawnAsPlayerObject(clientId, true);
                Debug.Log($"[Multiplayer] Oyuncu {clientId} için karakter ({prefabToSpawn.name}) başarıyla spawn edildi.");
            }
            else
            {
                Debug.LogError($"[Multiplayer] {prefabToSpawn.name} üzerinde NetworkObject bulunamadı!");
                Destroy(playerInstance);
            }
        }
    }

    /// <summary>
    /// Oda bilgi panelini gösterir, oda kodunu ayarlar ve oyuncu kartlarını günceller.
    /// </summary>
    private void ShowRoomInfoPanel(string code, bool isHost)
    {
        if (roomInfoPanel != null)
        {
            roomInfoPanel.SetActive(true);
            if (roomCodeText != null) roomCodeText.text = $"ODA KODU: {code}";
        }

        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(isHost);
        }

        RefreshPlayerCards(isHost);
    }

    /// <summary>
    /// Odaya katılan oyuncuları temsil eden kartları canlı olarak günceller.
    /// </summary>
    public void RefreshPlayerCards(bool isHost = true)
    {
        if (playerCardsContainer == null) return;

        int activePlayerCount = (ActiveSession != null && ActiveSession.Players != null) ? ActiveSession.Players.Count : 1;
        if (activePlayerCount < 1) activePlayerCount = 1;

        // Yeterli kart öğesi yoksa ilk kartı şablon alıp kopyala
        while (playerCardsContainer.childCount < activePlayerCount)
        {
            if (playerCardsContainer.childCount > 0)
            {
                GameObject template = playerCardsContainer.GetChild(0).gameObject;
                Instantiate(template, playerCardsContainer);
            }
            else
            {
                break;
            }
        }

        int totalCards = playerCardsContainer.childCount;
        for (int i = 0; i < totalCards; i++)
        {
            Transform cardTransform = playerCardsContainer.GetChild(i);

            if (i < activePlayerCount)
            {
                cardTransform.gameObject.SetActive(true);

                IReadOnlyPlayer player = (ActiveSession != null && ActiveSession.Players != null && i < ActiveSession.Players.Count)
                    ? ActiveSession.Players[i]
                    : null;

                TMP_Text[] textComponents = cardTransform.GetComponentsInChildren<TMP_Text>(true);
                foreach (TMP_Text txt in textComponents)
                {
                    if (txt.name.Equals("PlayerNameText", StringComparison.OrdinalIgnoreCase))
                    {
                        string pName = $"Oyuncu {i + 1}";
                        if (player != null && ActiveSession != null && ActiveSession.CurrentPlayer != null && player.Id == ActiveSession.CurrentPlayer.Id)
                        {
                            pName += " (Siz)";
                        }
                        txt.text = pName;
                    }
                    else if (txt.name.Equals("PlayerRoleText", StringComparison.OrdinalIgnoreCase))
                    {
                        bool isPlayerHost = false;
                        if (player != null && ActiveSession != null)
                        {
                            isPlayerHost = (player.Id == ActiveSession.Host);
                        }
                        else if (i == 0)
                        {
                            isPlayerHost = true;
                        }

                        txt.text = isPlayerHost ? "KURUCU" : "KATILDI";
                    }
                }
            }
            else
            {
                cardTransform.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Harita görselini güncellemek için kullanılabilecek kolay erişim metodu.
    /// </summary>
    public void SetMapSprite(Sprite newMapSprite)
    {
        if (roomMapImage != null && newMapSprite != null)
        {
            roomMapImage.sprite = newMapSprite;
            roomMapImage.color = Color.white;
        }
    }

    private void SetStatus(TMP_Text textComponent, string message)
    {
        if (textComponent != null) textComponent.text = message;
    }
}