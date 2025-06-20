using GameGuild.Common.Entities;

namespace GameGuild.Modules.Auth.Models
{
    /// <summary>
    /// Refresh token entity for managing user sessions
    /// </summary>
    public class RefreshToken : BaseEntity
    {
        public Guid UserId
        {
            get;
            set;
        }

        public string Token
        {
            get;
            set;
        } = string.Empty;

        public DateTime ExpiresAt
        {
            get;
            set;
        }

        public bool IsRevoked
        {
            get;
            set;
        }

        public string? RevokedByIp
        {
            get;
            set;
        }

        public DateTime? RevokedAt
        {
            get;
            set;
        }

        public string? ReplacedByToken
        {
            get;
            set;
        }

        public string CreatedByIp
        {
            get;
            set;
        } = string.Empty;

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
