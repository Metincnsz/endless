using UnityEngine;
using Unity.Netcode; // Netcode eklendi

public class YolTetikleyici : MonoBehaviour
{
    private ZeminKontrol zeminKontrol;
    private bool tetiklendi = false;

    void Start()
    {
        zeminKontrol = ZeminKontrol.Instance;
    }

    void OnTriggerEnter(Collider other)
    {
        // 1. Eşleşme aktifse sadece SUNUCU (Host) yeni yol üretim tetiğini işlesin.
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
            return;

        // Karakterin çarpıp çarpmadığını kontrol et
        if (!tetiklendi && (other.CompareTag("Player") || other.GetComponent<KarakterKontrol>() != null))
        {
            tetiklendi = true;

            if (zeminKontrol != null)
            {
                zeminKontrol.YolSpawnEt();
            }

            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }
        }
    }
}