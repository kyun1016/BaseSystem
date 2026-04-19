public static class GameDataID
{
    public static int GetHeader(int packedId) => (packedId >> 24) & 0xFF;
    public static int GetNumber(int packedId) => packedId & 0x00FFFFFF;

    public static bool IsSameType(int packedIdA, int packedIdB)
        => GetHeader(packedIdA) == GetHeader(packedIdB);

    public static bool Matches(int packedId, int header, int number)
        => GetHeader(packedId) == header && GetNumber(packedId) == number;
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
