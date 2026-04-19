using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using IngameDebugConsole; // 릴리즈 빌드에서는 이 네임스페이스 참조조차 날려버립니다.
#endif

public class DebugManager : Singleton<DebugManager>
{
    [Header("Debug Settings")]
    public bool showFPS = true;

    protected override void Awake()
    {
        base.Awake();
        InitializeDebugger();
    }

    private void InitializeDebugger()
    {
// 에디터 환경이거나, 빌드 셋팅에서 'Development Build'를 체크했을 때만 컴파일되는 영역
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("<color=cyan><b>[DebugManager] 디버그 모드가 활성화되었습니다.</b></color>");
#else
        // 실제 라이브(Release) 빌드에서는 터미널을 생성하지 않고, 이 매니저 자체를 파괴하거나 비활성화할 수도 있습니다.
        Debug.Log("Release 빌드입니다. 디버그 기능을 비활성화합니다.");
        Destroy(gameObject); 
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // ==========================================
    // 터미널 명령어 등록 구역 (릴리즈 빌드에선 완벽히 삭제됨)
    // ==========================================

    [ConsoleMethod("add_item", "아이템을 강제로 획득합니다.")]
    public static void Cheat_AddItem(int itemID)
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(itemID);
            Debug.Log($"[Cheat] 아이템 {itemID}을(를) 1개 추가했습니다.");
        }
    }
    [ConsoleMethod("add_item", "아이템을 강제로 획득합니다.")]
    public static void Cheat_AddItem(int itemID, int amount = 1)
    {
        if (InventoryManager.Instance != null)
        {
            for (int i = 0; i < amount; i++)
            {
                InventoryManager.Instance.AddItem(itemID);
            }
            Debug.Log($"[Cheat] 아이템 {itemID}을(를) {amount}개 추가했습니다.");
        }
    }

    [ConsoleMethod("add_stat", "특정 스탯 값을 강제로 증가시킵니다.")]
    public static void Cheat_AddStat(StatType type, int amount = 1)
    {
        if (PlayerStatManager.Instance != null)
        {
            PlayerStatManager.Instance.AddStat(type, amount);
            Debug.Log($"[Cheat] {type} 스탯을 {amount}(으)로 증가시켰습니다.");
        }
    }

        [ConsoleMethod("set_stat", "특정 스탯 값을 강제로 설정합니다.")]
    public static void Cheat_SetStat(StatType type, int amount = 1)
    {
        if (PlayerStatManager.Instance != null)
        {
            PlayerStatManager.Instance.SetStat(type, amount);
            Debug.Log($"[Cheat] {type} 스탯을 {amount}(으)로 설정했습니다.");
        }
    }
#endif
}