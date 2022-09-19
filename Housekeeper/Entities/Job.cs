using System;
using System.Collections.Generic;
using Housekeeper.Extensions;

namespace Housekeeper.Entities;

public class Job
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DaysOfWeek DaysOfWeek { get; set; }
    public int WeekFrequency { get; set; }
    public List<ulong> UserIds { get; init; }
    public int PreviousUserIndex { get; set; }
    public DateTime StartOfFirstWeekMidday { get; set; }

    public DateTime GetNextActivation()
    {
        var now = DateTime.UtcNow;
        var startOfCurrentWeek = now.StartOfWeekMidday();

        DateTime startOfActivationWeek;
        if (StartOfFirstWeekMidday >= startOfCurrentWeek)
            startOfActivationWeek = StartOfFirstWeekMidday;
        else
        {
            var weekNumber = (startOfCurrentWeek - StartOfFirstWeekMidday).Days / 7;
            var weeksToNextActivationWeek = WeekFrequency - (weekNumber % WeekFrequency);
            if (weeksToNextActivationWeek == WeekFrequency) weeksToNextActivationWeek = 0;

            startOfActivationWeek = startOfCurrentWeek + (weeksToNextActivationWeek * TimeSpan.FromDays(7));
        }

        for (var i = 0; i < 2; i++)
        {
            var potentialNextActivation = startOfActivationWeek;
            for (var j = 0; j < 7; j++)
            {
                if (potentialNextActivation >= now && DaysOfWeek.HasDayOfWeek(potentialNextActivation.DayOfWeek))
                    return potentialNextActivation;
                potentialNextActivation += TimeSpan.FromDays(1);
            }

            // No valid days in first activation week (as it could be this week), so check the next activation week too
            startOfActivationWeek += TimeSpan.FromDays(7 * WeekFrequency);
        }

        return DateTime.MaxValue;
    }
}
