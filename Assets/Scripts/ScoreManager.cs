using UnityEngine;
using System;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Skor Değerleri")]
    private int currentScore = 0;
    private int highScore = 0;

    // Skor değiştiğinde UI'ı tetiklemek için Event (Observer Pattern - Performans için en iyisi)
    public static event Action<int, int> OnScoreChanged;

    private const string HighScoreKey = "HighScore";

    private void Awake()
    {
        // Singleton Kurulumu
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Kayıtlı yüksek skoru cihazdan yükle
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    private void Start()
    {
        // Oyun başında UI'ı ilk değerlerle başlat
        TriggerScoreUpdate();
    }

    public int GetCurrentScore() => currentScore;
    public int GetHighScore() => highScore;

    // Koşulan mesafeyi skora dönüştürmek için güncelleme metodu
    public void UpdateScore(int score)
    {
        if (score < 0) return;

        currentScore = score;

        // Yüksek skor kontrolü ve anlık kayıt
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
        }

        TriggerScoreUpdate();
    }

    // Gelecekte altın veya bonus eşya toplandığında kullanılacak metot
    public void AddBonusScore(int amount)
    {
        UpdateScore(currentScore + amount);
    }

    private void TriggerScoreUpdate()
    {
        OnScoreChanged?.Invoke(currentScore, highScore);
    }
}