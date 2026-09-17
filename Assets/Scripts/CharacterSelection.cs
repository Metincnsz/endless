using UnityEngine;

public static class CharacterSelection
{
    private const string SelectedKey = "SelectedCharacterId";
    private const string UnlockPrefix = "CharacterUnlocked_";

    /// <summary>
    /// Seçilen karakterin ID'sini PlayerPrefs'e kaydeder.
    /// </summary>
    public static void SetSelected(string characterId)
    {
        if (string.IsNullOrEmpty(characterId)) return;
        PlayerPrefs.SetString(SelectedKey, characterId);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Kayıtlı seçili karakter ID'sini döner. Eğer henüz seçilmediyse veritabanının ilk karakterini varsayılan seçer.
    /// </summary>
    public static string GetSelectedId(CharacterDatabase db)
    {
        string id = PlayerPrefs.GetString(SelectedKey, "");
        if (string.IsNullOrEmpty(id) && db != null && db.Count > 0)
        {
            var first = db.GetByIndex(0);
            if (first != null)
            {
                id = first.characterId;
                SetSelected(id);
            }
        }
        return id;
    }

    /// <summary>
    /// Karakterin açık olup olmadığını kontrol eder (Varsayılan açıksa, maliyeti 0 ise veya satın alındıysa true döner).
    /// </summary>
    public static bool IsUnlocked(CharacterDefinition def)
    {
        if (def == null) return false;
        if (def.unlockedByDefault || def.goldCost <= 0) return true;
        return PlayerPrefs.GetInt(UnlockPrefix + def.characterId, 0) == 1;
    }

    /// <summary>
    /// Karakterin kilidini açar ve kaydeder.
    /// </summary>
    public static void Unlock(CharacterDefinition def)
    {
        if (def == null) return;
        PlayerPrefs.SetInt(UnlockPrefix + def.characterId, 1);
        PlayerPrefs.Save();
    }
}