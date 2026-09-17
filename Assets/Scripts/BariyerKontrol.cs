using UnityEngine;
using Unity.Netcode;

public class BariyerKontrol : NetworkBehaviour
{
    [Header("Hareket ve Yok Olma Ayarları")]
    public float hareketHizi = 15f;
    public float yokOlmaX = 50f;

    private bool baslatildi = false;

    private void Start()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.AktifOrtam != null)
        {
            hareketHizi = LevelManager.Instance.AktifOrtam.engelHizi;
        }

        baslatildi = true;
    }

    /// <summary>
    /// EngelYoneticisi tarafından üretildiğinde hızını ayarlamak için çağrılır.
    /// </summary>
    public void EngelAyarlariniYap(float hiz)
    {
        hareketHizi = hiz;
        baslatildi = true;
    }

    private void Update()
    {
        if (!baslatildi) return;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !IsServer)
        {
            return;
        }

        if (!OyunAktifMi()) return;

        transform.position += Vector3.right * (hareketHizi * Time.deltaTime);

        if (transform.position.x > yokOlmaX)
        {
            YokEt();
        }
    }

    private bool OyunAktifMi()
    {
        return KarakterKontrol.EnAzBirOyuncuHayattaMi();
    }

    private void YokEt()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && IsServer)
        {
            var netObj = GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn(true);
                return;
            }
        }
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        CarpismaKontrol(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        CarpismaKontrol(collision.gameObject);
    }

    private void CarpismaKontrol(GameObject temasEdenObje)
    {
        var karakter = temasEdenObje.GetComponent<KarakterKontrol>();
        if (karakter != null)
        {
            var potionController = temasEdenObje.GetComponent<PlayerPotionController>();

            // 1. İksir Kontrolü (Görünmezlik VEYA Süzülme Aktif mi?)
            if (potionController != null && (potionController.IsInvulnerable || potionController.IsGliding))
            {
                var engelCollider = GetComponent<Collider>();
                var karakterCollider = temasEdenObje.GetComponent<Collider>();
                if (engelCollider != null && karakterCollider != null)
                {
                    Physics.IgnoreCollision(engelCollider, karakterCollider);
                }
                return;
            }

            // 2. Can Hakkı İksiri Kontrolü (1 Can Hakkı Kullan ve Ölümden Kurtul)
            if (potionController != null && potionController.CanHakkiKullan())
            {
                var engelCollider = GetComponent<Collider>();
                var karakterCollider = temasEdenObje.GetComponent<Collider>();
                if (engelCollider != null && karakterCollider != null)
                {
                    Physics.IgnoreCollision(engelCollider, karakterCollider);
                }
                return;
            }

            // 3. Ölümsüzlük Test Modu Kontrolü
            if (karakter.olumsuzlukTestModu)
            {
                var engelCollider = GetComponent<Collider>();
                var karakterCollider = temasEdenObje.GetComponent<Collider>();
                if (engelCollider != null && karakterCollider != null)
                {
                    Physics.IgnoreCollision(engelCollider, karakterCollider);
                }
                return;
            }

            // Ağ dinleniyorsa sadece sunucu tetikler
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !IsServer) return;

            if (karakter.IsAlive == null || karakter.IsAlive.Value)
            {
                Debug.Log($"{gameObject.name} ile çarpışma algılandı! Karakter eleniyor.");
                karakter.KarakteriEle();
            }
        }
    }
}