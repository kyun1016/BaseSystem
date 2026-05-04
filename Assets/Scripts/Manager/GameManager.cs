using UnityEngine;
using System;
using System.Collections.Generic;

public class GameManager : Singleton<GameManager>
{
    public bool isInit = false; // 게임 시작 여부를 추적하는 변수
    public event Action OnInitialized;

    protected override void Awake()
    {
        base.Awake();

        // 상태 관리 먼저 초기화 (다른 매니저들이 의존)
        GameStateManager.Instance.Init();
        
        // 입력 시스템 초기화
        InputManager.Instance.Init();

        // 게임 데이터 초기화
        DataManager.Instance.Init();
        PlayerStatManager.Instance.Init();
        InventoryManager.Instance.Init();
        DialogueManager.Instance.Init();

        // 던전 시스템 초기화 (Input 이벤트 구독)
        DungeonManager.Instance.Init();

        // UI 시스템 초기화 (Input/GameState 이벤트 구독)
        UIManager.Instance.Init();

        isInit = true;
        OnInitialized?.Invoke();
    }
}