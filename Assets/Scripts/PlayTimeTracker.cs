using System;
using UnityEngine;

public class PlayTimeTracker : MonoBehaviour
{
    private float secondCounter = 0f;

    private void Start()
    {
        CheckDailyReset();
    }

    private void Update()
    {
        secondCounter += Time.unscaledDeltaTime;

        // Her 60 saniyede (1 dakika) bir görevlere 1 dakika ekle
        if (secondCounter >= 60f)
        {
            secondCounter = 0f;
            TaskManager.AddProgressToTasks("play_time", 1);
        }
    }

    private void CheckDailyReset()
    {
        string today = DateTime.UtcNow.ToString("yyyyMMdd");
        string lastSavedDay = PlayerPrefs.GetString("LastPlayedDay", "");

        if (lastSavedDay != today)
        {
            // Yeni bir gün başladı -> Günlük görev ilerlemelerini sıfırla
            PlayerPrefs.SetString("LastPlayedDay", today);
            // İsteğe bağlı: Günlük görevlerin PlayerPrefs kayıtlarını sıfırla
            PlayerPrefs.Save();
        }
    }
}