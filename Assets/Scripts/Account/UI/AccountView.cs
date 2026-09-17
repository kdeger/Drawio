using UnityEngine;
using UnityEngine.UI;

public class AccountView : View<AccountView>
{
    [SerializeField] private InputField _emailField;
    [SerializeField] private InputField _passwordField;
    [SerializeField] private Text _statusText;
    [SerializeField] private Text _playerIdText;
    [SerializeField] private Button _linkButton;
    [SerializeField] private Button _loginButton;
    [SerializeField] private Button _logoutButton;
    [SerializeField] private Button _closeButton;

    protected override void Awake()
    {
        base.Awake();

        _linkButton.onClick.AddListener(OnLinkClicked);
        _loginButton.onClick.AddListener(OnLoginClicked);
        _logoutButton.onClick.AddListener(OnLogoutClicked);
        _closeButton.onClick.AddListener(OnCloseClicked);
    }

    private void Start()
    {
        AccountBootstrap.OnStateChanged += Refresh;
        Refresh();
    }

    protected override void OnDestroySpecific()
    {
        AccountBootstrap.OnStateChanged -= Refresh;
    }

    public void Show()
    {
        Transition(true);
        Refresh();
    }

    protected override void OnGamePhaseChanged(GamePhase gamePhase)
    {
        base.OnGamePhaseChanged(gamePhase);

        switch (gamePhase)
        {
            case GamePhase.LOADING:
            case GamePhase.GAME:
            case GamePhase.PRE_END:
            case GamePhase.END:
                if (m_Visible)
                    Transition(false);
                break;
        }
    }

    private void OnLinkClicked()
    {
        AccountBootstrap.Connection.SetCredentials(_emailField.text, _passwordField.text);
        AccountBootstrap.Connection.Login();
        SetBusy("Linking...");
    }

    private void OnLoginClicked()
    {
        AccountBootstrap.Connection.SetCredentials(_emailField.text, _passwordField.text);
        AccountBootstrap.Connection.SignIn();
        SetBusy("Logging in...");
    }

    private void OnLogoutClicked()
    {
        AccountBootstrap.Authenticator.CurrentProvider.LogOut();
        SetBusy("Logging out...");
    }

    private void OnCloseClicked()
    {
        Transition(false);
    }

    private void SetBusy(string status)
    {
        _statusText.text = status;
        _linkButton.interactable = false;
        _loginButton.interactable = false;
        _logoutButton.interactable = false;
    }

    private void SetFieldsVisible(bool visible)
    {
        _emailField.gameObject.SetActive(visible);
        _passwordField.gameObject.SetActive(visible);
    }

    private void Refresh()
    {
        var ready = AccountBootstrap.IsReady;
        var authenticator = ready ? AccountBootstrap.Authenticator : null;
        var linked = authenticator != null && authenticator.IsLinked;

        var localId = PlayerDataProvider.Model.LocalPlayerId ?? string.Empty;
        var userId = authenticator != null && !string.IsNullOrEmpty(authenticator.UserId) ? authenticator.UserId : "-";
        _playerIdText.text = "Player " + localId.Substring(0, Mathf.Min(8, localId.Length)) + " / " + userId;

        if (!ready)
            _statusText.text = AccountBootstrap.BootStatus;
        else if (linked)
            _statusText.text = "Linked: " + authenticator.Email;
        else
            _statusText.text = string.IsNullOrEmpty(authenticator.LastError) ? "Not linked" : authenticator.LastError;

        SetFieldsVisible(!linked);

        _linkButton.gameObject.SetActive(!linked);
        _loginButton.gameObject.SetActive(!linked);
        _logoutButton.gameObject.SetActive(linked);

        _linkButton.interactable = ready;
        _loginButton.interactable = ready;
    }
}
