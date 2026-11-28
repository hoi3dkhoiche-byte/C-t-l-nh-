namespace ClientAPP;

/// <summary>
/// Lớp tương thích cho Start-LanClient.ps1 / LanSmoke, ủy quyền trực tiếp sang ServerConfig.
/// </summary>
public static class ServerConfiguration
{
    public static Uri ServerUri => ServerConfig.BaseUri;
    public static Uri ApiUri => ServerConfig.ApiBaseUri;
    public static Uri AuthUri => ServerConfig.AuthApiBaseUri;
}
