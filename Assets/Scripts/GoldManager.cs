using UnityEngine;
using System;

public class GoldManager : MonoBehaviour
{
    private static GoldManager instance;

    public static GoldManager Instance
    {
        get
        {
            if (instance == null)
            {
                // Belirsizliği önlemek için UnityEngine.Object olarak tam adıyla çağırıyoruz
                instance = UnityEngine.Object.FindFirstObjectByType<GoldManager>();
                
                if (instance == null)
                {
                    GameObject go = new GameObject("GoldManager (Auto-Created)");
                    instance = go.AddComponent<GoldManager>();
                }
            }
            return instance;
        }
    }

    private int currentRunGold = 0; 
    private int totalGold = 0;      

    public static event Action<int> OnCurrentGoldChanged;
    public static event Action<int> OnTotalGoldChanged;

    private const string TotalGoldKey = "TotalGold";

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        totalGold = PlayerPrefs.GetInt(TotalGoldKey, 0);
    }

    private void Start()
    {
        OnCurrentGoldChanged?.Invoke(currentRunGold);
        OnTotalGoldChanged?.Invoke(totalGold);
    }

    public int GetCurrentRunGold() => currentRunGold;
    public int GetTotalGold() => totalGold;

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        currentRunGold += amount;
        OnCurrentGoldChanged?.Invoke(currentRunGold);
    }

    public void SaveRunGold()
    {
        totalGold += currentRunGold;
        PlayerPrefs.SetInt(TotalGoldKey, totalGold);
        PlayerPrefs.Save();
        
        OnTotalGoldChanged?.Invoke(totalGold);
        
        currentRunGold = 0;
        OnCurrentGoldChanged?.Invoke(currentRunGold);
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0) return false;
        
        if (totalGold >= amount)
        {
            totalGold -= amount;
            PlayerPrefs.SetInt(TotalGoldKey, totalGold);
            PlayerPrefs.Save();
            
            OnTotalGoldChanged?.Invoke(totalGold);
            return true;
        }
        
        return false;
    }
}