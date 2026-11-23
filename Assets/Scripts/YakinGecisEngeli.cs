using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class YakinGecisEngeli : MonoBehaviour
{
    private static readonly List<YakinGecisEngeli> _aktifEngeller = new List<YakinGecisEngeli>();
    public static IReadOnlyList<YakinGecisEngeli> AktifEngeller => _aktifEngeller;
    public static event Action<YakinGecisEngeli, KarakterKontrol> TemasEdildi;
    public static event Action<YakinGecisEngeli> KayitSilindi;

    public Collider[] Colliderlar { get; private set; }

    private void Awake()
    {
        Colliderlar = GetComponentsInChildren<Collider>(true);
    }

    private void OnEnable()
    {
        if (!_aktifEngeller.Contains(this)) _aktifEngeller.Add(this);
    }

    private void OnDisable()
    {
        _aktifEngeller.Remove(this);
        KayitSilindi?.Invoke(this);
    }

    private void OnDestroy()
    {
        _aktifEngeller.Remove(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        TemasiBildir(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TemasiBildir(collision.collider);
    }

    private void TemasiBildir(Collider other)
    {
        var karakter = other.GetComponentInParent<KarakterKontrol>();
        if (karakter != null) TemasEdildi?.Invoke(this, karakter);
    }
}
