using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerRoomManager : MonoBehaviour
{
    public static MultiplayerRoomManager Instance { get; private set; }

    public const string RoomMapIdKey = "mapId";
    public const string RoomSceneKey = "map";

    [Header("Harita Veritabanı")]
    [SerializeField] private HaritaVeritabani haritaVeritabani;

    [Header("Oda Kur Paneli Elemanları")]
    [SerializeField] private TMP_InputField createPasswordInput;
    [SerializeField] private TMP_Dropdown mapDropdown;
    [SerializeField] private TMP_Dropdown playerCountDropdown;
    [SerializeField] private UnityEngine.UI.Button createRoomButton;
    [SerializeField] private TMP_Text createStatusText;

    [Header("Odaya Katıl Paneli Elemanları")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_InputField joinPasswordInput;
    [SerializeField] private UnityEngine.UI.Button joinRoomButton;
    [SerializeField] private TMP_Text joinStatusText;

    [Header("Oda Bilgi / Bekleme Paneli")]
    [SerializeField] private GameObject roomInfoPanel;
    [SerializeField] private TMP_Text roomCodeText;
    [SerializeField] private UnityEngine.UI.Button startGameButton;
    [SerializeField] private UnityEngine.UI.Image roomMapImage;
    [SerializeField] private TMP_Text roomMapNameText;
    [SerializeField] private Transform playerCardsContainer;

    [Header("Ayar Parametreleri")]
    [SerializeField] private string defaultMapSceneName = "SampleScene";
    [SerializeField] private int maxPlayers = 5;

    [Header("Netcode Yapılandırması")]
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private GameObject fallbackPlayerPrefab;
    [Tooltip("Bağlantı başlamadan önce host ve client'a kaydedilecek ortak ağ prefabları.")]
    [SerializeField] private Unity.Netcode.NetworkPrefabsList networkPrefabsList;

    public ISession ActiveSession { get; private set; }

    private readonly List<HaritaVerisi> dropdownMaps =
        new List<HaritaVerisi>();

    private readonly Dictionary<ulong, string> clientCharacterIds =
        new Dictionary<ulong, string>();

    private bool busy;
    private bool startingGame;
    private string loadingSceneName;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (createRoomButton != null)
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);

        if (joinRoomButton != null)
            joinRoomButton.onClick.AddListener(OnJoinRoomClicked);

        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartGameClicked);

        if (roomInfoPanel != null)
            roomInfoPanel.SetActive(false);

        InitializePlayerCountDropdown();
        RefreshMapDropdown();
    }

    private void OnDestroy()
    {
        UnsubscribeSessionEvents(ActiveSession);

        if (createRoomButton != null)
            createRoomButton.onClick.RemoveListener(OnCreateRoomClicked);

        if (joinRoomButton != null)
            joinRoomButton.onClick.RemoveListener(OnJoinRoomClicked);

        if (startGameButton != null)
            startGameButton.onClick.RemoveListener(OnStartGameClicked);

        var nm = NetworkManager.Singleton;
        if (nm != null && nm.SceneManager != null)
            nm.SceneManager.OnLoadEventCompleted -= OnGameSceneLoadCompleted;

        if (Instance == this)
            Instance = null;
    }

    private void InitializePlayerCountDropdown()
    {
        if (playerCountDropdown == null)
            return;

        playerCountDropdown.ClearOptions();
        playerCountDropdown.AddOptions(
            new List<string>
            {
                "2 Oyuncu",
                "3 Oyuncu",
                "4 Oyuncu",
                "5 Oyuncu"
            });

        playerCountDropdown.SetValueWithoutNotify(3);
        playerCountDropdown.RefreshShownValue();
    }

    // Oda kur paneli her açıldığında da çağrılabilir.
    public void RefreshMapDropdown()
    {
        if (mapDropdown == null)
            return;

        dropdownMaps.Clear();
        mapDropdown.ClearOptions();

        if (haritaVeritabani != null)
        {
            dropdownMaps.AddRange(
                haritaVeritabani.KullanilabilirHaritalariGetir());
        }

        var options = new List<string>();

        foreach (HaritaVerisi map in dropdownMaps)
            options.Add(map.haritaAdi);

        if (options.Count == 0)
        {
            mapDropdown.AddOptions(
                new List<string> { "Kullanılabilir harita yok" });

            mapDropdown.interactable = false;
            return;
        }

        mapDropdown.AddOptions(options);

        HaritaVerisi personalSelection =
            haritaVeritabani.SecilenHaritayiGetir();

        int selectedIndex = dropdownMaps.IndexOf(personalSelection);

        mapDropdown.SetValueWithoutNotify(
            Mathf.Max(0, selectedIndex));

        mapDropdown.RefreshShownValue();

        // Oda kurulduktan sonra dropdown değiştirilemez.
        mapDropdown.interactable = ActiveSession == null;
    }

    private HaritaVerisi GetDropdownMap()
    {
        if (mapDropdown == null ||
            mapDropdown.value < 0 ||
            mapDropdown.value >= dropdownMaps.Count)
        {
            return null;
        }

        return dropdownMaps[mapDropdown.value];
    }

    private int GetSelectedPlayerCount()
    {
        return playerCountDropdown != null
            ? Mathf.Clamp(playerCountDropdown.value + 2, 2, 5)
            : Mathf.Clamp(maxPlayers, 2, 5);
    }

    // Multiplayer haritasını kişisel PlayerPrefs'ten değil,
    // aktif odanın verisinden bulur.
    public static HaritaVerisi GetRoomMap(
        HaritaVeritabani database,
        ISession session)
    {
        if (database == null)
            throw new InvalidOperationException(
                "Harita veritabanı atanmamış.");

        if (session == null ||
            !session.Properties.TryGetValue(
                RoomMapIdKey, out SessionProperty property) ||
            string.IsNullOrWhiteSpace(property.Value))
        {
            throw new InvalidOperationException(
                "Odada harita ID bilgisi bulunamadı.");
        }

        foreach (HaritaVerisi map
                 in database.KullanilabilirHaritalariGetir())
        {
            if (map.haritaId == property.Value)
                return map;
        }

        throw new InvalidOperationException(
            "Odanın haritası bu oyun sürümünde bulunamadı: " +
            property.Value);
    }

    private string GetRoomSceneName()
    {
        if (ActiveSession == null ||
            !ActiveSession.Properties.TryGetValue(
                RoomSceneKey, out SessionProperty property) ||
            string.IsNullOrWhiteSpace(property.Value))
        {
            throw new InvalidOperationException(
                "Odada oyun sahnesi bilgisi bulunamadı.");
        }

        return property.Value;
    }

    private void PrepareNetwork()
    {
        var nm = NetworkManager.Singleton;

        if (nm != null && nm.IsListening)
        {
            throw new InvalidOperationException(
                "Ağ bağlantısı zaten açık. Önce mevcut oyundan çık.");
        }

        if (haritaVeritabani == null)
            throw new InvalidOperationException(
                "Harita veritabanını Inspector'dan bağla.");

        if (nm == null)
        {
            var go = new GameObject("NetworkManager");
            var transport = go.AddComponent<UnityTransport>();

            nm = go.AddComponent<NetworkManager>();
            nm.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                EnableSceneManagement = true
            };

            DontDestroyOnLoad(go);
        }

        nm.NetworkConfig.EnableSceneManagement = true;
        nm.NetworkConfig.ConnectionApproval = true;
        nm.NetworkConfig.PlayerPrefab = null;
        nm.NetworkConfig.AutoSpawnPlayerPrefabClientSide = false;

        nm.ConnectionApprovalCallback = ApprovalCheck;

        clientCharacterIds.Clear();

        // PrepareNetwork hem oda kurarken hem katılırken bağlantıdan önce çağrılır.
        // Altın, engel ve büyü prefabları da iki tarafta kayıtlı olmalı.
        if (networkPrefabsList != null)
        {
            foreach (var networkPrefab in networkPrefabsList.PrefabList)
                RegisterPrefab(nm, networkPrefab.Prefab);
        }

        if (characterDatabase != null &&
            characterDatabase.characters != null)
        {
            foreach (var character in characterDatabase.characters)
            {
                if (character != null)
                    RegisterPrefab(nm, character.playerPrefab);
            }
        }

        RegisterPrefab(nm, fallbackPlayerPrefab);

        // Hem host hem client bütün haritaların yol prefablarını bilir.
        foreach (HaritaVerisi map
                 in haritaVeritabani.KullanilabilirHaritalariGetir())
        {
            foreach (GameObject road in map.yolPrefablari)
                RegisterPrefab(nm, road);
        }

        string characterId =
            CharacterSelection.GetSelectedId(characterDatabase);

        nm.NetworkConfig.ConnectionData =
            System.Text.Encoding.UTF8.GetBytes(characterId ?? "");
    }

    private void RegisterPrefab(NetworkManager nm, GameObject prefab)
    {
        if (prefab == null)
            return;

        if (prefab.GetComponent<NetworkObject>() == null)
        {
            throw new InvalidOperationException(
                prefab.name +
                " prefabının kökünde NetworkObject bulunmalı.");
        }

        foreach (var registered in nm.NetworkConfig.Prefabs.Prefabs)
        {
            if (registered.Prefab == prefab)
                return;
        }

        nm.NetworkConfig.Prefabs.Add(
            new NetworkPrefab { Prefab = prefab });
    }

    private void ApprovalCheck(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        string characterId =
            request.Payload != null && request.Payload.Length > 0
                ? System.Text.Encoding.UTF8.GetString(request.Payload)
                : "";

        clientCharacterIds[request.ClientNetworkId] = characterId;

        response.Approved = true;
        response.CreatePlayerObject = false;
        response.Pending = false;
    }

    private void SetActiveSession(ISession session)
    {
        UnsubscribeSessionEvents(ActiveSession);
        ActiveSession = session;
        SubscribeSessionEvents(ActiveSession);
    }

    private void SubscribeSessionEvents(ISession session)
    {
        if (session == null)
            return;

        session.PlayerJoined += OnPlayerChanged;
        session.PlayerLeaving += OnPlayerChanged;
        session.PlayerHasLeft += OnPlayerChanged;
        session.Changed += OnSessionChanged;
    }

    private void UnsubscribeSessionEvents(ISession session)
    {
        if (session == null)
            return;

        session.PlayerJoined -= OnPlayerChanged;
        session.PlayerLeaving -= OnPlayerChanged;
        session.PlayerHasLeft -= OnPlayerChanged;
        session.Changed -= OnSessionChanged;
    }

    private void OnPlayerChanged(string playerId)
    {
        OnSessionChanged();
    }

    private void OnSessionChanged()
    {
        RefreshPlayerCards(
            ActiveSession != null && ActiveSession.IsHost);

        RefreshRoomMap();
    }

    public async void OnCreateRoomClicked()
    {
        if (busy || ActiveSession != null)
            return;

        busy = true;
        SetRoomButtonsInteractable(false);

        try
        {
            HaritaVerisi selectedMap = GetDropdownMap();

            if (selectedMap == null)
                throw new InvalidOperationException(
                    "Önce kullanılabilir bir harita seç.");

            string password = createPasswordInput != null
                ? createPasswordInput.text.Trim()
                : "";

            ValidatePassword(password);

            if (!Application.CanStreamedLevelBeLoaded(
                    defaultMapSceneName))
            {
                throw new InvalidOperationException(
                    "Oyun sahnesi Build Profiles listesinde yok: " +
                    defaultMapSceneName);
            }

            SetStatus(createStatusText, "Servislere bağlanılıyor...");

            await UnityServicesInitializer.InitializeServicesAsync();

            if (!UnityServicesInitializer.IsInitialized)
                throw new InvalidOperationException(
                    "İnternet / servis bağlantısı sağlanamadı.");

            PrepareNetwork();

            var options = new SessionOptions
            {
                Name = "Oda_" + UnityEngine.Random.Range(1000, 9999),
                MaxPlayers = GetSelectedPlayerCount(),
                Password = string.IsNullOrEmpty(password)
                    ? null
                    : password,
                IsPrivate = true,
                SessionProperties =
                    new Dictionary<string, SessionProperty>
                    {
                        {
                            RoomSceneKey,
                            new SessionProperty(
                                defaultMapSceneName,
                                VisibilityPropertyOptions.Member)
                        },
                        {
                            RoomMapIdKey,
                            new SessionProperty(
                                selectedMap.haritaId,
                                VisibilityPropertyOptions.Member)
                        }
                    }
            }.WithRelayNetwork();

            IHostSession session =
                await MultiplayerService.Instance
                    .CreateSessionAsync(options);

            SetActiveSession(session);

            // Networking bu akışta paket tarafından başlatılır.
            // Burada ayrıca StartHost çağrılmaz.

            if (mapDropdown != null)
                mapDropdown.interactable = false;

            MainMenuManager.SecilenMod = "Host";

            ShowRoomInfoPanel(session.Code, true);

            SetStatus(
                createStatusText,
                "Oda kuruldu. Harita: " + selectedMap.haritaAdi);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            SetStatus(createStatusText, "Oda kurulamadı: " + ex.Message);
        }
        finally
        {
            busy = false;
            SetRoomButtonsInteractable(ActiveSession == null);
        }
    }

    public async void OnJoinRoomClicked()
    {
        if (busy || ActiveSession != null)
            return;

        busy = true;
        SetRoomButtonsInteractable(false);

        ISession joinedSession = null;

        try
        {
            string code = joinCodeInput != null
                ? joinCodeInput.text.Trim().ToUpperInvariant()
                : "";

            string password = joinPasswordInput != null
                ? joinPasswordInput.text.Trim()
                : "";

            if (string.IsNullOrWhiteSpace(code))
                throw new InvalidOperationException(
                    "Oda kodunu gir.");

            ValidatePassword(password);

            SetStatus(joinStatusText, "Servislere bağlanılıyor...");

            await UnityServicesInitializer.InitializeServicesAsync();

            if (!UnityServicesInitializer.IsInitialized)
                throw new InvalidOperationException(
                    "İnternet / servis bağlantısı sağlanamadı.");

            PrepareNetwork();

            var options = new JoinSessionOptions
            {
                Password = string.IsNullOrEmpty(password)
                    ? null
                    : password
            };

            SetStatus(joinStatusText, "Odaya katılınıyor...");

            joinedSession =
                await MultiplayerService.Instance
                    .JoinSessionByCodeAsync(code, options);

            // Bu cihaz odanın seçili haritasını tanıyor mu?
            GetRoomMap(haritaVeritabani, joinedSession);

            if (!joinedSession.Properties.TryGetValue(
                    RoomSceneKey, out SessionProperty sceneProperty) ||
                !Application.CanStreamedLevelBeLoaded(
                    sceneProperty.Value))
            {
                throw new InvalidOperationException(
                    "Odanın oyun sahnesi bu sürümde bulunamadı.");
            }

            SetActiveSession(joinedSession);

            // Burada ayrıca StartClient çağrılmaz.

            MainMenuManager.SecilenMod = "Client";

            ShowRoomInfoPanel(joinedSession.Code, false);

            SetStatus(
                joinStatusText,
                "Katıldın. Kurucunun başlatması bekleniyor.");
        }
        catch (Exception ex)
        {
            // Harita/sahne uyumsuzsa odada kalma.
            if (joinedSession != null && ActiveSession == null)
            {
                try
                {
                    await joinedSession.LeaveAsync();
                }
                catch (Exception leaveException)
                {
                    Debug.LogException(leaveException);
                }

                var nm = NetworkManager.Singleton;
                if (nm != null && nm.IsListening)
                    nm.Shutdown();
            }

            Debug.LogException(ex);
            SetStatus(joinStatusText, "Katılım başarısız: " + ex.Message);
        }
        finally
        {
            busy = false;
            SetRoomButtonsInteractable(ActiveSession == null);
        }
    }

    private void ValidatePassword(string password)
    {
        if (!string.IsNullOrEmpty(password) &&
            (password.Length < 8 || password.Length > 64))
        {
            throw new InvalidOperationException(
                "Şifre 8–64 karakter olmalı.");
        }
    }

    public async void OnStartGameClicked()
    {
        if (startingGame ||
            ActiveSession == null ||
            !ActiveSession.IsHost)
        {
            return;
        }

        var nm = NetworkManager.Singleton;

        if (nm == null ||
            !nm.IsListening ||
            !nm.IsServer ||
            nm.SceneManager == null)
        {
            SetStatus(
                createStatusText,
                "Ağ bağlantısı henüz hazır değil.");
            return;
        }

        if (nm.ConnectedClientsIds.Count < ActiveSession.PlayerCount)
        {
            SetStatus(
                createStatusText,
                "Oyuncuların ağ bağlantısının tamamlanmasını bekle.");
            return;
        }

        startingGame = true;

        if (startGameButton != null)
            startGameButton.interactable = false;

        try
        {
            // Dropdown değil, odada kayıtlı harita esas alınır.
            HaritaVerisi map =
                GetRoomMap(haritaVeritabani, ActiveSession);

            loadingSceneName = GetRoomSceneName();

            if (!Application.CanStreamedLevelBeLoaded(
                    loadingSceneName))
            {
                throw new InvalidOperationException(
                    "Oyun sahnesi yüklenemiyor: " +
                    loadingSceneName);
            }

            // Oyun başladıktan sonra yeni katılımı kapat.
            IHostSession host = ActiveSession.AsHost();
            host.IsLocked = true;
            await host.SavePropertiesAsync();

            nm.SceneManager.OnLoadEventCompleted -=
                OnGameSceneLoadCompleted;

            nm.SceneManager.OnLoadEventCompleted +=
                OnGameSceneLoadCompleted;

            SceneEventProgressStatus status =
                nm.SceneManager.LoadScene(
                    loadingSceneName,
                    LoadSceneMode.Single);

            if (status != SceneEventProgressStatus.Started)
            {
                nm.SceneManager.OnLoadEventCompleted -=
                    OnGameSceneLoadCompleted;

                throw new InvalidOperationException(
                    "Sahne yükleme başlatılamadı: " + status);
            }

            Debug.Log(
                "[Multiplayer] Harita: " + map.haritaAdi +
                ", ID: " + map.haritaId +
                ", Sahne: " + loadingSceneName);
        }
        catch (Exception ex)
        {
            startingGame = false;

            if (startGameButton != null)
                startGameButton.interactable = true;

            Debug.LogException(ex);
            SetStatus(createStatusText, ex.Message);
        }
    }

    private void OnGameSceneLoadCompleted(
        string sceneName,
        LoadSceneMode loadSceneMode,
        List<ulong> clientsCompleted,
        List<ulong> clientsTimedOut)
    {
        var nm = NetworkManager.Singleton;

        if (nm == null ||
            !nm.IsServer ||
            sceneName != loadingSceneName)
        {
            return;
        }

        nm.SceneManager.OnLoadEventCompleted -=
            OnGameSceneLoadCompleted;

        if (clientsTimedOut.Count > 0)
        {
            Debug.LogWarning(
                "[Multiplayer] Sahne yükleme zaman aşımı: " +
                clientsTimedOut.Count + " oyuncu.");
        }

        foreach (ulong clientId in clientsCompleted)
        {
            if (!nm.ConnectedClients.TryGetValue(
                    clientId, out var client) ||
                client.PlayerObject != null)
            {
                continue;
            }

            string characterId =
                clientCharacterIds.TryGetValue(clientId, out string id)
                    ? id
                    : "";

            CharacterDefinition definition =
                characterDatabase != null
                    ? characterDatabase.GetById(characterId)
                    : null;

            GameObject prefab =
                definition != null && definition.playerPrefab != null
                    ? definition.playerPrefab
                    : fallbackPlayerPrefab;

            if (prefab == null)
            {
                Debug.LogError(
                    "[Multiplayer] Karakter prefabı yok: " + clientId);
                continue;
            }

            GameObject instance = Instantiate(prefab);
            NetworkObject networkObject =
                instance.GetComponent<NetworkObject>();

            if (networkObject == null)
            {
                Debug.LogError(
                    prefab.name + ": NetworkObject bulunamadı.");

                Destroy(instance);
                continue;
            }

            networkObject.SpawnAsPlayerObject(clientId, true);
        }
    }

    private void ShowRoomInfoPanel(string code, bool isHost)
    {
        if (roomInfoPanel != null)
            roomInfoPanel.SetActive(true);

        if (roomCodeText != null)
            roomCodeText.text = "ODA KODU: " + code;

        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(isHost);
            startGameButton.interactable = true;
        }

        RefreshPlayerCards(isHost);
        RefreshRoomMap();
    }

    private void RefreshRoomMap()
    {
        if (ActiveSession == null)
            return;

        try
        {
            HaritaVerisi map =
                GetRoomMap(haritaVeritabani, ActiveSession);

            if (roomMapNameText != null)
                roomMapNameText.text = map.haritaAdi;

            SetMapSprite(map.kartGorseli);
        }
        catch (Exception ex)
        {
            Debug.LogError("[Multiplayer] " + ex.Message);

            if (roomMapNameText != null)
                roomMapNameText.text = "Harita bilgisi alınamadı";
        }
    }

    public void RefreshPlayerCards(bool isHost = true)
    {
        if (playerCardsContainer == null)
            return;

        int count = ActiveSession != null
            ? Mathf.Max(1, ActiveSession.PlayerCount)
            : 1;

        while (playerCardsContainer.childCount < count)
        {
            if (playerCardsContainer.childCount == 0)
                break;

            Instantiate(
                playerCardsContainer.GetChild(0).gameObject,
                playerCardsContainer);
        }

        for (int i = 0; i < playerCardsContainer.childCount; i++)
        {
            Transform card = playerCardsContainer.GetChild(i);
            card.gameObject.SetActive(i < count);

            if (i >= count)
                continue;

            IReadOnlyPlayer player =
                ActiveSession != null &&
                i < ActiveSession.Players.Count
                    ? ActiveSession.Players[i]
                    : null;

            foreach (TMP_Text text
                     in card.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name.Equals(
                        "PlayerNameText",
                        StringComparison.OrdinalIgnoreCase))
                {
                    bool isMe =
                        player != null &&
                        ActiveSession.CurrentPlayer != null &&
                        player.Id == ActiveSession.CurrentPlayer.Id;

                    text.text =
                        "Oyuncu " + (i + 1) +
                        (isMe ? " (Siz)" : "");
                }
                else if (text.name.Equals(
                             "PlayerRoleText",
                             StringComparison.OrdinalIgnoreCase))
                {
                    bool playerIsHost =
                        player != null
                            ? player.Id == ActiveSession.Host
                            : i == 0;

                    text.text = playerIsHost ? "KURUCU" : "KATILDI";
                }
            }
        }
    }

    public void SetMapSprite(Sprite sprite)
    {
        if (roomMapImage == null)
            return;

        roomMapImage.sprite = sprite;
        roomMapImage.color = sprite != null
            ? Color.white
            : new Color(0.25f, 0.25f, 0.25f, 1f);
    }

    // Bekleme panelinden ayrılmak için kullanılır.
    public async void OnLeaveRoomClicked()
    {
        if (busy || startingGame)
            return;

        busy = true;

        try
        {
            if (ActiveSession != null)
                await ActiveSession.LeaveAsync();

            SetActiveSession(null);

            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsListening)
                nm.Shutdown();

            clientCharacterIds.Clear();
            MainMenuManager.SecilenMod = "Offline";

            if (roomInfoPanel != null)
                roomInfoPanel.SetActive(false);

            RefreshMapDropdown();
            SetRoomButtonsInteractable(true);

            var menu =
                UnityEngine.Object.FindFirstObjectByType<MainMenuManager>();

            if (menu != null)
                menu.BackToMainMenu();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            SetStatus(createStatusText, "Odadan çıkılamadı: " + ex.Message);
        }
        finally
        {
            busy = false;
        }
    }

    private void SetRoomButtonsInteractable(bool value)
    {
        if (createRoomButton != null)
            createRoomButton.interactable = value;

        if (joinRoomButton != null)
            joinRoomButton.interactable = value;
    }

    private void SetStatus(TMP_Text text, string message)
    {
        if (text != null)
            text.text = message;
    }
}