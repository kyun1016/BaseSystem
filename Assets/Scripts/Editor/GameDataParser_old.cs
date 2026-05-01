using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Text;

public class GameDataParser_old : EditorWindow
{
    private const string GeneratedMapFolder = "Assets/Datas/_Generated";

    // =======================================================
    // 파싱 설정
    private static readonly string[] DataNames = { "Stat", "Item", "Monster" };
        [MenuItem("Tools/Data Parse/Parse All CSV")]
    public static void ParseAllCSV()
    {
        ResetAliasCaches();

        foreach (string dataName in DataNames)
        {
            ParseByDataName(dataName);
        }

        ExportAliasMapTables();
    }

    private static void ParseByDataName(string dataName)
    {
        string typeName = $"{dataName}Data";
        Type dataType = ResolveType(typeName);
        if (dataType == null || !typeof(ScriptableObject).IsAssignableFrom(dataType))
        {
            Debug.LogError($"<color=red><b>타입을 찾을 수 없습니다: {typeName}</b></color>\n컴파일 에러가 없는지 확인하고, 클래스명이 정확히 '{typeName}' 인지 확인하세요.");
            return;
        }

        string csvPath = $"Assets/Datas/{dataName}.csv";
        string savePath = $"Assets/Resources/{dataName}Data";

        MethodInfo parseMethod = typeof(GameDataParser).GetMethod(nameof(ParseData), BindingFlags.NonPublic | BindingFlags.Static);
        if (parseMethod == null)
        {
            Debug.LogError("<color=red><b>ParseData 메서드를 찾을 수 없습니다.</b></color>");
            return;
        }
        MethodInfo genericParseMethod = parseMethod.MakeGenericMethod(dataType);
        genericParseMethod.Invoke(null, new object[] { csvPath, savePath });
    }

    private static Type ResolveType(string typeName)
    {
        // Unity가 로드한 ScriptableObject 타입 캐시를 우선 사용하면
        // 에디터 타이밍/리플렉션 로드 이슈를 피할 수 있다.
        foreach (Type type in TypeCache.GetTypesDerivedFrom<ScriptableObject>())
        {
            if (type != null && type.Name == typeName)
                return type;
        }

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(typeName);
            if (type != null)
                return type;

            Type[] assemblyTypes;
            try
            {
                assemblyTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                assemblyTypes = e.Types;
            }

            if (assemblyTypes == null)
                continue;

            foreach (Type assemblyType in assemblyTypes)
            {
                if (assemblyType != null && assemblyType.FullName != null &&
                    (assemblyType.Name == typeName || assemblyType.FullName.EndsWith("." + typeName, StringComparison.Ordinal)))
                    return assemblyType;
            }
        }

        return null;
    }


    // =======================================================`
    // ID 필드 설정 (접두사 + 헤더)

    private static readonly Dictionary<string, (string Prefix, int Header)> IdFieldConfigs = new()
    {
        { "MonsterID", ("mon", GameDataHeaders.Monster) },
        { "ObjectID", ("ob", GameDataHeaders.Object) },
        { "SpriteID", ("sp", GameDataHeaders.Sprite) },
        { "ItemID", ("item", GameDataHeaders.Item) },
        { "StatID", ("stat", GameDataHeaders.Stat) },
    };

    private struct AliasRecord
    {
        public string DataType;
        public string Prefix;
        public int PackedId;
        public string NameKey;
    }

    private static readonly Dictionary<int, Dictionary<string, int>> AliasToIdByHeader = new();
    private static readonly Dictionary<int, Dictionary<int, string>> IdToAliasByHeader = new();
    private static readonly List<AliasRecord> AliasRecords = new();

    // =======================================================
    // 핵심 파싱 엔진
    // =======================================================
    private static void ParseData<T>(string csvFilePath, string savePath) where T : ScriptableObject
    {
        //==========================================================
        // Part 1. 전처리
        Debug.Log($"<color=cyan><b>[{typeof(T).Name}] CSV 파싱 시작...</b></color>");
        
        TextAsset csvData = AssetDatabase.LoadAssetAtPath<TextAsset>(csvFilePath);
        if (csvData == null)
        {
            Debug.LogError($"<color=red><b>CSV 파일을 찾을 수 없습니다: {csvFilePath}</b></color>");
            return;
        }

        // 저장 폴더 자동 생성 로직 개선 (중첩 폴더 지원)
        CreateFolderRecursive(savePath);

        // string[] rows = csvData.text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        List<string[]> rows = ReadCSV(csvData.text);
        if (rows.Count <= 1)
        {
            Debug.LogWarning($"<color=yellow><b>CSV 파일에 데이터가 없습니다: {csvFilePath}</b></color>");
            return;
        }

        //==========================================================
        // Part 2. 헤더 매핑 및 리플렉션 준비
        Dictionary<string, int> columnIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        string[] headers = rows[0];
        for (int i = 0; i < headers.Length; i++)
        {
            string headerName = headers[i].Trim();
            if (!string.IsNullOrEmpty(headerName) && !columnIndex.ContainsKey(headerName))
                columnIndex.Add(headerName, i);

            // "DropSoul (SoulID:Weight)" 같은 주석형 헤더를 필드명("DropSoul")으로도 조회 가능하게 만든다.
            int bracketIndex = headerName.IndexOf('(');
            if (bracketIndex > 0)
            {
                string alias = headerName.Substring(0, bracketIndex).Trim();
                if (!string.IsNullOrEmpty(alias) && !columnIndex.ContainsKey(alias))
                    columnIndex.Add(alias, i);
            }
        }

        //==========================================================
        // Part 3. 리플렉션 및 데이터 바인딩
        FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
        int count = 0;
        HashSet<string> validAssetPaths = new HashSet<string>();
        var idFields = new List<(string Name, string Prefix, int Header)>();
        foreach (var entry in IdFieldConfigs)
            if (columnIndex.ContainsKey(entry.Key))
                idFields.Add((entry.Key, entry.Value.Prefix, entry.Value.Header));

        if (idFields.Count == 0)
        {
            Debug.LogWarning($"[{typeof(T).Name}] ID 컬럼이 없어 파싱을 건너뜁니다.");
            return;
        }

        for (int i = 1; i < rows.Count; i++)
        {
            string[] columns = rows[i];

            //==========================================================
            // Step 1. 공통 식별자 (ID, Name) 추출 - 첫 번째 ID 필드를 Primary ID로 사용
            var (primaryFieldName, primaryPrefix, primaryHeader) = idFields[0];
            string idStr = GetColumnValue(columns, columnIndex, primaryFieldName);
            string nameStr = GetColumnValue(columns, columnIndex, "Name");

            if (!TryParseFixedPrefixId(idStr, primaryPrefix, primaryHeader, out int id))
            {
                if (!string.IsNullOrWhiteSpace(idStr))
                    Debug.LogWarning($"[Row:{i}] {primaryFieldName} 형식 오류: {idStr}");
                continue;
            }

            if (string.IsNullOrEmpty(nameStr))
                nameStr = "NoName";

            RegisterAlias(primaryHeader, id, nameStr, typeof(T).Name, primaryPrefix);

            string fullPath = $"{savePath}/{typeof(T).Name}_{id}_{nameStr}.asset";
            validAssetPaths.Add(fullPath);

            T assetData = AssetDatabase.LoadAssetAtPath<T>(fullPath);
            bool isNew = false;
            
            if (assetData == null)
            {
                assetData = ScriptableObject.CreateInstance<T>();
                isNew = true;
            }

            // 기존 에셋 GUID는 유지하되, 이전 파싱 값이 남지 않도록 필드를 기본값으로 초기화
            ResetAssetFields(assetData, fields);

            //==========================================================
            // Step 2. 리플렉션 자동 매핑 (기본 자료형 및 Enum)
            foreach (FieldInfo field in fields)
            {
                string csvValue = GetColumnValue(columns, columnIndex, field.Name);
                if (string.IsNullOrEmpty(csvValue)) continue;

                Type fieldType = field.FieldType;
                try
                {
                    // 1. 기본 자료형
                    if (fieldType == typeof(int) && int.TryParse(csvValue, out int intVal)) field.SetValue(assetData, intVal);
                    else if (fieldType == typeof(float) && float.TryParse(csvValue, out float floatVal)) field.SetValue(assetData, floatVal);
                    else if (fieldType == typeof(string)) field.SetValue(assetData, csvValue);
                    else if (fieldType == typeof(bool)) field.SetValue(assetData, (csvValue == "1" || csvValue.ToLower() == "true" || csvValue.ToLower() == "t"));
                    else if (fieldType.IsEnum && Enum.TryParse(fieldType, csvValue, true, out object enumVal)) field.SetValue(assetData, enumVal);
                    // 2. 특수 자료형
                    else if (fieldType == typeof(List<string>)) field.SetValue(assetData, ParseStringList(csvValue));
                    else if (fieldType == typeof(List<ItemEffect>)) field.SetValue(assetData, ParseEffects(csvValue));
                    else if (fieldType == typeof(List<MonsterSoulDrop>)) field.SetValue(assetData, ParseMonsterSoulDrops(csvValue));
                    else if (fieldType == typeof(List<MonsterItemDrop>)) field.SetValue(assetData, ParseMonsterItemDrops(csvValue));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ID:{id}] {field.Name} 파싱 오류: {e.Message}");
                }
            }

            // Step 2-1. 모든 ID 필드 파싱 및 설정

            foreach (var (fieldName, prefix, header) in idFields)
            {
                string value = GetColumnValue(columns, columnIndex, fieldName);
                if (TryParseIdOrAlias(value, prefix, header, out int packed))
                    typeof(T).GetField(fieldName)?.SetValue(assetData, packed);
                else if (!string.IsNullOrWhiteSpace(value))
                    Debug.LogWarning($"[Row:{i}] {fieldName} 해석 실패: {value} (허용: {prefix}_001 또는 Name Key)");
            }

            if (isNew) AssetDatabase.CreateAsset(assetData, fullPath);
            EditorUtility.SetDirty(assetData);
            count++;
        }

        // =======================================================
        // Part 4. 저장 및 삭제 로직
        // =======================================================
        int deletedCount = 0;
        string[] existingGuids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { savePath });      
        foreach (string guid in existingGuids)
        {
            string existingPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!validAssetPaths.Contains(existingPath))
            {
                AssetDatabase.DeleteAsset(existingPath);
                deletedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"<color=green><b>[{typeof(T).Name}] 파싱 완료! 갱신/생성: {count}개 | 삭제된 더미: {deletedCount}개</b></color>");
    }

    // =======================================================
    // 헬퍼 함수 모음
    // =======================================================
    private static List<string[]> ReadCSV(string text)
    {
        List<string[]> rows = new List<string[]>();
        List<string> columns = new List<string>();
        bool inQuotes = false;
        string currentValue = "";

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '\"')
            {
                // CSV 규칙: 큰따옴표 안에서 "" 는 하나의 " 로 취급함
                if (inQuotes && i + 1 < text.Length && text[i + 1] == '\"')
                {
                    currentValue += '\"';
                    i++; // 이스케이프된 따옴표 건너뛰기
                }
                else
                {
                    inQuotes = !inQuotes; // 따옴표 진입/탈출 토글
                }
            }
            else if (c == ',' && !inQuotes)
            {
                // 따옴표 밖의 쉼표는 컬럼 구분자
                columns.Add(currentValue);
                currentValue = "";
            }
            else if ((c == '\n' || c == '\r') && !inQuotes)
            {
                // 따옴표 밖의 줄바꿈은 행 구분자
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') 
                    i++; // \r\n 형태일 경우 \n 건너뛰기
                
                columns.Add(currentValue);
                rows.Add(columns.ToArray());
                columns.Clear();
                currentValue = "";
            }
            else
            {
                // 일반 문자
                currentValue += c;
            }
        }

        // 마지막 남은 데이터 처리
        if (columns.Count > 0 || !string.IsNullOrEmpty(currentValue))
        {
            columns.Add(currentValue);
            rows.Add(columns.ToArray());
        }

        return rows;
    }

    private static string GetColumnValue(string[] columns, Dictionary<string, int> columnIndexMap, string columnName)
    {
        if (columnIndexMap.TryGetValue(columnName, out int index) && index < columns.Length)
            return columns[index].Trim();
        return string.Empty;
    }

    private static bool TryParseFixedPrefixId(string rawValue, string prefix, int header, out int internalId)
    {
        internalId = 0;

        if (string.IsNullOrWhiteSpace(rawValue))
            return false;

        string value = rawValue.Trim();
        string expectedPrefix = prefix + "_";
        if (!value.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        string numberPart = value.Substring(expectedPrefix.Length);
        if (numberPart.Length == 0)
            return false;

        for (int i = 0; i < numberPart.Length; i++)
        {
            if (!char.IsDigit(numberPart[i]))
                return false;
        }

        if (!int.TryParse(numberPart, out int number))
            return false;

        internalId = BuildPackedId(header, number);
        return true;
    }

    private static bool TryParseIdOrAlias(string rawValue, string prefix, int header, out int packedId)
    {
        packedId = 0;
        if (string.IsNullOrWhiteSpace(rawValue))
            return false;

        if (TryParseFixedPrefixId(rawValue, prefix, header, out packedId))
            return true;

        string alias = rawValue.Trim();
        if (AliasToIdByHeader.TryGetValue(header, out var aliasMap) && aliasMap.TryGetValue(alias, out packedId))
            return true;

        return false;
    }

    private static int BuildPackedId(int header, int number)
    {
        if (number < 0 || number > 0x00FFFFFF)
            return 0;

        return (header << 24) | number;
    }

    private static void CreateFolderRecursive(string path)
    {
        string[] folders = path.Split('/');
        string currentPath = folders[0];
        for (int i = 1; i < folders.Length; i++)
        {
            if (!AssetDatabase.IsValidFolder(currentPath + "/" + folders[i]))
            {
                AssetDatabase.CreateFolder(currentPath, folders[i]);
            }
            currentPath += "/" + folders[i];
        }
    }

    private static void ResetAssetFields<T>(T assetData, FieldInfo[] fields) where T : ScriptableObject
    {
        foreach (FieldInfo field in fields)
        {
            Type fieldType = field.FieldType;

            if (fieldType.IsValueType)
            {
                field.SetValue(assetData, Activator.CreateInstance(fieldType));
            }
            else if (fieldType == typeof(string))
            {
                field.SetValue(assetData, string.Empty);
            }
            else if (fieldType.IsGenericType)
            {
                Type genericDef = fieldType.GetGenericTypeDefinition();
                if (genericDef == typeof(List<>) || genericDef == typeof(Dictionary<,>))
                    field.SetValue(assetData, Activator.CreateInstance(fieldType));
                else
                    field.SetValue(assetData, null);
            }
            else
            {
                field.SetValue(assetData, null);
            }
        }
    }

    // =======================================================
    // 커스텀 리스트 파서
    // =======================================================
    private static List<ItemEffect> ParseEffects(string data)
    {
        List<ItemEffect> list = new List<ItemEffect>();
        foreach (string eff in data.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string[] split = eff.Split(':');
            if (split.Length != 2)
                continue;

            string statToken = split[0].Trim();
            if (!int.TryParse(split[1], out int val))
                continue;

            if (TryResolveStatId(statToken, out int statId))
                list.Add(new ItemEffect { StatID = statId, Value = val });
            else
                Debug.LogWarning($"EffectStats StatID 해석 실패: {statToken} (허용: stat_001, StatData Name Key)");
        }
        return list;
    }

    private static bool TryResolveStatId(string token, out int statId)
    {
        statId = 0;
        if (string.IsNullOrWhiteSpace(token))
            return false;

        return TryParseIdOrAlias(token, "stat", GameDataHeaders.Stat, out statId);
    }

    private static void RegisterAlias(int header, int packedId, string nameKey, string dataType, string prefix)
    {
        if (string.IsNullOrWhiteSpace(nameKey))
            return;

        string trimmed = nameKey.Trim();

        if (!AliasToIdByHeader.TryGetValue(header, out var aliasToId))
        {
            aliasToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            AliasToIdByHeader.Add(header, aliasToId);
        }

        if (!IdToAliasByHeader.TryGetValue(header, out var idToAlias))
        {
            idToAlias = new Dictionary<int, string>();
            IdToAliasByHeader.Add(header, idToAlias);
        }

        aliasToId[trimmed] = packedId;
        idToAlias[packedId] = trimmed;

        for (int i = 0; i < AliasRecords.Count; i++)
        {
            if (AliasRecords[i].PackedId == packedId)
                return;
        }

        AliasRecords.Add(new AliasRecord
        {
            DataType = dataType,
            Prefix = prefix,
            PackedId = packedId,
            NameKey = trimmed
        });
    }

    private static void ResetAliasCaches()
    {
        AliasToIdByHeader.Clear();
        IdToAliasByHeader.Clear();
        AliasRecords.Clear();
    }

    private static void ExportAliasMapTables()
    {
        CreateFolderRecursive(GeneratedMapFolder);
        string generatedDiskFolder = Path.Combine(Application.dataPath, "Datas/_Generated");

        StringBuilder all = new StringBuilder();
        all.AppendLine("DataType,Prefix,PackedID,Header,Number,NameKey");

        StringBuilder item = new StringBuilder();
        item.AppendLine("PackedID,Number,NameKey");

        StringBuilder stat = new StringBuilder();
        stat.AppendLine("PackedID,Number,NameKey");

        for (int i = 0; i < AliasRecords.Count; i++)
        {
            var r = AliasRecords[i];
            int header = GameDataID.GetHeader(r.PackedId);
            int number = GameDataID.GetNumber(r.PackedId);
            string escapedName = EscapeCsv(r.NameKey);

            all.AppendLine($"{r.DataType},{r.Prefix},{r.PackedId},{header},{number},{escapedName}");

            if (string.Equals(r.DataType, nameof(ItemData), StringComparison.Ordinal))
                item.AppendLine($"{r.PackedId},{number},{escapedName}");
            else if (string.Equals(r.DataType, nameof(StatData), StringComparison.Ordinal))
                stat.AppendLine($"{r.PackedId},{number},{escapedName}");
        }

        File.WriteAllText(Path.Combine(generatedDiskFolder, "ID_Name_Map_All.csv"), all.ToString(), Encoding.UTF8);
        File.WriteAllText(Path.Combine(generatedDiskFolder, "ID_Name_Map_Item.csv"), item.ToString(), Encoding.UTF8);
        File.WriteAllText(Path.Combine(generatedDiskFolder, "ID_Name_Map_Stat.csv"), stat.ToString(), Encoding.UTF8);

        AssetDatabase.Refresh();
        Debug.Log($"<color=cyan><b>ID/Name 매핑 테이블 출력 완료: {GeneratedMapFolder}</b></color>");
    }

    private static string EscapeCsv(string value)
    {
        if (value == null)
            return string.Empty;

        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            return "\"" + value.Replace("\"", "\"\"") + "\"";

        return value;
    }

    private static List<string> ParseStringList(string data)
    {
        List<string> list = new List<string>();
        foreach (string entry in data.Split(';'))
        {
            string value = entry.Trim();
            if (!string.IsNullOrEmpty(value))
                list.Add(value);
        }
        return list;
    }

    private static List<MonsterSoulDrop> ParseMonsterSoulDrops(string data)
    {
        List<MonsterSoulDrop> list = new List<MonsterSoulDrop>();
        foreach (string entry in data.Split(';'))
        {
            string value = entry.Trim();
            if (string.IsNullOrEmpty(value))
                continue;

            string[] split = value.Split(':');
            if (split.Length != 2)
                continue;

            if (!int.TryParse(split[1], out int weight))
                continue;

            string soulId = split[0].Trim();
            if (!TryParseIdOrAlias(soulId, "soul", GameDataHeaders.Soul, out int soulId_packed))
            {
                Debug.LogWarning($"DropSoul ID 형식 오류 (예: soul_0001 또는 Name Key): {soulId}");
                continue;
            }

            list.Add(new MonsterSoulDrop
            {
                SoulID = soulId_packed,
                Weight = weight
            });
        }

        return list;
    }

    private static List<MonsterItemDrop> ParseMonsterItemDrops(string data)
    {
        List<MonsterItemDrop> list = new List<MonsterItemDrop>();
        foreach (string entry in data.Split(';'))
        {
            string value = entry.Trim();
            if (string.IsNullOrEmpty(value))
                continue;

            string[] split = value.Split(':');
            if (split.Length != 3)
                continue;

            if (!int.TryParse(split[1], out int weight) || !int.TryParse(split[2], out int count))
                continue;

            string itemId = split[0].Trim();
            if (!TryParseIdOrAlias(itemId, "item", GameDataHeaders.Item, out int itemId_packed))
            {
                Debug.LogWarning($"DropItems ID 형식 오류 (예: item_0001 또는 Name Key): {itemId}");
                continue;
            }

            list.Add(new MonsterItemDrop
            {
                ItemID = itemId_packed,
                Weight = weight,
                Count = count
            });
        }

        return list;
    }
}