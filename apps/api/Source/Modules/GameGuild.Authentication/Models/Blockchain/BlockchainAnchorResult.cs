namespace GameGuild.Authentication.Models.Blockchain;

/// <summary>
///     Result of anchoring data to blockchain.
/// </summary>
public abstract class BlockchainAnchorResult
{
    /// <summary>
    ///     Whether the anchor was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    ///     Blockchain transaction hash.
    /// </summary>
    public string? TransactionHash { get; set; }

    /// <summary>
    ///     Block number where transaction was included.
    /// </summary>
    public long? BlockNumber { get; set; }

    /// <summary>
    ///     Blockchain network used (Ethereum, Polygon, etc.).
    /// </summary>
    public string? Network { get; set; }

    /// <summary>
    ///     Hash of the data that was anchored.
    /// </summary>
    public string? DataHash { get; set; }

    /// <summary>
    ///     When the anchor was created.
    /// </summary>
    public DateTime? Timestamp { get; set; }

    /// <summary>
    ///     Gas cost of the transaction (if applicable).
    /// </summary>
    public string? GasCost { get; set; }

    /// <summary>
    ///     Error message if anchor failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     Additional anchor metadata.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
