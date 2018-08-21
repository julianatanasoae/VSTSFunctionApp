using System;

namespace MyVSTSFunction
{
    public static class Settings
    {
        internal static readonly string VstsResourceId = Environment.GetEnvironmentVariable("VstsResourceId");

        internal static readonly string VstsCollectionUrl = Environment.GetEnvironmentVariable("VstsCollectionUrl");
        internal static readonly string ClientId = Environment.GetEnvironmentVariable("VstsClientId");
        internal static readonly string Username = Environment.GetEnvironmentVariable("VstsUsername");
        internal static readonly string Password = Environment.GetEnvironmentVariable("VstsPassword");
        internal static readonly string Project = Environment.GetEnvironmentVariable("VstsProject");
        internal static readonly string ApiVersion = Environment.GetEnvironmentVariable("VstsApiVersion");
    }
}