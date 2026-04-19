using UnityEngine;
using System;

// 게임 상태 정의
public enum GameState
{
    UI,         // UI 창 열림 (인벤토리, 스탯 등)
    Dungeon,    // 필드 탐험 중
    Battle,     // 전투 중 (추후 구현)
    Paused      // 일시 정지
}

public class GameStateManager : Singleton<GameStateManager>
{
    private GameState currentState = GameState.UI;

    // 상태 변경 이벤트
    public event Action<GameState, GameState> OnStateChanged;

    public void Init()
    {
        currentState = GameState.UI;
        Debug.Log($"[GameStateManager] 게임 상태 초기화: {currentState}");
    }

    // =======================================================
    // 상태 관리
    // =======================================================
    public GameState GetCurrentState()
    {
        return currentState;
    }

    public void ChangeState(GameState newState)
    {
        if (currentState == newState) return;

        GameState prevState = currentState;
        currentState = newState;

        Debug.Log($"[GameStateManager] 상태 변경: {prevState} → {currentState}");
        OnStateChanged?.Invoke(prevState, currentState);
    }

    // 상태 확인 헬퍼 함수
    public bool IsDungeonState() => currentState == GameState.Dungeon;
    public bool IsUIState() => currentState == GameState.UI;
    public bool IsBattleState() => currentState == GameState.Battle;
    public bool IsPausedState() => currentState == GameState.Paused;
}
