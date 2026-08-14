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
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private List<GameObject> networkPrefabs = new List<GameObject>();

    public ISession ActiveSession { get; private set; }

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

        ConfigureNetworkConfig(NetworkManager.Singleton);
    }

    private void ConfigureNetworkConfig(NetworkManager nm)
    {
        if (nm == null || nm.NetworkConfig == null) return;

        nm.NetworkConfig.EnableSceneManagement = true;

        // Menüdeyken karakterin spawn olmasını engellemek için Lobi aşamasında PlayerPrefab null kalmalı.
        // Oyunu başlat butonuna basıldığında PlayerPrefab atanacaktır.
        nm.NetworkConfig.PlayerPrefab = null;

        if (networkPrefabs != null)
        {
            foreach (var prefab in networkPrefabs)
            {
                if (prefab == null) continue;

                bool alreadyAdded = false;
                foreach (var registered in nm.NetworkConfig.Prefabs.Prefabs)
                {
                    if (registered.Prefab == prefab)
                    {
                        alreadyAdded = true;
                        break;
                    }
                }

                if (!alreadyAdded)
                {
                    nm.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = prefab });
                }
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
            var joinOptions = new JoinSessionOptions
            {
                Password = string.IsNullOrEmpty(password) ? null : password
            };

            ISession joinedSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(code, joinOptions);
            SetActiveSession(joinedSession);

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
        if (ActiveSession != null && ActiveSession.IsHost)
        {
            // Oyun sahnesine geçmeden önce PlayerPrefab'i NetworkConfig'e atıyoruz ki sahnede karakter türetilsin.
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.NetworkConfig != null)
            {
                NetworkManager.Singleton.NetworkConfig.PlayerPrefab = playerPrefab;
            }

            string targetMap = GetSelectedMapSceneName();

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.SceneManager.LoadScene(targetMap, LoadSceneMode.Single);
            }
            else
            {
                SceneManager.LoadScene(targetMap);
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