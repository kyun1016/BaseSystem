using UnityEngine;
using System.Collections.Generic;

public class UI_StatWindow : MonoBehaviour
{
    [SerializeField] private GameObject windowRoot;
    [SerializeField] private GameObject statItemPrefab;
    [SerializeField] private Transform statContentParent; // ScrollView의 Content
    [SerializeField] private eStatCategory statCategoryFilter; // 표시할 스텟 카테고리 필터

    // 생성된 프리팹들을 추적하기 위한 딕셔너리
    private Dictionary<eStatType, UI_StatItem> spawnedItems = new Dictionary<eStatType, UI_StatItem>();
    private bool isInitialized = false;
    private bool isWindowInitialized = false;

    private void Awake()
    {
        if (windowRoot == null)
        {
            UI_StatWindowRoot statRoot = GetComponentInParent<UI_StatWindowRoot>(true);
            windowRoot = statRoot != null ? statRoot.gameObject : gameObject;
        }

        PlayerStatManager.Instance.OnStatChanged += HandleStatChanged;
        isInitialized = true;
    }

    private void OnEnable()
    {
        Sync();
    }

    private void OnDisable()
    {
        // 창이 비활성화되어도 구독은 유지 (계속 이벤트 감지)
        // UI 업데이트만 하지 않음
    }

    // private void OnDestroy()
    // {
    //     // 완전히 파괴될 때만 구독 해제
    //     if (PlayerStatManager.Instance != null)
    //     {
    //         PlayerStatManager.Instance.OnStatChanged -= HandleStatChanged;
    //     }
    // }
    private void InitializeWindow()
    {
        if (isWindowInitialized) return; // 이미 초기화된 경우 중복 실행 방지

        for (eStatType type = (eStatType)1; type < eStatType.MAX_COUNT; type++)
        {
            StatData data = DataManager.Instance.GetStat(type);
            if(data == null) continue;
            if (data.StatCategory != statCategoryFilter) continue; // 필터링된 카테고리만 표시

            // 프리팹 생성 및 부모 설정
            GameObject go = Instantiate(statItemPrefab, statContentParent);
            UI_StatItem uiItem = go.GetComponent<UI_StatItem>();

            // 현재 스텟 값을 가져와서 UI 초기화
            int currentVal = PlayerStatManager.Instance.GetStatValue(type);
            uiItem.Initialize(data, currentVal);

            spawnedItems[type] = uiItem;
        }
        isWindowInitialized = true;
    }
    private void SyncStats()
    {
        InitializeWindow();

        for (eStatType type = (eStatType)1; type < eStatType.MAX_COUNT; type++)
        {
            if (spawnedItems.TryGetValue(type, out UI_StatItem uiItem))
            {
                int currentVal = PlayerStatManager.Instance.GetStatValue(type);
                uiItem.UpdateValue(currentVal);
            }
        }
    }

    public void Sync()
    {
        if (!isInitialized) return;
        if (!IsWindowVisible()) return;
        SyncStats();
    }

    // 이벤트가 발생하면 자동으로 실행되는 콜백 함수
    private void HandleStatChanged(eStatType type, int newValue)
    {
        // UI가 비활성화되어 있으면 이벤트만 내부 상태에 반영하고
        // 다시 활성화될 때 SyncStats()로 동기화
        if (!IsWindowVisible()) return;

        // 내 화면에 해당 스텟 UI가 띄워져 있다면 값 갱신
        if (spawnedItems.TryGetValue(type, out UI_StatItem uiItem))
        {
            uiItem.UpdateValue(newValue);
        }
    }

    private bool IsWindowVisible()
    {
        return windowRoot != null ? windowRoot.activeInHierarchy : gameObject.activeInHierarchy;
    }
}