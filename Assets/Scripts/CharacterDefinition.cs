using UnityEngine;

[CreateAssetMenu(fileName = "Character_", menuName = "Endless/Character Definition")]
public class CharacterDefinition : ScriptableObject
{
    [Tooltip("Benzersiz kimlik. Kayıt ve ağ senkronizasyonu bunun üzerinden yapılır. Oluşturduktan sonra ASLA değiştirmeyin.")]
    public string characterId;

    [Header("Mağaza Bilgileri")]
    public string displayName;
    public string characterClass;
    public Sprite portrait;
    public int goldCost;
    public bool unlockedByDefault;

    [Header("Oyun İçi")]
    [Tooltip("İçinde NetworkObject + KarakterKontrol bulunan oynanabilir karakter prefabı.")]
    public GameObject playerPrefab;
}
