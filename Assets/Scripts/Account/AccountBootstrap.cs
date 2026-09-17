using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AccountBootstrap : SingletonMB<AccountBootstrap>
{
    [SerializeField] private AccountSettings _settings;

    private static Authenticator _authenticator;
    private static EmailConnection _connection;
    private static Task _initializeTask;
    private static bool _isReady;
    private static string _bootStatus = "Loading profile";

    public static event Action OnStateChanged;

    public static Authenticator Authenticator => _authenticator;
    public static EmailConnection Connection => _connection;
    public static bool IsReady => _isReady;
    public static string BootStatus => _bootStatus;

    public static Task InitializeServicesAsync(AccountSettings settings)
    {
        if (_initializeTask == null)
            _initializeTask = RunInitializeAsync(settings);

        return _initializeTask;
    }

    private void Start()
    {
        GameManager.Instance.onGamePhaseChanged += OnGamePhaseChanged;

        if (_settings == null)
        {
            Debug.LogError("Boot | AccountSettings is not assigned on AccountBootstrap");
            return;
        }

        InitializeServicesAsync(_settings);
    }

    private static async Task RunInitializeAsync(AccountSettings settings)
    {
        try
        {
            SetStatus("Loading profile");
            PlayerDataProvider.Initialize();
            PlayerSettingsProvider.Initialize();

            var session = new AccountSession();
            var server = new DecoyServer(settings, session);
            var synchronizer = new PlayerDataSynchronizer(server, settings, session);

            var firebase = settings.UseMockAuthenticator ? null : new FirebaseAuthenticator(synchronizer, session);
            _authenticator = firebase ?? (Authenticator)new MockAuthenticator(settings, synchronizer, session);

            SetStatus("Signing in");
            var timeout = Task.Delay(TimeSpan.FromSeconds(settings.FirebaseInitTimeoutSeconds));
            await Task.WhenAny(_authenticator.InitializeAsync(), timeout);

            if (!_authenticator.IsInitialized)
            {
                var reason = string.IsNullOrEmpty(_authenticator.LastError) ? "no answer in time" : _authenticator.LastError;
                Debug.LogWarning("Boot | Firebase unavailable (" + reason + "), using mock");

                if (firebase != null)
                    firebase.Abandon();

                _authenticator = new MockAuthenticator(settings, synchronizer, session);
                await _authenticator.InitializeAsync();
            }

            _connection = new EmailConnection(_authenticator);
            _authenticator.CurrentProvider = _connection;
            _authenticator.OnLoginSuccess += OnAuthChanged;
            _authenticator.OnLogout += OnAuthChanged;
            _connection.OnLoginFail += RaiseStateChanged;

            SetStatus("Checking server");
            await synchronizer.InitializeAsync();

            _isReady = true;
            SetStatus("Ready");
        }
        catch (Exception e)
        {
            SetStatus("Offline");
            Debug.LogException(new Exception("Boot | Account initialization failed : " + e));
        }
    }

    private static void SaveLocalData()
    {
        PlayerDataProvider.Save();
        PlayerSettingsProvider.Save();
    }

    private static void OnAuthChanged(bool isNewUser)
    {
        SaveLocalData();
        RaiseStateChanged();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnGamePhaseChanged(GamePhase gamePhase)
    {
        if (gamePhase == GamePhase.END)
            SaveLocalData();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
            SaveLocalData();
    }

    protected override void OnDestroySpecific()
    {
        SaveLocalData();

        var gameManager = GameManager.Instance;
        if (gameManager != null)
            gameManager.onGamePhaseChanged -= OnGamePhaseChanged;
    }

    private static void SetStatus(string status)
    {
        _bootStatus = status;
        Debug.Log("Boot | " + status);
        RaiseStateChanged();
    }

    private static void RaiseStateChanged()
    {
        OnStateChanged?.Invoke();
    }
}
