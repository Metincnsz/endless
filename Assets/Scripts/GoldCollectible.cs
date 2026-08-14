using UnityEngine;
using Unity.Netcode; // Netcode eklendi

public class GoldCollectible : NetworkBehaviour // NetworkBehaviour yapıldı
{
    [Header("Ayarlar")]
    [SerializeField] private int goldValue = 1;
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private GameObject collectEffectPrefab;

    private void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Çarpışmayı sadece Sunucu işler
        if (!IsServer) return;

        if (other.CompareTag("Player") || other.GetComponent<KarakterKontrol>() != null)
        {
            var netObj = other.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                ulong toplayanOyuncuId = netObj.OwnerClientId;

                // Sadece toplayan oyuncunun yerel UI'ına altını ekle (Hedefli RPC)
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { toplayanOyuncuId }
                    }
                };
                AddGoldToClientRpc(goldValue, clientRpcParams);

                // Toplama efektini tüm oyuncularda oynat
                PlayCollectEffectClientRpc(transform.position);

                // Altını ağdan kaldır (Herkesin ekranında silinir)
                GetComponent<NetworkObject>().Despawn(true);
            }
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