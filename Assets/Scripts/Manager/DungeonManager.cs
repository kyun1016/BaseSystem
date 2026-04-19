using UnityEngine;
using System;

public class DungeonManager : Singleton<DungeonManager>
{
    private int currentTurn = 0;
    private Vector2Int playerPosition = Vector2Int.zero;

    // 턴 경과 이벤트
    public event Action<int> OnTurnPassed;          // 턴 경과 시
    public event Action<Vector2Int> OnPlayerMoved;  // 플레이어 이동 시

    public void Init()
    {
        currentTurn = 0;
        playerPosition = Vector2Int.zero;

        // InputManager의 이동 이벤트 구독
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnMove += HandlePlayerMove;
        }

        Debug.Log("[DungeonManager] 던전 시스템 초기화");
    }

    // private void OnDestroy()
    // {
    //     // 구독 해제
    //     if (InputManager.Instance != null)
    //     {
    //         InputManager.Instance.OnMove -= HandlePlayerMove;
    //     }
    // }

    // =======================================================
    // 플레이어 이동 처리
    // =======================================================
    private void HandlePlayerMove(Vector2Int direction)
    {
        // 필드 탐험 상태만 이동 가능
        if (!GameStateManager.Instance.IsDungeonState()) return;

        // 플레이어 위치 업데이트 (실제 이동 검증, 벽 충돌 등은 여기서 처리)
        Vector2Int newPosition = playerPosition + direction;

        // TODO: 벽, 장애물 충돌 검사
        // if (IsWalkable(newPosition))
        playerPosition = newPosition;

        // 이동 이벤트 발행
        OnPlayerMoved?.Invoke(playerPosition);
        Debug.Log($"[DungeonManager] 플레이어 이동: {direction} → 위치({playerPosition.x}, {playerPosition.y})");

        // 턴 경과
        PassTurn();
    }

    // =======================================================
    // 턴 시스템
    // =======================================================
    private void PassTurn()
    {
        currentTurn++;
        OnTurnPassed?.Invoke(currentTurn);
        Debug.Log($"[DungeonManager] 턴 경과: {currentTurn}");

        // TODO: 여기서 던전 로직 진행
        // - 적 이동/공격
        // - 시간 경과 효과 (독, 버프 등)
        // - 랜덤 이벤트 등
    }

    // =======================================================
    // 상태 조회
    // =======================================================
    public int GetCurrentTurn() => currentTurn;
    public Vector2Int GetPlayerPosition() => playerPosition;
    public void ResetDungeon()
    {
        currentTurn = 0;
        playerPosition = Vector2Int.zero;
        Debug.Log("[DungeonManager] 던전 초기화");
    }
}
