namespace GameGuild.Modules.Votes;

/// <summary>
/// Interface for entities that support voting (upvote/downvote).
/// </summary>
public interface IVotable {
  /// <summary>
  /// Gets the collection of votes for this entity.
  /// </summary>
  ICollection<Vote> Votes { get; set; }
}
