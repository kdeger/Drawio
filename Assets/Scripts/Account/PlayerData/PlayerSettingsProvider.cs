public static class PlayerSettingsProvider
{
    private static PlayerSettingsHandler _handler;

    public static PlayerSettingsData Settings
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

        _handler = new PlayerSettingsHandler(new SaveEncryptor(SaveKeys.EncryptionKey));
        _handler.Load();
    }

    public static void Save()
    {
        if (_handler != null)
            _handler.Save();
    }
}
