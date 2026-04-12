using UnityEngine;
using System.Collections.Generic;

public class DataManager : Singleton<DataManager>
{
    private Dictionary<int, ItemData> itemDict = new Dictionary<int, ItemData>();
    private Dictionary<StatType, StatData> statDict = new Dictionary<StatType, StatData>();

    public void Init()
    {
        ItemData[] items = Resources.LoadAll<ItemData>("ItemData");
        foreach (var item in items) itemDict[item.ID] = item;

        StatData[] stats = Resources.LoadAll<StatData>("StatData");
        foreach (var stat in stats) statDict[stat.Type] = stat;

        Debug.Log($"<color=cyan>[DataManager] 데이터 로드 완료: 아이템 {itemDict.Count}개, 스탯 {statDict.Count}개</color>");
    }
    public ItemData GetItem(int id) {return itemDict.TryGetValue(id, out var data) ? data : null;}
    public StatData GetStat(StatType type) {return statDict.TryGetValue(type, out var data) ? data : null;}
}