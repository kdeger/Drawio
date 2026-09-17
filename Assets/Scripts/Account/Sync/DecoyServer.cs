using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public enum DecoyScenario
{
    NoData,
    HasData,
    Failure,
    Timeout
}

public class DecoyServer : IServerSync
{
    private const int NoProfile = -1;

    private class StoredProfile
    {
        public string Json;
        public int Revision;
    }

    private readonly AccountSettings _settings;
    private readonly AccountSession _session;
    private readonly Dictionary<string, StoredProfile> _profiles = new Dictionary<string, StoredProfile>();

    public DecoyServer(AccountSettings settings, AccountSession session)
    {
        _settings = settings;
        _session = session;
    }

    public async Task<int> GetRevisionAsync(string userId)
    {
        await Simulate();

        var revision = NoProfile;

        if (_profiles.TryGetValue(userId, out var stored))
            revision = stored.Revision;
        else if (_settings.Scenario == DecoyScenario.HasData && _session.IsLinked)
            revision = _settings.SeedRevision;

        Debug.Log($"Decoy | Revision of {userId} -> {revision}");
        return revision;
    }

    public async Task<PlayerSaveData> LoadAsync(string userId)
    {
        await Simulate();

        if (!_profiles.TryGetValue(userId, out var stored))
        {
            var seed = CreateSeedProfile();
            stored = Store(userId, seed, seed.Revision);
        }

        var data = JsonUtility.FromJson<PlayerSaveData>(stored.Json);
        data.Revision = stored.Revision;
        Debug.Log($"Decoy | Loaded {userId} (rev {stored.Revision})");
        return data;
    }

    public async Task<int> SaveAsync(string userId, PlayerSaveData data)
    {
        await Simulate();

        var stored = Store(userId, data, NextRevision(userId));
        Debug.Log($"Decoy | Saved {userId} (rev {stored.Revision})");
        return stored.Revision;
    }

    private StoredProfile Store(string userId, PlayerSaveData data, int revision)
    {
        var stored = new StoredProfile { Json = JsonUtility.ToJson(data), Revision = revision };
        _profiles[userId] = stored;
        return stored;
    }

    private int NextRevision(string userId)
    {
        return _profiles.TryGetValue(userId, out var stored) ? stored.Revision + 1 : 1;
    }

    private async Task Simulate()
    {
        if (_settings.Scenario == DecoyScenario.Timeout)
        {
            Debug.Log("Decoy | Never answering");
            await NeverAnswer();
        }

        await Task.Delay(TimeSpan.FromSeconds(_settings.DecoyDelaySeconds));

        if (_settings.Scenario == DecoyScenario.Failure)
            throw new Exception("Decoy | server failure");
    }

    private static Task NeverAnswer()
    {
        return new TaskCompletionSource<bool>().Task;
    }

    private PlayerSaveData CreateSeedProfile()
    {
        var data = PlayerSaveData.CreateNew();
        data.Revision = _settings.SeedRevision;
        data.Progression.Level = _settings.SeedLevel;
        data.Progression.XP = _settings.SeedXP;
        data.Progression.Nickname = _settings.SeedNickname;
        return data;
    }
}
