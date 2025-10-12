namespace GameGuild.Modules.Tenants;

/// <summary> Service for validating tenant operations and data integrity </summary>
public interface ITenantValidationService
{
    /// <summary> Validate tenant creation request </summary>
    Task<ValidationResult> ValidateCreateTenantAsync(CreateTenantRequestDto request, CancellationToken cancellationToken = default);
    
    /// <summary> Validate tenant update request </summary>
    Task<ValidationResult> ValidateUpdateTenantAsync(Guid tenantId, UpdateTenantRequestDto request, CancellationToken cancellationToken = default);
    
    /// <summary> Validate tenant slug availability and format </summary>
    Task<ValidationResult> ValidateSlugAsync(string slug, Guid? excludeTenantId = null, CancellationToken cancellationToken = default);
    
    /// <summary> Validate tenant member operations </summary>
    Task<ValidationResult> ValidateMemberOperationAsync(Guid tenantId, Guid userId, string operation, CancellationToken cancellationToken = default);
    
    /// <summary> Validate tenant domain operations </summary>
    Task<ValidationResult> ValidateDomainOperationAsync(TenantDomain domain, string operation, CancellationToken cancellationToken = default);
    
    /// <summary> Validate tenant data integrity </summary>
    Task<ValidationResult> ValidateDataIntegrityAsync(Guid tenantId, CancellationToken cancellationToken = default);
    
    /// <summary> Validate bulk operations </summary>
    Task<ValidationResult> ValidateBulkOperationAsync(IEnumerable<Guid> tenantIds, string operation, CancellationToken cancellationToken = default);
}

/// <summary> Implementation of tenant validation service </summary>
public class TenantValidationService : ITenantValidationService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantMemberRepository _memberRepository;
    private readonly ITenantDomainsRepository _domainsRepository;
    private readonly ITenantSettingsRepository _settingsRepository;

    public TenantValidationService(
        ITenantRepository tenantRepository,
        ITenantMemberRepository memberRepository,
        ITenantDomainsRepository domainsRepository,
        ITenantSettingsRepository settingsRepository)
    {
        _tenantRepository = tenantRepository;
        _memberRepository = memberRepository;
        _domainsRepository = domainsRepository;
        _settingsRepository = settingsRepository;
    }

    public async Task<ValidationResult> ValidateCreateTenantAsync(CreateTenantRequestDto request, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        // Validate name
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            result.AddError("Name", "Tenant name is required");
        }
        else if (request.Name.Length < 2 || request.Name.Length > 100)
        {
            result.AddError("Name", "Tenant name must be between 2 and 100 characters");
        }

        // Validate slug
        var slug = request.Slug ?? request.Name.ToLowerInvariant().Replace(" ", "-");
        var slugValidation = await ValidateSlugAsync(slug, null, cancellationToken);
        if (!slugValidation.IsValid)
        {
            result.AddErrors(slugValidation.Errors);
        }

        // Validate admin email if provided
        if (!string.IsNullOrEmpty(request.AdminEmail))
        {
            if (!IsValidEmail(request.AdminEmail))
            {
                result.AddError("AdminEmail", "Invalid email address format");
            }
        }

        // Validate initial domains if provided
        if (request.InitialDomains != null)
        {
            foreach (var domain in request.InitialDomains)
            {
                if (!IsValidDomain(domain))
                {
                    result.AddError("InitialDomains", $"Invalid domain format: {domain}");
                }
            }
        }

        return result;
    }

    public async Task<ValidationResult> ValidateUpdateTenantAsync(Guid tenantId, UpdateTenantRequestDto request, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        // Check if tenant exists
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            result.AddError("TenantId", "Tenant not found");
            return result;
        }

        // Validate name
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            result.AddError("Name", "Tenant name is required");
        }
        else if (request.Name.Length < 2 || request.Name.Length > 100)
        {
            result.AddError("Name", "Tenant name must be between 2 and 100 characters");
        }

        // Validate admin email if provided
        if (!string.IsNullOrEmpty(request.AdminEmail))
        {
            if (!IsValidEmail(request.AdminEmail))
            {
                result.AddError("AdminEmail", "Invalid email address format");
            }
        }

        return result;
    }

    public async Task<ValidationResult> ValidateSlugAsync(string slug, Guid? excludeTenantId = null, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(slug))
        {
            result.AddError("Slug", "Slug is required");
            return result;
        }

        // Validate slug format
        if (!IsValidSlug(slug))
        {
            result.AddError("Slug", "Slug must contain only lowercase letters, numbers, and hyphens");
        }

        // Check if slug is available
        var isAvailable = await _tenantRepository.IsSlugAvailableAsync(slug, excludeTenantId, cancellationToken);
        if (!isAvailable)
        {
            result.AddError("Slug", "Slug is already in use");
        }

        return result;
    }

    public async Task<ValidationResult> ValidateMemberOperationAsync(Guid tenantId, Guid userId, string operation, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        // Check if tenant exists and is active
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            result.AddError("TenantId", "Tenant not found");
            return result;
        }

        if (!tenant.IsActive && operation != "remove")
        {
            result.AddError("Tenant", "Cannot perform member operations on inactive tenant");
        }

        // Check existing membership
        var existingMember = await _memberRepository.GetByUserAndTenantAsync(userId, tenantId, cancellationToken);
        
        switch (operation.ToLower())
        {
            case "add":
                if (existingMember != null && existingMember.IsActive)
                {
                    result.AddError("Member", "User is already a member of this tenant");
                }
                break;
            case "remove":
                if (existingMember == null)
                {
                    result.AddError("Member", "User is not a member of this tenant");
                }
                break;
            case "update":
                if (existingMember == null || !existingMember.IsActive)
                {
                    result.AddError("Member", "User is not an active member of this tenant");
                }
                break;
        }

        return result;
    }

    public async Task<ValidationResult> ValidateDomainOperationAsync(TenantDomain domain, string operation, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        // Validate domain format
        if (!IsValidDomain(domain.FullDomain))
        {
            result.AddError("Domain", "Invalid domain format");
        }

        // Check if domain already exists
        var existingDomain = await _domainsRepository.GetByDomainAsync(domain.FullDomain, cancellationToken);
        if (operation == "create" && existingDomain != null)
        {
            result.AddError("Domain", "Domain is already registered");
        }

        return result;
    }

    public async Task<ValidationResult> ValidateDataIntegrityAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            result.AddError("TenantId", "Tenant not found");
            return result;
        }

        // Check for orphaned members
        var members = await _memberRepository.GetByTenantIdAsync(tenantId, includeInactive: true, cancellationToken);
        // Add validation logic for orphaned members

        // Check for invalid domains
        var domains = await _domainsRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        // Add validation logic for invalid domains

        // Check settings consistency
        var settings = await _settingsRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        // Add validation logic for settings

        return result;
    }

    public async Task<ValidationResult> ValidateBulkOperationAsync(IEnumerable<Guid> tenantIds, string operation, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        if (!tenantIds.Any())
        {
            result.AddError("TenantIds", "At least one tenant ID is required");
            return result;
        }

        if (tenantIds.Count() > 100)
        {
            result.AddError("TenantIds", "Cannot process more than 100 tenants at once");
        }

        // Validate each tenant exists
        foreach (var tenantId in tenantIds)
        {
            var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
            if (tenant == null)
            {
                result.AddError("TenantIds", $"Tenant {tenantId} not found");
            }
        }

        return result;
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private bool IsValidSlug(string slug)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(slug, @"^[a-z0-9\-]+$");
    }

    private bool IsValidDomain(string domain)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(domain, 
            @"^[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?)*$");
    }
}

/// <summary> Validation result with errors </summary>
public class ValidationResult
{
    private readonly List<ValidationError> _errors = new();

    public bool IsValid => !_errors.Any();
    public IReadOnlyList<ValidationError> Errors => _errors.AsReadOnly();

    public void AddError(string field, string message)
    {
        _errors.Add(new ValidationError(field, message));
    }

    public void AddErrors(IEnumerable<ValidationError> errors)
    {
        _errors.AddRange(errors);
    }
}

/// <summary> Validation error </summary>
public class ValidationError
{
    public ValidationError(string field, string message)
    {
        Field = field;
        Message = message;
    }

    public string Field { get; }
    public string Message { get; }
}