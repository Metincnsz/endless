using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
public class KarakterKontrol : NetworkBehaviour
{
    [Header("Hareket Ayarları")]
    public float ileriKosmaHizi = 8f;   // Otomatik ileri gitme hızı
    public float yanHareketHizi = 10f;  // Yan hareket hızı
    public float ziplamaKuvveti = 9.5f; // Zıplama gücü

    [Header("Gelişmiş Zıplama ve Yerçekimi")]
    public float ekstraYercekimi = 4.5f;
    public float dususHiziCarpani = 1.6f;

    [Header("Yanal Sınır (Görünmez Bariyer)")]
    public float minZ = -13.0f;
    public float maxZ = 15.0f;

    [Header("Yer Kontrolü")]
    public float yerMesafesi = 1.1f;
    public LayerMask yerKatmani;

    [Header("Mobil / Fare Kontrol Ayarları")]
    [Tooltip("Parmağı/fareyi yatayda kaydırınca yön kazancı. Yükseldikçe daha hassas dönüş.")]
    public float dragHassasiyeti = 0.015f;

    [Tooltip("Yukarı swipe'ın zıplama sayılması için gereken minimum dikey piksel mesafesi.")]
    public float minSwipeDistance = 45f;

    // --- MULTIPLAYER SENKRONİZASYON DEĞİŞKENLERİ ---
    [HideInInspector]
    public NetworkVariable<bool> IsAlive = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [HideInInspector]
    public NetworkVariable<int> MevcutSkor = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Rigidbody rb;
    private Animator anim;

    private Vector2 keyboardGamepadInput; // Klavye/Gamepad'den gelen girdi
    private Vector2 hareketInput;         // Son uygulanan nihai girdi
    private bool yerdeMi;

    private Vector2 touchStartPos;        // Basılmanın başladığı ekran koordinatı (swipe için)
    private Vector2 sonPointerPos;        // Bir önceki karedeki pointer konumu (drag için)

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        rb.freezeRotation = true;

        if (LevelManager.Instance != null && LevelManager.Instance.AktifOrtam != null)
        {
            ileriKosmaHizi = LevelManager.Instance.AktifOrtam.karakterHizi;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            float baslangicZ = OwnerClientId % 2 == 0 ? -6.45f : 11.11f; 
            transform.position = new Vector3(transform.position.x, transform.position.y, baslangicZ);
        }

        if (!IsOwner)
        {
            var altKamera = GetComponentInChildren<Camera>();
            if (altKamera != null)
            {
                altKamera.gameObject.SetActive(false);
                
                var listener = altKamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
            }

            var tracker = GetComponent<DistanceScoreTracker>();
            if (tracker != null)
            {
                tracker.enabled = false;
            }

            var playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.enabled = false;
            }
        }
    }

    public void OnMove(InputValue value)
    {
        if (!IsOwner || !IsAlive.Value) return; 
        keyboardGamepadInput = value.Get<Vector2>();
    }

    public void OnJump()
    {
        if (!IsOwner || !IsAlive.Value) return; 
        if (yerdeMi)
        {
            rb.AddForce(Vector3.up * ziplamaKuvveti, ForceMode.Impulse);

            if (anim != null)
            {
                anim.SetTrigger("Jump");
            }
        }
    }

    void Update()
    {
        if (!IsOwner) return; 

        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        bool raycastYerdeMi = Physics.Raycast(rayStart, Vector3.down, yerMesafesi, yerKatmani);

        yerdeMi = raycastYerdeMi && rb.linearVelocity.y <= 0.1f;

        if (anim != null)
        {
            anim.SetBool("Grounded", yerdeMi);
        }

        Debug.DrawRay(rayStart, Vector3.down * yerMesafesi, yerdeMi ? Color.green : Color.red);

        if (IsAlive.Value)
        {
            IsleMobilGirdileri();
        }
    }

    private void IsleMobilGirdileri()
    {
        hareketInput = keyboardGamepadInput;

        if (!PointerOku(out Vector2 pos, out bool basili, out bool basildi, out bool birakildi))
            return;

        if (basili)
        {
            if (basildi)
            {
                sonPointerPos = pos;
                touchStartPos = pos;
            }

            float deltaX = pos.x - sonPointerPos.x;
            hareketInput.x = Mathf.Clamp(deltaX * dragHassasiyeti, -1f, 1f);

            sonPointerPos = pos;
        }

        if (birakildi)
        {
            Vector2 swipeVector = pos - touchStartPos;

            if (swipeVector.y > minSwipeDistance &&
                Mathf.Abs(swipeVector.y) > Mathf.Abs(swipeVector.x))
            {
                OnJump();
            }
        }

        if (!IsFinite(hareketInput.x)) hareketInput.x = 0f;
        if (!IsFinite(hareketInput.y)) hareketInput.y = 0f;
    }

    private bool PointerOku(out Vector2 pos, out bool basili, out bool basildi, out bool birakildi)
    {
        pos = sonPointerPos;
        basili = false;
        basildi = false;
        birakildi = false;

        var pointer = Pointer.current;
        if (pointer == null)
            return false;

        pos = pointer.position.ReadValue();
        basili = pointer.press.isPressed;
        basildi = pointer.press.wasPressedThisFrame;
        birakildi = pointer.press.wasReleasedThisFrame;

        if (!IsFinite(pos.x) || !IsFinite(pos.y))
        {
            pos = sonPointerPos;
            return false;
        }

        return basili || birakildi;
    }

    private static bool IsFinite(float v)
    {
        return !(float.IsNaN(v) || float.IsInfinity(v));
    }

    void FixedUpdate()
    {
        if (!IsOwner) return; 

        // Karakter öldüyse veya Rigidbody Kinematic yapıldıysa fizik işlemlerini durdur (Hata engelleme)
        if (!IsAlive.Value || (rb != null && rb.isKinematic))
        {
            return;
        }

        Vector3 ileriHareket = transform.forward * ileriKosmaHizi;
        Vector3 yanHareket = transform.right * hareketInput.x * yanHareketHizi;
        Vector3 toplamHareket = ileriHareket + yanHareket;

        Vector3 hedefHiz = new Vector3(toplamHareket.x, rb.linearVelocity.y, toplamHareket.z);

        if (IsFinite(hedefHiz.x) && IsFinite(hedefHiz.y) && IsFinite(hedefHiz.z))
        {
            rb.linearVelocity = hedefHiz;
        }

        if (!yerdeMi)
        {
            if (rb.linearVelocity.y < 0.1f)
            {
                rb.AddForce(Physics.gravity * (ekstraYercekimi * dususHiziCarpani - 1f), ForceMode.Acceleration);
            }
            else
            {
                rb.AddForce(Physics.gravity * (ekstraYercekimi - 1f), ForceMode.Acceleration);
            }
        }

        float kisitlanmisZ = Mathf.Clamp(rb.position.z, minZ, maxZ);
        rb.position = new Vector3(rb.position.x, rb.position.y, kisitlanmisZ);
    }

    [ServerRpc]
    public void GuncelleSkorServerRpc(int yeniSkor)
    {
        if (IsAlive.Value)
        {
            MevcutSkor.Value = yeniSkor;
        }
    }

    public void KarakteriEle()
    {
        if (!IsServer) return;

        IsAlive.Value = false;
        KarakteriDurdurClientRpc();

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RunTamamlandi(MevcutSkor.Value);
        }
        if (GoldManager.Instance != null)
        {
            GoldManager.Instance.SaveRunGold();
        }
    }

    [ClientRpc]
    private void KarakteriDurdurClientRpc()
    {
        ileriKosmaHizi = 0f;
        yanHareketHizi = 0f;

        if (rb != null)
        {
            // isKinematic yapılmadan ÖNCE hız sıfırlanır, böylece uyarı vermez
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
        }

        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true; 
        }

        if (IsOwner)
        {
            if (DeathMenuManager.Instance != null)
            {
                DeathMenuManager.Instance.ShowDeathMenu();
            }
        }
    }
}