using UnityEngine;
using System.Collections.Generic;
using System;
using System.Text;

public class DataManager : Singleton<DataManager>
{
    private const bool AutoDumpIdNameMapOnInit = true;

    private readonly Dictionary<int, IGameData> DataMap = new Dictionary<int, IGameData>();

    public void Init()
    {
        DataMap.Clear();

        LoadAll<ItemData>("ItemData");
        LoadAll<StatData>("StatData");
        LoadAll<DialogueData>("DialogueData");
        LoadAll<DialogueGroupData>("DialogueGroupData");
    }

    private void LoadAll<T>(string path) where T : ScriptableObject
    {
        int countBefore = DataMap.Count;
        foreach (IGameData asset in Resources.LoadAll<T>(path))
            DataMap[asset.Key] = asset; // 공통 인터페이스로 Key 추출
        Debug.Log($"<color=cyan>[DataManager] {path} 경로에서 {DataMap.Count - countBefore}개의 {typeof(T).Name} 데이터를 로드했습니다.</color>");
    }

    public IGameData GetData(int key) {return DataMap.TryGetValue(key, out var data) ? data : null;}
    public ItemData GetItem(int key) {return DataMap.TryGetValue(key, out var data) ? data as ItemData : null;}
    public StatData GetStat(int key) {return DataMap.TryGetValue(key, out var data) ? data as StatData : null;}
    public DialogueData GetDialogue(int key) {return DataMap.TryGetValue(key, out var data) ? data as DialogueData : null;}
    public DialogueGroupData GetDialogueGroup(int key) {return DataMap.TryGetValue(key, out var data) ? data as DialogueGroupData : null;}
}