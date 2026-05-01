using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Text;

public class GameDataParser : EditorWindow
{
    private const string GeneratedMapFolder = "Assets/Datas/_Generated";
    private static readonly Dictionary<string, int> NameToKey = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<int, string> KeyToName = new();

    // =======================================================
    // 파싱 설정
    private static readonly string[] DataNames = { "Stat", "Item", "Monster" };
        [MenuItem("Tools/Data Parse/Parse All CSV")]
    public static void ParseAllCSV()
    {
        {       
            ResetMapCaches();
            foreach (string dataName in DataNames)
            {
                ParseMapByDataName(dataName);
            }
            ExportKeyNameMap();
        }

        foreach (string dataName in DataNames)
        {
            ParseByDataName(dataName);
        }

        
    }

    private static void ParseMapByDataName(string dataName)
    {
        string typeName = $"{dataName}Data";
        string csvPath = $"Assets/Datas/{dataName}.csv";
        string savePath = $"Assets/Resources/{dataName}Data";

        TextAsset csvData = AssetDatabase.LoadAssetAtPath<TextAsset>(csvPath);
        if (csvData == null)
        {
            Debug.LogError($"<color=red><b>CSV 파일을 찾을 수 없습니다: {csvPath}</b></color>");
            return;
        }

        List<string[]> rows = ReadCSV(csvData.text);
        if (rows.Count <= 1)
        {
            Debug.LogWarning($"<color=yellow><b>CSV 파일에 데이터가 없습니다: {csvPath}</b></color>");
            return;
        }

        for (int i = 1; i < rows.Count; i++)
        {
            string[] columns = rows[i];
            string id = columns[0];
            string name = $"{dataName}_{columns[1]}";

            int key = int.Parse(id) + (int)(eHeader)Enum.Parse(typeof(eHeader), dataName, true) * BaseData.HEADER_SIZE;
            string trimmed = name.Trim();
            NameToKey[trimmed] = key;
            KeyToName[key] = trimmed;
        }
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
        genericParseMethod.Invoke(null, new object[] { dataName });
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

    // =======================================================
    // 핵심 파싱 엔진
    // =======================================================
    private static void ParseData<T>(string dataName) where T : ScriptableObject
    {
        //==========================================================
        // Part 1. 전처리
        Debug.Log($"<color=cyan><b>[{typeof(T).Name}] CSV 파싱 시작...</b></color>");
        Debug.Log($"<color=cyan><b>[{dataName}] CSV 파싱 시작...</b></color>");
        string csvPath = $"Assets/Datas/{dataName}.csv";
        string savePath = $"Assets/Resources/{dataName}Data";
        
        TextAsset csvData = AssetDatabase.LoadAssetAtPath<TextAsset>(csvPath);
        if (csvData == null)
        {
            Debug.LogError($"<color=red><b>CSV 파일을 찾을 수 없습니다: {csvPath}</b></color>");
            return;
        }

        // 저장 폴더 자동 생성 로직 개선 (중첩 폴더 지원)
        CreateFolderRecursive(savePath);

        List<string[]> rows = ReadCSV(csvData.text);
        if (rows.Count <= 1)
        {
            Debug.LogWarning($"<color=yellow><b>CSV 파일에 데이터가 없습니다: {csvPath}</b></color>");
            return;
        }

        //==========================================================
        // Part 2. 헤더 매핑 및 리플렉션 준비
        FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
        HashSet<string> validAssetPaths = new HashSet<string>();
        int count = 0;
        //==========================================================
        // Part 3. 리플렉션 및 데이터 바인딩
        for (int i = 1; i < rows.Count; i++)
        {
            string[] columns = rows[i];
            string id = columns[0];
            string name = columns[1];

            string fullPath = $"{savePath}/{typeof(T).Name}_{id}_{name}.asset";
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
            int fieldIndex = 0;
            for (int j = 0; j < columns.Length; j++)
            {
                FieldInfo field = fields[fieldIndex++];
                string csvValue = columns[j];
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
                    else if (fieldType == typeof(LocalizedString))
                    {
                        LocalizedString locStr = new LocalizedString
                        {
                            EN = csvValue,
                            KR = columns[++j],
                            // 추가 언어는 여기서 확장...
                        };
                        field.SetValue(assetData, locStr);
                    }
                    else if (fieldType == typeof(BaseData)) {
                        LocalizedString locStr = new LocalizedString {
                                EN = columns[++j],
                                KR = columns[++j],
                                // 추가 언어는 여기서 확장...
                        };
                        BaseData data = new BaseData(int.Parse(csvValue), (eHeader)Enum.Parse(typeof(eHeader), dataName, true), locStr);
                        field.SetValue(assetData, data);
                    }
                    else if (fieldType == typeof(List<string>)) field.SetValue(assetData, ParseStringList(csvValue));
                    else if (fieldType == typeof(List<ReferenceData>))
                    {
                        string header = rows[0][j].Trim();
                        int bracketOpen = header.IndexOf('[');
                        int bracketClose = header.IndexOf(']');
                        if( (bracketOpen < 0 || bracketClose <= bracketOpen)) // [Type] 명세 또는 "Name:Value;Name:Value" 형식 감지
                        {
                            Debug.LogError($"필드 '{field.Name}'의 헤더 형식이 잘못되었습니다. 'FieldName [Type]' 형식으로 수정하세요. (현재: '{header}')");
                        }
                        string type = header.Substring(bracketOpen + 1, bracketClose - bracketOpen - 1).Trim();
                        field.SetValue(assetData, ParseReferenceList(type, csvValue));
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ID:{id}] {field.Name} 파싱 오류: {e.Message}");
                }
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
    private static void ResetMapCaches()
    {
        NameToKey.Clear();
        KeyToName.Clear();
    }

    private static void ExportKeyNameMap()
    {
        CreateFolderRecursive(GeneratedMapFolder);
        string generatedDiskFolder = Path.Combine(Application.dataPath, "Datas/_Generated");

        StringBuilder all = new StringBuilder();
        all.AppendLine("Key,Name");
        foreach (var pair in KeyToName)
            all.AppendLine($"{pair.Key},{pair.Value}");

        File.WriteAllText(Path.Combine(generatedDiskFolder, "Key_Name_Map.csv"), all.ToString(), Encoding.UTF8);

        AssetDatabase.Refresh();
        Debug.Log($"<color=cyan><b>ID/Name 매핑 테이블 출력 완료: {GeneratedMapFolder} ({KeyToName.Count}개)</b></color>");
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

    private static List<ReferenceData> ParseReferenceList(string type, string data)
    {
        List<ReferenceData> list = new List<ReferenceData>();
        foreach (string entry in data.Split(';'))
        {
            string[] split = entry.Split(':');
            if (split.Length != 2)
                continue;

            if (!int.TryParse(split[1], out int weight))
                continue;

            string name = $"{type}_{split[0].Trim()}";
            int value = int.Parse(split[1]);

            int key = NameToKey.TryGetValue(name, out int cachedKey) ? cachedKey : -1;
            if (key == -1)
            {
                Debug.LogWarning($"참조 이름을 찾을 수 없습니다: {name}");
                continue;
            }

            list.Add(new ReferenceData
            {
                Key = key,
                KeyName = name,
                Name = split[0].Trim(),
                Value = value
            });
        }

        return list;
    }
}