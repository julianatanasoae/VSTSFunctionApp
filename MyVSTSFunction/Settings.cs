using System;

namespace MyVSTSFunction
{
    public static class Settings
    {
        internal static readonly string VstsResourceId = Environment.GetEnvironmentVariable("VstsResourceId");
        internal static readonly string VstsCollectionUrl = Environment.GetEnvironmentVariable("VstsCollectionUrl");
        internal static readonly string VstsAlmSearchUrl = Environment.GetEnvironmentVariable("VstsAlmSearchUrl");
        internal static readonly string VstsClientId = Environment.GetEnvironmentVariable("VstsClientId");
        internal static readonly string VstsUsername = Environment.GetEnvironmentVariable("VstsUsername");
        internal static readonly string VstsPassword = Environment.GetEnvironmentVariable("VstsPassword");
        internal static readonly string VstsProject = Environment.GetEnvironmentVariable("VstsProject");
        internal static readonly string VstsApiVersion = Environment.GetEnvironmentVariable("VstsApiVersion");
        internal static readonly string VstsFullUsername = Environment.GetEnvironmentVariable("VstsFullUsername");
    }
}