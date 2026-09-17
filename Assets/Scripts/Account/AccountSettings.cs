using System.IO;
using UnityEngine;

[CreateAssetMenu(menuName = "Draw.io/Account Settings")]
public class AccountSettings : ScriptableObject
{
    public bool UseMockAuthenticator;
    public DecoyScenario Scenario;
    public float DecoyDelaySeconds = 0.5f;
    public float ServerTimeoutSeconds = 3f;
    public float FirebaseInitTimeoutSeconds = 8f;
    public string MockExistingEmail = "koray@deger.com";
    public string MockExistingPassword = "123456";
    public string MockExistingUserId = "mock-existing";
    public int SeedRevision = 5;
    public int SeedLevel = 4;
    public int SeedXP = 90;
    public string SeedNickname = "ServerPlayer";

    [ContextMenu("Reset Local Data")]
    private void ResetLocalData()
    {
        DeleteDirectory(Application.persistentDataPath + "/PD");
        DeleteDirectory(Application.persistentDataPath + "/Bckp");
        PlayerPrefs.DeleteAll();
        Debug.Log("Boot | Local data reset");
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);
    }
}
