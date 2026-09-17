using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class PotionCollectible : NetworkBehaviour
{
    [Header("İksir Verisi")]
    [SerializeField] private PotionData potionData;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float floatAmplitude = 0.25f;

    [Header("Ömür Ayarı")]
    [SerializeField] private float lifeTime = 12f;

    private bool isCollected = false;
    private Vector3 startPos;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer && lifeTime > 0f)
        {
            StartCoroutine(YokOlmaRoutine());
        }
    }

    private void Start()
    {
        startPos = transform.position;
        bool isNetworkActive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (!isNetworkActive && lifeTime > 0f)
        {
            StartCoroutine(YokOlmaRoutine());
        }
    }

    private IEnumerator YokOlmaRoutine()
    {
        yield return new WaitForSeconds(lifeTime);
        if (!isCollected) YokEt();
    }

    private void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        PlayerPotionController potionController = other.GetComponent<PlayerPotionController>();
        if (potionController == null)
        {
            potionController = other.GetComponentInParent<PlayerPotionController>();
        }

        if (potionController != null)
        {
            isCollected = true;

            // İksir yeteneğini karaktere aktar
            potionController.ApplyPotion(potionData);

            // Görev İlerlemesi: İksir toplama görevine +1 ekle
            TaskManager.AddProgressToTasks("potion", 1);

            // Efekt oluştur
            if (potionData != null && potionData.collectVFX != null)
            {
                Instantiate(potionData.collectVFX, transform.position, Quaternion.identity);
            }

            YokEt();
        }
    }

    private void YokEt()
    {
        var netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}