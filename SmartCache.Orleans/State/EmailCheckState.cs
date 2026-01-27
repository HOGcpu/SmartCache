namespace SmartCache.Orleans
{
    public class EmailCheckState
    {
        public HashSet<string> BreachedEmails { get; set; } = new HashSet<string>();
    }

}
