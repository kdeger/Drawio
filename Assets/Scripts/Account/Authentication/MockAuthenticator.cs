using System;
using System.Threading.Tasks;
using UnityEngine;

public class MockAuthenticator : Authenticator
{
    public override event Action<bool> OnLoginSuccess;
    public override event Action<bool> OnLogout;

    private readonly AccountSettings _settings;
    private readonly DataSynchronizer _dataSynchronizer;
    private bool _isInitialized;
    private string _email;

    public override bool IsInitialized => _isInitialized;
    public override string Email => _email;

    public MockAuthenticator(AccountSettings settings, DataSynchronizer dataSynchronizer, AccountSession session) : base(session)
    {
        _settings = settings;
        _dataSynchronizer = dataSynchronizer;
    }

    public override async Task InitializeAsync()
    {
        await Delay();
        _session.Save(CreateAnonymousId(), false);
        _isInitialized = true;
        Debug.Log("Auth | Mock ready, user " + UserId);
    }

    public override async Task AuthenticateWithEmailAsync(string email, string password, Action onSuccess, Action onFail)
    {
        Debug.Log("Auth | Mock linking email account..");
        LastError = null;
        _dataSynchronizer.CreateLocalBackUp();

        try
        {
            await Delay();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                throw new Exception("Email and password required");

            var userId = UserId;

            if (email == _settings.MockExistingEmail)
            {
                if (password != _settings.MockExistingPassword)
                    throw new Exception("Wrong password");

                Debug.Log("Auth | Account already in use, signing in to it instead..");
                userId = _settings.MockExistingUserId;
            }

            _email = email;
            _session.Save(userId, true);
            await LoginSuccess(false);
            onSuccess?.Invoke();
        }
        catch (Exception e)
        {
            LastError = e.Message;
            Debug.LogWarning("Auth | Mock link failed : " + LastError);
            onFail?.Invoke();
        }
    }

    public override async Task SignInWithEmailAsync(string email, string password, Action onSuccess, Action onFail)
    {
        Debug.Log("Auth | Mock signing in with email..");
        LastError = null;
        _dataSynchronizer.CreateLocalBackUp();

        try
        {
            await Delay();

            if (email != _settings.MockExistingEmail)
                throw new Exception("No account with this email");

            if (password != _settings.MockExistingPassword)
                throw new Exception("Wrong password");

            _email = email;
            _session.Save(_settings.MockExistingUserId, true);
            await LoginSuccess(false);
            onSuccess?.Invoke();
        }
        catch (Exception e)
        {
            LastError = e.Message;
            Debug.LogWarning("Auth | Mock sign in failed : " + LastError);
            onFail?.Invoke();
        }
    }

    private async Task LoginSuccess(bool isNewUser)
    {
        await _dataSynchronizer.SynchronizeDataByUserStatus(isNewUser);
        OnLoginSuccess?.Invoke(isNewUser);
    }

    public override async void LogoutAllSessionsAsync()
    {
        try
        {
            Debug.Log("Auth | Mock session logout");
            await Delay();
            await _dataSynchronizer.UpdateRemoteUserData();
            _dataSynchronizer.LoadLocalBackUpData();
            _email = null;
            _session.Save(CreateAnonymousId(), false);
            OnLogout?.Invoke(false);
        }
        catch (Exception e)
        {
            OnLogout?.Invoke(false);
            Debug.LogException(new Exception("Auth | Mock logout exception : " + e));
        }
    }

    private Task Delay()
    {
        return Task.Delay(TimeSpan.FromSeconds(_settings.DecoyDelaySeconds));
    }

    private static string CreateAnonymousId()
    {
        return "anonym-" + Guid.NewGuid().ToString().Substring(0, 8);
    }
}
