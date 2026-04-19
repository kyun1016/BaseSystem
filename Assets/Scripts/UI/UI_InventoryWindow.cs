using UnityEngine;
using System.Collections.Generic;

public class UI_InventoryWindow : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private UI_InventoryItem inventoryItemPrefab; 
    [SerializeField] private Transform inventoryContentParent;

    // 활성화된 슬롯 UI(인덱스 순서)와 풀링 대기열
    private List<UI_InventoryItem> activeItems = new List<UI_InventoryItem>();
    private Queue<UI_InventoryItem> itemPool = new Queue<UI_InventoryItem>();
    private bool isInitialized = false;

    private void Awake()
    {
        // 한 번만 구독 (창이 열려있든 닫혀있든 이벤트 감지)
        InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;
        isInitialized = true;
    }

    private void OnEnable()
    {
        // 창이 활성화될 때 현재 인벤토리 상태로 동기화
        if (isInitialized)
        {
            DrawInventory();
        }
    }

    private void OnDisable()
    {
        // 창이 비활성화되어도 구독은 유지 (계속 이벤트 감지)
        // UI 업데이트만 하지 않음
    }

    // private void OnDestroy()
    // {
    //     // 완전히 파괴될 때만 구독 해제
    //     if (InventoryManager.Instance != null)
    //     {
    //         InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
    //     }
    // }

    public void DrawInventory()
    {
        ClearActiveItems(); // 기존 항목 풀에 모두 반납

        // InventoryManager에서 현재 소지중인 모든 아이템을 가져와서 순회
        List<int> items = InventoryManager.Instance.GetAllItems();
        for (int i = 0; i < items.Count; i++)
        {
            AddItemUI(items[i], i);
        }
    }

    private void HandleInventoryChanged(int itemID, int index, bool isAdded)
    {
        // UI가 비활성화되어 있으면 이벤트만 내부 상태에 반영하고
        // 다시 활성화될 때 DrawInventory()로 동기화
        if (!gameObject.activeSelf) return;

        if (isAdded)
        {
            AddItemUI(itemID, index);
        }
        else
        {
            RemoveItemUI(index);
        }
    }

    // 풀에서 가져오거나 갱신하는 공통 헬퍼 함수
    private void AddItemUI(int itemID, int index)
    {
        ItemData data = DataManager.Instance.GetItem(itemID);
        if (data == null) return;

        UI_InventoryItem newUI = GetItemFromPool();
        newUI.Initialize(data, index);

        if (index < 0 || index > activeItems.Count)
            index = activeItems.Count;

        activeItems.Insert(index, newUI);
        newUI.transform.SetSiblingIndex(index);

        RefreshItemIndices(index + 1);
    }

    private void RemoveItemUI(int index)
    {
        if (index < 0 || index >= activeItems.Count) return;

        UI_InventoryItem item = activeItems[index];
        item.gameObject.SetActive(false);
        itemPool.Enqueue(item);
        activeItems.RemoveAt(index);
        RefreshItemIndices(index);
    }

    private void RefreshItemIndices(int startIndex)
    {
        for (int i = startIndex; i < activeItems.Count; i++)
        {
            activeItems[i].SetIndex(i);
        }
    }

    // =======================================================
    // 오브젝트 풀링 (Get / Clear)
    // =======================================================
    private UI_InventoryItem GetItemFromPool()
    {
        UI_InventoryItem item = itemPool.Count > 0 ? itemPool.Dequeue() : Instantiate(inventoryItemPrefab, inventoryContentParent);
        item.gameObject.SetActive(true);
        item.transform.SetAsLastSibling();
        return item;
    }

    private void ClearActiveItems()
    {
        foreach (var item in activeItems)
        {
            item.gameObject.SetActive(false);
            itemPool.Enqueue(item);
        }
        activeItems.Clear();
    }
}