namespace GameGuild;

/// <summary> Configuration options for the Infrastructure Layer services. </summary>
public class InfrastructureLayerOptions {
  /// <summary> Enables database services. </summary>
  public bool EnableDatabase { get; set; } = true;

  /// <summary> Database configuration options. </summary>
  public DatabaseOptions? Database { get; set; }

  /// <summary> Enables message queue services. </summary>
  public bool EnableMessageQueue { get; set; } = false;

  /// <summary> Message queue configuration options. </summary>
  public MessageQueueOptions? MessageQueue { get; set; }

  /// <summary> Enables external API integration services. </summary>
  public bool EnableExternalApis { get; set; } = false;

  /// <summary> External API configuration options. </summary>
  public ExternalApiOptions? ExternalApis { get; set; }

  /// <summary> Enables file storage services. </summary>
  public bool EnableFileStorage { get; set; } = false;

  /// <summary> File storage configuration options. </summary>
  public FileStorageOptions? FileStorage { get; set; }

  /// <summary> Enables monitoring and logging services. </summary>
  public bool EnableMonitoring { get; set; } = true;

  /// <summary> Monitoring configuration options. </summary>
  public MonitoringOptions? Monitoring { get; set; }

  /// <summary> Validates the infrastructure layer options. </summary>
  public void Validate() {
    Database?.Validate();
    MessageQueue?.Validate();
    ExternalApis?.Validate();
    FileStorage?.Validate();
    Monitoring?.Validate();
  }
}
