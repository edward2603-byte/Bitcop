using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkControl.Api.Contracts;
using WorkControl.Api.Domain;
using WorkControl.Api.Infrastructure;
using Xunit;

namespace WorkControl.Api.Tests;

public sealed class ApiFlowTests : IAsyncLifetime
{
    private const string Password = "Test-password-123!";
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"workcontrol-api-{Guid.NewGuid():N}.db");
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private Guid _employeeId;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing")
                .UseSetting("ConnectionStrings:WorkControl", $"Data Source={_databasePath}")
                .UseSetting("Database:Provider", "Sqlite")
                .UseSetting("Database:AutoMigrate", "true")
                .UseSetting("WorkControl:Mode", "Local")
                .UseSetting("Security:DataProtectionKeyPath", Path.Combine(Path.GetTempPath(), $"workcontrol-keys-{Guid.NewGuid():N}"));
        });
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkControlDbContext>();
        var group = await db.WorkGroups.FirstAsync();
        var employee = new Employee { Id = Guid.NewGuid(), WorkGroupId = group.Id, DisplayName = "Интеграционный тест" };
        db.Employees.Add(employee); await db.SaveChangesAsync(); _employeeId = employee.Id;
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var admin = await users.FindByEmailAsync("admin@test.local");
        if (admin is null)
        {
            admin = new AppUser { Id = Guid.NewGuid(), UserName = "admin@test.local", Email = "admin@test.local", EmailConfirmed = true };
            Assert.True((await users.CreateAsync(admin, Password)).Succeeded);
            Assert.True((await users.AddToRoleAsync(admin, AppRoles.Admin)).Succeeded);
        }
    }

    [Fact]
    public async Task Protected_enrollment_approval_event_duplicate_dashboard_logout_form_one_chain()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client!.GetAsync("/api/dashboard")).StatusCode);
        await LoginAsync("admin@test.local", Password);

        var enrollmentResponse = await PostCsrfAsync("/api/admin/enrollments");
        enrollmentResponse.EnsureSuccessStatusCode();
        var enrollment = await enrollmentResponse.Content.ReadFromJsonAsync<CreateEnrollmentResponse>();
        Assert.NotNull(enrollment);

        var enrolledResponse = await _client.PostAsJsonAsync("/api/agent/v1/enroll", new EnrollAgentRequest(enrollment.Code, "INTEGRATION-PC"));
        enrolledResponse.EnsureSuccessStatusCode();
        var enrolled = await enrolledResponse.Content.ReadFromJsonAsync<EnrollAgentResponse>();
        Assert.NotNull(enrolled); Assert.Equal(DeviceStatuses.Pending, enrolled.Status);
        var pending = await _client.GetFromJsonAsync<List<PendingDeviceResponse>>("/api/admin/devices/pending");
        Assert.Contains(pending!, device => device.DeviceId == enrolled.DeviceId && device.ComputerName == "INTEGRATION-PC");
        var employees = await _client.GetFromJsonAsync<List<EmployeeOptionResponse>>("/api/admin/employees");
        Assert.Contains(employees!, employee => employee.Id == _employeeId);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PostAsJsonAsync("/api/agent/v1/enroll", new EnrollAgentRequest(enrollment.Code, "REPLAY-PC"))).StatusCode);
        var expired = await (await PostCsrfAsync("/api/admin/enrollments")).Content.ReadFromJsonAsync<CreateEnrollmentResponse>(); Assert.NotNull(expired);
        using (var expireScope = _factory!.Services.CreateScope())
        {
            var db = expireScope.ServiceProvider.GetRequiredService<WorkControlDbContext>();
            var record = await db.DeviceEnrollments.SingleAsync(x => x.UsedAtUtc == null); record.ExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(-1); await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PostAsJsonAsync("/api/agent/v1/enroll", new EnrollAgentRequest(expired!.Code, "EXPIRED-PC"))).StatusCode);

        var activityEvent = new AgentEventRequest(Guid.NewGuid(), "activity.status", DateTimeOffset.UtcNow,
            "{\"state\":\"idle\",\"idleSeconds\":65,\"longAbsence\":false}");
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAgentEventAsync(enrolled, activityEvent)).StatusCode);
        var approval = await PostCsrfAsync($"/api/admin/devices/{enrolled.DeviceId}/approve", new ApproveDeviceRequest(_employeeId));
        Assert.Equal(HttpStatusCode.NoContent, approval.StatusCode);

        var first = await SendAgentEventAsync(enrolled, activityEvent); first.EnsureSuccessStatusCode();
        Assert.False((await first.Content.ReadFromJsonAsync<AgentEventResponse>())!.Duplicate);
        var duplicate = await SendAgentEventAsync(enrolled, activityEvent); duplicate.EnsureSuccessStatusCode();
        Assert.True((await duplicate.Content.ReadFromJsonAsync<AgentEventResponse>())!.Duplicate);
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAgentEventAsync(enrolled, activityEvent with { EventId = Guid.NewGuid(), PayloadJson = "not-json" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAgentEventAsync(enrolled, activityEvent with { EventId = Guid.NewGuid(), OccurredAtUtc = DateTimeOffset.UtcNow.AddHours(1) })).StatusCode);
        var oversizedUtf8Payload = $"\"{new string('я', 33_000)}\"";
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAgentEventAsync(enrolled,
            activityEvent with { EventId = Guid.NewGuid(), PayloadJson = oversizedUtf8Payload })).StatusCode);

        using (var scope = _factory!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WorkControlDbContext>();
            Assert.Equal(1, await db.AgentEvents.CountAsync(x => x.EventId == activityEvent.EventId));
            Assert.DoesNotContain(await db.AuditEvents.Select(x => x.Details).ToListAsync(), x => x?.Contains(enrolled.Secret) == true);
        }
        var rows = await _client.GetFromJsonAsync<List<DashboardRow>>("/api/dashboard");
        Assert.Equal("INTEGRATION-PC", Assert.Single(rows!).Computer);

        using var rotateRequest = new HttpRequestMessage(HttpMethod.Post, "/api/agent/v1/credentials/rotate");
        rotateRequest.Headers.Authorization = new AuthenticationHeaderValue("WorkControl", $"{enrolled.KeyId}.{enrolled.Secret}");
        var rotateResponse = await _client.SendAsync(rotateRequest); rotateResponse.EnsureSuccessStatusCode();
        var rotated = await rotateResponse.Content.ReadFromJsonAsync<RotateCredentialResponse>(); Assert.NotNull(rotated);
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAgentEventAsync(enrolled, activityEvent with { EventId = Guid.NewGuid() })).StatusCode);
        var rotatedEvent = activityEvent with { EventId = Guid.NewGuid() };
        Assert.True((await SendAgentEventAsync(rotated!.KeyId, rotated.Secret, rotatedEvent)).IsSuccessStatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await PostCsrfAsync($"/api/admin/devices/{enrolled.DeviceId}/revoke")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAgentEventAsync(rotated.KeyId, rotated.Secret, activityEvent with { EventId = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PostAsJsonAsync("/api/telemetry/events", activityEvent)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PostAsJsonAsync("/api/devices/register", new { })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await PostCsrfAsync("/api/auth/logout")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/dashboard")).StatusCode);
    }

    [Fact]
    public async Task Invalid_login_is_audited_and_csrf_is_required()
    {
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _client!.PostAsJsonAsync("/api/agent/v1/enroll", new EnrollAgentRequest("", ""))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client!.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@test.local", Password))).StatusCode);
        var token = await GetCsrfAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login") { Content = JsonContent.Create(new LoginRequest("admin@test.local", "Wrong-password-123!")) };
        request.Headers.Add("X-CSRF-TOKEN", token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client!.SendAsync(request)).StatusCode);
        for (var attempt = 0; attempt < 4; attempt++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await PostCsrfAsync("/api/auth/login", new LoginRequest("admin@test.local", "Wrong-password-123!"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await PostCsrfAsync("/api/auth/login", new LoginRequest("admin@test.local", Password))).StatusCode);
        using var scope = _factory!.Services.CreateScope();
        Assert.Contains(await scope.ServiceProvider.GetRequiredService<WorkControlDbContext>().AuditEvents.ToListAsync(), x => x.Action == "user.login" && !x.Success);
    }

    [Fact]
    public async Task Group_manager_is_scoped_in_the_database_query()
    {
        Guid allowedGroupId; Guid deniedGroupId;
        using (var scope = _factory!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WorkControlDbContext>();
            var organization = await db.Organizations.FirstAsync();
            var allowed = new WorkGroup { Id = Guid.NewGuid(), OrganizationId = organization.Id, Name = "Разрешённая группа" };
            var denied = new WorkGroup { Id = Guid.NewGuid(), OrganizationId = organization.Id, Name = "Чужая группа" };
            var allowedEmployee = new Employee { Id = Guid.NewGuid(), WorkGroupId = allowed.Id, DisplayName = "Разрешённый" };
            var deniedEmployee = new Employee { Id = Guid.NewGuid(), WorkGroupId = denied.Id, DisplayName = "Скрытый" };
            db.AddRange(allowed, denied, allowedEmployee, deniedEmployee,
                new Device { Id = Guid.NewGuid(), EmployeeId = allowedEmployee.Id, AgentId = "allowed", ComputerName = "ALLOWED-PC", Status = DeviceStatuses.Active },
                new Device { Id = Guid.NewGuid(), EmployeeId = deniedEmployee.Id, AgentId = "denied", ComputerName = "DENIED-PC", Status = DeviceStatuses.Active });
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var manager = new AppUser { Id = Guid.NewGuid(), UserName = "manager@test.local", Email = "manager@test.local", EmailConfirmed = true };
            Assert.True((await users.CreateAsync(manager, Password)).Succeeded); Assert.True((await users.AddToRoleAsync(manager, AppRoles.GroupManager)).Succeeded);
            db.UserGroupAccess.Add(new UserGroupAccess { Id = Guid.NewGuid(), UserId = manager.Id, WorkGroupId = allowed.Id });
            await db.SaveChangesAsync(); allowedGroupId = allowed.Id; deniedGroupId = denied.Id;
        }
        await LoginAsync("manager@test.local", Password);
        var rows = await _client!.GetFromJsonAsync<List<DashboardRow>>("/api/dashboard");
        Assert.Contains(rows!, x => x.Computer == "ALLOWED-PC"); Assert.DoesNotContain(rows!, x => x.Computer == "DENIED-PC");
        Assert.Equal(HttpStatusCode.Forbidden, (await _client!.GetAsync($"/api/dashboard?groupId={deniedGroupId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/dashboard?groupId={allowedGroupId}")).StatusCode);
        using (var scope = _factory!.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var manager = await users.FindByEmailAsync("manager@test.local");
            manager!.IsActive = false; Assert.True((await users.UpdateAsync(manager)).Succeeded);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/dashboard")).StatusCode);
    }

    private async Task LoginAsync(string email, string password)
    {
        var response = await PostCsrfAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
    }
    private async Task<string> GetCsrfAsync() => (await (await _client!.GetAsync("/api/auth/csrf")).Content.ReadFromJsonAsync<CsrfResponse>())!.Token;
    private async Task<HttpResponseMessage> PostCsrfAsync(string path, object? body = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = body is null ? null : JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", await GetCsrfAsync());
        return await _client!.SendAsync(request);
    }
    private async Task<HttpResponseMessage> SendAgentEventAsync(EnrollAgentResponse credential, AgentEventRequest activityEvent)
        => await SendAgentEventAsync(credential.KeyId, credential.Secret, activityEvent);
    private async Task<HttpResponseMessage> SendAgentEventAsync(Guid keyId, string secret, AgentEventRequest activityEvent)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/agent/v1/events") { Content = JsonContent.Create(activityEvent) };
        request.Headers.Authorization = new AuthenticationHeaderValue("WorkControl", $"{keyId}.{secret}");
        return await _client!.SendAsync(request);
    }
    private sealed record CsrfResponse(string Token);

    public Task DisposeAsync()
    {
        _client?.Dispose(); _factory?.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath)) File.Delete(_databasePath);
        return Task.CompletedTask;
    }
}
