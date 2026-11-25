using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;

namespace WorkControl.Agent;

public partial class MainWindow : Window
{
    private readonly HttpClient _httpClient = new();
    private readonly LocalEventStore _store = new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WorkControl", "agent-buffer.db"));
    private readonly AgentCredentialStore _credentialStore = new();
    private readonly ActivityOptions _activityOptions;
    private readonly AgentEventDispatcher _dispatcher;
    private readonly DispatcherTimer _heartbeatTimer;
    private bool _isSending;

    public MainWindow()
    {
        InitializeComponent();
        _store.Initialize();
        _activityOptions = ActivityOptions.Load();
        _dispatcher = new AgentEventDispatcher(_httpClient, _store);
        var settings = AgentSettings.Load();
        ApiUrlBox.Text = settings.ApiUrl;
        AgentIdBox.Text = settings.AgentId;

        _heartbeatTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _heartbeatTimer.Tick += async (_, _) => await SendControlEventAsync(isManual: false);
        Loaded += async (_, _) =>
        {
            if (HasCredential()) await SendControlEventAsync(isManual: false);
            else StatusText.Text = "Введите одноразовый код, созданный администратором. Секрет агента будет защищён Windows DPAPI.";
            _heartbeatTimer.Start();
        };
        Closed += (_, _) => _heartbeatTimer.Stop();
    }

    private bool HasCredential()
    {
        var settings = AgentSettings.Load();
        return settings.KeyId.HasValue && !string.IsNullOrWhiteSpace(_credentialStore.Load());
    }

    private async void EnrollButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isSending) return;
        _isSending = true;
        try
        {
            var apiUrl = ApiUrlBox.Text.Trim().TrimEnd('/');
            var code = EnrollmentCodeBox.Text.Trim();
            AgentSettings.EnsureApiUrl(apiUrl);
            if (string.IsNullOrWhiteSpace(code)) throw new InvalidOperationException("Введите одноразовый код подключения.");
            var enrolled = await _dispatcher.EnrollAsync(apiUrl, code);
            new AgentSettings(apiUrl, enrolled.AgentId, enrolled.DeviceId, enrolled.KeyId).Save();
            _credentialStore.Save(enrolled.Secret);
            AgentIdBox.Text = enrolled.AgentId;
            EnrollmentCodeBox.Clear();
            StatusText.Text = "Компьютер подключён и ожидает одобрения администратора. До одобрения телеметрия не принимается.";
        }
        catch (Exception error)
        {
            StatusText.Text = $"Подключение не выполнено: {error.Message}";
            MessageBox.Show(StatusText.Text, "WorkControl", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally { _isSending = false; }
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e) => await SendControlEventAsync(isManual: true);

    private async Task SendControlEventAsync(bool isManual)
    {
        if (_isSending) return;
        _isSending = true;
        try
        {
            var settings = AgentSettings.Load();
            var secret = _credentialStore.Load();
            if (!settings.KeyId.HasValue || string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("Сначала подключите компьютер одноразовым кодом.");
            AgentSettings.EnsureApiUrl(settings.ApiUrl);

            var activity = UserActivityProbe.GetCurrent(_activityOptions);
            var activityEvent = new AgentControlEvent(
                Guid.NewGuid(), settings.AgentId, "activity.status", DateTimeOffset.UtcNow,
                JsonSerializer.Serialize(new { state = activity.State, idleSeconds = activity.IdleSeconds, longAbsence = activity.IsLongAbsence }));
            await _dispatcher.QueueAndSendAsync(settings.ApiUrl, new AgentCredential(settings.KeyId.Value, secret), activityEvent);

            var activityLabel = activity.State switch
            {
                "active" => "активен",
                "idle" when activity.IsLongAbsence => "длительное отсутствие",
                "idle" => "неактивен",
                _ => "не определён"
            };
            StatusText.Text = $"API доступен. Активность: {activityLabel}; простой {activity.IdleSeconds} с. Последнее сообщение: {DateTime.Now:HH:mm:ss}.";
        }
        catch (Exception error)
        {
            StatusText.Text = $"Нет связи или устройство ещё не одобрено. Событие сохранено локально: {error.Message}";
            if (isManual) MessageBox.Show(StatusText.Text, "WorkControl", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally { _isSending = false; }
    }
}
