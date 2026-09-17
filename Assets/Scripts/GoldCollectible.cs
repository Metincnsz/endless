using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class GoldCollectible : NetworkBehaviour
{
    [Header("Ayarlar")]
    [SerializeField] private int goldValue = 1;
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private GameObject collectEffectPrefab;

    [Header("Ömür / Süre Ayarı")]
    [Tooltip("Toplanmayan altının sahnede kalacağı maksimum süre (Saniye).")]
    [SerializeField] private float lifeTime = 10f; // Inspector'dan süreyi buradan ayarlayabilirsiniz

    private bool isCollected = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Çok oyunculu (Multiplayer) modda zamanlayıcıyı yalnızca Sunucu/Host yönetir
        if (IsServer && lifeTime > 0f)
        {
            StartCoroutine(YokOlmaZamanlayicisi());
        }
    }

    private void Start()
    {
        // Çevrimdışı (Offline / Tek Oyunculu) mod kontrolü
        bool isNetworkActive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (!isNetworkActive && lifeTime > 0f)
        {
            StartCoroutine(YokOlmaZamanlayicisi());
        }
    }

    private IEnumerator YokOlmaZamanlayicisi()
    {
        yield return new WaitForSeconds(lifeTime);

        // Belirlenen süre sonunda altın henüz toplanmadıysa yok et
        if (!isCollected)
        {
            YokEt();
        }
    }

    private void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        // Çarpışanın bir oyuncu olup olmadığını doğrula
        bool isPlayer = other.CompareTag("Player") || other.GetComponent<KarakterKontrol>() != null;
        if (!isPlayer) return;

        // İksir çarpanını al (Aktif bonus iksiri varsa 2x, yoksa 1x)
        PlayerPotionController potionCtrl = other.GetComponent<PlayerPotionController>();
        if (potionCtrl == null) potionCtrl = other.GetComponentInParent<PlayerPotionController>();
        int multiplier = (potionCtrl != null) ? potionCtrl.GoldMultiplier : 1;
        int finalGoldAmount = goldValue * multiplier;

        bool isNetworkActive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        if (isNetworkActive)
        {
            // Çok oyunculu modda altın toplama işlemini sadece Sunucu/Host yönetir
            if (!IsServer) return;

            var netObj = other.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                isCollected = true;
                ulong toplayanOyuncuId = netObj.OwnerClientId;

                // Sadece toplayan oyuncuya özel altını ekle (Hedefli ClientRpc)
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { toplayanOyuncuId }
                    }
                };
                AddGoldToClientRpc(finalGoldAmount, clientRpcParams);

                // Efekti tüm istemcilerde oynat
                PlayCollectEffectClientRpc(transform.position);

                // Altını ağdan kaldır (Herkesin ekranında silinir)
                YokEt();
            }
        }
        else
        {
            // Çevrimdışı (Offline / Tek Oyunculu) mod
            isCollected = true;

            if (GoldManager.Instance != null)
            {
                GoldManager.Instance.AddGold(finalGoldAmount);
            }

            if (collectEffectPrefab != null)
            {
                Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }

    private void YokEt()
    {
        var goldNetObj = GetComponent<NetworkObject>();
        if (goldNetObj != null && goldNetObj.IsSpawned)
        {
            goldNetObj.Despawn(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [ClientRpc]
    private void AddGoldToClientRpc(int miktar, ClientRpcParams clientRpcParams = default)
    {
        if (GoldManager.Instance != null)
        {
            GoldManager.Instance.AddGold(miktar);
        }
    }

    [ClientRpc]
    private void PlayCollectEffectClientRpc(Vector3 pos)
    {
        if (collectEffectPrefab != null)
        {
            Instantiate(collectEffectPrefab, pos, Quaternion.identity);
        }
    }
}