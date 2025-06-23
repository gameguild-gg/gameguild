using System.Text.Json.Serialization;

namespace GameGuild.Modules.Auth.Dtos
{
    /// <summary>
    /// DTO for sign-in response
    /// </summary>
    public class SignInResponseDto
    {
        private string _accessToken = string.Empty;

        private string _refreshToken = string.Empty;

        private DateTime _expires;

        private UserDto _user = new UserDto();

        private Guid? _tenantId;

        private IEnumerable<TenantInfoDto>? _availableTenants;

        /// <summary>
        /// JWT access token
        /// </summary>
        public string AccessToken
        {
            get => _accessToken;
            set => _accessToken = value;
        }

        /// <summary>
        /// Refresh token
        /// </summary>
        public string RefreshToken
        {
            get => _refreshToken;
            set => _refreshToken = value;
        }

        /// <summary>
        /// When the access token expires
        /// </summary>
        public DateTime Expires
        {
            get => _expires;
            set => _expires = value;
        }

        /// <summary>
        /// User information
        /// </summary>
        public UserDto User
        {
            get => _user;
            set => _user = value;
        }

        /// <summary>
        /// Current tenant ID
        /// </summary>
        public Guid? TenantId
        {
            get => _tenantId;
            set => _tenantId = value;
        }

        /// <summary>
        /// List of tenants the user has access to
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IEnumerable<TenantInfoDto>? AvailableTenants
        {
            get => _availableTenants;
            set => _availableTenants = value;
        }
    }
}
