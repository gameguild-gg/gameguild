namespace GameGuild.Modules.Auth.Dtos
{
    /// <summary>
    /// Request DTO for OAuth sign-in
    /// </summary>
    public class OAuthSignInRequestDto
    {
        /// <summary>
        /// OAuth authorization code
        /// </summary>
        public string Code
        {
            get;
            set;
        } = string.Empty;

        /// <summary>
        /// State parameter for CSRF protection
        /// </summary>
        public string State
        {
            get;
            set;
        } = string.Empty;

        /// <summary>
        /// Redirect URI used in the OAuth flow
        /// </summary>
        public string RedirectUri
        {
            get;
            set;
        } = string.Empty;

        /// <summary>
        /// Optional tenant ID to use for the sign-in
        /// If not provided, will use the first available tenant for the user
        /// </summary>
        public Guid? TenantId
        {
            get;
            set;
        }
    }

    public class GitHubUserDto
    {
        public long Id
        {
            get;
            set;
        }

        public string Login
        {
            get;
            set;
        } = string.Empty;

        public string Name
        {
            get;
            set;
        } = string.Empty;

        public string Email
        {
            get;
            set;
        } = string.Empty;

        public string AvatarUrl
        {
            get;
            set;
        } = string.Empty;
    }

    public class GoogleUserDto
    {
        public string Id
        {
            get;
            set;
        } = string.Empty;

        public string Email
        {
            get;
            set;
        } = string.Empty;

        public string Name
        {
            get;
            set;
        } = string.Empty;

        public string Picture
        {
            get;
            set;
        } = string.Empty;

        public bool EmailVerified
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Request DTO for Google ID Token validation (NextAuth.js integration)
    /// </summary>
    public class GoogleIdTokenRequestDto
    {
        /// <summary>
        /// Google ID Token from NextAuth.js
        /// </summary>
        public string IdToken
        {
            get;
            set;
        } = string.Empty;

        /// <summary>
        /// Optional tenant ID to use for the sign-in
        /// If not provided, will use the first available tenant for the user
        /// </summary>
        public Guid? TenantId
        {
            get;
            set;
        }
    }
}
