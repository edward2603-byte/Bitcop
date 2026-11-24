using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;

namespace WorkControl.Agent;

public partial class MainWindow : Window
{
    private readonly HttpClient _httpClient = new();
    private readonly LocalEventStore _store = new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WorkControl",
        "agent-buffer.db"));
    private readonly DispatcherTimer _heartbeatTimer;
    private bool _isSending;

    public MainWindow()
    {
        InitializeComponent();
        _store.Initialize();
        var settings = AgentSettings.Load();
        ApiUrlBox.Text = settings.ApiUrl;
        AgentIdBox.Text = settings.AgentId;

        _heartbeatTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _heartbeatTimer.Tick += async (_, _) => await SendControlEventAsync(isManual: false);
        Loaded += async (_, _) =>
        {
            await SendControlEventAsync(isManual: false);
            _heartbeatTimer.Start();
        };
        Closed += (_, _) => _heartbeatTimer.Stop();
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e) => await SendControlEventAsync(isManual: true);

    private async Task SendControlEventAsync(bool isManual)
    {
        if (_isSending) return;
        _isSending = true;
        try
        {
            var agentId = AgentIdBox.Text.Trim();
            var apiUrl = ApiUrlBox.Text.Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(agentId) || string.IsNullOrWhiteSpace(apiUrl)) throw new InvalidOperationException("Укажите адрес API и идентификатор агента.");
            new AgentSettings(apiUrl, agentId).Save();

            var activity = UserActivityProbe.GetCurrent();
            var activityEvent = new AgentControlEvent(
                Guid.NewGuid(),
                agentId,
                "activity.status",
                DateTimeOffset.UtcNow,
                JsonSerializer.Serialize(new { state = activity.State, idleSeconds = activity.IdleSeconds }));
            _store.Enqueue(activityEvent);
            var registration = new
            {
                agentId,
                computerName = Environment.MachineName,
                employeeId = (Guid?)null,
                employeeName = Environment.UserName
            };
            var registered = await _httpClient.PostAsJsonAsync($"{apiUrl}/api/devices/register", registration);
            registered.EnsureSuccessStatusCode();

            foreach (var pending in _store.GetPending())
            {
                var response = await _httpClient.PostAsJsonAsync($"{apiUrl}/api/telemetry/events", pending);
                response.EnsureSuccessStatusCode();
                _store.Remove(pending.EventId);
            }
            var activityLabel = activity.State switch { "active" => "активен", "idle" => "неактивен", _ => "не определён" };
            StatusText.Text = $"На связи: {activityLabel}. Последнее сообщение: {DateTime.Now:HH:mm:ss}.";
        }
        catch (Exception error)
        {
            StatusText.Text = $"Нет связи. Событие сохранено локально: {error.Message}";
            if (isManual) MessageBox.Show(StatusText.Text, "WorkControl", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _isSending = false;
        }
    }
}

public sealed record AgentControlEvent(Guid EventId, string AgentId, string EventType, DateTimeOffset OccurredAtUtc, string PayloadJson);
