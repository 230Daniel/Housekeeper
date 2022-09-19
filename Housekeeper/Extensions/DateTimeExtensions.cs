using System;

namespace Housekeeper.Extensions;

public static class DateTimeExtensions
{
    public static DateTime StartOfWeekMidday(this DateTime date)
    {
        // Set time to midday
        date += TimeSpan.FromHours(12) - date.TimeOfDay;

        // Subtract the number of days since Monday
        var daysSinceMonday = date.DayOfWeek.DaysSinceMonday();
        return date.AddDays(-daysSinceMonday);
    }

    public static int DaysSinceMonday(this DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            0 => 6,
            _ => (int) dayOfWeek - 1
        };
    }
}
