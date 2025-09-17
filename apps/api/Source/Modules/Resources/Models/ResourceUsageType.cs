namespace GameGuild.Modules.Resources.Models;

/// <summary> Types of resource usage that can be tracked and limited </summary>
public enum ResourceUsageType {
  /// <summary> Number of users in the system </summary>
  Users = 1,

  /// <summary> Number of projects/guilds </summary>
  Projects = 2,

  /// <summary> Storage space usage (in bytes) </summary>
  Storage = 3,

  /// <summary> API calls made </summary>
  ApiCalls = 4,

  /// <summary> Number of courses created </summary>
  Courses = 5,

  /// <summary> Number of tournaments </summary>
  Tournaments = 6,

  /// <summary> Number of teams </summary>
  Teams = 7,

  /// <summary> Number of events </summary>
  Events = 8,

  /// <summary> Number of media files uploaded </summary>
  MediaFiles = 9,

  /// <summary> Bandwidth usage (in bytes) </summary>
  Bandwidth = 10,

  /// <summary> Number of notifications sent </summary>
  Notifications = 11,

  /// <summary> Number of webhooks configured </summary>
  Webhooks = 12,

  /// <summary> Number of integrations enabled </summary>
  Integrations = 13,
}
