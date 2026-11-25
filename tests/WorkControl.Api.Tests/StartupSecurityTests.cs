using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace WorkControl.Api.Tests;

public sealed class StartupSecurityTests
{
    [Fact]
    public void Pilot_refuses_to_start_without_explicit_https_dns_and_key_ring()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("WorkControl:Mode", "Pilot")
                .UseSetting("ASPNETCORE_URLS", "http://0.0.0.0:5080")
                .UseSetting("Security:PilotHost", "")
                .UseSetting("Security:DataProtectionKeyPath", ""));
        var error = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("PILOT", error.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Pilot_refuses_https_without_an_explicit_certificate_secret()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("WorkControl:Mode", "Pilot")
                .UseSetting("ASPNETCORE_URLS", "https://workcontrol.validation.test:5443")
                .UseSetting("Security:PilotHost", "workcontrol.validation.test")
                .UseSetting("Security:DataProtectionKeyPath", Path.Combine(Path.GetTempPath(), "workcontrol-pilot-keys")));
        var error = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("PILOT", error.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
