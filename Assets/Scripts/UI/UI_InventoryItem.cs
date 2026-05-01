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
    private int index;

    private void Awake()
    {
        // 버튼 클릭 이벤트 연결
        useButton.onClick.AddListener(OnUseButtonClicked);
    }

    // 풀에서 꺼내져 처음 화면에 세팅될 때 호출
    public void Initialize(ItemData data, int slotIndex)
    {
        currentItemID = data.Base.ID;
        index = slotIndex;
        nameText.text = data.Base.Name.KR;
        sellPriceText.text = data.SellPrice.ToString();
        effect1Text.text = BuildEffectText(data, 0);
        effect2Text.text = BuildEffectText(data, 1);

        // 아이콘 연결 로직 (추후 추가)
        // if (!string.IsNullOrEmpty(data.IconPath)) 
        //     iconImage.sprite = Resources.Load<Sprite>(data.IconPath);
    }

    private static string BuildEffectText(ItemData data, int effectIndex)
    {
        if (data == null || data.EffectStats == null || effectIndex < 0 || effectIndex >= data.EffectStats.Count)
            return string.Empty;

        var effect = data.EffectStats[effectIndex];

        string statName = null;
        if (DataManager.Instance != null)
            DataManager.Instance.TryGetStatName(effect.Key, out statName);

        if (string.IsNullOrWhiteSpace(statName))
            statName = effect.Key.ToString();

        if (effect.Value > 0)
            return statName + "+" + effect.Value.ToString();
        if (effect.Value < 0)
            return statName + "-" + Mathf.Abs(effect.Value).ToString();

        return statName;
    }

    // 사용 버튼을 눌렀을 때 실행
    private void OnUseButtonClicked()
    {
        // 현재 UI 슬롯 인덱스를 기준으로 정확한 위치의 아이템 사용
        // 사용에 성공하면 내부적으로 스탯이 오르고, OnInventoryChanged 이벤트가 발생합니다.
        InventoryManager.Instance.UseItemAt(index);
    }

    public void SetIndex(int slotIndex)
    {
        index = slotIndex;
    }
}