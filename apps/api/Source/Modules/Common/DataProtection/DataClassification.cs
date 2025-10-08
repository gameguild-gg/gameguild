using System;

namespace GameGuild.Modules.Common.DataProtection;

/// <summary>
/// Data classification levels for GDPR/privacy compliance.
/// </summary>
[Flags]
public enum DataClassification
{
    /// <summary>
    /// Public data - no restrictions.
    /// </summary>
    Public = 0,

    /// <summary>
    /// Internal data - restricted to organization.
    /// </summary>
    Internal = 1 << 0,

    /// <summary>
    /// Confidential data - restricted access.
    /// </summary>
    Confidential = 1 << 1,

    /// <summary>
    /// Personally Identifiable Information (PII).
    /// </summary>
    PII = 1 << 2,

    /// <summary>
    /// Sensitive Personal Information (race, religion, health, etc.).
    /// </summary>
    SensitivePII = 1 << 3,

    /// <summary>
    /// Financial data (payment info, bank details).
    /// </summary>
    Financial = 1 << 4,

    /// <summary>
    /// Health-related data (HIPAA).
    /// </summary>
    Health = 1 << 5,

    /// <summary>
    /// Biometric data (fingerprints, facial recognition).
    /// </summary>
    Biometric = 1 << 6,

    /// <summary>
    /// Location data (GPS coordinates, IP addresses).
    /// </summary>
    Location = 1 << 7,

    /// <summary>
    /// Authentication credentials (passwords, tokens).
    /// </summary>
    Credentials = 1 << 8
}

/// <summary>
/// GDPR lawful basis for processing personal data.
/// </summary>
public enum GdprLawfulBasis
{
    /// <summary>
    /// User has given explicit consent.
    /// </summary>
    Consent,

    /// <summary>
    /// Necessary for contract performance.
    /// </summary>
    Contract,

    /// <summary>
    /// Legal obligation.
    /// </summary>
    LegalObligation,

    /// <summary>
    /// Protect vital interests.
    /// </summary>
    VitalInterests,

    /// <summary>
    /// Public task or official authority.
    /// </summary>
    PublicTask,

    /// <summary>
    /// Legitimate interests.
    /// </summary>
    LegitimateInterests
}

/// <summary>
/// Attribute to mark properties/classes with data classification.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Field, AllowMultiple = false)]
public sealed class DataClassificationAttribute : Attribute
{
    /// <summary>
    /// Gets the data classification level.
    /// </summary>
    public DataClassification Classification { get; }

    /// <summary>
    /// Gets the GDPR lawful basis for processing.
    /// </summary>
    public GdprLawfulBasis? LawfulBasis { get; set; }

    /// <summary>
    /// Gets the retention period in days (null = indefinite).
    /// </summary>
    public int? RetentionDays { get; set; }

    /// <summary>
    /// Gets whether this data should be encrypted at rest.
    /// </summary>
    public bool RequiresEncryption { get; set; }

    /// <summary>
    /// Gets whether this data should be masked in logs.
    /// </summary>
    public bool MaskInLogs { get; set; } = true;

    /// <summary>
    /// Gets the purpose of processing this data.
    /// </summary>
    public string? ProcessingPurpose { get; set; }

    public DataClassificationAttribute(DataClassification classification)
    {
        Classification = classification;
    }
}

/// <summary>
/// Attribute to mark data subject to GDPR right to erasure ("right to be forgotten").
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false)]
public sealed class GdprErasableAttribute : Attribute
{
    /// <summary>
    /// Gets whether erasure cascades to related entities.
    /// </summary>
    public bool Cascade { get; set; }

    /// <summary>
    /// Gets the anonymization strategy instead of deletion.
    /// </summary>
    public string? AnonymizationStrategy { get; set; }
}

/// <summary>
/// Attribute to mark data subject to GDPR right to data portability.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false)]
public sealed class GdprPortableAttribute : Attribute
{
    /// <summary>
    /// Gets the export format (JSON, CSV, XML).
    /// </summary>
    public string ExportFormat { get; set; } = "JSON";
}

/// <summary>
/// Attribute to exclude from GDPR exports (system-generated data).
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class GdprExcludeAttribute : Attribute
{
}
