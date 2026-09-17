using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BootLoader : MonoBehaviour
{
    [SerializeField] private AccountSettings _settings;
    [SerializeField] private Text _statusText;
    [SerializeField] private string _gameSceneName = "Game";
    [SerializeField] private float _maxWaitSeconds = 10f;

    private async void Start()
    {
        if (_settings == null)
        {
            Debug.LogError("Boot | AccountSettings is not assigned on BootLoader");
            LoadGameScene();
            return;
        }

        AccountBootstrap.OnStateChanged += RefreshStatus;
        RefreshStatus();

        var timeout = Task.Delay(TimeSpan.FromSeconds(_maxWaitSeconds));
        if (await Task.WhenAny(AccountBootstrap.InitializeServicesAsync(_settings), timeout) == timeout)
            Debug.LogWarning("Boot | Initialization did not finish in time, opening the menu with local data");

        LoadGameScene();
    }

    private void OnDestroy()
    {
        AccountBootstrap.OnStateChanged -= RefreshStatus;
    }

    private void RefreshStatus()
    {
        if (_statusText != null)
            _statusText.text = AccountBootstrap.BootStatus;
    }

    private void LoadGameScene()
    {
        SceneManager.LoadScene(_gameSceneName);
    }
}
