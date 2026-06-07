using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Seeder service for default policy definitions.
///     Creates standard policies for common authorization scenarios.
/// </summary>
public sealed class PolicyDefinitionSeeder
{
    private readonly IPolicyDefinitionRepository _repository;
    private readonly ILogger<PolicyDefinitionSeeder> _logger;

    /// <summary>
    ///     Initializes a new instance of <see cref="PolicyDefinitionSeeder"/>.
    /// </summary>
    public PolicyDefinitionSeeder(
        IPolicyDefinitionRepository repository,
        ILogger<PolicyDefinitionSeeder> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    ///     Seeds default policies if they don't already exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting policy definition seeding...");

        var defaultPolicies = GetDefaultPolicies();
        var seededCount = 0;

        foreach (var policy in defaultPolicies)
        {
            var existing = await _repository.GetByNameAsync(policy.PolicyName, null, cancellationToken)
                .ConfigureAwait(false);

            if (existing is null)
            {
                await _repository.AddAsync(policy, cancellationToken).ConfigureAwait(false);
                seededCount++;
                _logger.LogDebug("Seeded policy: {PolicyName}", policy.PolicyName);
            }
            else
            {
                _logger.LogTrace("Policy already exists: {PolicyName}", policy.PolicyName);
            }
        }

        if (seededCount > 0)
        {
            await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Seeded {Count} new policy definitions", seededCount);
        }
        else
        {
            _logger.LogInformation("No new policies to seed - all default policies already exist");
        }
    }

    /// <summary>
    ///     Gets the collection of default policies to seed.
    ///     All policies use rule-based evaluation.
    /// </summary>
    private static IEnumerable<PolicyDefinitionEntity> GetDefaultPolicies()
    {
        // ========================
        // AUTHENTICATION POLICIES
        // ========================
        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = "Authenticated",
            Description = "Requires authenticated user",
            RequireAuthentication = true,
            UseRuleBasedEvaluation = true,
            RulesJson = "[]",
            IsActive = true,
            PolicyVersion = 1
        };

        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = "Anonymous",
            Description = "Allows anonymous access",
            RequireAuthentication = false,
            UseRuleBasedEvaluation = true,
            RulesJson = "[]",
            IsActive = true,
            PolicyVersion = 1
        };

        // ========================
        // TENANT-SCOPED POLICIES
        // ========================
        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = "TenantMember",
            Description = "Requires authenticated user with matching tenant context",
            RequireAuthentication = true,
            IsTenantScoped = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Ensure user belongs to the request tenant",
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };

        // ========================
        // PROJECT POLICIES (DAC)
        // ========================
        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = "Project.Read",
            Description = "Read access to projects",
            RequireAuthentication = true,
            ResourceType = "Project",
            MinimumAccessLevel = "Read",
            IsTenantScoped = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Ensure user belongs to the request tenant",
                    "Enabled": true
                },
                {
                    "Type": "OwnerOrAcl",
                    "Description": "Check ACL access for read",
                    "Params": {
                        "allowOwner": true,
                        "minimumAccessLevel": "Read"
                    },
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };

        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = "Project.Edit",
            Description = "Edit access to projects",
            RequireAuthentication = true,
            ResourceType = "Project",
            MinimumAccessLevel = "Write",
            IsTenantScoped = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Ensure user belongs to the request tenant",
                    "Enabled": true
                },
                {
                    "Type": "OwnerOrAcl",
                    "Description": "Check ACL access for write",
                    "Params": {
                        "allowOwner": true,
                        "minimumAccessLevel": "Write"
                    },
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };

        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = "Project.Delete",
            Description = "Delete access to projects",
            RequireAuthentication = true,
            ResourceType = "Project",
            IsTenantScoped = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Ensure user belongs to the request tenant",
                    "Enabled": true
                },
                {
                    "Type": "OwnerOrAcl",
                    "Description": "Only owner can delete",
                    "Params": {
                        "allowOwner": true,
                        "minimumAccessLevel": "Owner"
                    },
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };

        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = "Project.Owner",
            Description = "Full owner access to projects",
            RequireAuthentication = true,
            ResourceType = "Project",
            MinimumAccessLevel = "Owner",
            IsTenantScoped = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Ensure user belongs to the request tenant",
                    "Enabled": true
                },
                {
                    "Type": "OwnerOrAcl",
                    "Description": "Require owner access",
                    "Params": {
                        "allowOwner": true,
                        "minimumAccessLevel": "Owner"
                    },
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };

        // ========================
        // CONTENT POLICIES (DAC)
        // ========================
        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = "Content.Read",
            Description = "Read access to content items",
            RequireAuthentication = true,
            ResourceType = "Content",
            MinimumAccessLevel = "Read",
            IsTenantScoped = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Ensure user belongs to the request tenant",
                    "Enabled": true
                },
                {
                    "Type": "RequireAnyPermission",
                    "Description": "Any of these permissions grants read access",
                    "Params": {
                        "permissions": ["content:read", "content:write", "content:admin", "admin"]
                    },
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };

        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = "Content.Edit",
            Description = "Edit access to content items",
            RequireAuthentication = true,
            ResourceType = "Content",
            MinimumAccessLevel = "Write",
            IsTenantScoped = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Ensure user belongs to the request tenant",
                    "Enabled": true
                },
                {
                    "Type": "OwnerOrAcl",
                    "Description": "Check ACL access for write",
                    "Params": {
                        "allowOwner": true,
                        "minimumAccessLevel": "Write"
                    },
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };

        // ========================
        // COURSE POLICIES (DAC)
        // ========================
        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = "Course.Read",
            Description = "Read access to courses",
            RequireAuthentication = true,
            ResourceType = "Course",
            MinimumAccessLevel = "Read",
            IsTenantScoped = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Ensure user belongs to the request tenant",
                    "Enabled": true
                },
                {
                    "Type": "OwnerOrAcl",
                    "Description": "Check ACL access for read",
                    "Params": {
                        "allowOwner": true,
                        "minimumAccessLevel": "Read"
                    },
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };

        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = "Course.Manage",
            Description = "Management access to courses",
            RequireAuthentication = true,
            ResourceType = "Course",
            MinimumAccessLevel = "Admin",
            IsTenantScoped = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Ensure user belongs to the request tenant",
                    "Enabled": true
                },
                {
                    "Type": "OwnerOrAcl",
                    "Description": "Check ACL access for admin",
                    "Params": {
                        "allowOwner": true,
                        "minimumAccessLevel": "Admin"
                    },
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };

        // ========================
        // PERMISSION-BASED POLICIES (RBAC)
        // ========================
        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = "Admin",
            Description = "Administrator role with full access",
            RequireAuthentication = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "RequireAllPermissions",
                    "Description": "Require admin permission",
                    "Params": {
                        "permissions": ["admin:*"]
                    },
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };

        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = "TenantAdmin",
            Description = "Tenant administrator with full tenant access",
            RequireAuthentication = true,
            IsTenantScoped = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Ensure user belongs to the request tenant",
                    "Enabled": true
                },
                {
                    "Type": "RequireAllPermissions",
                    "Description": "Require tenant admin permission",
                    "Params": {
                        "permissions": ["tenant:admin"]
                    },
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };

        // ========================
        // GRANULAR USER POLICIES (Users Module)
        // ========================
        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = "Users.Read",
            Description = "Read access to user data",
            RequireAuthentication = true,
            IsTenantScoped = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Ensure user belongs to the request tenant",
                    "Enabled": true
                },
                {
                    "Type": "RequireAllPermissions",
                    "Description": "Require users.read permission",
                    "Params": {
                        "permissions": ["users:read"]
                    },
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };

        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = "Users.Create",
            Description = "Permission to create new users",
            RequireAuthentication = true,
            IsTenantScoped = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Ensure user belongs to the request tenant",
                    "Enabled": true
                },
                {
                    "Type": "RequireAllPermissions",
                    "Description": "Require users.create permission",
                    "Params": {
                        "permissions": ["users:create"]
                    },
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };

        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = "Users.Update",
            Description = "Permission to update existing users",
            RequireAuthentication = true,
            IsTenantScoped = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Ensure user belongs to the request tenant",
                    "Enabled": true
                },
                {
                    "Type": "RequireAllPermissions",
                    "Description": "Require users.update permission",
                    "Params": {
                        "permissions": ["users:update"]
                    },
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };

        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = "Users.Delete",
            Description = "Permission to soft-delete users",
            RequireAuthentication = true,
            IsTenantScoped = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Ensure user belongs to the request tenant",
                    "Enabled": true
                },
                {
                    "Type": "RequireAllPermissions",
                    "Description": "Require users.delete permission",
                    "Params": {
                        "permissions": ["users:delete"]
                    },
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };

        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = "Users.Admin",
            Description = "Administrative user operations (activate, deactivate, suspend, restore)",
            RequireAuthentication = true,
            IsTenantScoped = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Ensure user belongs to the request tenant",
                    "Enabled": true
                },
                {
                    "Type": "RequireAllPermissions",
                    "Description": "Require users.admin permission",
                    "Params": {
                        "permissions": ["users:admin"]
                    },
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };

        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = "Users.Purge",
            Description = "Dangerous: Permission to permanently delete users (irreversible)",
            RequireAuthentication = true,
            IsTenantScoped = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Ensure user belongs to the request tenant",
                    "Enabled": true
                },
                {
                    "Type": "RequireAllPermissions",
                    "Description": "Require users.purge permission",
                    "Params": {
                        "permissions": ["users:purge"]
                    },
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };

        // ========================
        // EMPLOYEE POLICIES (Human Resources Module)
        // ========================
        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = Policies.EmployeesRead,
            Description = "Read access to employee records",
            RequireAuthentication = true,
            IsTenantScoped = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Ensure user belongs to the request tenant",
                    "Enabled": true
                },
                {
                    "Type": "RequireAllPermissions",
                    "Description": "Require users.read permission",
                    "Params": {
                        "permissions": ["users:read"]
                    },
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };

        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = Policies.EmployeesCreate,
            Description = "Permission to create employee records",
            RequireAuthentication = true,
            IsTenantScoped = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Ensure user belongs to the request tenant",
                    "Enabled": true
                },
                {
                    "Type": "RequireAllPermissions",
                    "Description": "Require users.create permission",
                    "Params": {
                        "permissions": ["users:create"]
                    },
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };

        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = Policies.EmployeesUpdate,
            Description = "Permission to update employee records",
            RequireAuthentication = true,
            IsTenantScoped = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Ensure user belongs to the request tenant",
                    "Enabled": true
                },
                {
                    "Type": "RequireAllPermissions",
                    "Description": "Require users.update permission",
                    "Params": {
                        "permissions": ["users:update"]
                    },
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };

        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = Policies.EmployeesDelete,
            Description = "Permission to delete employee records",
            RequireAuthentication = true,
            IsTenantScoped = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Ensure user belongs to the request tenant",
                    "Enabled": true
                },
                {
                    "Type": "RequireAllPermissions",
                    "Description": "Require users.delete permission",
                    "Params": {
                        "permissions": ["users:delete"]
                    },
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };

        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = "Users.ReadSelf",
            Description = "Read own user data OR manage any user",
            RequireAuthentication = true,
            IsTenantScoped = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Ensure user belongs to the request tenant",
                    "Enabled": true
                },
                {
                    "Type": "SelfOrPermission",
                    "Description": "Allow reading self or users with manage permission",
                    "Params": {
                        "selfPermission": "users:read:self",
                        "anyPermission": "users:manage"
                    },
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };

        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = "Users.EditSelf",
            Description = "Edit own profile OR manage other users",
            RequireAuthentication = true,
            IsTenantScoped = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Ensure user belongs to the request tenant",
                    "Enabled": true
                },
                {
                    "Type": "SelfOrPermission",
                    "Description": "Allow editing self or users with manage permission",
                    "Params": {
                        "selfPermission": "users:edit:self",
                        "anyPermission": "users:manage"
                    },
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };

        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = "Users.DeleteSelf",
            Description = "Delete own account OR manage other users",
            RequireAuthentication = true,
            IsTenantScoped = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Ensure user belongs to the request tenant",
                    "Enabled": true
                },
                {
                    "Type": "SelfOrPermission",
                    "Description": "Allow deleting self or users with manage permission",
                    "Params": {
                        "selfPermission": "users:delete:self",
                        "anyPermission": "users:manage"
                    },
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };

        // ========================
        // SECURE ADMIN POLICIES
        // ========================
        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = "SecureAdmin",
            Description = "Admin operations requiring MFA",
            RequireAuthentication = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Ensure user belongs to the request tenant",
                    "Enabled": true
                },
                {
                    "Type": "RequireAllPermissions",
                    "Description": "Require admin permission",
                    "Params": {
                        "permissions": ["admin"]
                    },
                    "Enabled": true
                },
                {
                    "Type": "RequireMfa",
                    "Description": "Require recent MFA verification",
                    "Params": {
                        "requireRecent": true,
                        "maxAgeMinutes": 30
                    },
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };

        // ========================
        // DOCUMENT POLICIES
        // ========================
        yield return new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = "Document.Edit",
            Description = "Edit documents user owns or has ACL access to",
            RequireAuthentication = true,
            IsTenantScoped = true,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Ensure user belongs to the request tenant",
                    "Enabled": true
                },
                {
                    "Type": "OwnerOrAcl",
                    "Description": "Allow owner or users with Write ACL access",
                    "Params": {
                        "allowOwner": true,
                        "minimumAccessLevel": "Write"
                    },
                    "Enabled": true
                }
            ]
            """,
            IsActive = true,
            PolicyVersion = 1
        };
    }
}
