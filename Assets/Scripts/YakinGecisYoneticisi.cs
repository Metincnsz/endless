using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Unity.Netcode.NetworkObject))]
public sealed class YakinGecisYoneticisi : Unity.Netcode.NetworkBehaviour
{
    [Header("Yakın Geçiş Bonusu")]
    [SerializeField] private bool bonusAktif = true;
    [Tooltip("Collider yüzeyleri arasındaki en fazla boşluk (dünya birimi). Artırmak yakın geçişi kolaylaştırır. 0 bonusu kapatır.")]
    [Min(0f)] [SerializeField] private float yakinlikMesafesi = 1f;
    [Tooltip("Tamamlanan her temassız yakın geçişte kazanılan ekstra altın. İksir çarpanı uygulanmaz.")]
    [Min(0)] [SerializeField] private int bonusAltin = 3;

    public event Action<KarakterKontrol, int> YakinGecisBasarili;

    private sealed class Govde
    {
        public readonly Collider[] Colliderlar;
        public readonly YakinGecisMesafe.Sekil[] Onceki;
        public readonly YakinGecisMesafe.Sekil[] Simdiki;
        public Bounds Sinirlar;
        public bool Gecerli;
        private bool _ilkOrnek = true;

        public Govde(Collider[] colliderlar)
        {
            Colliderlar = colliderlar;
            Onceki = new YakinGecisMesafe.Sekil[colliderlar.Length];
            Simdiki = new YakinGecisMesafe.Sekil[colliderlar.Length];
        }

        public void Oku()
        {
            Gecerli = false;
            for (int i = 0; i < Colliderlar.Length; i++)
            {
                Onceki[i] = Simdiki[i];
                Simdiki[i] = YakinGecisMesafe.Oku(Colliderlar[i]);
                if (_ilkOrnek) Onceki[i] = Simdiki[i];
                if (!Simdiki[i].Gecerli) continue;
                if (!Gecerli) Sinirlar = Simdiki[i].Sinirlar;
                else Sinirlar.Encapsulate(Simdiki[i].Sinirlar);
                Gecerli = true;
            }
            _ilkOrnek = false;
        }
    }

    private sealed class Gecis
    {
        public bool Yakin;
        public bool Temas;
        public bool Bitti;
    }

    private readonly Dictionary<KarakterKontrol, Govde> _oyuncular = new Dictionary<KarakterKontrol, Govde>();
    private readonly Dictionary<YakinGecisEngeli, Govde> _engeller = new Dictionary<YakinGecisEngeli, Govde>();
    private readonly Dictionary<(KarakterKontrol, YakinGecisEngeli), Gecis> _gecisler = new Dictionary<(KarakterKontrol, YakinGecisEngeli), Gecis>();
    private readonly List<KarakterKontrol> _silinecekOyuncular = new List<KarakterKontrol>();
    private readonly List<YakinGecisEngeli> _silinecekEngeller = new List<YakinGecisEngeli>();
    private readonly List<(KarakterKontrol, YakinGecisEngeli)> _silinecekGecisler = new List<(KarakterKontrol, YakinGecisEngeli)>();

    private void OnEnable()
    {
        YakinGecisEngeli.TemasEdildi += TemasiKaydet;
        YakinGecisEngeli.KayitSilindi += EngeliSil;
    }

    private void OnDisable()
    {
        YakinGecisEngeli.TemasEdildi -= TemasiKaydet;
        YakinGecisEngeli.KayitSilindi -= EngeliSil;
        KayitlariTemizle();
    }

    public override void OnNetworkDespawn()
    {
        KayitlariTemizle();
        base.OnNetworkDespawn();
    }

    private void OnValidate()
    {
        yakinlikMesafesi = float.IsNaN(yakinlikMesafesi) || float.IsInfinity(yakinlikMesafesi) ? 0f : Mathf.Max(0f, yakinlikMesafesi);
        bonusAltin = Mathf.Max(0, bonusAltin);
    }

    private bool AgAktif => Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening;
    private bool Yetkili => !AgAktif || (IsSpawned && IsServer);

    private static bool Hayatta(KarakterKontrol player)
    {
        return player != null && player.isActiveAndEnabled && player.IsAlive != null && player.IsAlive.Value;
    }

    private void LateUpdate()
    {
        if (!Yetkili) return;
        if (!bonusAktif || yakinlikMesafesi <= 0f || bonusAltin <= 0)
        {
            KayitlariTemizle();
            return;
        }
        EskiKayitlariSil();
        foreach (var player in KarakterKontrol.AktifKarakterler)
        {
            if (!Hayatta(player) || (AgAktif && !player.IsSpawned)) continue;
            if (!_oyuncular.TryGetValue(player, out var body))
            {
                body = new Govde(player.GetComponentsInChildren<Collider>(true));
                _oyuncular.Add(player, body);
            }
            body.Oku();
        }
        foreach (var obstacle in YakinGecisEngeli.AktifEngeller)
        {
            if (obstacle == null || !obstacle.isActiveAndEnabled) continue;
            if (!_engeller.TryGetValue(obstacle, out var body))
            {
                body = new Govde(obstacle.Colliderlar);
                _engeller.Add(obstacle, body);
            }
            body.Oku();
        }
        // Capture but do not evaluate movement while paused (no retrospective sweep on resume).
        if (Time.timeScale <= 0f) return;

        foreach (var playerEntry in _oyuncular)
        {
            KarakterKontrol player = playerEntry.Key;
            Govde playerBody = playerEntry.Value;
            if (!Hayatta(player) || !playerBody.Gecerli) continue;
            foreach (var obstacleEntry in _engeller)
            {
                YakinGecisEngeli obstacle = obstacleEntry.Key;
                Govde obstacleBody = obstacleEntry.Value;
                if (!obstacleBody.Gecerli) continue;
                var key = (player, obstacle);
                if (!_gecisler.TryGetValue(key, out var pass))
                {
                    pass = new Gecis { Bitti = obstacleBody.Sinirlar.min.x > playerBody.Sinirlar.max.x };
                    _gecisler.Add(key, pass);
                }
                if (pass.Bitti) continue;
                for (int p = 0; p < playerBody.Simdiki.Length && !pass.Temas; p++)
                {
                    for (int o = 0; o < obstacleBody.Simdiki.Length; o++)
                    {
                        YakinGecisMesafe.GecisiOlc(playerBody.Onceki[p], playerBody.Simdiki[p], obstacleBody.Onceki[o], obstacleBody.Simdiki[o], yakinlikMesafesi, out bool near, out bool contact);
                        pass.Yakin |= near;
                        pass.Temas |= contact;
                        if (pass.Temas) break;
                    }
                }
                if (obstacleBody.Sinirlar.min.x <= playerBody.Sinirlar.max.x) continue;
                // Mark complete BEFORE delivering an event/RPC, so no duplicate payment is possible.
                pass.Bitti = true;
                if (pass.Yakin && !pass.Temas && Hayatta(player)) Odullendir(player);
            }
        }
    }

    private void TemasiKaydet(YakinGecisEngeli obstacle, KarakterKontrol player)
    {
        if (!Yetkili || player == null) return;
        var key = (player, obstacle);
        if (!_gecisler.TryGetValue(key, out var pass))
        {
            pass = new Gecis();
            _gecisler.Add(key, pass);
        }
        pass.Temas = true;
    }

    private void EngeliSil(YakinGecisEngeli obstacle)
    {
        _engeller.Remove(obstacle);
        GecisleriSil(null, obstacle);
    }

    private void EskiKayitlariSil()
    {
        _silinecekOyuncular.Clear();
        foreach (var entry in _oyuncular)
            if (!Hayatta(entry.Key) || !KarakterKontrol.AktifKarakterler.Contains(entry.Key) || (AgAktif && !entry.Key.IsSpawned)) _silinecekOyuncular.Add(entry.Key);
        foreach (var player in _silinecekOyuncular)
        {
            _oyuncular.Remove(player);
            GecisleriSil(player, null);
        }
        _silinecekEngeller.Clear();
        foreach (var entry in _engeller)
            if (entry.Key == null || !entry.Key.isActiveAndEnabled) _silinecekEngeller.Add(entry.Key);
        foreach (var obstacle in _silinecekEngeller) EngeliSil(obstacle);
    }

    private void GecisleriSil(KarakterKontrol player, YakinGecisEngeli obstacle)
    {
        _silinecekGecisler.Clear();
        foreach (var entry in _gecisler)
            if ((player != null && entry.Key.Item1 == player) || (!ReferenceEquals(obstacle, null) && entry.Key.Item2 == obstacle)) _silinecekGecisler.Add(entry.Key);
        foreach (var key in _silinecekGecisler) _gecisler.Remove(key);
    }

    private void KayitlariTemizle()
    {
        _oyuncular.Clear();
        _engeller.Clear();
        _gecisler.Clear();
    }

    private void Odullendir(KarakterKontrol player)
    {
        if (AgAktif)
        {
            if (!IsServer || !IsSpawned || !player.IsSpawned) return;
            var rpcParams = new Unity.Netcode.ClientRpcParams
            {
                Send = new Unity.Netcode.ClientRpcSendParams { TargetClientIds = new[] { player.OwnerClientId } }
            };
            BonusVerClientRpc(bonusAltin, player.OwnerClientId, rpcParams);
        }
        else YerelBonusVer(player, bonusAltin);
    }

    [Unity.Netcode.ClientRpc]
    private void BonusVerClientRpc(int amount, ulong ownerId, Unity.Netcode.ClientRpcParams rpcParams = default)
    {
        foreach (var player in KarakterKontrol.AktifKarakterler)
        {
            if (player != null && player.IsSpawned && player.IsOwner && player.OwnerClientId == ownerId)
            {
                YerelBonusVer(player, amount);
                return;
            }
        }
    }

    private void YerelBonusVer(KarakterKontrol player, int amount)
    {
        if (!Hayatta(player) || amount <= 0) return;
        GoldManager.Instance.AddGold(amount);
        YakinGecisBasarili?.Invoke(player, amount);
    }
}
