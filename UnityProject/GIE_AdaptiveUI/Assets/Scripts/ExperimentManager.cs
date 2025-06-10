using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExperimentManager : MonoBehaviour
{
    // Singleton
    private static ExperimentManager instance;
    public static ExperimentManager Instance
    {
        get
        {
            if (instance == null)
            {
                // 씬에서 기존 인스턴스 찾기
                instance = FindObjectOfType<ExperimentManager>();

                // 기존 인스턴스가 없으면 새로 생성
                if (instance == null)
                {
                    GameObject obj = new GameObject("ExperimentManager");
                    instance = obj.AddComponent<ExperimentManager>();
                    DontDestroyOnLoad(obj);
                }
            }
            return instance;
        }
    }

    // 시선 오프셋 X 범위
    private int gazeOffsetX = 0;
    public int GazeOffsetX => gazeOffsetX;

    // 시선 오프셋 Y 범위
    private int gazeOffsetY = 0;
    public int GazeOffsetY => gazeOffsetY;

    // 시선 감도 (기존 유지)
    private float gazeSensitivityX = 1;
    public float GazeSensitivityX => gazeSensitivityX;
    private float gazeSensitivityY = 1;
    public float GazeSensitivityY => gazeSensitivityY;

    private float gazeMovementLerp = 30f;
    public float GazeMovementLerp => gazeMovementLerp;

    // Input Fields
    private TMP_InputField gazeOffsetXInputField;
    private TMP_InputField gazeOffsetYInputField;

    private TMP_InputField gazeSensitivityXInputField;
    private TMP_InputField gazeSensitivityYInputField;

    private TMP_InputField gazeMovementLerpInputField;


    void Awake()
    {
        // 싱글톤 인스턴스 확인 및 설정
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeInputFields();
        }
        else if (instance != this)
        {
            // 이미 인스턴스가 존재하면 현재 오브젝트 파괴
            Destroy(gameObject);
            return;
        }
    }

    private void InitializeInputFields()
    {
        // InputField 초기화를 별도 메서드로 분리
        StartCoroutine(FindAndSetupInputFields());
    }

    private IEnumerator FindAndSetupInputFields()
    {
        // 씬이 완전히 로드될 때까지 잠시 대기
        yield return new WaitForEndOfFrame();

        // 오프셋 X 입력 필드
        gazeOffsetXInputField = GameObject.Find("GazeOffsetXInputField")?.GetComponent<TMP_InputField>();

        // 오프셋 Y 입력 필드
        gazeOffsetYInputField = GameObject.Find("GazeOffsetYInputField")?.GetComponent<TMP_InputField>();

        // 감도 입력 필드 (기존)
        gazeSensitivityXInputField = GameObject.Find("GazeSensitivityXInputField")?.GetComponent<TMP_InputField>();
        gazeSensitivityYInputField = GameObject.Find("GazeSensitivityYInputField")?.GetComponent<TMP_InputField>();

        // 시선 이동 보간 입력 필드
        gazeMovementLerpInputField = GameObject.Find("GazeMovementLerpInputField")?.GetComponent<TMP_InputField>();

        SetupInputFieldListeners();
    }

    private void SetupInputFieldListeners()
    {
        // 오프셋 X 설정
        if (gazeOffsetXInputField != null)
        {
            gazeOffsetXInputField.onValueChanged.RemoveAllListeners();
            gazeOffsetXInputField.onValueChanged.AddListener(OnGazeOffsetXChanged);
            gazeOffsetXInputField.text = gazeOffsetX.ToString();
        }

        // 오프셋 Y 설정
        if (gazeOffsetYInputField != null)
        {
            gazeOffsetYInputField.onValueChanged.RemoveAllListeners();
            gazeOffsetYInputField.onValueChanged.AddListener(OnGazeOffsetYChanged);
            gazeOffsetYInputField.text = gazeOffsetY.ToString();
        }

        // 감도 X 설정 (기존)
        if (gazeSensitivityXInputField != null)
        {
            gazeSensitivityXInputField.onValueChanged.RemoveAllListeners();
            gazeSensitivityXInputField.onValueChanged.AddListener(OnGazeSensitivityXChanged);
            gazeSensitivityXInputField.text = gazeSensitivityX.ToString();
        }

        // 감도 Y 설정 (기존)
        if (gazeSensitivityYInputField != null)
        {
            gazeSensitivityYInputField.onValueChanged.RemoveAllListeners();
            gazeSensitivityYInputField.onValueChanged.AddListener(OnGazeSensitivityYChanged);
            gazeSensitivityYInputField.text = gazeSensitivityY.ToString();
        }

        // 시선 이동 보간 설정
        if (gazeMovementLerpInputField != null)
        {
            gazeMovementLerpInputField.onValueChanged.RemoveAllListeners();
            gazeMovementLerpInputField.onValueChanged.AddListener(OnGazeMovementLerpChanged);
            gazeMovementLerpInputField.text = gazeMovementLerp.ToString();
        }
    }


    // 오프셋 X Max 변경 핸들러
    private void OnGazeOffsetXChanged(string value)
    {
        if (int.TryParse(value, out int xMax))
        {
            gazeOffsetX = xMax;
        }
        else
        {
            // 잘못된 입력시 이전 값으로 복원
            if (gazeOffsetXInputField != null)
                gazeOffsetXInputField.text = gazeOffsetX.ToString();
        }
    }

    // 오프셋 Y Max 변경 핸들러
    private void OnGazeOffsetYChanged(string value)
    {
        if (int.TryParse(value, out int yMax))
        {
            gazeOffsetY = yMax;
        }
        else
        {
            // 잘못된 입력시 이전 값으로 복원
            if (gazeOffsetYInputField != null)
                gazeOffsetYInputField.text = gazeOffsetY.ToString();
        }
    }

    // 감도 X 변경 핸들러 (기존)
    private void OnGazeSensitivityXChanged(string value)
    {
        if (float.TryParse(value, out float x))
        {
            gazeSensitivityX = x;
        }
        else
        {
            // 잘못된 입력시 이전 값으로 복원
            if (gazeSensitivityXInputField != null)
                gazeSensitivityXInputField.text = gazeSensitivityX.ToString();
        }
    }

    // 감도 Y 변경 핸들러 (기존)
    private void OnGazeSensitivityYChanged(string value)
    {
        if (float.TryParse(value, out float y))
        {
            gazeSensitivityY = y;
        }
        else
        {
            // 잘못된 입력시 이전 값으로 복원
            if (gazeSensitivityYInputField != null)
                gazeSensitivityYInputField.text = gazeSensitivityY.ToString();
        }
    }

    // 시선 이동 보간 변경 핸들러
    private void OnGazeMovementLerpChanged(string value)
    {
        if (float.TryParse(value, out float lerpValue))
        {
            gazeMovementLerp = lerpValue;
        }
        else
        {
            // 잘못된 입력시 이전 값으로 복원
            if (gazeMovementLerpInputField != null)
                gazeMovementLerpInputField.text = gazeMovementLerp.ToString();
        }
    }


    // 씬이 변경될 때 InputField 재설정
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 새 씬이 로드되면 InputField를 다시 찾아서 설정
        StartCoroutine(FindAndSetupInputFields());
    }

    void Update()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (currentSceneName == "Experiment_Main" || currentSceneName == "Experiment_End")
        {
            Cursor.lockState = CursorLockMode.None; // 커서 잠금 해제
            Cursor.visible = true; // 커서 보이기
        }
        else
        {
#if !UNITY_EDITOR
            Cursor.lockState = CursorLockMode.Locked; // 커서 잠금
            Cursor.visible = false; // 커서 숨김
#endif
        }
    }

    private void OnDestroy()
    {
        // 씬 이벤트 리스너 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // 인스턴스가 파괴될 때 참조 해제
        if (instance == this)
        {
            instance = null;
        }
    }
}