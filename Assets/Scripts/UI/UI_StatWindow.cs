using UnityEngine;
using System.Collections.Generic;

public class UI_StatWindow : MonoBehaviour
{
    [SerializeField] private GameObject statItemPrefab;
    [SerializeField] private Transform statContentParent; // ScrollView의 Content
    [SerializeField] private StatCategory statCategoryFilter; // 표시할 스텟 카테고리 필터

    // 생성된 프리팹들을 추적하기 위한 딕셔너리
    private Dictionary<StatType, UI_StatItem> spawnedItems = new Dictionary<StatType, UI_StatItem>();

    private void Start()
    {
        InitializeWindow();

        // ★ 핵심: 매니저의 방송국(이벤트) 주파수 맞추기 (구독)
        if (PlayerStatManager.Instance != null)
        {
            PlayerStatManager.Instance.OnStatChanged += HandleStatChanged;
        }
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지를 위해 창이 닫히거나 파괴될 때 구독 해제
        if (PlayerStatManager.Instance != null)
        {
            PlayerStatManager.Instance.OnStatChanged -= HandleStatChanged;
        }
    }

    private void InitializeWindow()
    {
        // 1. (임시) 모든 스텟을 순회하며 프리팹 생성
        // 추후에는 선택된 Tab의 카테고리만 생성하도록 로직을 변경하면 좋습니다.
        for (StatType type = (StatType)1; type < StatType.MAX_COUNT; type++)
        {
            StatData data = DataManager.Instance.GetStat(type);
            
            if (data.StatCategory != statCategoryFilter) continue; // 필터링된 카테고리만 표시

            // 프리팹 생성 및 부모 설정
            GameObject go = Instantiate(statItemPrefab, statContentParent);
            UI_StatItem uiItem = go.GetComponent<UI_StatItem>();

            // 현재 스텟 값을 가져와서 UI 초기화
            int currentVal = PlayerStatManager.Instance.GetStatValue(type);
            uiItem.Initialize(data, currentVal);

            spawnedItems[type] = uiItem;
        }
    }

    // 이벤트가 발생하면 자동으로 실행되는 콜백 함수
    private void HandleStatChanged(StatType type, int newValue)
    {
        // 내 화면에 해당 스텟 UI가 띄워져 있다면 값 갱신
        if (spawnedItems.TryGetValue(type, out UI_StatItem item))
        {
            StatData data = DataManager.Instance.GetStat(type);
            item.UpdateValue(newValue, data.MaxValue);
        }
    }
}