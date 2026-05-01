using UnityEngine;
using System.Collections.Generic;

// 조건 타입 정의 (CSV의 'ConditionType' 값들과 일치해야 합니다)
public enum eConditionType
{
    None,
    Item,
    Stat_Stress,
    Stat_Health,
    Stat_Charm,
    MAX_COUNT
    // 필요에 따라 추가: CONSUMABLE, EQUIP_WEAPON 등
}