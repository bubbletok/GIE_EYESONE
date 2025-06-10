using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SceneLoadButton : MonoBehaviour
{
    private Button loadButton; // 버튼 컴포넌트
    public string sceneName; // 로드할 씬의 이름

    void Awake()
    {
        loadButton = GetComponent<Button>();
    }
    void Start()
    {
        // 버튼이 설정되어 있으면 클릭 이벤트에 메서드 추가
        if (loadButton != null)
        {
            loadButton.onClick.AddListener(OnButtonClick);
        }
        else
        {
            Debug.LogWarning("Load Button is not assigned in the inspector.");
        }
    }

    // 버튼 클릭 시 호출되는 메서드
    public void OnButtonClick()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
