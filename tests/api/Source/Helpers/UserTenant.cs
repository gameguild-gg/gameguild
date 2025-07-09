using System;
using GameGuild.Common.Entities;

namespace GameGuild.Modules.Tenants.Models
{
    /// <summary>
    /// Represents a relationship between a User and a Tenant
    /// Used only for testing purposes
    /// </summary>
    public class UserTenant : BaseEntity
    {
        /// <summary>
        /// The ID of the user
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The ID of the tenant
        /// </summary>
        public Guid TenantId { get; set; }
        
        /// <summary>
        /// Whether this user-tenant relationship is active
        /// </summary>
        public new bool IsActive { get; set; }
    }
}
