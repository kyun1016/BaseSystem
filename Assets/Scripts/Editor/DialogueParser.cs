using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;

public static class DialogueParser
{
    private const string CsvFolder    = "Assets/Datas/Dialogue";
    private const string NodeSavePath = "Assets/Resources/DialogueData";
    private const string GroupSavePath = "Assets/Resources/DialogueGroupData";
    private const int GROUP_KEY_OFFSET = 10000;

    // Dialogue 전용 Alias → Key 맵 (모든 Dialogue CSV를 통합해 빌드)
    private static readonly Dictionary<string, int> DialogueAliasToKey
        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    // =======================================================
    // 진입점
    // =======================================================
    [MenuItem("Tools/Data Parse/Parse Dialogue CSV")]
    public static void ParseAllDialogue()
    {
        Debug.Log("<color=cyan><b>[DialogueParser] Dialogue 파싱 시작...</b></color>");

        // 1. 다른 타입(Stat, Item 등) 참조 해석을 위해 GameDataParser Alias 맵 빌드
        GameDataParser.RebuildAliasMaps();

        // 2. Dialogue 노드 Alias 맵 빌드 (NextNodes 해석용)
        BuildDialogueAliasMap();

        // 3. 저장 폴더 준비
        GameDataParser.CreateFolderRecursive(NodeSavePath);
        GameDataParser.CreateFolderRecursive(GroupSavePath);

        // 4. Dialogue/ 폴더 내 모든 CSV 파싱
        string[] csvGuids = AssetDatabase.FindAssets("t:TextAsset", new[] { CsvFolder });
        if (csvGuids.Length == 0)
        {
            Debug.LogWarning($"<color=yellow><b>[DialogueParser] CSV 없음: {CsvFolder}</b></color>");
            return;
        }

        HashSet<string> validNodePaths  = new HashSet<string>();
        HashSet<string> validGroupPaths = new HashSet<string>();

        foreach (string guid in csvGuids)
        {
            string csvPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!csvPath.EndsWith(".csv")) continue;
            ParseCsv(csvPath, validNodePaths, validGroupPaths);
        }

        // 5. 스탈(stale) 에셋 삭제
        DeleteStale<DialogueData>(NodeSavePath, validNodePaths);
        DeleteStale<DialogueGroup>(GroupSavePath, validGroupPaths);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green><b>[DialogueParser] 전체 Dialogue 파싱 완료!</b></color>");
    }

    // =======================================================
    // Step 1. Dialogue Alias 맵 빌드
    //   - 모든 CSV의 ID가 있는 행만 읽어 "Dialogue_{Alias}" → Key 맵 구성
    //   - NextNodes 컬럼 해석 시 사용
    // =======================================================
    private static void BuildDialogueAliasMap()
    {
        DialogueAliasToKey.Clear();
        string[] csvGuids = AssetDatabase.FindAssets("t:TextAsset", new[] { CsvFolder });
        foreach (string guid in csvGuids)
        {
            string csvPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!csvPath.EndsWith(".csv")) continue;

            TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>(csvPath);
            if (csv == null) continue;

            List<string[]> rows = GameDataParser.ReadCSV(csv.text);
            foreach (string[] row in rows)
            {
                if (row.Length < 2) continue;
                if (!int.TryParse(row[0].Trim(), out int id)) continue; // 헤더 및 서브 행 스킵
                string alias = row[1].Trim();
                if (string.IsNullOrEmpty(alias)) continue;
                int key = id + (int)eHeader.Dialogue * BaseData.HEADER_SIZE;
                DialogueAliasToKey[$"Dialogue_{alias}"] = key;
            }
        }
        Debug.Log($"<color=cyan>[DialogueParser] Dialogue Alias 맵 빌드 완료: {DialogueAliasToKey.Count}개</color>");
    }

    // =======================================================
    // Step 2. CSV 1개 파싱 → DialogueData 에셋 + DialogueGroup 에셋 생성
    // =======================================================
    private static void ParseCsv(
        string csvPath,
        HashSet<string> validNodePaths,
        HashSet<string> validGroupPaths)
    {
        // 파일명에서 그룹 이름 추출: "Dialogue_Intro.csv" → "Intro"
        string fileName  = Path.GetFileNameWithoutExtension(csvPath);
        string groupName = fileName.StartsWith("Dialogue_", StringComparison.OrdinalIgnoreCase)
            ? fileName.Substring("Dialogue_".Length)
            : fileName;

        TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>(csvPath);
        if (csv == null)
        {
            Debug.LogError($"<color=red><b>CSV 로드 실패: {csvPath}</b></color>");
            return;
        }

        List<string[]> rows = GameDataParser.ReadCSV(csv.text);
        if (rows.Count <= 1)
        {
            Debug.LogWarning($"<color=yellow><b>데이터 없음: {csvPath}</b></color>");
            return;
        }

        // 행을 노드 단위로 그루핑 (빈 ID 행 = 이전 노드의 서브 행)
        List<(string[] main, List<string[]> subs)> nodeGroups = GroupByNode(rows);

        int startKey = -1;
        List<DialogueData> groupNodes = new List<DialogueData>();

        foreach (var (mainRow, subRows) in nodeGroups)
        {
            if (!int.TryParse(mainRow[0].Trim(), out int id)) continue;
            string alias = SafeGet(mainRow, 1).Trim();
            int key = id + (int)eHeader.Dialogue * BaseData.HEADER_SIZE;
            if (startKey == -1) startKey = key;

            string assetPath = $"{NodeSavePath}/DialogueData_{id}_{alias}.asset";
            validNodePaths.Add(assetPath);

            DialogueData node = AssetDatabase.LoadAssetAtPath<DialogueData>(assetPath);
            bool isNew = node == null;
            if (isNew) node = ScriptableObject.CreateInstance<DialogueData>();

            FillNode(node, id, alias, mainRow, subRows);

            if (isNew) AssetDatabase.CreateAsset(node, assetPath);
            EditorUtility.SetDirty(node);
            groupNodes.Add(node);
        }

        // DialogueGroup 생성/갱신
        string groupPath = $"{GroupSavePath}/DialogueGroup_{groupName}.asset";
        validGroupPaths.Add(groupPath);

        DialogueGroup group = AssetDatabase.LoadAssetAtPath<DialogueGroup>(groupPath);
        if (group == null)
        {
            group = ScriptableObject.CreateInstance<DialogueGroup>();
            AssetDatabase.CreateAsset(group, groupPath);
        }
        group.Base = new BaseData(GROUP_KEY_OFFSET * (groupNodes[0].ID / GROUP_KEY_OFFSET), eHeader.Dialogue, groupName);
        group.StartNodeKey = startKey;
        group.Nodes        = groupNodes;
        EditorUtility.SetDirty(group);

        Debug.Log($"<color=green>[DialogueParser] {groupName}: 노드 {groupNodes.Count}개 파싱 완료</color>");
    }

    // =======================================================
    // Step 3. 노드 필드 채우기
    //
    // CSV 컬럼 구조:
    //  0:ID  1:Alias  2:Text_EN  3:Text_KR  4:Speakder [NPC]  5:Type
    //  6:ActionType  7:ActionTarget  8:StatConditions [Stat]
    //  9:ItemConditions [Item]  10:QuestConditions [Quest]  11:NextNodes [Dialogue]
    //
    // Texts 수집: 메인 행 + 모든 서브 행 (행 순서 = 텍스트 출력 순서 = Choice 인덱스)
    // NextNodeKeys: 각 행의 col11에서 수집 (Line: 메인 행만 값 있음, Choice: 각 행에 값 있음)
    // =======================================================
    private static void FillNode(DialogueData node, int id, string alias, string[] mainRow, List<string[]> subRows)
    {
        node.Base = new BaseData(id, eHeader.Dialogue, alias);

        // Speaker (col 4)
        node.Speakder = SafeGet(mainRow, 4).Trim();

        // Type (col 5)
        node.DialogueType = Enum.TryParse(SafeGet(mainRow, 5).Trim(), true, out eDialogueType dialogueType)
            ? dialogueType : eDialogueType.Line;

        // ActionType (col 6)
        node.ActionType = Enum.TryParse(SafeGet(mainRow, 6).Trim(), true, out eDialogueAction actionType)
            ? actionType : eDialogueAction.None;

        // ActionTarget (col 7)
        node.ActionTarget = SafeGet(mainRow, 7).Trim();

        // Conditions (col 8, 9, 10) — 메인 행에서만 읽음
        node.StatCondition  = ParseCrossRefList("Stat",  SafeGet(mainRow, 8));
        node.ItemCondition  = ParseCrossRefList("Item",  SafeGet(mainRow, 9));
        node.QuestCondition = ParseCrossRefList("Quest", SafeGet(mainRow, 10));

        // Texts + NextNodeKeys — 메인 행 포함 모든 행에서 순서대로 수집
        node.Texts        = new List<LocalizedString>();
        node.NextNodeKeys = new List<int>();

        List<string[]> allRows = new List<string[]> { mainRow };
        allRows.AddRange(subRows);

        foreach (string[] row in allRows)
        {
            node.Texts.Add(new LocalizedString
            {
                EN = SafeGet(row, 2),
                KR = SafeGet(row, 3)
            });

            string nextRaw = SafeGet(row, 11).Trim();
            if (string.IsNullOrEmpty(nextRaw)) continue;

            foreach (string entry in nextRaw.Split(';'))
            {
                string trimmed = entry.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                string fullAlias = $"Dialogue_{trimmed}";
                if (!DialogueAliasToKey.TryGetValue(fullAlias, out int nextKey))
                {
                    Debug.LogWarning($"[DialogueParser] NextNode Alias 미등록: '{trimmed}' (노드 ID:{id})");
                    nextKey = -1;
                }
                node.NextNodeKeys.Add(nextKey);
            }
        }
    }

    // =======================================================
    // 헬퍼
    // =======================================================

    /// <summary>
    /// 빈 ID 행(서브 행)을 앞 노드에 묶어 그룹 리스트로 반환
    /// </summary>
    private static List<(string[] main, List<string[]> subs)> GroupByNode(List<string[]> rows)
    {
        var groups = new List<(string[] main, List<string[]> subs)>();
        string[] currentMain = null;
        List<string[]> currentSubs = null;

        for (int i = 1; i < rows.Count; i++) // i=0은 헤더 행
        {
            string[] row = rows[i];
            bool hasId = row.Length > 0
                && !string.IsNullOrWhiteSpace(row[0])
                && int.TryParse(row[0].Trim(), out _);

            if (hasId)
            {
                if (currentMain != null) groups.Add((currentMain, currentSubs));
                currentMain = row;
                currentSubs = new List<string[]>();
            }
            else if (currentMain != null)
            {
                currentSubs.Add(row);
            }
        }
        if (currentMain != null) groups.Add((currentMain, currentSubs));
        return groups;
    }

    /// <summary>
    /// Stat / Item / Quest 조건 컬럼 파싱 ("Name:Value;Name:Value" 형식)
    /// GameDataParser.AliasToKey 맵 사용 (RebuildAliasMaps() 호출 후 유효)
    /// </summary>
    private static List<ReferenceData> ParseCrossRefList(string type, string data)
    {
        var list = new List<ReferenceData>();
        if (string.IsNullOrWhiteSpace(data)) return list;

        foreach (string entry in data.Split(';'))
        {
            string[] split = entry.Split(':');
            if (split.Length != 2) continue;
            if (!int.TryParse(split[1].Trim(), out int value)) continue;

            string alias = $"{type}_{split[0].Trim()}";
            int key = GameDataParser.TryResolveAlias(alias);
            if (key == -1)
            {
                Debug.LogWarning($"[DialogueParser] 참조 미등록: {alias}");
                continue;
            }
            list.Add(new ReferenceData(key, alias, split[0].Trim(), value));
        }
        return list;
    }

    private static void DeleteStale<T>(string folder, HashSet<string> validPaths) where T : ScriptableObject
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
        int deleted = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!validPaths.Contains(path)) { AssetDatabase.DeleteAsset(path); deleted++; }
        }
        if (deleted > 0)
            Debug.Log($"[DialogueParser] 스탈 에셋 {deleted}개 삭제: {typeof(T).Name}");
    }

    private static string SafeGet(string[] row, int index)
        => row != null && index < row.Length ? row[index] : string.Empty;
}
