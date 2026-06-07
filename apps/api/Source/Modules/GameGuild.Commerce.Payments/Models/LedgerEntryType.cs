namespace GameGuild.Commerce.Payments;

/// <summary>Ledger entry types</summary>
public enum LedgerEntryType
{
    /// <summary>Revenue</summary>
    Revenue = 0,

    /// <summary>Expense</summary>
    Expense = 1,

    /// <summary>Refund</summary>
    Refund = 2,

    /// <summary>Fee</summary>
    Fee = 3,

    /// <summary>Transfer</summary>
    Transfer = 4,

    /// <summary>Adjustment</summary>
    Adjustment = 5,

    /// <summary>Credit</summary>
    Credit = 6,

    /// <summary>Debit</summary>
    Debit = 7
}
