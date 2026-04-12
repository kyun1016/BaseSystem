using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_InventoryItem : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI sellPriceText;
    [SerializeField] private TextMeshProUGUI effect1Text;
    [SerializeField] private TextMeshProUGUI effect2Text;

    [SerializeField] private Button useButton;

    private int currentItemID;

    private void Awake()
    {
        // 버튼 클릭 이벤트 연결
        useButton.onClick.AddListener(OnUseButtonClicked);
    }

    // 풀에서 꺼내져 처음 화면에 세팅될 때 호출
    public void Initialize(ItemData data)
    {
        currentItemID = data.ID;
        nameText.text = data.Name_KR;
        sellPriceText.text = data.SellPrice.ToString();
        if(data.EffectStatType1 != StatType.None)
            if(data.EffectValue1 > 0)
                effect1Text.text = data.EffectStatType1.ToString() + "+" + data.EffectValue1.ToString();
            else if(data.EffectValue1 < 0)
                effect1Text.text = data.EffectStatType1.ToString() + "-" + Mathf.Abs(data.EffectValue1).ToString();
            else
                effect1Text.text = data.EffectStatType1.ToString();   
        else
            effect1Text.text = "";
        if(data.EffectStatType2 != StatType.None)
            if(data.EffectValue2 > 0)
                effect2Text.text = data.EffectStatType2.ToString() + "+" + data.EffectValue2.ToString();
            else if(data.EffectValue2 < 0)
                effect2Text.text = data.EffectStatType2.ToString() + "-" + Mathf.Abs(data.EffectValue2).ToString();
            else
                effect2Text.text = data.EffectStatType2.ToString();
        else
            effect2Text.text = "";

        // 아이콘 연결 로직 (추후 추가)
        // if (!string.IsNullOrEmpty(data.IconPath)) 
        //     iconImage.sprite = Resources.Load<Sprite>(data.IconPath);
    }

    // 사용 버튼을 눌렀을 때 실행
    private void OnUseButtonClicked()
    {
        // 실제 인벤토리 시스템에 아이템 사용 명령을 내립니다.
        // 사용에 성공하면 내부적으로 스탯이 오르고, OnInventoryChanged 이벤트가 발생합니다.
        InventoryManager.Instance.UseItem(currentItemID);
    }
}