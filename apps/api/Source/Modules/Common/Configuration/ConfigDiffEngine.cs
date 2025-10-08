using System;
using System.Collections.Generic;
using System.Linq;

namespace GameGuild.Modules.Common.Configuration;

/// <summary>
/// Implementation of configuration diff engine.
/// </summary>
public sealed class ConfigDiffEngine : IConfigDiffEngine
{
    public List<ConfigChange> Compare(Dictionary<string, object> from, Dictionary<string, object> to)
    {
        var changes = new List<ConfigChange>();

        // Find added and modified
        foreach (var toKvp in to)
        {
            if (!from.TryGetValue(toKvp.Key, out var fromValue))
            {
                // Added
                changes.Add(new ConfigChange
                {
                    Path = toKvp.Key,
                    ChangeType = ConfigChangeType.Added,
                    NewValue = toKvp.Value
                });
            }
            else if (!Equals(fromValue, toKvp.Value))
            {
                // Modified
                changes.Add(new ConfigChange
                {
                    Path = toKvp.Key,
                    ChangeType = ConfigChangeType.Modified,
                    OldValue = fromValue,
                    NewValue = toKvp.Value
                });
            }
        }

        // Find removed
        foreach (var fromKvp in from)
        {
            if (!to.ContainsKey(fromKvp.Key))
            {
                changes.Add(new ConfigChange
                {
                    Path = fromKvp.Key,
                    ChangeType = ConfigChangeType.Removed,
                    OldValue = fromKvp.Value
                });
            }
        }

        return changes.OrderBy(c => c.Path).ToList();
    }
}
