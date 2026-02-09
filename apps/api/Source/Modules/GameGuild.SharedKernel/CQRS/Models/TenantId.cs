namespace GameGuild.CQRS.Models;

public readonly record struct TenantId(Guid Value)
{
    public static TenantId New() { return new TenantId(Guid.NewGuid()); }

    // Implicit conversion to Guid
    public static implicit operator Guid(TenantId id) { return id.Value; }

    // Implicit conversion from Guid to TenantId
    public static implicit operator TenantId(Guid value) { return new TenantId(value); }

    public override string ToString() { return Value.ToString(); }
}
