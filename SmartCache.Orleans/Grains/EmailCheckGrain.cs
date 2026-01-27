using Orleans.Providers;
using SmartCache.Contracts;

namespace SmartCache.Orleans.Grains
{
    [StorageProvider(ProviderName = "AzureBlobStore")]
    public class EmailCheckGrain : Grain<EmailCheckState>, IEmailCheckGrain
    {
        private IDisposable _timer;

        public override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            _timer = this.RegisterGrainTimer<object?>(
                callback: async (_, ct) => await WriteStateAsync(),
                state: null,
                options: new GrainTimerCreationOptions(
                    dueTime: TimeSpan.FromMinutes(1),
                    period: TimeSpan.FromMinutes(1))
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
                Source = "memory"
            });
        }

        public async Task<bool> AddEmailAsync(string email)
        {
            email = email.ToLowerInvariant();

            if (State.BreachedEmails.Contains(email))
                return false;

            State.BreachedEmails.Add(email);
            await WriteStateAsync();
            return true;
        }
    }
}
