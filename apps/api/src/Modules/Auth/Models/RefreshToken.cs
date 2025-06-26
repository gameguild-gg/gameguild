using GameGuild.Common.Entities;

namespace GameGuild.Modules.Auth.Models
{
    /// <summary>
    /// Refresh token entity for managing user sessions
    /// </summary>
    public class RefreshToken : BaseEntity
    {
        private Guid _userId;

        private string _token = string.Empty;

        private DateTime _expiresAt;

        private bool _isRevoked;

        private string? _revokedByIp;

        private DateTime? _revokedAt;

        private string? _replacedByToken;

        private string _createdByIp = string.Empty;

        public Guid UserId
        {
            get => _userId;
            set => _userId = value;
        }

        public string Token
        {
            get => _token;
            set => _token = value;
        }

        public DateTime ExpiresAt
        {
            get => _expiresAt;
            set => _expiresAt = value;
        }

        public bool IsRevoked
        {
            get => _isRevoked;
            set => _isRevoked = value;
        }

        public string? RevokedByIp
        {
            get => _revokedByIp;
            set => _revokedByIp = value;
        }

        public DateTime? RevokedAt
        {
            get => _revokedAt;
            set => _revokedAt = value;
        }

        public string? ReplacedByToken
        {
            get => _replacedByToken;
            set => _replacedByToken = value;
        }

        public string CreatedByIp
        {
            get => _createdByIp;
            set => _createdByIp = value;
        }

        public bool IsExpired
        {
            get => DateTime.UtcNow >= ExpiresAt;
        }

        public bool IsActive
        {
            get => !IsRevoked && !IsExpired;
        }
    }
}
