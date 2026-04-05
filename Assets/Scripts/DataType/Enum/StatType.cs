using UnityEngine;
using System.Collections.Generic;

// 스탯 타입 정의 (CSV의 'Effects' 항목에 들어가는 Health 등과 일치해야 합니다)
public enum StatType
{
    None,
    Stamina,
    Strength,
    Intelligence,
    Elegance,
    Charm,
    Ethics,
    Faith,
    Karma,
    Sensitivity,
    Stress,
    CombatSkill,
    Attack,
    Defense,
    MagicSkill,
    MagicPower,
    AntiMagic,
    Etiquette,
    Art,
    Speech,
    Cooking,
    Housework,
    Temperament,
    Motherhood,
    RelationPrince,
    RelationFather,
    RelationButler,
    RelationDemonKing,
    KillCount,

    MAX_COUNT
    // 필요에 따라 추가
}