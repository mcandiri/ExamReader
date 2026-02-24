namespace ExamReader.Core.Analytics;

public class ScoreDistribution
{
    public List<DistributionBucket> Buckets { get; set; } = new();

    public static ScoreDistribution FromPercentages(IEnumerable<double> percentages)
    {
        var distribution = new ScoreDistribution();

        // Create 10 buckets: 0-10, 10-20, ..., 90-100
        var bucketCounts = new int[10];
        foreach (var pct in percentages)
        {
            int index = pct >= 100 ? 9 : (int)(pct / 10);
            if (index < 0) index = 0;
            if (index > 9) index = 9;
            bucketCounts[index]++;
        }

        for (int i = 0; i < 10; i++)
        {
            distribution.Buckets.Add(new DistributionBucket
            {
                RangeStart = i * 10,
                RangeEnd = (i + 1) * 10,
                Label = $"{i * 10}-{(i + 1) * 10}",
                Count = bucketCounts[i]
            });
        }

        return distribution;
    }
}

public class DistributionBucket
{
    public int RangeStart { get; set; }
    public int RangeEnd { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}
