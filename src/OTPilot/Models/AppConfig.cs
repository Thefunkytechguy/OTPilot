namespace OTPilot.Models;

public class AppConfig
{
    /// <summary>
    /// "Local", "OneDrive", or "Custom"
    /// </summary>
    public string StorageType { get; set; } = "Local";

    /// <summary>
    /// Used when StorageType is "Custom" — full path to the folder.
    /// </summary>
    public string? CustomPath { get; set; }

    /// <summary>
    /// False until the user completes the setup wizard.
    /// </summary>
    public bool SetupComplete { get; set; } = false;
}
