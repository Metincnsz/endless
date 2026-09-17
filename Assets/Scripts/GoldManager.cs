using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class GoldManager : MonoBehaviour
{
    private static GoldManager instance;

    public static GoldManager Instance
    {
        get
        {
            if (instance == null)
            {
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
            totalGold = PlayerPrefs.GetInt(TotalGoldKey, 0);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Oyun sahnesi başladığında o koşudaki altını sıfırla
        if (scene.name == "SampleScene" || scene.name != "MainMenuScene")
        {
            ResetCurrentRunGold();
        }
        
        OnTotalGoldChanged?.Invoke(totalGold);
    }

    private void Start()
    {
        OnCurrentGoldChanged?.Invoke(currentRunGold);
        OnTotalGoldChanged?.Invoke(totalGold);
    }

    public int GetCurrentRunGold() => currentRunGold;
    public int GetTotalGold() => totalGold;

    public void ResetCurrentRunGold()
    {
        currentRunGold = 0;
        OnCurrentGoldChanged?.Invoke(currentRunGold);
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        currentRunGold += amount;
        OnCurrentGoldChanged?.Invoke(currentRunGold);

        // Görev İlerlemesi: Altın toplama görevlerini otomatik güncelle
        TaskManager.AddProgressToTasks("gold", amount);
    }

    public void SaveRunGold()
    {
        if (currentRunGold > 0)
        {
            totalGold += currentRunGold;
            PlayerPrefs.SetInt(TotalGoldKey, totalGold);
            PlayerPrefs.Save();
            OnTotalGoldChanged?.Invoke(totalGold);
        }
        
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