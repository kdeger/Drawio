using System;
using System.Threading.Tasks;

public abstract class Authenticator
{
    public abstract event Action<bool> OnLoginSuccess;
    public abstract event Action<bool> OnLogout;

    public IConnection CurrentProvider;

    protected readonly AccountSession _session;

    public string LastError { get; protected set; }

    public abstract bool IsInitialized { get; }
    public abstract string Email { get; }

    public bool IsLinked => _session.IsLinked;
    public string UserId => _session.UserId;

    protected Authenticator(AccountSession session)
    {
        _session = session;
    }

    public abstract Task InitializeAsync();
    public abstract Task AuthenticateWithEmailAsync(string email, string password, Action onSuccess, Action onFail);
    public abstract Task SignInWithEmailAsync(string email, string password, Action onSuccess, Action onFail);
    public abstract void LogoutAllSessionsAsync();
}
