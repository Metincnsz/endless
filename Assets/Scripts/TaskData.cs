using System;
using System.Collections.Generic;

[Serializable]
public class TaskItemData
{
    public string id;             // Örn: "task_collect_gold_1000"
    public string title;          // Örn: "1000 Altın Topla"
    public string description;    // Açıklama
    public string taskType;       // Örn: "gold", "distance", "play_game"
    public int targetAmount;      // Örn: 1000
    public int rewardAmount;      // Örn: 250 (Kazanılacak altın/ödül)
    public string iconType;       // İkon türü (isteğe bağlı ikon eşleme için)
}

[Serializable]
public class TaskListWrapper
{
    public List<TaskItemData> tasks;
}