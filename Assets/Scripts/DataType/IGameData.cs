public interface IGameData
{
    int ID { get; }
    int TypeHeader { get; }
    int Number { get; }
    void SetID(int id);
}
