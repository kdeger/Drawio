public class AccountSession
{
    public string UserId { get; private set; }
    public bool IsLinked { get; private set; }

    public void Save(string userId, bool isLinked)
    {
        UserId = userId;
        IsLinked = isLinked;
    }
}
