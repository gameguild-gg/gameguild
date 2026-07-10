using FluentValidation;

namespace GameGuild.Identity.Tenants;

public sealed class UpdateTenantMemberInviteCommandValidator : AbstractValidator<UpdateTenantMemberInviteCommand>
{
    public UpdateTenantMemberInviteCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required");
        RuleFor(x => x.Action).IsInEnum().WithMessage("Invite action is invalid");
    }
}
