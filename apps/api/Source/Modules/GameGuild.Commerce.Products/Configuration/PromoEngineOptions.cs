namespace GameGuild.Commerce.Products;

/// <summary>
/// Promo engine defaults (issue #418). Bound from configuration under
/// <c>PromoCodes</c> (e.g. <c>PromoCodes:DefaultTimeZone=America/Sao_Paulo</c>).
/// </summary>
public class PromoEngineOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "PromoCodes";

    /// <summary>
    /// Default IANA timezone promo validity windows are expressed in when a code
    /// does not carry its own <c>TimeZoneId</c> (the configurable tenant default).
    /// Unresolvable ids fall back to UTC.
    /// </summary>
    public string DefaultTimeZoneId { get; set; } = PromoValidityWindow.UtcTimeZoneId;
}

/// <summary>
/// Promo abuse-detection thresholds (issue #418 unusual-usage alerting). Velocity
/// anomalies are computed from recorded validation attempts and redemptions.
/// </summary>
public class PromoAbuseDetectionOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "PromoAbuseDetection";

    /// <summary>Validation attempts (per subject, any code) within the window before an anomaly is raised. Clamp: ≥ 2.</summary>
    public int ValidationBurstThreshold { get; set; } = 30;

    /// <summary>Window (minutes) for the validation velocity check. Clamp: 1–1440.</summary>
    public int ValidationWindowMinutes { get; set; } = 10;

    /// <summary>Recorded redemptions (per subject) within the window before an anomaly is raised. Clamp: ≥ 2.</summary>
    public int RedemptionBurstThreshold { get; set; } = 10;

    /// <summary>Window (minutes) for the redemption velocity check. Clamp: 1–1440.</summary>
    public int RedemptionWindowMinutes { get; set; } = 10;
}

/// <summary>
/// CAPTCHA gate for promo code redemption (issue #418 anti-farming). Bound from
/// configuration under <c>PromoRedemptionCaptcha</c>. When enabled, redemption
/// from untrusted contexts (anonymous callers) requires a CAPTCHA token and the
/// gate fails closed: a missing or unverifiable token rejects the redemption.
/// </summary>
public class PromoRedemptionCaptchaOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "PromoRedemptionCaptcha";

    /// <summary>
    /// Enables the gate. Disabled by default so existing integrations keep working;
    /// enabling it requires the web side to complete a CAPTCHA challenge for
    /// anonymous redemptions.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Shared secret used to verify self-issued CAPTCHA tokens (HMAC-SHA256 of the
    /// challenge). When empty and <see cref="Enabled"/> is set, every untrusted
    /// redemption is rejected — fail closed, never fail open.
    /// </summary>
    public string? VerificationSecret { get; set; }
}
