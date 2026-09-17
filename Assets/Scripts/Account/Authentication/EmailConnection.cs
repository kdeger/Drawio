using System;
using UnityEngine;

public class EmailConnection : IConnection
{
    public event Action OnLoginFail;

    private readonly Authenticator _authenticator;
    private string _email;
    private string _password;

    public EmailConnection(Authenticator authenticator)
    {
        _authenticator = authenticator;
    }

    public void SetCredentials(string email, string password)
    {
        _email = email;
        _password = password;
    }

    public async void Login()
    {
        try
        {
            await _authenticator.AuthenticateWithEmailAsync(_email, _password, LoginSuccess, LoginFail);
        }
        catch (Exception e)
        {
            Debug.LogException(new Exception("Auth | Email link exception : " + e));
        }
    }

    public async void SignIn()
    {
        try
        {
            await _authenticator.SignInWithEmailAsync(_email, _password, LoginSuccess, LoginFail);
        }
        catch (Exception e)
        {
            Debug.LogException(new Exception("Auth | Email sign in exception : " + e));
        }
    }

    public void LoginFail()
    {
        OnLoginFail?.Invoke();
    }

    public void LoginSuccess()
    {
        Debug.Log("Auth | Email login success. " + _authenticator.Email);
    }

    public void LogOut()
    {
        _authenticator.LogoutAllSessionsAsync();
    }
}
