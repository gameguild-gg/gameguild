namespace GameGuild.Models {
  public class Team {
    private Guid _id;

    private string _name = string.Empty;

    private string? _description;

    private ICollection<TeamMember> _members = new List<TeamMember>();

    public Guid Id {
      get => _id;
      set => _id = value;
    }

    public string Name {
      get => _name;
      set => _name = value;
    }

    public string? Description {
      get => _description;
      set => _description = value;
    }

    public ICollection<TeamMember> Members {
      get => _members;
      set => _members = value;
    }
  }
}
