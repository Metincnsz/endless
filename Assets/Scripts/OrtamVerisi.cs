using UnityEngine;
using UnityEngine.Video; // VideoClip kullanabilmek için gerekli kütüphane

[CreateAssetMenu(fileName = "Ortam_", menuName = "Seviye Sistemi/Ortam Verisi")]
public class OrtamVerisi : ScriptableObject
{
    [Header("Giriş Koşulu")]
    [Tooltip("Oyuncu bu Seviyeye/Rank'e ulaştığında bu ortam aktif olur.")]
    public int minOyuncuSeviyesi; 

    [Header("Karşıdaki Görsel / Video")]
    [Tooltip("Eğer bu ortamda sabit bir görsel/resim olacaksa buraya sürükleyin.")]
    public Texture2D ortamGorseli;

    [Tooltip("Eğer bu ortamda hareketli bir video olacaksa buraya sürükleyin. (Resimden daha önceliklidir)")]
    public VideoClip ortamVideosu;

    [Header("Bu Ortama Özel Prefablar")]
    [Tooltip("Bu temadaki yol çeşitleri")]
    public GameObject[] yolPrefablari;
    [Tooltip("Bu temadaki engel ve bariyerler")]
    public GameObject[] engelPrefablari;

    [Header("Zorluk Dengesi")]
    public float karakterHizi = 8f;
    public float engelHizi = 15f;
    public float spawnAraligi = 35f;

    [Header("Görsel Atmosfer")]
    public Material skyboxMaterial;
    public bool sisAktif = true;
    public Color sisRengi = Color.gray;
    public float sisYogunlugu = 0.02f;
    public Color ortamIsigiRengi = Color.white;
    public AudioClip arkaPlanMuzigi;
}