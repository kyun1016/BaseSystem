using UnityEngine;
using System.Collections.Generic;

public class UI_DialogueWindow : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform dialogueContentParent;


    private void Awake()
    {
        // 한 번만 구독 (창이 열려있든 닫혀있든 이벤트 감지)
        // DialogueManager.Instance.OnChanged += HandleChanged;
    }

    private void OnEnable()
    {
        
    }
    public void Draw()  
    {

    }

    private void HandleChanged(int key)
    {

    }
}