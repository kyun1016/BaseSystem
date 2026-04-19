using UnityEngine;

public class UI_StatWindowRoot : MonoBehaviour
{
	[SerializeField] private UI_StatWindow[] statWindows;

	private void Awake()
	{
		if (statWindows == null || statWindows.Length == 0)
		{
			statWindows = GetComponentsInChildren<UI_StatWindow>(true);
		}
	}

	private void OnEnable()
	{
		for (int i = 0; i < statWindows.Length; i++)
		{
			if (statWindows[i] != null)
			{
				statWindows[i].Sync();
			}
		}
	}
}