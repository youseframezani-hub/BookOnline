namespace SearchEngine.Scoring;

public enum MatchKind { Exact = 60, PreviousAdjacent = 25, NextAdjacent = 15 }

public static class MatchiurityScoreFactory
{
    public static double GetScore(MatchKind kind) => kind switch
    {
        MatchKind.Exact => 0.60,
        MatchKind.PreviousAdjacent => 0.25,
        MatchKind.NextAdjacent => 0.15,
        _ => 0.0
    };

    public static double Combine(IEnumerable<MatchKind> matches, int paragraphLength)
    {
        var seen = new HashSet<MatchKind>();   // O(1) membership
        double sum = 0;
        foreach (var m in matches)
            if (seen.Add(m))                   // جلوگیری از بازنویسی/تکرار امتیاز
                sum += GetScore(m);
        return sum / Math.Max(1, paragraphLength);
    }
}
