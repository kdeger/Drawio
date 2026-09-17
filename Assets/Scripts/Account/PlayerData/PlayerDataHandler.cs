using System.IO;
using UnityEngine;

public class PlayerDataHandler : DataHandler<PlayerSaveData>
{
    private const string SaveFileName = "plr";
    private const string FavoriteSkinKey = "FavoriteSkin";

    public PlayerDataHandler(SaveEncryptor encryptor) : base(SaveFileName, encryptor)
    {
    }

    public override PlayerSaveData Load()
    {
        if (Data != null)
            return Data;

        if (File.Exists(FilePath) && base.Load() != null)
            return Data;

        if (RestoreBackup() && base.Load() != null)
        {
            Debug.LogWarning("Boot | Save was damaged, restored the last backup");
            return Data;
        }

        Data = PlayerSaveData.CreateNew();
        MigrateFromPlayerPrefs(Data);
        Save();
        Debug.Log("Boot | New player created " + Data.LocalPlayerId);
        return Data;
    }

    private static void MigrateFromPlayerPrefs(PlayerSaveData data)
    {
        data.Progression.XP = PlayerPrefs.GetInt(Constants.c_PlayerXPSave, 0);
        data.Progression.Level = PlayerPrefs.GetInt(Constants.c_PlayerLevelSave, 1);
        data.Progression.BestScore = PlayerPrefs.GetInt(Constants.c_BestScoreSave, 0);
        data.Progression.Nickname = PlayerPrefs.GetString(Constants.c_PlayerNameSave, null);

        for (var i = 0; i < Constants.c_SavedGameCount; i++)
            data.Progression.RecentResults.Add(PlayerPrefs.GetInt(Constants.c_GameResultSave + "_" + i, 0));

        data.Customization.SkinIndex = PlayerPrefs.GetInt(FavoriteSkinKey, 0);
    }
}
