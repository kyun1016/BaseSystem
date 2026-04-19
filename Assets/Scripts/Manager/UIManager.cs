using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    // UI 창 참조
    private GameObject inventoryWindow;
    private GameObject statWindow;

    // UI 상태
    private bool isInventoryOpen;
    private bool isStatOpen;
    private GameObject currentOpenWindow;

    public void Init()
    {
        // 씬에서 UI 창 찾기 (Deactive 상태도 찾을 수 있음)
        inventoryWindow = FindUIWindow("UI_InventoryWindow");
        statWindow = FindUIWindow("UI_StatWindowRoot");

        // 초기 상태는 모두 닫혀 있음
        if (inventoryWindow != null) inventoryWindow.SetActive(false);
        if (statWindow != null) statWindow.SetActive(false);

        isInventoryOpen = false;
        isStatOpen = false;
        currentOpenWindow = null;

        // InputManager 이벤트 구독
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnInventoryToggle += HandleInventoryToggle;
            InputManager.Instance.OnStatToggle += HandleStatToggle;
            InputManager.Instance.OnUIClose += HandleUIClose;
        }

        // GameStateManager 상태 변경 이벤트 구독
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged += HandleStateChanged;
        }

        Debug.Log("[UIManager] 초기화 완료");
    }

    // private void OnDestroy()
    // {
    //     // 구독 해제
    //     if (InputManager.Instance != null)
    //     {
    //         InputManager.Instance.OnInventoryToggle -= HandleInventoryToggle;
    //         InputManager.Instance.OnStatToggle -= HandleStatToggle;
    //         InputManager.Instance.OnUIClose -= HandleUIClose;
    //     }

    //     if (GameStateManager.Instance != null)
    //     {
    //         GameStateManager.Instance.OnStateChanged -= HandleStateChanged;
    //     }
    // }

    // =======================================================
    // 입력 이벤트 핸들러
    // =======================================================
    private void HandleInventoryToggle()
    {
        if (inventoryWindow == null) return;

        if (isInventoryOpen)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }

    private void HandleStatToggle()
    {
        if (statWindow == null) return;

        if (isStatOpen)
        {
            CloseStatWindow();
        }
        else
        {
            OpenStatWindow();
        }
    }

    private void HandleUIClose()
    {
        if (isInventoryOpen) CloseInventory();
        else if (isStatOpen) CloseStatWindow();
    }

    private void HandleStateChanged(GameState prevState, GameState newState)
    {
        // 필드로 복귀할 때 열려있는 UI 자동 닫기 (선택사항)
        if (newState == GameState.Dungeon && (isInventoryOpen || isStatOpen))
        {
            CloseAllUI();
        }
    }

    // =======================================================
    // UI 제어 함수
    // =======================================================
    public void OpenInventory()
    {
        if (inventoryWindow == null || isInventoryOpen) return;

        // 다른 UI가 열려있으면 먼저 닫기
        if (isStatOpen) CloseStatWindow();

        isInventoryOpen = true;
        currentOpenWindow = inventoryWindow;
        inventoryWindow.SetActive(true);
        Debug.Log("[UIManager] 인벤토리 열기");
    }

    public void CloseInventory()
    {
        if (inventoryWindow == null || !isInventoryOpen) return;

        isInventoryOpen = false;
        if (currentOpenWindow == inventoryWindow) currentOpenWindow = null;
        inventoryWindow.SetActive(false);
        Debug.Log("[UIManager] 인벤토리 닫기");
    }

    public void OpenStatWindow()
    {
        if (statWindow == null || isStatOpen) return;

        // 다른 UI가 열려있으면 먼저 닫기
        if (isInventoryOpen) CloseInventory();

        isStatOpen = true;
        currentOpenWindow = statWindow;
        statWindow.SetActive(true);
        Debug.Log("[UIManager] 스탯창 열기");
    }

    public void CloseStatWindow()
    {
        if (statWindow == null || !isStatOpen) return;

        isStatOpen = false;
        if (currentOpenWindow == statWindow) currentOpenWindow = null;
        statWindow.SetActive(false);
        Debug.Log("[UIManager] 스탯창 닫기");
    }

    public void CloseAllUI()
    {
        if (isInventoryOpen) CloseInventory();
        if (isStatOpen) CloseStatWindow();
    }

    // =======================================================
    // 헬퍼 함수
    // =======================================================
    private GameObject FindUIWindow(string windowName)
    {
        // 방법 1: GameObject.Find() - 활성화된 GameObject만 찾음
        GameObject window = GameObject.Find(windowName);
        
        // 방법 2: Find 실패 시, Canvas 하위에서 검색 (Deactive도 찾음)
        if (window == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform childTransform = canvas.transform.Find(windowName);
                if (childTransform != null)
                {
                    window = childTransform.gameObject;
                }
            }
        }

        // 방법 3: 마지막 시도 - 모든 UI 컴포넌트 검색 (Deactive도 포함)
        if (window == null)
        {
            if (windowName == "UI_InventoryWindow")
            {
                UI_InventoryWindow inventoryUI = FindFirstObjectByType<UI_InventoryWindow>(FindObjectsInactive.Include);
                if (inventoryUI != null)
                    window = inventoryUI.gameObject;
            }
            else if (windowName == "UI_StatWindowRoot")
            {
                UI_StatWindowRoot statRoot = FindFirstObjectByType<UI_StatWindowRoot>(FindObjectsInactive.Include);
                if (statRoot != null)
                    window = statRoot.gameObject;
            }
        }

        if (window == null)
        {
            Debug.LogWarning($"[UIManager] UI 창을 찾을 수 없습니다: {windowName}");
        }
        else
        {
            Debug.Log($"[UIManager] UI 창 찾음: {windowName} (Active: {window.activeSelf})");
        }

        return window;
    }

    // UI 상태 조회
    public bool IsInventoryOpen => isInventoryOpen;
    public bool IsStatWindowOpen => isStatOpen;
}
