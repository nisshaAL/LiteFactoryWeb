namespace LiteFactoryWeb.Models;

public sealed class LiteFactoryAuthSession
{
    public LiteFactoryAccount User { get; set; } = new();

    public string AccessToken { get; set; } = "";

    public DateTimeOffset ExpiresAtUtc { get; set; }
}
