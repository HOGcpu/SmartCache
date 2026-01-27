namespace SmartCache.Contracts
{
    public interface IEmailCheckGrain : IGrainWithStringKey
    {
        Task<PwnedResultDto> CheckAsync(string email);
        Task<bool> AddEmailAsync(string email);
    }
}
