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
    // Yayın başlangıcı startTimeUtc; süre GÜN/SAAT/DAKİKA (ör. 2/05/30).
    public string duration;

    public bool TryGetTimeWindow(out DateTime startUtc, out DateTime endUtc)
    {
        startUtc = DateTime.MinValue;
        endUtc = DateTime.MinValue;
        bool hasStart = !string.IsNullOrWhiteSpace(startTimeUtc);
        if (hasStart && !TryParseUtc(startTimeUtc, out startUtc)) return false;

        if (!string.IsNullOrWhiteSpace(duration))
        {
            if (!hasStart) return false;
            string[] parts = duration.Split('/');
            if (parts.Length != 3 ||
                !int.TryParse(parts[0], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int days) ||
                !int.TryParse(parts[1], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int hours) ||
                !int.TryParse(parts[2], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int minutes) ||
                days < 0 || hours < 0 || hours > 23 || minutes < 0 || minutes > 59 ||
                (days == 0 && hours == 0 && minutes == 0)) return false;
            try
            {
                endUtc = startUtc.Add(new TimeSpan(days, hours, minutes, 0));
            }
            catch (ArgumentOutOfRangeException) { return false; }
            catch (OverflowException) { return false; }
        }
        else if (!TryParseUtc(endTimeUtc, out endUtc)) return false;

        return endUtc > startUtc;
    }

    private static bool TryParseUtc(string value, out DateTime utc)
    {
        return DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out utc);
    }
}

[Serializable]
public class EventListWrapper
{
    public List<EventItemData> events;
}