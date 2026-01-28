using SmartCache.Contracts;

namespace SmartCache.Tests
{
    // Fake implementation of IEmailCheckGrain for unit testing
    public class FakeEmailCheckGrain : IEmailCheckGrain
    {
        private readonly HashSet<string> _emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly StorageType _storageType;

        public FakeEmailCheckGrain(StorageType storageType = StorageType.InMemory)
        {
            _storageType = storageType;
        }

        public Task<bool> AddEmailAsync(string email)
        {
            email = email.ToLowerInvariant();
            if (_emails.Contains(email)) return Task.FromResult(false);

            _emails.Add(email);
            return Task.FromResult(true);
        }

        public Task<PwnedResultDto> CheckAsync(string email)
        {
            email = email.ToLowerInvariant();
            bool isPwned = _emails.Contains(email);

            return Task.FromResult(new PwnedResultDto
            {
                IsPwned = isPwned,
                BreachCount = isPwned ? 1 : 0,
                CheckedAtUtc = DateTime.UtcNow,
                Source = _storageType == StorageType.AzureBlob ? "azure-blob" : "memory"
            });
        }

        // Optional: enum to indicate storage type
        public enum StorageType
        {
            InMemory,
            AzureBlob
        }
    }

    public class EmailCheckGrainTests
    {
        [Fact]
        public async Task AddEmailAsync_Should_AddEmailAndReturnTrue()
        {
            // Arrange
            var grain = new FakeEmailCheckGrain();

            // Act
            var added = await grain.AddEmailAsync("test@example.com");
            var result = await grain.CheckAsync("test@example.com");

            // Assert
            Assert.True(added);
            Assert.True(result.IsPwned);
            Assert.Equal("memory", result.Source);
        }

        [Fact]
        public async Task AddEmailAsync_Should_ReturnFalse_IfEmailExists()
        {
            var grain = new FakeEmailCheckGrain();
            var email = "duplicate@example.com";

            await grain.AddEmailAsync(email);
            var addedAgain = await grain.AddEmailAsync(email);

            Assert.False(addedAgain);
        }

        [Fact]
        public async Task CheckAsync_Should_ReturnPwnedResult_WithCorrectSource()
        {
            var grain = new FakeEmailCheckGrain(FakeEmailCheckGrain.StorageType.AzureBlob);
            var email = "azure@example.com";

            await grain.AddEmailAsync(email);
            var result = await grain.CheckAsync(email);

            Assert.True(result.IsPwned);
            Assert.Equal("azure-blob", result.Source);
        }

        [Fact]
        public async Task CheckAsync_Should_ReturnNotPwned_ForUnknownEmail()
        {
            var grain = new FakeEmailCheckGrain();
            var result = await grain.CheckAsync("unknown@example.com");

            Assert.False(result.IsPwned);
            Assert.Equal(0, result.BreachCount);
            Assert.Equal("memory", result.Source);
        }
    }
}
