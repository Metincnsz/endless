using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Endless/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    public List<CharacterDefinition> characters = new List<CharacterDefinition>();

    public int Count => characters != null ? characters.Count : 0;

    public CharacterDefinition GetByIndex(int index)
    {
        if (characters == null || index < 0 || index >= characters.Count) return null;
        return characters[index];
    }

    public CharacterDefinition GetById(string id)
    {
        if (string.IsNullOrEmpty(id) || characters == null) return null;
        for (int i = 0; i < characters.Count; i++)
        {
            if (characters[i] != null && characters[i].characterId == id)
                return characters[i];
        }
        return null;
    }
}
