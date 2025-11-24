namespace GameGuild.Abstractions;

/// <summary>
///     Interface for entities that support checksum validation
/// </summary>
public interface IChecksum
{
    /// <summary>
    ///     Gets or sets the checksum value used for data integrity verification
    /// </summary>
    /// <remarks>
    ///     The checksum can be used to verify that the entity data hasn't been tampered with
    ///     or corrupted. Common implementations include MD5, SHA256, or other hash algorithms.
    /// </remarks>
    string Checksum { get; set; }
}
