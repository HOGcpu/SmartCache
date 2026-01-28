using Orleans.Providers;
using SmartCache.Contracts;

namespace SmartCache.Orleans.Grains
{
    [StorageProvider(ProviderName = "AzureBlobStore")]
    public class EmailCheckGrain : Grain<EmailCheckState>, IEmailCheckGrain
    {
        private IDisposable _timer;
        private StorageType _storageType;

        public override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            // storage type based on provider
            _storageType = this.GrainReference.GetPrimaryKeyString().Contains("InMemory")
                ? StorageType.InMemory
                : StorageType.AzureBlob;

            _timer = this.RegisterGrainTimer<object?>(
                callback: async (_, ct) =>
                {
                   await WriteStateAsync();
                },
                state: null,
                options: new GrainTimerCreationOptions(
                    dueTime: TimeSpan.FromMinutes(5),
                    period: TimeSpan.FromMinutes(5))
                {
                    Interleave = true,
                    KeepAlive = true
                }
            );

            return base.OnActivateAsync(cancellationToken);
        }

        public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
        {
            _timer?.Dispose();
            return base.OnDeactivateAsync(reason, cancellationToken);
        }

        public Task<PwnedResultDto> CheckAsync(string email)
        {
            email = email.ToLowerInvariant();
            bool isPwned = State.BreachedEmails.Contains(email);

            return Task.FromResult(new PwnedResultDto
            {
                IsPwned = isPwned,
                BreachCount = isPwned ? 1 : 0,
                CheckedAtUtc = DateTime.UtcNow,
                Source = _storageType.ToString().ToLower() // "inmemory" or "azureblob"
            });
        }

        public async Task<bool> AddEmailAsync(string email)
        {
            email = email.ToLowerInvariant();

            if (State.BreachedEmails.Contains(email))
                return false;

            State.BreachedEmails.Add(email);
            //await WriteStateAsync();
            return true;
        }

        public enum StorageType
        {
            InMemory,
            AzureBlob
        }
    }
}
