using System.Threading.Tasks;

public interface IServerSync
{
    Task<int> GetRevisionAsync(string userId);
    Task<PlayerSaveData> LoadAsync(string userId);
    Task<int> SaveAsync(string userId, PlayerSaveData data);
}
