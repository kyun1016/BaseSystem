using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ItemEffect
{
    public int StatID;
    public int Value;
}

[CreateAssetMenu(fileName = "NewItemData", menuName = "ScriptableObjects/ItemData", order = 1)]
public class ItemData : ScriptableObject, IGameData
{
    public int ItemID;
    public string Name;
    public string Name_KR;

    public int ID         => ItemID;
    public int TypeHeader => GameDataID.GetHeader(ItemID);
    public int Number     => GameDataID.GetNumber(ItemID);
    public void SetID(int id) => ItemID = id;
    [TextArea] 
    public string Description;      // Description
    public ItemType ItemType;
    public int BuyPrice;
    public int SellPrice;
    public List<ItemEffect> EffectStats = new List<ItemEffect>();
    public bool IsHidden;           // IsHidden
    public string SpritePath;       // SpritePath
}