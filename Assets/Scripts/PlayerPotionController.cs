using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerPotionController : NetworkBehaviour
{
    public static PlayerPotionController LocalInstance { get; private set; }

    // UI ve Harici Dinleyiciler İçin Event'ler
    public static event Action<PotionData, float> OnPotionActivated;
    public static event Action<PotionData, float, float> OnPotionTimerUpdated;
    public static event Action<PotionData> OnPotionEnded;
    public static event Action<int> OnExtraLifeChanged; // Can hakkı değiştiğinde tetiklenir

    // Karakter ve Fizik Referansları
    public KarakterKontrol Karakter { get; private set; }
    public Rigidbody Rb { get; private set; }
    public float GroundBaseY { get; set; } = 0f;
    public bool IsGlideLanding { get; set; } = false;

    // Can Hakkı Sayısı (Oyun bitene veya harcanana kadar kalır)
    public int CanHakki { get; private set; } = 0;

    // Aktif Durum Sorguları
    public bool IsInvulnerable
    {
        get
        {
            foreach (var kvp in activePotions)
            {
                if (kvp.Key.IsInvulnerable) return true;
            }
            return false;
        }
    }

    public bool IsGliding
    {
        get
        {
            foreach (var kvp in activePotions)
            {
                if (kvp.Key.IsGliding) return true;
            }
            return false;
        }
    }

    public int GoldMultiplier
    {
        get
        {
            int maxMultiplier = 1;
            foreach (var kvp in activePotions)
            {
                if (kvp.Key.GoldMultiplier > maxMultiplier)
                {
                    maxMultiplier = kvp.Key.GoldMultiplier;
                }
            }
            return maxMultiplier;
        }
    }

    private readonly Dictionary<PotionData, Coroutine> activePotions = new Dictionary<PotionData, Coroutine>();
    private readonly Dictionary<PotionData, GameObject> activeAuras = new Dictionary<PotionData, GameObject>();
    private readonly List<PotionData> updateList = new List<PotionData>();

    private void Awake()
    {
        Karakter = GetComponent<KarakterKontrol>();
        Rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner) LocalInstance = this;
    }

    private void Start()
    {
        bool isNetworkActive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (!isNetworkActive) LocalInstance = this;
        GroundBaseY = transform.position.y;
    }

    public override void OnDestroy()
    {
        if (LocalInstance == this) LocalInstance = null;
        base.OnDestroy();
    }

    public void CanHakkiEkle(int miktar)
    {
        CanHakki += miktar;
        OnExtraLifeChanged?.Invoke(CanHakki);
    }

    /// <summary>
    /// Engele çarpıldığında çağrılır. Can hakkı varsa 1 adet harcar ve true döner (ölümü engeller).
    /// </summary>
    public bool CanHakkiKullan()
    {
        if (CanHakki > 0)
        {
            CanHakki--;
            OnExtraLifeChanged?.Invoke(CanHakki);
            Debug.Log($"[Can Hakkı Kullanıldı] Kalan Can Hakkı: {CanHakki}. Karakter kurtuldu!");
            return true;
        }
        return false;
    }

    public void ApplyPotion(PotionData data)
    {
        if (data == null) return;

        if (IsOwner || !IsSpawned)
        {
            if (activePotions.TryGetValue(data, out Coroutine existingCo))
            {
                StopCoroutine(existingCo);
                data.OnEnd(this);
                RemoveAura(data);
                activePotions.Remove(data);
            }

            Coroutine co = StartCoroutine(PotionLifecycleRoutine(data));
            activePotions[data] = co;
        }
    }

    private IEnumerator PotionLifecycleRoutine(PotionData data)
    {
        data.OnStart(this);
        SpawnAura(data);

        if (IsOwner || !IsSpawned)
        {
            OnPotionActivated?.Invoke(data, data.duration);
        }

        float remaining = data.duration;
        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            if (IsOwner || !IsSpawned)
            {
                OnPotionTimerUpdated?.Invoke(data, remaining, data.duration);
            }
            yield return null;
        }

        data.OnEnd(this);
        RemoveAura(data);

        if (IsOwner || !IsSpawned)
        {
            OnPotionEnded?.Invoke(data);
        }

        activePotions.Remove(data);
    }

    private void Update()
    {
        if (IsSpawned && !IsOwner) return;

        updateList.Clear();
        updateList.AddRange(activePotions.Keys);

        for (int i = 0; i < updateList.Count; i++)
        {
            updateList[i].OnUpdate(this);
        }
    }

    private void FixedUpdate()
    {
        if (IsSpawned && !IsOwner) return;

        updateList.Clear();
        updateList.AddRange(activePotions.Keys);

        for (int i = 0; i < updateList.Count; i++)
        {
            updateList[i].OnFixedUpdate(this);
        }

        if (IsGlideLanding)
        {
            ProcessGlideLandingFixed();
        }
    }

    private void ProcessGlideLandingFixed()
    {
        if (Rb == null) return;

        if (transform.position.y > GroundBaseY + 0.1f)
        {
            float newY = Mathf.MoveTowards(Rb.position.y, GroundBaseY, 4f * Time.fixedDeltaTime);
            Rb.position = new Vector3(Rb.position.x, newY, Rb.position.z);
            Rb.linearVelocity = new Vector3(Rb.linearVelocity.x, 0f, Rb.linearVelocity.z);
        }
        else
        {
            IsGlideLanding = false;
        }
    }

    private void SpawnAura(PotionData data)
    {
        if (data.auraVFXPrefab != null && !activeAuras.ContainsKey(data))
        {
            GameObject aura = Instantiate(data.auraVFXPrefab, transform);
            aura.transform.localPosition = Vector3.zero;
            activeAuras[data] = aura;
        }
    }

    private void RemoveAura(PotionData data)
    {
        if (activeAuras.TryGetValue(data, out GameObject aura))
        {
            if (aura != null) Destroy(aura);
            activeAuras.Remove(data);
        }
    }
}