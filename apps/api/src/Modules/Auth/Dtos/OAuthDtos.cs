namespace GameGuild.Modules.Auth.Dtos
{
    /// <summary>
    /// Request DTO for OAuth sign-in
    /// </summary>
    public class OAuthSignInRequestDto
    {
        private string _code = string.Empty;

        private string _state = string.Empty;

        private string _redirectUri = string.Empty;

        private Guid? _tenantId;

        /// <summary>
        /// OAuth authorization code
        /// </summary>
        public string Code
        {
            get => _code;
            set => _code = value;
        }

        /// <summary>
        /// State parameter for CSRF protection
        /// </summary>
        public string State
        {
            get => _state;
            set => _state = value;
        }

        /// <summary>
        /// Redirect URI used in the OAuth flow
        /// </summary>
        public string RedirectUri
        {
            get => _redirectUri;
            set => _redirectUri = value;
        }

        /// <summary>
        /// Optional tenant ID to use for the sign-in
        /// If not provided, will use the first available tenant for the user
        /// </summary>
        public Guid? TenantId
        {
            get => _tenantId;
            set => _tenantId = value;
        }
    }

    public class GitHubUserDto
    {
        private long _id;

        private string _login = string.Empty;

        private string _name = string.Empty;

        private string _email = string.Empty;

        private string _avatarUrl = string.Empty;

        public long Id
        {
            get => _id;
            set => _id = value;
        }

        public string Login
        {
            get => _login;
            set => _login = value;
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public string Email
        {
            get => _email;
            set => _email = value;
        }

        public string AvatarUrl
        {
            get => _avatarUrl;
            set => _avatarUrl = value;
        }
    }

    public class GoogleUserDto
    {
        private string _id = string.Empty;

        private string _email = string.Empty;

        private string _name = string.Empty;

        private string _picture = string.Empty;

        private bool _emailVerified;

        public string Id
        {
            get => _id;
            set => _id = value;
        }

        public string Email
        {
            get => _email;
            set => _email = value;
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public string Picture
        {
            get => _picture;
            set => _picture = value;
        }

        public bool EmailVerified
        {
            get => _emailVerified;
            set => _emailVerified = value;
        }
    }

    /// <summary>
    /// Request DTO for Google ID Token validation (NextAuth.js integration)
    /// </summary>
    public class GoogleIdTokenRequestDto
    {
        private string _idToken = string.Empty;

        private Guid? _tenantId;

        /// <summary>
        /// Google ID Token from NextAuth.js
        /// </summary>
        public string IdToken
        {
            get => _idToken;
            set => _idToken = value;
        }

        /// <summary>
        /// Optional tenant ID to use for the sign-in
        /// If not provided, will use the first available tenant for the user
        /// </summary>
        public Guid? TenantId
        {
            get => _tenantId;
            set => _tenantId = value;
        }
    }
}
