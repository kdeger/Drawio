using System.IO;
using UnityEngine;

public class PlayerSettingsHandler : DataHandler<PlayerSettingsData>
{
    private const string SaveFileName = "set";

    public PlayerSettingsHandler(SaveEncryptor encryptor) : base(SaveFileName, encryptor)
    {
    }

    public override PlayerSettingsData Load()
    {
        if (Data != null)
            return Data;

        if (File.Exists(FilePath) && base.Load() != null)
            return Data;

        Data = new PlayerSettingsData();
        Data.Vibration = PlayerPrefs.GetInt(Constants.c_VibrationSave, 1) == 1;
        Save();
        Debug.Log("Boot | Settings created");
        return Data;
    }
}
