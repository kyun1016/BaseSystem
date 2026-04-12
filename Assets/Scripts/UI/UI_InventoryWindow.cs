using UnityEngine;
using System.Collections.Generic;

public class UI_InventoryWindow : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private UI_InventoryItem inventoryItemPrefab; 
    [SerializeField] private Transform inventoryContentParent;

    // 활성화된 아이템과 풀링 대기열
    private Dictionary<int, UI_InventoryItem> activeItems = new Dictionary<int, UI_InventoryItem>();
    private Queue<UI_InventoryItem> itemPool = new Queue<UI_InventoryItem>();

    private void Start()
    {
        // 매니저의 아이템 변동 이벤트 구독
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;
        }

        DrawInventory(); // 창이 켜질 때 현재 인벤토리 상태 그리기
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    public void DrawInventory()
    {
        ClearActiveItems(); // 기존 항목 풀에 모두 반납

        // InventoryManager에서 현재 소지중인 모든 아이템을 가져와서 순회
        foreach (var itemID in InventoryManager.Instance.GetAllItems())
        {
            CreateItemUI(itemID);
        }
    }

    private void HandleInventoryChanged(int itemID)
    {
        CreateItemUI(itemID);
    }

    // 풀에서 가져오거나 갱신하는 공통 헬퍼 함수
    private void CreateItemUI(int itemID)
    {
        ItemData data = DataManager.Instance.GetItem(itemID);
        if (data == null) return;

        UI_InventoryItem newUI = GetItemFromPool();
        newUI.Initialize(data);
        activeItems[itemID] = newUI;
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
        foreach (var item in activeItems.Values)
        {
            item.gameObject.SetActive(false);
            itemPool.Enqueue(item);
        }
        activeItems.Clear();
    }
}