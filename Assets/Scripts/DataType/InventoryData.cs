using System;
using System.Collections.Generic;
using UnityEngine;

// 세이브/로드 시 그대로 JSON으로 직렬화하기 좋은 순수 데이터 구조
[Serializable]
public class InventoryData
{
    public List<int> slots = new List<int>();
}