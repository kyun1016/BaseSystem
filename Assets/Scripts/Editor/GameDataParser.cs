using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;

public class GameDataParser : EditorWindow
{
    private static readonly string[] DataNames = { "Item", "Stat", "Monster" };

    [MenuItem("Tools/Data Parse/Parse All CSV")]
    public static void ParseAllCSV()
    {
        foreach (string dataName in DataNames)
        {
            ParseByDataName(dataName);
        }
    }

    [MenuItem("Tools/Data Parse/Select CSV")]
    public static void ShowParseMenu()
    {
        GenericMenu menu = new GenericMenu();
        foreach (string dataName in DataNames)
        {
            string capturedName = dataName;
            menu.AddItem(new GUIContent($"{capturedName} CSV"), false, () => ParseByDataName(capturedName));
        }
        menu.ShowAsContext();
    }

    private static void ParseByDataName(string dataName)
    {
        string typeName = $"{dataName}Data";
        Type dataType = ResolveType(typeName);
        if (dataType == null || !typeof(ScriptableObject).IsAssignableFrom(dataType))
        {
            Debug.LogError($"<color=red><b>타입을 찾을 수 없습니다: {typeName}</b></color>");
            return;
        }

        string csvPath = $"Assets/Datas/{dataName}.csv";
        string savePath = $"Assets/Resources/{dataName}Data";

        MethodInfo parseMethod = typeof(GameDataParser).GetMethod(nameof(ParseData), BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo genericParseMethod = parseMethod.MakeGenericMethod(dataType);
        genericParseMethod.Invoke(null, new object[] { csvPath, savePath });
    }

    private static Type ResolveType(string typeName)
    {
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
                if (assemblyType != null && assemblyType.Name == typeName)
                    return assemblyType;
            }
        }

        return null;
    }


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
        }

        //==========================================================
        // Part 3. 리플렉션 및 데이터 바인딩
        FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
        int count = 0;
        HashSet<string> validAssetPaths = new HashSet<string>();
        for (int i = 1; i < rows.Count; i++)
        {
            string[] columns = rows[i];

            //==========================================================
            // Step 1. 공통 식별자 (ID, Name) 추출
            string idStr = GetColumnValue(columns, columnIndex, "ID");
            string nameStr = GetColumnValue(columns, columnIndex, "Name");
            if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out int id)) continue;

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
                    else if (fieldType == typeof(List<ItemEffect>)) field.SetValue(assetData, ParseEffects(csvValue));
                    else if (fieldType == typeof(List<ItemCondition>)) field.SetValue(assetData, ParseConditions(csvValue));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ID:{id}] {field.Name} 파싱 오류: {e.Message}");
                }
            }

            //==========================================================
            // Step 3. 데이터 저장
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
        foreach (string eff in data.Split('|'))
        {
            string[] split = eff.Split(':');
            if (split.Length == 2 && Enum.TryParse(split[0], out StatType stat) && float.TryParse(split[1], out float val))
                list.Add(new ItemEffect { Stat = stat, Value = val });
        }
        return list;
    }

    private static List<ItemCondition> ParseConditions(string data)
    {
        List<ItemCondition> list = new List<ItemCondition>();
        foreach (string cond in data.Split('|'))
        {
            string[] split = cond.Split(':');
            if (split.Length == 2 && Enum.TryParse(split[0], out ConditionType condType))
                list.Add(new ItemCondition { Condition = condType, Value = split[1] });
        }
        return list;
    }
}