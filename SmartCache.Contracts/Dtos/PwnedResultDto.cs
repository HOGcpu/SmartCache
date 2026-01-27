namespace SmartCache.Contracts
{
    [GenerateSerializer]
    public class PwnedResultDto
    {
        [Id(0)]
        public bool IsPwned { get; set; }
        [Id(1)]
        public int BreachCount { get; set; }
        [Id(2)]
        public DateTime CheckedAtUtc { get; set; }
        [Id(3)]
        public string Source { get; set; } // memory or blob enum
    }
}
