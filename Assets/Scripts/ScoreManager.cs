using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    private static ScoreManager instance;

    public static ScoreManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = UnityEngine.Object.FindFirstObjectByType<ScoreManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("ScoreManager (Auto-Created)");
                    instance = go.AddComponent<ScoreManager>();
                }
            }
            return instance;
        }
    }

    private int currentScore = 0;
    private int highScore = 0;

    public static event Action<int, int> OnScoreChanged;

    private const string HighScoreKey = "HighScore";

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

        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    private void Start()
    {
        OnScoreChanged?.Invoke(currentScore, highScore);
    }

    public int GetCurrentScore() => currentScore;
    public int GetHighScore() => highScore;

    public void UpdateScore(int newScore)
    {
        currentScore = newScore;
        if (currentScore > highScore)
        {
            highScore = currentScore;
        }
        OnScoreChanged?.Invoke(currentScore, highScore);
    }

    public void SaveHighScore()
    {
        if (currentScore > PlayerPrefs.GetInt(HighScoreKey, 0))
        {
            PlayerPrefs.SetInt(HighScoreKey, currentScore);
            PlayerPrefs.Save();
        }
    }

    public void ResetCurrentScore()
    {
        currentScore = 0;
        OnScoreChanged?.Invoke(currentScore, highScore);
    }
}