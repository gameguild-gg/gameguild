using System.ComponentModel.DataAnnotations;

namespace GameGuild.Compliance.Audit;

/// <summary>Preserves decimal range metadata while allowing cached validators to be shared by concurrent requests.</summary>
public sealed class ThreadSafeDecimalRangeAttribute(string minimum, string maximum)
    : RangeAttribute(typeof(decimal), minimum, maximum)
{
    private readonly object _conversionLock = new();

    public override bool IsValid(object? value)
    {
        // RangeAttribute lazily replaces string bounds with decimals before publishing
        // its converter. A second first-use call must not observe that partial state.
        lock (_conversionLock)
        {
            return base.IsValid(value);
        }
    }

    public override string FormatErrorMessage(string name)
    {
        // Formatting can initialize the same conversion state as validation.
        lock (_conversionLock)
        {
            return base.FormatErrorMessage(name);
        }
    }
}
