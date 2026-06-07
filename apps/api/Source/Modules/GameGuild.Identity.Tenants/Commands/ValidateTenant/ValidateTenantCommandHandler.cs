using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for validating tenant data before creation
/// </summary>
[ExcludeFromCodeCoverage]
public partial class ValidateTenantCommandHandler(ITenantRepository tenantRepository) 
    : IRequestHandler<ValidateTenantCommand, TenantValidationResponse>
{
    /// <summary>
    ///     Validates tenant data without creating the tenant
    /// </summary>
    public async Task<TenantValidationResponse> Handle(ValidateTenantCommand request, CancellationToken cancellationToken)
    {
        var response = new TenantValidationResponse
        {
            IsValid = true,
            Errors = new List<TenantValidationError>(),
            Warnings = new List<TenantValidationWarning>(),
            Suggestions = new List<string>(),
            SlugValidation = new SlugValidation()
        };

        // Validate name
        ValidateName(request.Name, response);

        // Validate and check slug availability
        await ValidateSlugAsync(request.Slug, response, cancellationToken).ConfigureAwait(false);

        // Validate admin email
        ValidateAdminEmail(request.AdminEmail, response);

        // Set overall validity
        response.IsValid = response.Errors.Count == 0;

        return response;
    }

    private static void ValidateName(string name, TenantValidationResponse response)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            response.Errors.Add(new TenantValidationError
            {
                Field = "name",
                Code = "REQUIRED",
                Message = "Tenant name is required"
            });
            return;
        }

        if (name.Length < 2)
        {
            response.Errors.Add(new TenantValidationError
            {
                Field = "name",
                Code = "TOO_SHORT",
                Message = "Tenant name must be at least 2 characters"
            });
        }

        if (name.Length > 100)
        {
            response.Errors.Add(new TenantValidationError
            {
                Field = "name",
                Code = "TOO_LONG",
                Message = "Tenant name must be at most 100 characters"
            });
        }

        // Warn about special characters
        if (name.Any(c => !char.IsLetterOrDigit(c) && c != ' ' && c != '-' && c != '_'))
        {
            response.Warnings.Add(new TenantValidationWarning
            {
                Field = "name",
                Code = "SPECIAL_CHARACTERS",
                Message = "Tenant name contains special characters which may cause display issues"
            });
        }
    }

    private async Task ValidateSlugAsync(string slug, TenantValidationResponse response, CancellationToken cancellationToken)
    {
        response.SlugValidation = new SlugValidation
        {
            IsValid = true,
            IsAvailable = true,
            SuggestedAlternatives = new List<string>()
        };

        if (string.IsNullOrWhiteSpace(slug))
        {
            response.Errors.Add(new TenantValidationError
            {
                Field = "slug",
                Code = "REQUIRED",
                Message = "Tenant slug is required"
            });
            response.SlugValidation.IsValid = false;
            return;
        }

        // Check slug format
        if (!SlugRegex().IsMatch(slug))
        {
            response.Errors.Add(new TenantValidationError
            {
                Field = "slug",
                Code = "INVALID_FORMAT",
                Message = "Slug must contain only lowercase letters, numbers, and hyphens, and cannot start or end with a hyphen"
            });
            response.SlugValidation.IsValid = false;
        }

        if (slug.Length < 3)
        {
            response.Errors.Add(new TenantValidationError
            {
                Field = "slug",
                Code = "TOO_SHORT",
                Message = "Slug must be at least 3 characters"
            });
            response.SlugValidation.IsValid = false;
        }

        if (slug.Length > 50)
        {
            response.Errors.Add(new TenantValidationError
            {
                Field = "slug",
                Code = "TOO_LONG",
                Message = "Slug must be at most 50 characters"
            });
            response.SlugValidation.IsValid = false;
        }

        // Check for reserved slugs
        var reservedSlugs = new[] { "admin", "api", "app", "www", "mail", "support", "help", "system" };
        if (reservedSlugs.Contains(slug.ToLowerInvariant()))
        {
            response.Errors.Add(new TenantValidationError
            {
                Field = "slug",
                Code = "RESERVED",
                Message = $"The slug '{slug}' is reserved and cannot be used"
            });
            response.SlugValidation.IsAvailable = false;
        }

        // Check availability in database
        var isUnique = await tenantRepository.IsSlugUniqueAsync(slug, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!isUnique)
        {
            response.Errors.Add(new TenantValidationError
            {
                Field = "slug",
                Code = "ALREADY_EXISTS",
                Message = $"The slug '{slug}' is already in use"
            });
            response.SlugValidation.IsAvailable = false;

            // Generate alternative suggestions
            response.SlugValidation.SuggestedAlternatives = await GenerateSlugAlternativesAsync(slug, cancellationToken).ConfigureAwait(false);
            response.Suggestions.Add($"Try one of these alternatives: {string.Join(", ", response.SlugValidation.SuggestedAlternatives.Take(3))}");
        }
    }

    private async Task<List<string>> GenerateSlugAlternativesAsync(string baseSlug, CancellationToken cancellationToken)
    {
        var alternatives = new List<string>();
        var suffixes = new[] { "1", "2", "org", "team", "hq" };

        foreach (var suffix in suffixes)
        {
            var candidate = $"{baseSlug}-{suffix}";
            if (await tenantRepository.IsSlugUniqueAsync(candidate, cancellationToken: cancellationToken))
            {
                alternatives.Add(candidate);
                if (alternatives.Count >= 5) break;
            }
        }

        // If we still need more, try random numbers
        if (alternatives.Count < 3)
        {
            var random = new Random();
            for (var i = 0; i < 5 && alternatives.Count < 5; i++)
            {
                var candidate = $"{baseSlug}-{random.Next(100, 999)}";
                if (await tenantRepository.IsSlugUniqueAsync(candidate, cancellationToken: cancellationToken))
                {
                    alternatives.Add(candidate);
                }
            }
        }

        return alternatives;
    }

    [ExcludeFromCodeCoverage]
    private static void ValidateAdminEmail(string email, TenantValidationResponse response)
    {
        var safeEmail = email ?? string.Empty;

        if (string.IsNullOrWhiteSpace(safeEmail))
        {
            response.Errors.Add(new TenantValidationError
            {
                Field = "adminEmail",
                Code = "REQUIRED",
                Message = "Admin email is required"
            });
            return;
        }

        if (!EmailRegex().IsMatch(safeEmail))
        {
            response.Errors.Add(new TenantValidationError
            {
                Field = "adminEmail",
                Code = "INVALID_FORMAT",
                Message = "Invalid email format"
            });
        }

        // Warn about personal email domains
        if (IsPersonalEmailDomain(safeEmail))
        {
            response.Warnings.Add(new TenantValidationWarning
            {
                Field = "adminEmail",
                Code = "PERSONAL_EMAIL",
                Message = "Using a personal email address. Consider using a business email for better organization management"
            });
        }
    }

    [ExcludeFromCodeCoverage]
    private static string ExtractEmailDomainOrEmpty(string email)
    {
        var atIndex = email.LastIndexOf('@');
        return atIndex >= 0 ? email.Substring(atIndex + 1).ToLowerInvariant() : string.Empty;
    }

    [ExcludeFromCodeCoverage]
    private static bool IsPersonalEmailDomain(string email)
    {
        var domain = ExtractEmailDomainOrEmpty(email);

        return domain == "gmail.com"
            || domain == "yahoo.com"
            || domain == "hotmail.com"
            || domain == "outlook.com"
            || domain == "live.com";
    }

    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SlugRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();
}
