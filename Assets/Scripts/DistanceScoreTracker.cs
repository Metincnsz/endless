using UnityEngine;
using Unity.Netcode;

public class DistanceScoreTracker : NetworkBehaviour
{
    [Header("Skor Ayarları")]
    [Tooltip("Her 1 metre koşu için kaç puan verilsin?")]
    public float metreBasinaPuan = 1f;

    private float startingX;
    private bool isTracking = true;
    private KarakterKontrol karakter;

    private void Start()
    {
        karakter = GetComponent<KarakterKontrol>();
        startingX = transform.position.x;
    }

    private void Update()
    {
        // Sadece bu karakterin asıl sahibi skoru takip eder ve sunucuya bildirir
        if (!IsOwner || !isTracking) return;

        if (karakter != null && !karakter.IsAlive.Value)
        {
            isTracking = false;
            return;
        }

        float katedilenMesafe = Mathf.Abs(transform.position.x - startingX);
        int hesaplananSkor = Mathf.FloorToInt(katedilenMesafe * metreBasinaPuan);

        // Sunucudaki ağ değişkenini güncellemek için RPC göndeririz
        if (karakter != null)
        {
            karakter.GuncelleSkorServerRpc(hesaplananSkor);
        }

        // Yerel arayüzü de hemen güncelle
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.UpdateScore(hesaplananSkor);
        }
    }

    public void StopTracking()
    {
        isTracking = false;
    }
}