using System;
using System.IO;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

public class FirebaseAuthenticator : Authenticator
{
    public override event Action<bool> OnLoginSuccess;
    public override event Action<bool> OnLogout;

    private readonly DataSynchronizer _dataSynchronizer;
    private FirebaseAuth _auth;
    private bool _isInitialized;
    private bool _isAbandoned;

    public override bool IsInitialized => _isInitialized;
    public override string Email => CurrentUser != null ? CurrentUser.Email : null;

    private FirebaseUser CurrentUser => _auth != null ? _auth.CurrentUser : null;

    public FirebaseAuthenticator(DataSynchronizer dataSynchronizer, AccountSession session) : base(session)
    {
        _dataSynchronizer = dataSynchronizer;
    }

    private void SaveSession()
    {
        var user = CurrentUser;
        _session.Save(user != null ? user.UserId : null, user != null && !user.IsAnonymous);
    }

    public override async Task InitializeAsync()
    {
        try
        {
            var status = await OnMainThread(FirebaseApp.CheckAndFixDependenciesAsync());
            if (status != DependencyStatus.Available)
            {
                LastError = "Firebase dependencies " + status;
                return;
            }

            CreateEditorApp();
            _auth = FirebaseAuth.DefaultInstance;

            if (_auth.CurrentUser == null)
            {
                Debug.Log("Auth | Creating device session..");
                await OnMainThread(_auth.SignInAnonymouslyAsync());
            }

            if (_isAbandoned)
                return;

            SaveSession();
            _isInitialized = true;
            Debug.Log("Auth | Firebase ready, user " + UserId);
        }
        catch (Exception e)
        {
            LastError = Describe(e);
            Debug.LogWarning("Auth | Firebase initialization failed : " + LastError);
        }
    }

    public void Abandon()
    {
        _isAbandoned = true;
    }

    public override async Task AuthenticateWithEmailAsync(string email, string password, Action onSuccess, Action onFail)
    {
        Debug.Log("Auth | Linking email account..");
        LastError = null;
        _dataSynchronizer.CreateLocalBackUp();

        try
        {
            var isNewUser = await TryLinkEmailAccount(email, password);
            await LoginSuccess(isNewUser);
            onSuccess?.Invoke();
        }
        catch (Exception e)
        {
            LastError = Describe(e);
            Debug.LogWarning("Auth | Email link failed : " + LastError);
            onFail?.Invoke();
        }
    }

    public override async Task SignInWithEmailAsync(string email, string password, Action onSuccess, Action onFail)
    {
        Debug.Log("Auth | Signing in with email..");
        LastError = null;
        _dataSynchronizer.CreateLocalBackUp();

        try
        {
            await OnMainThread(_auth.SignInWithEmailAndPasswordAsync(email, password));
            await LoginSuccess(false);
            onSuccess?.Invoke();
        }
        catch (Exception e)
        {
            LastError = Describe(e);
            Debug.LogWarning("Auth | Email sign in failed : " + LastError);
            onFail?.Invoke();
        }
    }

    private async Task<bool> TryLinkEmailAccount(string email, string password)
    {
        var credential = EmailAuthProvider.GetCredential(email, password);

        if (_auth.CurrentUser == null)
            await OnMainThread(_auth.SignInAnonymouslyAsync());

        try
        {
            await OnMainThread(_auth.CurrentUser.LinkWithCredentialAsync(credential));
            return false;
        }
        catch (FirebaseException e)
        {
            var error = (AuthError)e.ErrorCode;
            if (error != AuthError.EmailAlreadyInUse && error != AuthError.CredentialAlreadyInUse)
                throw;

            Debug.Log("Auth | Account already in use, signing in to it instead..");
            await OnMainThread(_auth.SignInWithEmailAndPasswordAsync(email, password));
            return false;
        }
    }

    private async Task LoginSuccess(bool isNewUser)
    {
        SaveSession();
        await _dataSynchronizer.SynchronizeDataByUserStatus(isNewUser);
        OnLoginSuccess?.Invoke(isNewUser);
    }

    public override async void LogoutAllSessionsAsync()
    {
        try
        {
            Debug.Log("Auth | Session logout");
            await _dataSynchronizer.UpdateRemoteUserData();
            _auth.SignOut();
            _dataSynchronizer.LoadLocalBackUpData();
            await OnMainThread(_auth.SignInAnonymouslyAsync());
            SaveSession();
            OnLogout?.Invoke(false);
        }
        catch (Exception e)
        {
            OnLogout?.Invoke(false);
            Debug.LogException(new Exception("Auth | Logout exception : " + e));
        }
    }

    private static void CreateEditorApp()
    {
#if UNITY_EDITOR
        if (File.Exists(Application.streamingAssetsPath + "/google-services-desktop.json"))
            return;

        try
        {
            var json = File.ReadAllText(Application.dataPath + "/google-services.json");
            FirebaseApp.Create(AppOptions.LoadFromJsonConfig(json));
            Debug.Log("Auth | Firebase app created from google-services.json");
        }
        catch (Exception e)
        {
            Debug.LogException(new Exception("Auth | Editor app creation failed : " + e));
        }
#endif
    }

    private static Task<T> OnMainThread<T>(Task<T> task)
    {
        var completion = new TaskCompletionSource<T>();
        task.ContinueWithOnMainThread(finished =>
        {
            if (finished.IsFaulted)
                completion.SetException(Unwrap(finished.Exception));
            else if (finished.IsCanceled)
                completion.SetCanceled();
            else
                completion.SetResult(finished.Result);
        });
        return completion.Task;
    }

    private static Task OnMainThread(Task task)
    {
        var completion = new TaskCompletionSource<bool>();
        task.ContinueWithOnMainThread(finished =>
        {
            if (finished.IsFaulted)
                completion.SetException(Unwrap(finished.Exception));
            else if (finished.IsCanceled)
                completion.SetCanceled();
            else
                completion.SetResult(true);
        });
        return completion.Task;
    }

    private static Exception Unwrap(Exception exception)
    {
        var aggregate = exception as AggregateException;
        if (aggregate == null)
            return exception;

        var flattened = aggregate.Flatten();
        foreach (var inner in flattened.InnerExceptions)
        {
            if (inner is FirebaseException)
                return inner;
        }

        return flattened.InnerException ?? flattened;
    }

    private static string Describe(Exception exception)
    {
        var firebaseException = exception as FirebaseException;
        if (firebaseException == null)
            return exception.Message;

        switch ((AuthError)firebaseException.ErrorCode)
        {
            case AuthError.WrongPassword: return "Wrong password";
            case AuthError.UserNotFound: return "No account with this email";
            case AuthError.InvalidEmail: return "Invalid email";
            case AuthError.WeakPassword: return "Password too weak";
            case AuthError.EmailAlreadyInUse: return "Email already in use";
            case AuthError.NetworkRequestFailed: return "No connection";
            default: return $"{firebaseException.Message} ({(AuthError)firebaseException.ErrorCode})";
        }
    }
}
