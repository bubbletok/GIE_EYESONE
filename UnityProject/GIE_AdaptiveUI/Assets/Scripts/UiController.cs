using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Tobii.GameIntegration.Net;

public enum UiHidingMode
{
    None,               // 아무것도 안함
    Mixed,              // 종합
    Transparent,        // 투명도 조절
    Icon,               // 아이콘화
    Scale               // 크기 최소 최대화
}

[System.Serializable]
public class UiPatch
{
    [Header("적용 대상")]
    public GameObject uiTarget;   // 본 UI (RectTransform 필요)
    public GameObject uiIcon;     // 아이콘

    [Header("변형 값")]
    public Vector2 sizeOut;       // 숨김 상태 크기
    public float stayTime = 0.5f; // 커서 떠난 뒤 유지 시간
    public float lerpSpeed = 20f; // 크기 보간 속도

    [Header("반경")]
    public float boldRadius = 100f; // 아이콘 불투명 전환 거리
}

// JSON 로그 클래스
[System.Serializable]
public class GazeLogEntry
{
    public string timestamp;
    public string hidingMode;
    public float totalTime;
    public List<TargetEntry> targets = new();
}

[System.Serializable]
public class TargetEntry
{
    public string targetName;
    public float duration;
}

public class UiController : MonoBehaviour
{
    [Header("필수 레퍼런스")]
    public GraphicRaycaster raycaster;  // Canvas에 붙은 GraphicRaycaster
    public EventSystem eventSystem;   // 씬에 하나 있어야 함

    [Header("시선 커서")]
    public RectTransform gazeCursor;    // (선택) 커서 이미지

    [Header("감지 대상 UI 리스트")]
    public List<UiPatch> uiPatches = new List<UiPatch>();

    [Header("디버그/테스트")]
    public bool simulateWithMouse = false;

    [Header("UI 조절 방식")]
    public UiHidingMode currentMode;

    private bool trackerAvailable = false;

    // 원본 크기 저장
    private Dictionary<GameObject, Vector2> originalSizes = new Dictionary<GameObject, Vector2>();
    private Dictionary<GameObject, float> originalAlphas = new Dictionary<GameObject, float>();
    // private Dictionary<RectTransform, Vector2> originalChildPositions = new Dictionary<RectTransform, Vector2>();
    // private Dictionary<RectTransform, Vector2> originalChildSizes = new();
    // private Dictionary<RectTransform, Vector3> originalChildScales = new();
    // 머무름 타이머 저장
    private Dictionary<GameObject, float> stayTimers = new Dictionary<GameObject, float>();

    // 추가: 전체 세션 시간
    private float totalGazeTime = 0f;
    // 추가: UI별 머문 시간(초)
    private Dictionary<string, float> gazeDurations = new Dictionary<string, float>();

    // 에디터에서는 Assets/Scripts 폴더 아래에 저장
    // 빌드에서는 사용자 Downloads 폴더에 저장

    // #if UNITY_EDITOR
    string logFilePath = Path.Combine(Application.streamingAssetsPath, "Logs");
    // #else
    //     string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    //     string downloads = Path.Combine(home, "Downloads");
    //     string logFilePath = downloads;
    // #endif

    // DLL Import
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    #region Unity Methods
    void Awake()
    {
        if (raycaster == null) raycaster = GetComponent<GraphicRaycaster>();
        if (eventSystem == null) eventSystem = FindObjectOfType<EventSystem>();
    }

    void Start()
    {
        InitializeUIPatches();
        InitializeTobii();
    }

    void Update()
    {
        totalGazeTime += Time.unscaledDeltaTime;

        Vector2 screenPos = simulateWithMouse ? GetMousePosition() : GetGazePosition();
        if (gazeCursor != null && screenPos != new Vector2(-1, -1))
        {
            // Debug.Log("Gaze Cursor anchoredPosition: " + gazeCursor.anchoredPosition);
            // Debug.Log($"Gaze cursor screen position: {screenPos}");
            var newPos = Vector2.Lerp(
                gazeCursor.anchoredPosition,
                screenPos,
                Time.deltaTime * ExperimentManager.Instance.GazeMovementLerp // 보간 속도 조절 (값이 클수록 빠르게 이동)
            );
            // var newPos = screenPos;
            // Debug.Log($"Gaze cursor new position: {newPos}");
            gazeCursor.anchoredPosition = newPos;
            UpdateUiByMode(screenPos);
        }
        else if (simulateWithMouse) UpdateUiByMode(screenPos);
    }

    private void OnDestroy()
    {
        SaveLog();
    }

    // private void OnApplicationQuit()
    // {
    //     SaveLog();
    // }

    #endregion

    #region Data Logging

    private void SaveLog()
    {
        var nowTime = DateTime.Now;
        var logEntry = new GazeLogEntry
        {
            timestamp = nowTime.ToString("s"),
            hidingMode = Enum.GetName(typeof(UiHidingMode), currentMode),
            totalTime = totalGazeTime,
            targets = new List<TargetEntry>()
        };

        foreach (var kv in gazeDurations)
        {
            logEntry.targets.Add(new TargetEntry { targetName = kv.Key, duration = kv.Value });
        }

        string logFileName = $"GazeLog_{nowTime.ToString("s").Replace(':', '_')}_{Enum.GetName(typeof(UiHidingMode), currentMode)}.json";
        string finalLogFilePath = Path.Combine(logFilePath, logFileName);

        if (!Directory.Exists(logFilePath))
            Directory.CreateDirectory(logFilePath);

        File.WriteAllText(finalLogFilePath, JsonUtility.ToJson(logEntry, true));
#if UNITY_EDITOR
        Debug.Log($"Gaze log saved to: {finalLogFilePath}");
#endif
        TobiiGameIntegrationApi.StopTracking();
        TobiiGameIntegrationApi.Shutdown();
    }
    #endregion

    #region Initialization
    // 모든 uiTarget에 대해 원본 크기를 저장하고, 초기 상태 설정
    private void InitializeUIPatches()
    {
        foreach (var p in uiPatches)
        {
            var targetName = p.uiTarget != null ? p.uiTarget.name : "null";
            if (!gazeDurations.ContainsKey(targetName))
                gazeDurations[targetName] = 0f;
            // uiTarget 처리
            if (p.uiTarget != null)
            {
                var targetRT = p.uiTarget.GetComponent<RectTransform>();
                if (targetRT != null)
                {
                    // 1) 원본 크기 저장
                    originalSizes[p.uiTarget] = targetRT.sizeDelta;

                    // 2) 원본 투명도 저장
                    var img = p.uiTarget.GetComponent<Image>();
                    originalAlphas[p.uiTarget] = img != null ? img.color.a : 1f;

                    // 3) 직속 자식 위치, 크기, 스케일 저장
                    for (int i = 0; i < targetRT.childCount; i++)
                    {
                        var childRT = targetRT.GetChild(i) as RectTransform;
                        if (childRT == null) continue;

                        // 위치
                        // originalChildPositions[childRT] = childRT.anchoredPosition;
                        // // 크기
                        // originalChildSizes[childRT] = childRT.sizeDelta;
                        // // 스케일
                        // originalChildScales[childRT] = childRT.localScale;
                    }

                    // 4) 초기 숨김 상태
                    targetRT.sizeDelta = p.sizeOut;
                    p.uiTarget.SetActive(false);
                }
            }

            // uiIcon 처리
            if (p.uiIcon != null)
            {
                var iconImg = p.uiIcon.GetComponent<Image>();
                originalAlphas[p.uiIcon] = iconImg != null ? iconImg.color.a : 1f;

                p.uiIcon.SetActive(true);
                SetAlpha(p.uiIcon, 0.3f);
            }
        }
    }

    // Tobii 초기화
    private void InitializeTobii()
    {
        TobiiGameIntegrationApi.PrelinkAll();
        TobiiGameIntegrationApi.SetApplicationName(Application.productName);
        RefreshTrackWindow();
    }
    #endregion

    #region Update Methods

    private Vector2 GetMousePosition()
    {
        Vector2 screenPos = Input.mousePosition;
        // Debug.Log($"Mouse screen position: {screenPos}");
        return screenPos;
    }

    private Vector2 GetGazePosition()
    {
        Vector2 screenPos = new Vector2(-1, -1); // 기본값: 유효하지 않은 위치
        if (!trackerAvailable) RefreshTrackWindow();
        TobiiGameIntegrationApi.Update();
        if (!TobiiGameIntegrationApi.IsTrackerConnected() || !TobiiGameIntegrationApi.IsPresent()) return screenPos;

        if (TobiiGameIntegrationApi.TryGetLatestGazePoint(out var gaze))
        {
            Resolution currentResolution = Screen.currentResolution;
            gaze.X *= ExperimentManager.Instance.GazeSensitivityX;
            gaze.Y *= ExperimentManager.Instance.GazeSensitivityY;
            Debug.Log($"[Tobii] Gaze point: {gaze.X}, {gaze.Y} (sensitivity: {ExperimentManager.Instance.GazeSensitivityX}, {ExperimentManager.Instance.GazeSensitivityY})");
            Debug.Log($"[Tobii] Current Resolution {currentResolution.width}x{currentResolution.height}");
            float mappedX = Screen.width / 2 + (gaze.X * currentResolution.width / 2);
            float mappedY = Screen.height / 2 + (gaze.Y * currentResolution.height / 2);
            mappedX = Mathf.Clamp(mappedX, 0, currentResolution.width) + ExperimentManager.Instance.GazeOffsetX;
            mappedY = Mathf.Clamp(mappedY, 0, currentResolution.height) + ExperimentManager.Instance.GazeOffsetY;
            Debug.Log($"[Tobii] Mapped gaze position: {mappedX}, {mappedY}, offset ({ExperimentManager.Instance.GazeOffsetX}, {ExperimentManager.Instance.GazeOffsetY})");
            screenPos = new Vector2(mappedX, mappedY);
#if UNITY_EDITOR
            // Debug.Log($"[Tobii] Gaze position: {screenPos}");
#endif
        }
        return screenPos;
    }

    private void RefreshTrackWindow()
    {
        TobiiGameIntegrationApi.Update();
        TobiiGameIntegrationApi.UpdateTrackerInfos();
        var infos = TobiiGameIntegrationApi.GetTrackerInfos();
        Debug.Log($"[Tobii] Found {infos?.Count ?? 0} tracker(s)");

        if (infos == null || infos.Count == 0)
        {
            Debug.LogError("[Tobii] No tracker infos – is Core Runtime running?");
            trackerAvailable = false;
            return;
        }

        IntPtr hwnd = GetForegroundWindow();
        // Debug.Log($"[Tobii] Foreground HWND: {hwnd}");
        if (hwnd != IntPtr.Zero) trackerAvailable = TobiiGameIntegrationApi.TrackWindow(hwnd);
        Debug.Log($"[Tobii] TrackWindow returned {trackerAvailable}");

        if (!trackerAvailable)
        {
            var url = infos[0].Url;
            trackerAvailable = TobiiGameIntegrationApi.TrackTracker(url);
            Debug.Log($"[Tobii] TrackTracker({url}) returned {trackerAvailable}");
        }
    }

    public HashSet<GameObject> GetUiObjectsOnPointer(Vector2 screenPos)
    {
        var detected = new HashSet<GameObject>();
        var pointerData = new PointerEventData(eventSystem) { position = screenPos };
        var results = new List<RaycastResult>();
        raycaster.Raycast(pointerData, results);
        foreach (var res in results)
            detected.Add(res.gameObject);
        return detected;
    }

    private void UpdateUiByMode(Vector2 screenPos)
    {
        HashSet<GameObject> hitSet = GetUiObjectsOnPointer(screenPos);
        switch (currentMode)
        {
            case UiHidingMode.None:
                // 아무것도 안함
                UpdateUiByNone(hitSet);
                return;
            case UiHidingMode.Mixed:
                UpdateUiByMixed(screenPos, hitSet);
                break;
            case UiHidingMode.Transparent:
                UpdateUiByTransparent(hitSet);
                break;
            case UiHidingMode.Icon:
                UpdateUiByIcon(hitSet);
                break;
            case UiHidingMode.Scale:
                UpdateUiByScale(hitSet);
                break;
            default:
                UpdateUiByMixed(screenPos, hitSet);
                Debug.LogWarning("알 수 없는 모드");
                break;
        }
    }

    void UpdateUiByNone(HashSet<GameObject> hitSet)
    {
        foreach (var patch in uiPatches)
        {
            UpdateGazeDuration(patch, hitSet);
            if (patch.uiTarget != null)
            {
                patch.uiTarget.SetActive(true);
                var targetRT = patch.uiTarget.GetComponent<RectTransform>();
                if (targetRT != null && originalSizes.ContainsKey(patch.uiTarget))
                {
                    targetRT.sizeDelta = originalSizes[patch.uiTarget];
                }
            }
            if (patch.uiIcon != null)
            {
                patch.uiIcon.SetActive(false);
            }
        }
    }

    void UpdateUiByMixed(Vector2 cursorScreenPos, HashSet<GameObject> hitSet)
    {
        if (raycaster == null || eventSystem == null) return;

        // var hitSet = GetUIObjectsInRange(cursorScreenPos);

        // 2) 각 패치별 처리
        foreach (var patch in uiPatches)
        {
            UpdateGazeDuration(patch, hitSet);

            // string targetName = patch.uiTarget.name;
            // if ((patch.uiIcon != null && hitSet.Contains(patch.uiIcon)) ||
            //     (patch.uiTarget != null && hitSet.Contains(patch.uiTarget)))
            // {
            //     if (!gazeDurations.ContainsKey(targetName))
            //         gazeDurations[targetName] = 0f;
            //     gazeDurations[targetName] += Time.unscaledDeltaTime;
            // }

            GameObject iconGO = patch.uiIcon;
            GameObject targetGO = patch.uiTarget;

            RectTransform iconRT = iconGO?.GetComponent<RectTransform>();
            RectTransform targetRT = targetGO?.GetComponent<RectTransform>();

            // 2-0) 아이콘과의 거리 계산 for boldRadius
            if (iconRT != null)
            {
                Vector2 iconCenterPx = RectTransformUtility.WorldToScreenPoint(null, iconRT.position);
                Vector2 cursorNorm = new Vector2(cursorScreenPos.x / Screen.width, cursorScreenPos.y / Screen.height);
                Vector2 iconNorm = new Vector2(iconCenterPx.x / Screen.width, iconCenterPx.y / Screen.height);
                float iconDistNorm = Vector2.Distance(cursorNorm, iconNorm);
                //Debug.Log($"Icon normalized distance: {iconDistNorm:F3}");

                if (iconGO.activeSelf)
                {
                    // 아이콘이 보이는 상태라면, 정규화 거리 ≤ 0.1 이면 α=1, 아니면 α=0.3
                    SetAlpha(iconGO, iconDistNorm <= 0.2f ? 1f : 0.3f);
                }
            }


            // (A) 커서가 아이콘 위에 있으면 → 아이콘 α=1, 아이콘 비활성화, 타겟 활성화 + 크기 보간
            if (iconGO != null && hitSet.Contains(iconGO))
            {
                // 아이콘 α = 1 (겹쳤을 때 강조)
                SetAlpha(iconGO, 1f);
                // 아이콘 비활성
                iconGO.SetActive(false);

                if (targetGO != null && targetRT != null)
                {
                    if (!targetGO.activeSelf)
                    {
                        // 첫 활성화 시 sizeOut으로 초기화
                        targetRT.sizeDelta = patch.sizeOut;
                        targetGO.SetActive(true);
                    }
                    // 활성화 상태: sizeOut -> original 크기로 보간
                    Vector2 original = originalSizes.ContainsKey(targetGO) ? originalSizes[targetGO] : targetRT.sizeDelta;
                    targetRT.sizeDelta = Vector2.Lerp(targetRT.sizeDelta, original, Time.unscaledDeltaTime * patch.lerpSpeed);
                }

                // stayTime 갱신
                stayTimers[targetGO] = patch.stayTime;
                continue;
            }

            // (B) 커서가 아이콘 위에 있진 않지만, 타겟 위에 있으면 → 타겟 유지 + 크기 보간
            if (targetGO != null && targetRT != null && hitSet.Contains(targetGO))
            {
                if (!targetGO.activeSelf)
                {
                    // 처음 타겟 겹칠 때 활성화 + sizeOut 초기화
                    targetRT.sizeDelta = patch.sizeOut;
                    targetGO.SetActive(true);
                }
                // 타겟 활성 상태: sizeOut -> 원본 크기로 보간
                Vector2 original = originalSizes.ContainsKey(targetGO) ? originalSizes[targetGO] : targetRT.sizeDelta;
                targetRT.sizeDelta = Vector2.Lerp(targetRT.sizeDelta, original, Time.unscaledDeltaTime * patch.lerpSpeed);

                // stayTime 갱신
                stayTimers[targetGO] = patch.stayTime;
                continue;
            }

            // (C) 아이콘/타겟 모두 커서에 겹치지 않을 때
            if (targetGO != null && targetRT != null)
            {
                // 1) stayTime이 남아 있으면 타이머만 깎고 원본 크기 유지
                if (stayTimers.TryGetValue(targetGO, out float remain))
                {
                    remain -= Time.unscaledDeltaTime;
                    if (remain > 0f)
                    {
                        stayTimers[targetGO] = remain;
                        if (targetGO.activeSelf)
                        {
                            // 원본 크기로 부드럽게 유지 (Grow 시 남아도는 interpolation 방지)
                            Vector2 original = originalSizes[targetGO];
                            targetRT.sizeDelta = Vector2.MoveTowards(
                                targetRT.sizeDelta,
                                original,
                                patch.lerpSpeed * Time.unscaledDeltaTime
                            );
                        }
                        continue;
                    }
                    else
                    {
                        // 타이머 만료 → stayTimers에서 제거하고 Shrink 모드로 진입
                        stayTimers.Remove(targetGO);
                    }
                }

                // 2) Shrink 모드: sizeOut까지 부드럽게 줄이기
                if (targetGO.activeSelf)
                {
                    float step = patch.lerpSpeed * 150f * Time.unscaledDeltaTime;
                    targetRT.sizeDelta = Vector2.MoveTowards(
                        targetRT.sizeDelta,
                        patch.sizeOut,
                        step
                    );

                    // sizeOut에 도달하면 비활성화 및 아이콘 복귀
                    if (Vector2.Distance(targetRT.sizeDelta, patch.sizeOut) < 0.01f)
                    {
                        targetRT.sizeDelta = patch.sizeOut;
                        targetGO.SetActive(false);
                        if (iconGO != null)
                        {
                            iconGO.SetActive(true);
                            SetAlpha(iconGO, 0.3f);
                        }
                    }
                    continue;
                }
            }

            // (D) Idle 상태 (타이머도 없고 아직 활성화되지 않음)
            if (iconGO != null && !iconGO.activeSelf)
            {
                iconGO.SetActive(true);
                SetAlpha(iconGO, 0.3f);
            }
            if (targetGO != null && targetGO.activeSelf)
            {
                targetGO.SetActive(false);
                if (targetRT != null)
                    targetRT.sizeDelta = patch.sizeOut;
            }
        }
    }

    void UpdateUiByTransparent(HashSet<GameObject> hitSet)
    {
        if (raycaster == null || eventSystem == null) return;

        // 1) 레이캐스트
        // var hitSet = GetUIObjectsInRange(cursorScreenPos);

        // 2) 각 패치별 처리
        foreach (var p in uiPatches)
        {
            UpdateGazeDuration(p, hitSet);

            GameObject iconGO = p.uiIcon;
            GameObject targetGO = p.uiTarget;
            RectTransform targetRT = targetGO != null ? targetGO.GetComponent<RectTransform>() : null;

            // 아이콘은 항상 비활성화
            if (iconGO != null && iconGO.activeSelf)
                iconGO.SetActive(false);

            // 타겟은 항상 활성화 및 원본 크기 유지
            if (targetGO != null)
            {
                if (!targetGO.activeSelf)
                    targetGO.SetActive(true);

                if (targetRT != null && originalSizes.ContainsKey(targetGO))
                    targetRT.sizeDelta = originalSizes[targetGO];
            }

            // (A) 커서가 타겟 위에 있으면 → 
            //     타겟 자신의 Image는 저장된 원본 투명도, 
            //     자식 Image(자신 제외)는 α = 1
            if (targetGO != null && hitSet.Contains(targetGO) && targetRT != null)
            {
                float origA = originalAlphas.TryGetValue(targetGO, out var a) ? a : 1f;
                foreach (var img in targetGO.GetComponentsInChildren<Image>(true))
                {
                    Color c = img.color;
                    c.a = img.gameObject == targetGO ? origA : 1f;
                    img.color = c;
                }
                stayTimers[targetGO] = p.stayTime;
                continue;
            }

            // (B) 커서가 떠난 뒤 stayTime 동안은 동일하게 유지
            if (targetGO != null && stayTimers.TryGetValue(targetGO, out float remain))
            {
                remain -= Time.unscaledDeltaTime;
                if (remain > 0f)
                {
                    stayTimers[targetGO] = remain;
                    float origA2 = originalAlphas.TryGetValue(targetGO, out var a2) ? a2 : 1f;
                    foreach (var img in targetGO.GetComponentsInChildren<Image>(true))
                    {
                        Color c = img.color;
                        c.a = img.gameObject == targetGO ? origA2 : 1f;
                        img.color = c;
                    }
                    continue;
                }
                else
                {
                    stayTimers.Remove(targetGO);
                }
            }

            // (C) Idle 상태: stayTime 끝났으면 
            //     타겟 자신의 Image α = 0, 
            //     자식 Image는 α = 0.3
            if (targetGO != null && targetRT != null)
            {
                var images = targetGO.GetComponentsInChildren<Image>(true);
                // 자식 Image가 하나라도 있는지 확인
                bool hasChild = false;
                foreach (var img in images)
                {
                    if (img.gameObject != targetGO)
                    {
                        hasChild = true;
                        break;
                    }
                }

                foreach (var img in images)
                {
                    Color c = img.color;
                    if (img.gameObject == targetGO)
                        c.a = hasChild ? 0f : 0.1f;
                    else
                        c.a = 0.1f;
                    img.color = c;
                }
            }
        }
    }

    void UpdateUiByIcon(HashSet<GameObject> hitSet)
    {
        if (raycaster == null || eventSystem == null) return;

        // 1) 레이캐스트
        // var hitSet = GetUIObjectsInRange(cursorScreenPos);

        // 2) 각 패치별 처리
        foreach (var patch in uiPatches)
        {
            UpdateGazeDuration(patch, hitSet);

            string targetName = patch.uiTarget.name;
            if ((patch.uiIcon != null && hitSet.Contains(patch.uiIcon)) ||
                (patch.uiTarget != null && hitSet.Contains(patch.uiTarget)))
            {
                if (!gazeDurations.ContainsKey(targetName))
                    gazeDurations[targetName] = 0f;
                gazeDurations[targetName] += Time.unscaledDeltaTime;
            }

            GameObject iconGO = patch.uiIcon;
            GameObject targetGO = patch.uiTarget;

            RectTransform iconRT = iconGO != null ? iconGO.GetComponent<RectTransform>() : null;
            RectTransform targetRT = targetGO != null ? targetGO.GetComponent<RectTransform>() : null;


            // (A) 커서가 아이콘 위에 있으면 → 아이콘 α=1, 아이콘 비활성화, 타겟 활성화 + 크기 보간
            if (iconGO != null && hitSet.Contains(iconGO))
            {
                // 아이콘 비활성
                iconGO.SetActive(false);

                if (targetGO != null && targetRT != null)
                {
                    if (!targetGO.activeSelf)
                    {
                        // 첫 활성화 시 sizeOut으로 초기화
                        targetRT.sizeDelta = patch.sizeOut;
                        targetGO.SetActive(true);
                    }
                    // 활성화 상태: sizeOut -> original 크기로 보간
                    Vector2 original = originalSizes.ContainsKey(targetGO) ? originalSizes[targetGO] : targetRT.sizeDelta;
                    targetRT.sizeDelta = Vector2.Lerp(targetRT.sizeDelta, original, Time.unscaledDeltaTime * patch.lerpSpeed);
                }

                // stayTime 갱신
                stayTimers[targetGO] = patch.stayTime;
                continue;
            }

            // (B) 커서가 아이콘 위에 있진 않지만, 타겟 위에 있으면 → 타겟 유지 + 크기 보간
            if (targetGO != null && targetRT != null && hitSet.Contains(targetGO))
            {
                if (!targetGO.activeSelf)
                {
                    // 처음 타겟 겹칠 때 활성화 + sizeOut 초기화
                    targetRT.sizeDelta = patch.sizeOut;
                    targetGO.SetActive(true);
                }
                // 타겟 활성 상태: sizeOut -> 원본 크기로 보간
                Vector2 original = originalSizes.ContainsKey(targetGO) ? originalSizes[targetGO] : targetRT.sizeDelta;
                targetRT.sizeDelta = Vector2.Lerp(targetRT.sizeDelta, original, Time.unscaledDeltaTime * patch.lerpSpeed);

                // stayTime 갱신
                stayTimers[targetGO] = patch.stayTime;
                continue;
            }

            // (C) 아이콘/타겟 모두 커서에 겹치지 않을 때
            if (targetGO != null && targetRT != null)
            {
                // 1) stayTime이 남아 있으면 타이머만 깎고 원본 크기 유지
                if (stayTimers.TryGetValue(targetGO, out float remain))
                {
                    remain -= Time.unscaledDeltaTime;
                    if (remain > 0f)
                    {
                        stayTimers[targetGO] = remain;
                        if (targetGO.activeSelf)
                        {
                            // 원본 크기로 부드럽게 유지 (Grow 시 남아도는 interpolation 방지)
                            Vector2 original = originalSizes[targetGO];
                            targetRT.sizeDelta = Vector2.MoveTowards(
                                targetRT.sizeDelta,
                                original,
                                patch.lerpSpeed * Time.unscaledDeltaTime
                            );
                        }
                        continue;
                    }
                    else
                    {
                        // 타이머 만료 → stayTimers에서 제거하고 Shrink 모드로 진입
                        stayTimers.Remove(targetGO);
                    }
                }

                // 2) Shrink 모드: sizeOut까지 부드럽게 줄이기
                if (targetGO.activeSelf)
                {
                    float step = patch.lerpSpeed * 150f * Time.unscaledDeltaTime;
                    targetRT.sizeDelta = Vector2.MoveTowards(
                        targetRT.sizeDelta,
                        patch.sizeOut,
                        step
                    );

                    // sizeOut에 도달하면 비활성화 및 아이콘 복귀
                    if (Vector2.Distance(targetRT.sizeDelta, patch.sizeOut) < 0.01f)
                    {
                        targetRT.sizeDelta = patch.sizeOut;
                        targetGO.SetActive(false);
                        if (iconGO != null)
                        {
                            iconGO.SetActive(true);
                        }
                    }
                    continue;
                }
            }

            // (D) Idle 상태 (타이머도 없고 아직 활성화되지 않음)
            if (iconGO != null && !iconGO.activeSelf)
            {
                iconGO.SetActive(true);
            }
            if (targetGO != null && targetGO.activeSelf)
            {
                targetGO.SetActive(false);
                if (targetRT != null)
                    targetRT.sizeDelta = patch.sizeOut;
            }
        }
    }

    void UpdateUiByScale(HashSet<GameObject> hitSet)
    {
        if (raycaster == null || eventSystem == null) return;

        // 1) 레이캐스트
        // var hitSet = GetUIObjectsInRange(cursorScreenPos);

        // 2) 각 패치별 처리
        foreach (var patch in uiPatches)
        {
            UpdateGazeDuration(patch, hitSet);

            var targetGO = patch.uiTarget;
            if (targetGO == null) continue;
            var targetRT = targetGO.GetComponent<RectTransform>();
            if (targetRT == null) continue;

            // 아이콘은 항상 비활성화
            if (patch.uiIcon != null && patch.uiIcon.activeSelf)
                patch.uiIcon.SetActive(false);

            // 타겟은 항상 활성화
            if (!targetGO.activeSelf)
                targetGO.SetActive(true);

            bool isOver = hitSet.Contains(targetGO);

            // 원본 크기와 sizeOut 값으로 스케일 비율 계산
            if (originalSizes.TryGetValue(targetGO, out var origSize))
            {
                // float scaleX = patch.sizeOut.x / origSize.x;
                // float scaleY = patch.sizeOut.y / origSize.y;
                float scaleX = 0.5f;
                float scaleY = 0.5f;
                Vector3 outScale = new Vector3(scaleX, scaleY, 1f);
                Vector3 newScale = isOver ? Vector3.one : outScale;

                // 커서가 올라오면 스케일 1, 아니면 sizeOut/original 비율
                targetRT.sizeDelta = originalSizes[targetGO];
                targetGO.transform.localScale = Vector3.Lerp(targetGO.transform.localScale, newScale, isOver ? 1f : 0.16f);
            }
        }
    }

    static void SetAlpha(GameObject go, float a)
    {
        if (go == null) return;

        if (go.TryGetComponent(out Image img))
        {
            Color c = img.color;
            c.a = a;
            img.color = c;
        }
    }

    private void UpdateGazeDuration(UiPatch patch, HashSet<GameObject> hitSet)
    {
        if (patch.uiTarget == null && patch.uiIcon == null) return;
        string targetName = patch.uiTarget.name;

        if (hitSet.Contains(patch.uiIcon) || hitSet.Contains(patch.uiTarget))
        {
            if (!gazeDurations.ContainsKey(targetName))
                gazeDurations[targetName] = 0f;
            gazeDurations[targetName] += Time.unscaledDeltaTime;
        }
    }
    #endregion
}
