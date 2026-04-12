using UnityEngine;
using System;
using System.Collections.Generic;

public class InventoryManager : Singleton<InventoryManager>
{
    private List<int> inventoryData = new List<int>();

    // UI 갱신을 위한 이벤트 방송국 (아이템 ID)
    public event Action<int> OnInventoryChanged;

    public void Init()
    {
        // 추후 세이브 파일이 있다면 여기서 inventoryData를 로드해서 덮어씌웁니다.
        inventoryData.Clear();
        Debug.Log("인벤토리 시스템 초기화 완료");
    }

    // =======================================================
    // 1. 아이템 획득
    // =======================================================
    public void AddItem(int itemID)
    {
        // 존재하는 아이템인지 DataManager에 검증
        ItemData itemData = DataManager.Instance.GetItem(itemID);
        if (itemData == null)
        {
            Debug.LogWarning($"존재하지 않는 아이템 ID({itemID})를 획득하려고 시도했습니다.");
            return;
        }

        inventoryData.Add(itemID);
        OnInventoryChanged?.Invoke(itemID);
        Debug.Log($"[아이템 획득] {itemData.Name_KR} (총 {GetItemCount(itemID)}개)");
    }

    // =======================================================
    // 2. 아이템 소모 (상점 판매, 퀘스트 제출 등)
    // =======================================================
    public bool RemoveItem(int itemID)
    {
        if (!inventoryData.Contains(itemID))
        {
            Debug.Log("아이템이 부족합니다.");
            return false; // 아이템이 없거나 부족하면 실패 반환
        }

        inventoryData.Remove(itemID);
        OnInventoryChanged?.Invoke(itemID);
        return true;
    }

    // =======================================================
    // 3. 아이템 사용 (핵심: 스탯 매니저와의 연동)
    // =======================================================
    public bool UseItem(int itemID)
    {
        // 1. 인벤토리에 해당 아이템이 있는지 확인
         if (!inventoryData.Contains(itemID))
        {
            Debug.Log("아이템이 부족합니다.");
            return false; // 아이템이 없거나 부족하면 실패 반환
        }

        // 2. 아이템 사용 조건 검사
        ItemData itemData = DataManager.Instance.GetItem(itemID);
        if (itemData == null)
        {
            Debug.LogWarning($"존재하지 않는 아이템 ID({itemID})를 사용하려고 시도했습니다.");
            return false;
        }

        if(itemData.ItemType == ItemType.Consumable)
        {
            // 예시: 소비 아이템은 사용 조건이 없다고 가정 (추후 확장 가능)
        }
        else
        {
            Debug.LogWarning($"아이템 {itemData.Name_KR}은(는) 사용 불가능한 타입입니다.");
            return false;
        }

        // 3. 아이템 제거 (소비)
        if (!RemoveItem(itemID)) return false;

        

        // 4. 아이템의 효과(Effects) 리스트를 순회하며 스탯 올려주기
        if(itemData.EffectStatType1 != StatType.None)
            PlayerStatManager.Instance.AddStat(itemData.EffectStatType1, itemData.EffectValue1);
        if(itemData.EffectStatType2 != StatType.None)
            PlayerStatManager.Instance.AddStat(itemData.EffectStatType2, itemData.EffectValue2);

        Debug.Log($"<color=orange>[아이템 사용] {itemData.Name_KR}을(를) 사용했습니다.</color>");
        return true;
    }

    // 특정 아이템을 몇 개 가지고 있는지 반환
    public int GetItemCount(int itemID)
    {
        int count = 0;
        foreach (var id in inventoryData)
        {
            if (id == itemID) ++count; // 현재는 중복 아이템이 없으므로 1 또는 0 반환
        }
        return count;
    }

    public List<int> GetAllItems()
    {
        return inventoryData;
    }
}