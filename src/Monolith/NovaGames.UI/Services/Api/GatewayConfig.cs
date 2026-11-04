namespace NovaGames.UI.Services.Api;

public class GatewayConfig
{
    public const string DefaultBaseUrl = "http://localhost:5000";

    public string BaseUrl { get; set; } = DefaultBaseUrl;

    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }

    public void ClearTokens()
    {
        AccessToken = null;
        RefreshToken = null;
    }
}
