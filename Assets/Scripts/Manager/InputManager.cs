using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class InputManager : Singleton<InputManager>
{
    // 입력 이벤트 정의
    public event Action<Vector2Int> OnMove;           // 방향키 입력 (↑↓←→)
    public event Action OnInventoryToggle;             // I 키
    public event Action OnStatToggle;                  // S 키
    public event Action OnUIClose;                     // ESC 키

    public void Init()
    {
        Debug.Log("[InputManager] 입력 시스템 초기화 (Input System 패키지 사용)");
    }

    private void Update()
    {
        if (!GameManager.Instance.isInit) return;

        // 게임 상태에 따른 입력 처리
        GameState currentState = GameStateManager.Instance.GetCurrentState();

        switch (currentState)
        {
            case GameState.Dungeon:
                HandleDungeonInput();
                break;

            case GameState.UI:
                HandleUIInput();
                break;

            case GameState.Battle:
                HandleBattleInput();
                break;

            case GameState.Paused:
                HandlePausedInput();
                break;
        }
    }

    // =======================================================
    // 필드 탐험 상태 입력
    // =======================================================
    private void HandleDungeonInput()
    {
        // 이동 입력 (화살표 또는 WASD)
        Vector2Int moveDir = Vector2Int.zero;
        
        if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
            moveDir = Vector2Int.up;
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
            moveDir = Vector2Int.down;
        else if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
            moveDir = Vector2Int.left;
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
            moveDir = Vector2Int.right;

        if (moveDir != Vector2Int.zero)
        {
            OnMove?.Invoke(moveDir);
        }

        // UI 토글 (주의: S 키는 이동과 충돌 가능 - 우선순위 처리)
        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            OnInventoryToggle?.Invoke();
        }

        // S 키는 WASD 이동 입력이므로 별도 처리
        // 만약 S 키로 스탯창을 열길 원한다면 다른 키로 변경 권장 (예: Tab, T)
    }

    // =======================================================
    // UI 상태 입력
    // =======================================================
    private void HandleUIInput()
    {
        // ESC 또는 다시 눌은 키로 UI 닫기
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            OnUIClose?.Invoke();
        }

        // I 키로 인벤토리 토글
        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            OnInventoryToggle?.Invoke();
        }

        // Tab 키로 스탯창 토글 (S와 충돌하므로 변경)
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            OnStatToggle?.Invoke();
        }
    }

    // =======================================================
    // 전투 상태 입력 (추후 구현)
    // =======================================================
    private void HandleBattleInput()
    {
        // TODO: 스킬 선택, 아이템 사용 등
        // if (Keyboard.current.digit1Key.wasPressedThisFrame) { /* 스킬 1 */ }
    }

    // =======================================================
    // 일시 정지 상태 입력 (추후 구현)
    // =======================================================
    private void HandlePausedInput()
    {
        // TODO: 일시 정지 해제 등
    }
}
