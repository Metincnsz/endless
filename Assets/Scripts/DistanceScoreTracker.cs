using UnityEngine;
using Unity.Netcode;

public class DistanceScoreTracker : NetworkBehaviour
{
    [Header("Skor Ayarları")]
    [Tooltip("Her 1 metre koşu için kaç puan verilsin?")]
    public float metreBasinaPuan = 1f;

    [Header("Ağ Senkronizasyon Ayarları")]
    [Tooltip("Sunucuya skor güncelleme RPC'sinin gönderilme sıklığı aralığı (Saniye)")]
    public float networkSyncInterval = 0.25f;

    private float startingX;
    private bool isTracking = true;
    private KarakterKontrol karakter;

    private int lastLocalScore = 0;
    private int lastSentNetworkScore = -1;
    private float nextNetworkSyncTime = 0f;

    private void Start()
    {
        karakter = GetComponent<KarakterKontrol>();
        startingX = transform.position.x;
    }

    private void Update()
    {
        if (!IsOwner || !isTracking) return;

        if (karakter != null && !karakter.IsAlive.Value)
        {
            StopTracking();
            return;
        }

        float katedilenMesafe = Mathf.Abs(transform.position.x - startingX);
        int hesaplananSkor = Mathf.FloorToInt(katedilenMesafe * metreBasinaPuan);

        if (hesaplananSkor > lastLocalScore)
        {
            int deltaDistance = hesaplananSkor - lastLocalScore;
            lastLocalScore = hesaplananSkor;

            // Görev İlerlemesi: Koşulan farkı (metre) mesafe görevlerine ekle
            TaskManager.AddProgressToTasks("distance", deltaDistance);

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.UpdateScore(hesaplananSkor);
            }
        }

        if (hesaplananSkor != lastSentNetworkScore && Time.time >= nextNetworkSyncTime)
        {
            SendScoreToServer(hesaplananSkor);
        }
    }

    private void SendScoreToServer(int score)
    {
        lastSentNetworkScore = score;
        nextNetworkSyncTime = Time.time + networkSyncInterval;

        if (karakter != null)
        {
            karakter.GuncelleSkorServerRpc(score);
        }
    }

    public void StopTracking()
    {
        if (!isTracking) return;

        isTracking = false;

        if (IsOwner && lastLocalScore > lastSentNetworkScore && karakter != null)
        {
            SendScoreToServer(lastLocalScore);
        }
    }
}