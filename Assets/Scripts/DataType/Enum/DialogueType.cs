using UnityEngine;
using System.Collections.Generic;

// 대화 타입 정의 (CSV의 'Type' 항목에 들어가는 값과 일치해야 합니다)
public enum eDialogueType
{
    Line,
    Choice,
    Action,
    End,
}

public enum eDialogueAction { 
    None,
    SetActive,
    PlayVideo,
    PlaySound,
    FadeIn,
    FadeOut
}