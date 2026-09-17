using System;
using System.IO;
using UnityEngine;

public abstract class DataHandler<T> where T : class
{
    protected readonly string FilePath;

    private readonly SaveEncryptor _encryptor;
    private readonly string _backupDirectory;
    private readonly string _backupPath;

    public T Data;

    protected DataHandler(string fileName, SaveEncryptor encryptor)
    {
        _encryptor = encryptor;

        var persistentDataPath = Application.persistentDataPath;
        var saveDirectory = persistentDataPath + "/PD/";
        _backupDirectory = persistentDataPath + "/Bckp/";
        FilePath = saveDirectory + fileName;
        _backupPath = _backupDirectory + fileName;

        Directory.CreateDirectory(saveDirectory);
    }

    public virtual T Load()
    {
        try
        {
            var fileContents = File.ReadAllText(FilePath);
            var decryptedString = _encryptor.Decrypt(fileContents);
            if (decryptedString == null)
                throw new Exception("Decrypt failed.");

            Data = JsonUtility.FromJson<T>(decryptedString);
            return Data;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"{GetType().Name} | Read failed, keeping the damaged file : {e.Message}");
            CopyToBackup(_backupPath + ".corrupt");
            return null;
        }
    }

    public void Save()
    {
        if (Data == null)
            return;

        try
        {
            var json = JsonUtility.ToJson(Data);
            var encryptedData = _encryptor.Encrypt(json);
            var tempPath = FilePath + ".tmp";
            File.WriteAllText(tempPath, encryptedData);
            File.Copy(tempPath, FilePath, true);
            File.Delete(tempPath);
        }
        catch (Exception e)
        {
            Debug.LogException(new Exception($"{GetType().Name} | Write failed.", e));
        }
    }

    public void CreateBackup()
    {
        CopyToBackup(_backupPath);
    }

    public bool RestoreBackup()
    {
        if (!File.Exists(_backupPath))
            return false;

        try
        {
            File.Copy(_backupPath, FilePath, true);
            Data = null;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(new Exception($"{GetType().Name} | Backup restore failed.", e));
            return false;
        }
    }

    private void CopyToBackup(string backupPath)
    {
        if (!File.Exists(FilePath))
            return;

        try
        {
            Directory.CreateDirectory(_backupDirectory);
            File.Copy(FilePath, backupPath, true);
        }
        catch (Exception e)
        {
            Debug.LogException(new Exception($"{GetType().Name} | Backup failed.", e));
        }
    }
}
