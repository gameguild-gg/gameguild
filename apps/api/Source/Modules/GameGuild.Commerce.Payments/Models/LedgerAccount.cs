using System.ComponentModel;

namespace GameGuild.Commerce.Payments;

/// <summary>
/// Strongly-typed ledger account identifiers.
/// Replaces magic strings for account names to prevent typos and ensure consistency.
/// </summary>
public enum LedgerAccount
{
    // ===== ASSET ACCOUNTS (1xxx) =====
    
    /// <summary>Cash and cash equivalents</summary>
    [Description("Cash")]
    Cash = 1000,
    
    /// <summary>Accounts receivable from customers</summary>
    [Description("Accounts Receivable")]
    AccountsReceivable = 1100,
    
    /// <summary>Prepaid expenses</summary>
    [Description("Prepaid Expenses")]
    PrepaidExpenses = 1200,
    
    /// <summary>User wallet balances (liability on our books but asset from user perspective)</summary>
    [Description("User Wallet Deposits")]
    UserWalletDeposits = 1300,
    
    /// <summary>Merchant account pending settlement</summary>
    [Description("Payment Gateway Pending")]
    PaymentGatewayPending = 1400,
    
    // ===== LIABILITY ACCOUNTS (2xxx) =====
    
    /// <summary>Accounts payable to vendors</summary>
    [Description("Accounts Payable")]
    AccountsPayable = 2000,
    
    /// <summary>Deferred revenue (unearned income)</summary>
    [Description("Deferred Revenue")]
    DeferredRevenue = 2100,
    
    /// <summary>User wallet liability (owed to users)</summary>
    [Description("User Wallet Liability")]
    UserWalletLiability = 2200,
    
    /// <summary>Refunds payable</summary>
    [Description("Refunds Payable")]
    RefundsPayable = 2300,
    
    /// <summary>Taxes collected (VAT, Sales Tax)</summary>
    [Description("Taxes Payable")]
    TaxesPayable = 2400,
    
    /// <summary>Affiliate/referral commissions payable</summary>
    [Description("Commissions Payable")]
    CommissionsPayable = 2500,
    
    // ===== REVENUE ACCOUNTS (4xxx) =====
    
    /// <summary>Product sales revenue</summary>
    [Description("Product Revenue")]
    ProductRevenue = 4000,
    
    /// <summary>Subscription revenue</summary>
    [Description("Subscription Revenue")]
    SubscriptionRevenue = 4100,
    
    /// <summary>Course/content revenue</summary>
    [Description("Course Revenue")]
    CourseRevenue = 4200,
    
    /// <summary>Marketplace transaction fees</summary>
    [Description("Transaction Fee Revenue")]
    TransactionFeeRevenue = 4300,
    
    /// <summary>Platform fees</summary>
    [Description("Platform Fee Revenue")]
    PlatformFeeRevenue = 4400,
    
    // ===== EXPENSE ACCOUNTS (5xxx) =====
    
    /// <summary>Payment processing fees (Stripe, PayPal)</summary>
    [Description("Payment Processing Fees")]
    PaymentProcessingFees = 5000,
    
    /// <summary>Affiliate/referral commission expense</summary>
    [Description("Commission Expense")]
    CommissionExpense = 5100,
    
    /// <summary>Refunds and chargebacks</summary>
    [Description("Refunds and Chargebacks")]
    RefundsAndChargebacks = 5200,
    
    /// <summary>Bad debt (uncollectable)</summary>
    [Description("Bad Debt Expense")]
    BadDebtExpense = 5300,
    
    // ===== CONTRA ACCOUNTS (6xxx) =====
    
    /// <summary>Discounts given</summary>
    [Description("Sales Discounts")]
    SalesDiscounts = 6000,
    
    /// <summary>Returns and allowances</summary>
    [Description("Returns and Allowances")]
    ReturnsAndAllowances = 6100
}

/// <summary>
/// Extension methods for LedgerAccount enum
/// </summary>
public static class LedgerAccountExtensions
{
    /// <summary>
    /// Gets the account code as a string (for legacy compatibility)
    /// </summary>
    public static string ToAccountCode(this LedgerAccount account)
        => ((int)account).ToString("D4");
    
    /// <summary>
    /// Gets the description attribute value
    /// </summary>
    public static string GetDescription(this LedgerAccount account)
    {
        var field = account.GetType().GetField(account.ToString());
        var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
            .FirstOrDefault() as DescriptionAttribute;
        return attribute?.Description ?? account.ToString();
    }
    
    /// <summary>
    /// Determines if this is an asset account
    /// </summary>
    public static bool IsAsset(this LedgerAccount account)
        => (int)account >= 1000 && (int)account < 2000;
    
    /// <summary>
    /// Determines if this is a liability account
    /// </summary>
    public static bool IsLiability(this LedgerAccount account)
        => (int)account >= 2000 && (int)account < 4000;
    
    /// <summary>
    /// Determines if this is a revenue account
    /// </summary>
    public static bool IsRevenue(this LedgerAccount account)
        => (int)account >= 4000 && (int)account < 5000;
    
    /// <summary>
    /// Determines if this is an expense account
    /// </summary>
    public static bool IsExpense(this LedgerAccount account)
        => (int)account >= 5000 && (int)account < 6000;
    
    /// <summary>
    /// Determines if this is a contra account
    /// </summary>
    public static bool IsContra(this LedgerAccount account)
        => (int)account >= 6000 && (int)account < 7000;
}
