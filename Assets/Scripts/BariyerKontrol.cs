using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
public class BariyerKontrol : NetworkBehaviour
{
    [Header("Ömür Süresi")]
    [Tooltip("Karakterin arkasında kalıp görünmez olduğunda kendi kendini yok etme süresi (Saniye)")]
    public float omurSuresi = 8f; 

    private float hareketHizi = 0f; 
    private Rigidbody rb;
    private bool baslatildi = false;

    void Awake()
    {
        this.enabled = true;
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.useGravity = false;      
            rb.isKinematic = true;      
            rb.interpolation = RigidbodyInterpolation.Interpolate; 
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous; 
        }
    }

    void Start()
    {
        if (IsServer)
        {
            Destroy(gameObject, omurSuresi);
        }
    }

    public void EngelAyarlariniYap(float hiz)
    {
        this.hareketHizi = hiz;
        baslatildi = true;
    }

    private bool OyuncuHayattaMi()
    {
        var karakter = Object.FindFirstObjectByType<KarakterKontrol>();
        return karakter != null && karakter.IsAlive.Value;
    }

    void FixedUpdate()
    {
        if (!baslatildi) return;
        // Oyuncu öldüyse bariyer hareketi durur
        if (!OyuncuHayattaMi()) return;

        Vector3 yon = Vector3.right * hareketHizi * Time.fixedDeltaTime;

        if (rb != null)
        {
            rb.MovePosition(rb.position + yon);
        }
        else
        {
            transform.position += yon;
        }
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
        if (!IsServer) return;

        var karakter = temasEdenObje.GetComponent<KarakterKontrol>();
        if (karakter != null && karakter.IsAlive.Value)
        {
            Debug.Log($"{gameObject.name} ile çarpışma algılandı! Karakter eleniyor.");
            karakter.KarakteriEle();
        }
    }
}