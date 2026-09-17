using System.Threading.Tasks;

public abstract class DataSynchronizer
{
    public abstract Task InitializeAsync();
    public abstract Task SynchronizeDataByUserStatus(bool isNewUser);
    public abstract Task UpdateRemoteUserData();
    protected abstract Task LoadDataDeviceToRemote();

    public abstract void CreateLocalBackUp();
    public abstract void LoadLocalBackUpData();
}
