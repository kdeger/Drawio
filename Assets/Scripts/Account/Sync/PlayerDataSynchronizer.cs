using System;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerDataSynchronizer : DataSynchronizer
{
    private readonly IServerSync _server;
    private readonly AccountSettings _settings;
    private readonly AccountSession _session;

    public PlayerDataSynchronizer(IServerSync server, AccountSettings settings, AccountSession session)
    {
        _server = server;
        _settings = settings;
        _session = session;
    }

    public override Task InitializeAsync()
    {
        return SynchronizeDataByUserStatus(false);
    }

    public override async Task SynchronizeDataByUserStatus(bool isNewUser)
    {
        var userId = _session.UserId;
        var local = PlayerDataProvider.Model;
        Debug.Log($"Sync | Synchronizing {userId} (new user {isNewUser}, local rev {local.Revision})");

        try
        {
            var remoteRevision = await WithTimeout(_server.GetRevisionAsync(userId));

            if (remoteRevision <= local.Revision)
            {
                await LoadDataDeviceToRemote();
                return;
            }

            Apply(await WithTimeout(_server.LoadAsync(userId)));
        }
        catch (Exception e)
        {
            Debug.LogWarning("Sync | Server unavailable, continuing with local data : " + e.Message);
        }
    }

    public override async Task UpdateRemoteUserData()
    {
        try
        {
            await LoadDataDeviceToRemote();
        }
        catch (Exception e)
        {
            Debug.LogWarning("Sync | Could not upload before sign out : " + e.Message);
        }
    }

    protected override async Task LoadDataDeviceToRemote()
    {
        var model = PlayerDataProvider.Model;
        model.Revision = await WithTimeout(_server.SaveAsync(_session.UserId, model));
        PlayerDataProvider.Save();
        Debug.Log($"Sync | Device profile uploaded (rev {model.Revision})");
    }

    public override void CreateLocalBackUp()
    {
        PlayerDataProvider.Save();
        PlayerDataProvider.CreateBackup();
    }

    public override void LoadLocalBackUpData()
    {
        Debug.Log(PlayerDataProvider.RestoreBackup()
            ? "Sync | Local backup restored"
            : "Sync | No local backup to restore");
    }

    private void Apply(PlayerSaveData remote)
    {
        CreateLocalBackUp();

        var model = PlayerDataProvider.Model;
        model.Revision = remote.Revision;
        model.Progression = remote.Progression;
        model.Customization = remote.Customization;
        PlayerDataProvider.Save();
        Debug.Log($"Sync | Server profile applied (rev {remote.Revision})");
    }

    private async Task<T> WithTimeout<T>(Task<T> task)
    {
        await WithTimeout((Task)task);
        return await task;
    }

    private async Task WithTimeout(Task task)
    {
        var timeout = Task.Delay(TimeSpan.FromSeconds(_settings.ServerTimeoutSeconds));
        if (await Task.WhenAny(task, timeout) == timeout)
            throw new TimeoutException("Sync | Server timed out");

        await task;
    }
}
