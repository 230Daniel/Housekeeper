using System;

namespace Housekeeper.Entities;

[Flags]
public enum DaysOfWeek
{
    Monday = 1,
    Tuesday = 2,
    Wednesday = 4,
    Thursday = 8,
    Friday = 16,
    Saturday = 32,
    Sunday = 64
}

public static class DaysOfWeekHelper
{
    public static DaysOfWeek FromString(string input)
    {
        var weekdays = (DaysOfWeek) 0;

        foreach (var weekdayString in input.ToLower().Split(','))
        {
            weekdays |= weekdayString.Trim() switch
            {
                "monday" => DaysOfWeek.Monday,
                "tuesday" => DaysOfWeek.Tuesday,
                "wednesday" => DaysOfWeek.Wednesday,
                "thursday" => DaysOfWeek.Thursday,
                "friday" => DaysOfWeek.Friday,
                "saturday" => DaysOfWeek.Saturday,
                "sunday" => DaysOfWeek.Sunday,
                _ => throw new ArgumentException($"Unknown day of week \"{weekdayString}\"")
            };
        }

        return weekdays;
    }

    public static bool HasDayOfWeek(this DaysOfWeek daysOfWeek, DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            0 => daysOfWeek.HasFlag(DaysOfWeek.Sunday),
            _ => daysOfWeek.HasFlag((DaysOfWeek)Math.Pow(2, (int)dayOfWeek - 1))
        };
    }
}
