namespace GameGuild.Modules.Auth.Dtos
{
    /// <summary>
    /// Request DTO for refreshing authentication tokens
    /// </summary>
    public class RefreshTokenRequestDto
    {
        private string _refreshToken = string.Empty;

        private Guid? _tenantId;

        /// <summary>
        /// The refresh token to use
        /// </summary>
        public string RefreshToken
        {
            get => _refreshToken;
            set => _refreshToken = value;
        }

        /// <summary>
        /// Optional tenant ID to generate tenant-specific claims
        /// If not specified, no tenant claims will be included
        /// </summary>
        public Guid? TenantId
        {
            get => _tenantId;
            set => _tenantId = value;
        }
    }

    /// <summary>
    /// Response DTO with refreshed authentication tokens
    /// </summary>
    public class RefreshTokenResponseDto
    {
        private string _accessToken = string.Empty;

        private string _refreshToken = string.Empty;

        private DateTime _expiresAt;

        private Guid? _tenantId;

        /// <summary>
        /// New JWT access token
        /// </summary>
        public string AccessToken
        {
            get => _accessToken;
            set => _accessToken = value;
        }

        /// <summary>
        /// New refresh token
        /// </summary>
        public string RefreshToken
        {
            get => _refreshToken;
            set => _refreshToken = value;
        }

        /// <summary>
        /// When the refresh token expires
        /// </summary>
        public DateTime ExpiresAt
        {
            get => _expiresAt;
            set => _expiresAt = value;
        }

        /// <summary>
        /// Tenant ID associated with this token (if any)
        /// </summary>
        public Guid? TenantId
        {
            get => _tenantId;
            set => _tenantId = value;
        }
    }
}
