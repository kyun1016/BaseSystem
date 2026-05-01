using UnityEngine;
using System.Collections.Generic;

// 아이템 타입 정의 (CSV의 'ItemType' 항목에 들어가는 값과 일치해야 합니다)
public enum eItemType
{
    None,
    Equip_Helmet,
    Equip_Armor,
    Equip_Weapon,
    Consumable,
    MAX_COUNT
    // 필요에 따라 추가
}