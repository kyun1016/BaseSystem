using UnityEngine;
using System;
using System.Collections.Generic;

public class DialogueManager : Singleton<DialogueManager>
{
    public static readonly int BASE_KEY = (int)eHeader.Dialogue * BaseData.HEADER_SIZE;
    private List<DialogueData> historyData = new();
    private DialogueGroupData currentData = null;

    // UI 갱신을 위한 이벤트 방송국 (아이템 ID, 슬롯 인덱스, 추가 여부)
    public event Action<int, int, bool> OnChanged;

    public void Init()
    {
        // 추후 세이브 파일이 있다면 여기서 historyData를 로드해서 덮어씌웁니다.
        historyData.Clear();
    }
}