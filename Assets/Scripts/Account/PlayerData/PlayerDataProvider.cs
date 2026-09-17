public static class PlayerDataProvider
{
    private static PlayerDataHandler _handler;

    public static PlayerSaveData Model
    {
        get
        {
            if (_handler == null)
                Initialize();

            return _handler.Data;
        }
    }

    public static void Initialize()
    {
        if (_handler != null)
            return;

        _handler = new PlayerDataHandler(new SaveEncryptor(SaveKeys.EncryptionKey));
        _handler.Load();
    }

    public static void Save()
    {
        if (_handler != null)
            _handler.Save();
    }

    public static void CreateBackup()
    {
        if (_handler != null)
            _handler.CreateBackup();
    }

    public static bool RestoreBackup()
    {
        return _handler != null && _handler.RestoreBackup() && _handler.Load() != null;
    }
}
