using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.Auth.Dtos
{
    public class LocalSignInRequestDto
    {
        public string? Username
        {
            get;
            set;
        }

        [Required, EmailAddress]
        public string Email
        {
            get;
            set;
        } = string.Empty;

        [Required]
        public string Password
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
