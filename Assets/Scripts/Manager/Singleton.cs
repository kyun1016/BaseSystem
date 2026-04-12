using UnityEngine;

// 상속받는 클래스가 무조건 MonoBehaviour여야 함을 강제합니다.
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;

    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting)
            {
                Debug.LogWarning($"[Singleton] {typeof(T).Name}은 앱 종료 중이므로 반환하지 않습니다.");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    // 1. 씬에 이미 매니저가 있는지 찾습니다.
                    _instance = FindFirstObjectByType<T>();

                    // 2. 씬에 없다면, 코드로 직접 빈 오브젝트를 만들고 컴포넌트를 붙여서 생성합니다.
                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject();
                        _instance = singletonObject.AddComponent<T>();
                        singletonObject.name = typeof(T).Name; // 이름 자동 지정
                    }
                }
                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject.transform.root.gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }
}