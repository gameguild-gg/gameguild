using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class MfaSmsSetupTests
{
    [Fact]
    public void SmsMfaContract_ExposesSetupCompletionAvailabilityAndPersistenceFields()
    {
        typeof(IMfaService).GetMethod(
            "InitiateSmsSetupAsync",
            BindingFlags.Instance | BindingFlags.Public,
            [
                typeof(Guid),
                typeof(string),
                typeof(CancellationToken)
            ])
            .Should().NotBeNull();

        typeof(IMfaService).GetMethod(
            "CompleteSmsSetupAsync",
            BindingFlags.Instance | BindingFlags.Public,
            [
                typeof(Guid),
                typeof(string),
                typeof(CancellationToken)
            ])
            .Should().NotBeNull();

        typeof(IMfaService).GetMethod(
            "IsSmsMfaAvailableAsync",
            BindingFlags.Instance | BindingFlags.Public,
            [typeof(CancellationToken)])
            .Should().NotBeNull();

        typeof(UserMfaConfiguration).GetProperty("SmsPhoneNumber").Should().NotBeNull();
        typeof(UserMfaConfiguration).GetProperty("SmsVerificationCodeHash").Should().NotBeNull();
        typeof(UserMfaConfiguration).GetProperty("SmsVerificationExpiresAt").Should().NotBeNull();
        typeof(UserMfaConfiguration).GetProperty("IsSmsEnabled").Should().NotBeNull();
    }

    [Fact]
    public async Task SmsSetupRoundTrip_PersistsPendingCodeAndEnablesSmsMfaAfterVerification()
    {
        var userId = Guid.NewGuid();
        var repository = new InMemoryMfaConfigurationRepository();
        var smsService = new CapturingSmsService();
        var service = new MfaService(
            NullLogger<MfaService>.Instance,
            Mock.Of<ITotpMfaService>(),
            Mock.Of<IBackupCodeMfaService>(),
            Mock.Of<IMfaAttemptTrackingService>(),
            repository,
            smsService,
            Options.Create(new SmsMfaOptions { CodeLength = 6, CodeExpirationSeconds = 300 }));

        var setup = await service.InitiateSmsSetupAsync(userId, "+1 (555) 123-4567");

        setup.Success.Should().BeTrue();
        setup.PhoneNumberMasked.Should().Be("***-***-4567");
        smsService.LastPhoneNumber.Should().Be("+15551234567");
        smsService.LastCode.Should().HaveLength(6);
        var pending = await repository.GetByUserIdAsync(userId);
        pending.Should().NotBeNull();
        pending!.SmsVerificationCodeHash.Should().NotBeNullOrWhiteSpace();
        pending.SmsVerificationCodeHash.Should().NotBe(smsService.LastCode);
        pending.SmsVerificationExpiresAt.Should().BeAfter(SystemClock.UtcNow);
        pending.IsSmsEnabled.Should().BeFalse();

        var completed = await service.CompleteSmsSetupAsync(userId, smsService.LastCode!);

        completed.Success.Should().BeTrue();
        var configuration = await repository.GetByUserIdAsync(userId);
        configuration!.IsEnabled.Should().BeTrue();
        configuration.IsSetupComplete.Should().BeTrue();
        configuration.IsSmsEnabled.Should().BeTrue();
        configuration.PreferredMethod.Should().Be(MfaMethod.Sms);
        configuration.SmsVerificationCodeHash.Should().BeNull();
        configuration.SmsVerificationExpiresAt.Should().BeNull();
    }

    private sealed class CapturingSmsService : ISmsService
    {
        public string? LastPhoneNumber { get; private set; }

        public string? LastCode { get; private set; }

        public Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task SendVerificationCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
        {
            LastPhoneNumber = phoneNumber;
            LastCode = code;

            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryMfaConfigurationRepository : IUserMfaConfigurationRepository
    {
        private readonly Dictionary<Guid, UserMfaConfiguration> _configurations = [];

        public Task<UserMfaConfiguration> CreateAsync(UserMfaConfiguration configuration, CancellationToken cancellationToken = default)
        {
            configuration.Id = configuration.Id == Guid.Empty ? Guid.NewGuid() : configuration.Id;
            _configurations[configuration.UserId] = configuration;

            return Task.FromResult(configuration);
        }

        public Task<UserMfaConfiguration?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            _configurations.TryGetValue(userId, out var configuration);

            return Task.FromResult(configuration);
        }

        public Task<UserMfaConfiguration> UpdateAsync(UserMfaConfiguration configuration, CancellationToken cancellationToken = default)
        {
            _configurations[configuration.UserId] = configuration;

            return Task.FromResult(configuration);
        }

        public Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            _configurations.Remove(userId);

            return Task.CompletedTask;
        }

        public Task<bool> IsMfaEnabledAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_configurations.TryGetValue(userId, out var configuration) && configuration.IsEnabled);

        public Task<MfaMethod?> GetPreferredMethodAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_configurations.TryGetValue(userId, out var configuration) ? configuration.PreferredMethod : (MfaMethod?)null);

        public Task IncrementFailedAttemptsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            if (_configurations.TryGetValue(userId, out var configuration))
            {
                configuration.FailedAttempts++;
            }

            return Task.CompletedTask;
        }

        public Task ResetFailedAttemptsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            if (_configurations.TryGetValue(userId, out var configuration))
            {
                configuration.FailedAttempts = 0;
            }

            return Task.CompletedTask;
        }

        public Task SetLockoutAsync(Guid userId, DateTime lockoutUntil, CancellationToken cancellationToken = default)
        {
            if (_configurations.TryGetValue(userId, out var configuration))
            {
                configuration.LockedOutUntil = lockoutUntil;
            }

            return Task.CompletedTask;
        }
    }
}
