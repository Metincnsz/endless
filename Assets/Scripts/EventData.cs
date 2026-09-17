using System;
using System.Collections.Generic;

[Serializable]
public class EventItemData
{
    public string id;
    public string title;
    public string description;
    public int targetAmount;
    public int rewardAmount;
    public string startTimeUtc;
    public string endTimeUtc;
}

[Serializable]
public class EventListWrapper
{
    public List<EventItemData> events;
}