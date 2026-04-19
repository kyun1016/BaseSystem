using UnityEngine;
using System.Collections.Generic;
using System;
using System.Text;

public class DataManager : Singleton<DataManager>
{
    private const bool AutoDumpIdNameMapOnInit = true;

    private readonly Dictionary<int, ItemData> itemDict = new Dictionary<int, ItemData>();
    private readonly Dictionary<string, int> itemNameToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, string> itemIdToName = new Dictionary<int, string>();

    private readonly Dictionary<int, StatData> statDict = new Dictionary<int, StatData>();
    private readonly Dictionary<string, int> statNameToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, string> statIdToName = new Dictionary<int, string>();

    public void Init()
    {
        itemDict.Clear();
        itemNameToId.Clear();
        itemIdToName.Clear();

        statDict.Clear();
        statNameToId.Clear();
        statIdToName.Clear();

        ItemData[] items = Resources.LoadAll<ItemData>("ItemData");
        foreach (var item in items)
        {
            itemDict[item.ID] = item;
            RegisterNameIdMap(item.ID, item.Name, itemIdToName, itemNameToId, "Item");
        }

        StatData[] stats = Resources.LoadAll<StatData>("StatData");
        foreach (var stat in stats)
        {
            statDict[stat.ID] = stat;
            RegisterNameIdMap(stat.ID, stat.Name, statIdToName, statNameToId, "Stat");
        }

        Debug.Log($"<color=cyan>[DataManager] 데이터 로드 완료: 아이템 {itemDict.Count}개, 스탯 {statDict.Count}개</color>");
        if (AutoDumpIdNameMapOnInit)
            Debug.Log(BuildIdNameMapReport());
    }

    public ItemData GetItem(int id) {return itemDict.TryGetValue(id, out var data) ? data : null;}
    public ItemData GetItem(string nameKey)
    {
        return TryGetItemId(nameKey, out int id) ? GetItem(id) : null;
    }

    public bool TryGetItemId(string nameKey, out int id)
    {
        id = 0;
        if (string.IsNullOrWhiteSpace(nameKey))
            return false;
        return itemNameToId.TryGetValue(nameKey.Trim(), out id);
    }

    public bool TryGetItemName(int id, out string name)
    {
        return itemIdToName.TryGetValue(id, out name);
    }

    public StatData GetStat(StatType type) {return statDict.TryGetValue((int)type + GameDataHeaders.Stat, out var data) ? data : null;}
    public StatData GetStat(int id) {return statDict.TryGetValue(id, out var data) ? data : null;}
    public StatData GetStat(string nameKey)
    {
        return TryGetStatId(nameKey, out int id) ? GetStat(id) : null;
    }

    public bool TryGetStatId(string nameKey, out int id)
    {
        id = 0;
        if (string.IsNullOrWhiteSpace(nameKey))
            return false;
        return statNameToId.TryGetValue(nameKey.Trim(), out id);
    }

    public bool TryGetStatName(int id, out string name)
    {
        return statIdToName.TryGetValue(id, out name);
    }

    [ContextMenu("Data/Dump ID Name Maps")]
    public void DumpIdNameMaps()
    {
        Debug.Log(BuildIdNameMapReport());
    }

    private static void RegisterNameIdMap(
        int id,
        string name,
        Dictionary<int, string> idToName,
        Dictionary<string, int> nameToId,
        string label)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        string key = name.Trim();
        idToName[id] = key;

        if (nameToId.TryGetValue(key, out int existingId) && existingId != id)
        {
            Debug.LogWarning($"[DataManager] {label} Name 중복: {key} (기존:{existingId}, 신규:{id})");
            return;
        }

        nameToId[key] = id;
    }

    private string BuildIdNameMapReport()
    {
        var sb = new StringBuilder(1024);
        sb.AppendLine("[DataManager] ID <-> Name Map Dump");
        sb.AppendLine("[Item]");
        foreach (var pair in itemIdToName)
            sb.AppendLine($"{pair.Key} <-> {pair.Value}");

        sb.AppendLine("[Stat]");
        foreach (var pair in statIdToName)
            sb.AppendLine($"{pair.Key} <-> {pair.Value}");

        return sb.ToString();
    }
}