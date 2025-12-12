namespace GameGuild;

public readonly record struct TenantId(Guid Value)
{
    public static TenantId New() { return new TenantId(Guid.NewGuid()); }

    // conversão implícita para Guid
    public static implicit operator Guid(TenantId id) { return id.Value; }

    // conversão implícita de Guid para TenantId
    public static implicit operator TenantId(Guid value) { return new TenantId(value); }

    public override string ToString() { return Value.ToString(); }
}
