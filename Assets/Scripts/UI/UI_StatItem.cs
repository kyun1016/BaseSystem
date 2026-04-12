using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용 권장

public class UI_StatItem : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private Slider statSlider; // 게이지 바

    public StatType TargetStat { get; private set; }

    // 처음 생성될 때 한 번 세팅
    public void Initialize(StatData data, int currentValue)
    {
        TargetStat = data.Type;
        nameText.text = data.Name_KR;
        
        // 아이콘 로드 로직 (추후 Addressables로 변경 가능)
        // if (!string.IsNullOrEmpty(data.IconPath)) 
        //     iconImage.sprite = Resources.Load<Sprite>(data.IconPath);

        UpdateValue(currentValue, data.MaxValue);
    }

    // 값이 변할 때마다 호출됨
    public void UpdateValue(int currentValue, int maxValue)
    {
        valueText.text = $"{currentValue}";
        
        if (statSlider != null)
        {
            statSlider.maxValue = maxValue;
            statSlider.value = currentValue;
        }
    }
}