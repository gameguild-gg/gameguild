using GameGuild.Common;

namespace GameGuild.API.Tests.Helpers
{
    /// <summary>
    /// Represents a relationship between a User and a Tenant
    /// Used only for testing purposes
    /// </summary>
    public class UserTenant : Entity
    {
        /// <summary>
        /// The ID of the user
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// The ID of the tenant
        /// </summary>
        public Guid TenantId { get; init; }

        /// <summary>
        /// Whether this user-tenant relationship is active
        /// </summary>
        public bool IsActive { get; init; }
    }
}
