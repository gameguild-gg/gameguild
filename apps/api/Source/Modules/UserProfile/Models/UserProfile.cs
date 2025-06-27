using System.ComponentModel.DataAnnotations;
using GameGuild.Common.Entities;

namespace GameGuild.Modules.UserProfile.Models;

/// <summary>
/// Represents a user profile, which is a resource and can be localized and permissioned.
/// </summary>
public class UserProfile : ResourceBase {
  private string? _givenName;

  private string? _familyName;

  private string? _displayName;

  [MaxLength(100)]
  public string? GivenName {
    get => _givenName;
    set => _givenName = value;
  }

  [MaxLength(100)]
  public string? FamilyName {
    get => _familyName;
    set => _familyName = value;
  }

  [MaxLength(100)]
  public string? DisplayName {
    get => _displayName;
    set => _displayName = value;
  }
}
