using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ItemEffect
{
    public StatType Stat;
    public float Value;
}

[System.Serializable]
public class ItemCondition
{
    public ConditionType Condition;
    public string Value;
}

[CreateAssetMenu(fileName = "NewItemData", menuName = "ScriptableObjects/ItemData", order = 1)]
public class ItemData : ScriptableObject
{
    public int ID;
    public string Name;         // Name (영어 식별자 등)
    public string Name_KR;           // Name_KR (한글 이름)
    [TextArea] 
    public string Description;      // Description
    public ItemType ItemType;
    public int BuyPrice;
    public int SellPrice;
    public StatType EffectStatType1;
    public int EffectValue1;
    public StatType EffectStatType2;
    public int EffectValue2;
    
    public List<ItemCondition> Conditions = new List<ItemCondition>(); // Conditions
    public bool IsHidden;           // IsHidden
    public string SpritePath;       // SpritePath
}