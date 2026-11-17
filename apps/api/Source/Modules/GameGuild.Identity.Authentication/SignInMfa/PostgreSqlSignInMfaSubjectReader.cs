using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authentication;

public sealed class PostgreSqlSignInMfaSubjectReader(IApplicationDbContext context) : ISignInMfaSubjectReader
{
    public async Task<SignInMfaSubjectState?> ReadCurrentAsync(Guid subjectId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (subjectId == Guid.Empty) { throw new ArgumentException("A subject is required.", nameof(subjectId)); }
        var user = await context.Set<User>().AsNoTracking().SingleOrDefaultAsync(
            row => row.Id == subjectId && row.DeletedAt == null, cancellationToken).ConfigureAwait(false);
        if (user is null) { return null; }
        var enrollment = await context.Set<UserMfaConfiguration>().AsNoTracking().SingleOrDefaultAsync(
            row => row.UserId == subjectId, cancellationToken).ConfigureAwait(false);
        var enrolled = enrollment is { IsEnabled: true, IsSetupComplete: true };
        var methods = new List<MfaMethod>();
        if (enrolled && enrollment is not null)
        {
            if (!string.IsNullOrWhiteSpace(enrollment.TotpSecretKey)) { methods.Add(MfaMethod.Totp); }
            if (BackupCodeSet.Read(enrollment.BackupCodes).Hashes.Count > 0) { methods.Add(MfaMethod.BackupCode); }
            if (!string.IsNullOrWhiteSpace(enrollment.SmsPhoneNumber)) { methods.Add(MfaMethod.Sms); }
        }
        return new SignInMfaSubjectState(user, enrolled, enrollment?.LockedOutUntil, methods);
    }
}
