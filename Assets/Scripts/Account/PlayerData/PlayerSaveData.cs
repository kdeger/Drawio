using System;
using System.Collections.Generic;

[Serializable]
public class PlayerSaveData
{
    public int SchemaVersion = 1;
    public string LocalPlayerId;
    public int Revision;

    public PlayerProgressionData Progression;
    public PlayerCustomizationData Customization;

    public static PlayerSaveData CreateNew()
    {
        return new PlayerSaveData
        {
            LocalPlayerId = Guid.NewGuid().ToString(),
            Progression = new PlayerProgressionData(),
            Customization = new PlayerCustomizationData()
        };
    }
}

[Serializable]
public class PlayerProgressionData
{
    public int XP;
    public int Level = 1;
    public int BestScore;
    public string Nickname;
    public List<int> RecentResults = new List<int>();
}

[Serializable]
public class PlayerCustomizationData
{
    public int SkinIndex;
}
