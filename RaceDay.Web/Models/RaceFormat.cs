namespace RaceDay.Web.Services;

/// <summary>Small display helpers shared by the views.</summary>
public static class RaceFormat
{
    /// <summary>47 becomes 47th, 22 becomes 22nd.</summary>
    public static string Ordinal(int n)
    {
        var abs = Math.Abs(n);
        if (abs % 100 is >= 11 and <= 13) return $"{n}th";

        return (abs % 10) switch
        {
            1 => $"{n}st",
            2 => $"{n}nd",
            3 => $"{n}rd",
            _ => $"{n}th"
        };
    }

    public static string Time(TimeSpan t) => t.ToString(@"hh\:mm\:ss");

    public static string AgeRange(int? min, int? max) => (min, max) switch
    {
        (null, null) => "All ages",
        (not null, null) => $"{min}+",
        (null, not null) => $"Up to {max}",
        _ => $"{min}-{max}"
    };

    public static string Distance(decimal? km) => km.HasValue ? $"{km.Value:0.##} km" : "-";
}