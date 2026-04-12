using UnityEngine;
using System.Collections.Generic;

public class GameManager : Singleton<GameManager>
{
    protected override void Awake()
    {
        base.Awake();

        DataManager.Instance.Init();
        PlayerStatManager.Instance.Init();
        InventoryManager.Instance.Init();
    }
}