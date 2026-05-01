public enum eHeader
{
    None = 0,
    Stat,
    Item,
    Dialogue,
    Ending,
    Event,
    Location,
    Monster,
    NPC,
    Schedule,
    Shop,
    Skill,
    Soul,
    Count
}

public enum eLanguage
{
    KR,
    EN,
    // 추가 언어...
}

[System.Serializable]
public struct LocalizedString
{
    public string KR;
    public string EN;
    // 추가 언어...
}

[System.Serializable]
public class BaseData
{
    // Const
    static public int HEADER_SIZE = 100_000_000;

    // member variables
    public int ID;
    public eHeader Header;
    public int Key;
    public LocalizedString Name;

    // Constructors
    public BaseData() {}
    public BaseData(int id, eHeader header, LocalizedString name)
    {
        ID = id;
        Header = header;
        Key = ID + (int)Header * HEADER_SIZE;
        Name = name;
    }
    // Functions
    public int GetID() => ID;
    public eHeader GetHeader() => Header;
    public int GetKey() => Key;
    public LocalizedString GetName() => Name;

     // == / != 연산자 오버로딩
    public static bool operator ==(BaseData a, BaseData b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return a.GetKey() == b.GetKey();
    }
    public static bool operator !=(BaseData a, BaseData b) => !(a == b);
    public override bool Equals(object obj) => obj is BaseData other && this == other;
    public override int GetHashCode() => GetKey().GetHashCode();
}

[System.Serializable]
public struct ReferenceData
{
    public int Key;
    public string KeyName;
    public string Name;
    public int Value;

    public ReferenceData(int key, string keyName, string name, int value)
    {
        Key = key;
        KeyName = keyName;
        Name = name;
        Value = value;
    }
}

public static class GameDataHeaders
{
    public const int Monster = 0x10;
    public const int Soul = 0x20;
    public const int Item = 0x30;
    public const int Object = 0x40;
    public const int Sprite = 0x50;
    public const int Stat = 0x60;
}