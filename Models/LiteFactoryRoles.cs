namespace LiteFactoryWeb.Models;

public static class LiteFactoryRoles
{
    public const string User = "USER";
    public const string Developer = "DEVELOPER";
    public const string Admin = "ADMIN";

    public static bool IsKnown(string role)
    {
        return role is User or Developer or Admin;
    }
}
