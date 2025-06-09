using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Tobii.GameIntegration.Net;

[System.Serializable]
public class UiPatchN
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

public class UiControllerN : MonoBehaviour
{
    [Header("필수 레퍼런스")]
    public GraphicRaycaster raycaster;  // Canvas에 붙은 GraphicRaycaster
    public EventSystem eventSystem;   // 씬에 하나 있어야 함

    [Header("시선 커서")]
    public RectTransform gazeCursor;    // (선택) 커서 이미지

    [Header("감지 대상 UI 리스트")]
    public List<UiPatchN> uiPatches = new List<UiPatchN>();

    [Header("디버그/테스트")]
    public bool simulateWithMouse = false;

    private bool trackerAvailable = false;

    // 원본 크기 저장
    private Dictionary<GameObject, Vector2> originalSizes = new Dictionary<GameObject, Vector2>();
    // 머무름 타이머 저장
    private Dictionary<GameObject, float> stayTimers = new Dictionary<GameObject, float>();

    // 추가: 전체 세션 시간
    private float totalGazeTime = 0f;
    // 추가: UI별 머문 시간(초)
    private Dictionary<string, float> gazeDurations = new Dictionary<string, float>();
    // 추가: 로그 파일 경로
    private string logFilePath;

    void Awake()
    {
        if (raycaster == null)
            raycaster = GetComponentInParent<GraphicRaycaster>();
        if (eventSystem == null)
            eventSystem = FindObjectOfType<EventSystem>();
    }

    void Start()
    {
        // 모든 uiTarget에 대해 원본 크기를 저장하고, 초기 상태 설정
        foreach (var p in uiPatches)
        {
            if (p.uiTarget != null)
            {
                RectTransform targetRT = p.uiTarget.GetComponent<RectTransform>();
                if (targetRT != null)
                {
                    // 원본 사이즈 저장
                    originalSizes[p.uiTarget] = targetRT.sizeDelta;
                    // 숨김 상태: sizeOut
                    targetRT.sizeDelta = p.sizeOut;
                    // 비활성화
                    p.uiTarget.SetActive(false);
                }
            }
            if (p.uiIcon != null)
            {
                // 아이콘은 활성화 + 기본 α=0.3
                p.uiIcon.SetActive(true);
                SetAlpha(p.uiIcon, 0.3f);
            }
        }

        // Tobii 초기화
        TobiiGameIntegrationApi.PrelinkAll();
        TobiiGameIntegrationApi.SetApplicationName(Application.productName);
        RefreshTrackWindow();

        //Time.timeScale = 0.1f;

#if UNITY_EDITOR
        // 에디터에서는 Assets/Scripts 폴더 아래에 저장
        logFilePath = Path.Combine(Application.dataPath, "Scripts", "GazeLog.txt");
#else
        // 빌드에서는 사용자 Downloads 폴더에 저장
        // Windows/Mac/Linux 공통: 사용자 홈 디렉터리 + "Downloads"
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string downloads = Path.Combine(home, "Downloads");
        logFilePath = Path.Combine(downloads, "GazeLog.txt");
#endif

        // 파일 쓰기
        File.WriteAllText(logFilePath, $"# Gaze Log ({DateTime.Now:s})\n");
        File.AppendAllText(logFilePath, "TotalTime,Seconds\n");
        File.AppendAllText(logFilePath, "TargetName,Seconds\n");

        Debug.Log($"[GazeLog] 저장 위치: {logFilePath}");
    }

    void Update()
    {
        totalGazeTime += Time.unscaledDeltaTime;
        Vector2 screenPos;

        if (simulateWithMouse)
        {
            screenPos = Input.mousePosition;
            if (gazeCursor != null)
                gazeCursor.anchoredPosition = screenPos;
            UpdateUiPatches(screenPos);
            Debug.Log("mouse screenpos " + screenPos);
            return;
        }

        if (!trackerAvailable)
        {
            RefreshTrackWindow();
        }
        TobiiGameIntegrationApi.Update();
        if (!TobiiGameIntegrationApi.IsTrackerConnected()) return;
        if (!TobiiGameIntegrationApi.IsPresent()) return;

        if (TobiiGameIntegrationApi.TryGetLatestGazePoint(out var gaze))
        {
            Debug.Log("gaze " + gaze.X + " " + gaze.Y);
            float mappedX = Screen.width / 2 + (gaze.X * Screen.width / 2);
            float mappedY = Screen.height / 2 + (gaze.Y * Screen.height / 2);
            screenPos = new Vector2(mappedX, mappedY);
            if (gazeCursor != null)
                gazeCursor.anchoredPosition = screenPos;
            UpdateUiPatches(screenPos);
            Debug.Log("screenpos "+screenPos);
        }
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
        Debug.Log($"[Tobii] Foreground HWND: {hwnd}");
        if (hwnd != IntPtr.Zero)
            trackerAvailable = TobiiGameIntegrationApi.TrackWindow(hwnd);
        Debug.Log($"[Tobii] TrackWindow returned {trackerAvailable}");

        if (!trackerAvailable)
        {
            var url = infos[0].Url;
            trackerAvailable = TobiiGameIntegrationApi.TrackTracker(url);
            Debug.Log($"[Tobii] TrackTracker({url}) returned {trackerAvailable}");
        }
    }


    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    /// <summary>
    /// 단일 레이캐스트로 커서 하단의 모든 UI를 반환
    /// </summary>
    public List<GameObject> GetUIObjectsInRange(Vector2 screenPos, float radius)
    {
        var detected = new HashSet<GameObject>();
        var pointerData = new PointerEventData(eventSystem) { position = screenPos };
        var results = new List<RaycastResult>();
        raycaster.Raycast(pointerData, results);
        foreach (var res in results)
            detected.Add(res.gameObject);
        return new List<GameObject>(detected);
    }

    /// <summary>
    /// 커서가 uiIcon 위에 있으면 아이콘 α 변경, 아이콘 비활성화, 타겟 활성화 + 크기 보간
    /// cursor on target: 타겟 유지 + 크기 보간
    /// cursor 벗어나 stayTime 지난 후: 타겟 크기 원본->sizeOut 보간, 비활성, 아이콘 활성+α=0.3
    /// 그리고 아이콘과의 거리 ≤ boldRadius 이면 α=1 처리
    /// </summary>
    void UpdateUiPatches(Vector2 cursorScreenPos)
    {
        if (raycaster == null || eventSystem == null) return;

        // 1) 레이캐스트
        var pointerData = new PointerEventData(eventSystem) { position = cursorScreenPos };
        var results = new List<RaycastResult>();
        raycaster.Raycast(pointerData, results);
        var hitSet = new HashSet<GameObject>();
        foreach (var r in results)
            hitSet.Add(r.gameObject);   

        // 2) 각 패치별 처리
        foreach (var p in uiPatches)
        {
            string targetName = p.uiTarget.name;
            if ((p.uiIcon != null && hitSet.Contains(p.uiIcon)) ||
                (p.uiTarget != null && hitSet.Contains(p.uiTarget)))
            {
                if (!gazeDurations.ContainsKey(targetName))
                    gazeDurations[targetName] = 0f;
                gazeDurations[targetName] += Time.unscaledDeltaTime;
            }

            GameObject iconGO = p.uiIcon;
            GameObject targetGO = p.uiTarget;

            RectTransform iconRT = iconGO != null ? iconGO.GetComponent<RectTransform>() : null;
            RectTransform targetRT = targetGO != null ? targetGO.GetComponent<RectTransform>() : null;

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
                        targetRT.sizeDelta = p.sizeOut;
                        targetGO.SetActive(true);
                    }
                    // 활성화 상태: sizeOut -> original 크기로 보간
                    Vector2 original = originalSizes.ContainsKey(targetGO) ? originalSizes[targetGO] : targetRT.sizeDelta;
                    targetRT.sizeDelta = Vector2.Lerp(targetRT.sizeDelta, original, Time.unscaledDeltaTime * p.lerpSpeed);
                }

                // stayTime 갱신
                stayTimers[targetGO] = p.stayTime;
                continue;
            }

            // (B) 커서가 아이콘 위에 있진 않지만, 타겟 위에 있으면 → 타겟 유지 + 크기 보간
            if (targetGO != null && targetRT != null && hitSet.Contains(targetGO))
            {
                if (!targetGO.activeSelf)
                {
                    // 처음 타겟 겹칠 때 활성화 + sizeOut 초기화
                    targetRT.sizeDelta = p.sizeOut;
                    targetGO.SetActive(true);
                }
                // 타겟 활성 상태: sizeOut -> 원본 크기로 보간
                Vector2 original = originalSizes.ContainsKey(targetGO) ? originalSizes[targetGO] : targetRT.sizeDelta;
                targetRT.sizeDelta = Vector2.Lerp(targetRT.sizeDelta, original, Time.unscaledDeltaTime * p.lerpSpeed);

                // stayTime 갱신
                stayTimers[targetGO] = p.stayTime;
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
                                p.lerpSpeed * Time.unscaledDeltaTime
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
                    float step = p.lerpSpeed *150f* Time.unscaledDeltaTime;
                    targetRT.sizeDelta = Vector2.MoveTowards(
                        targetRT.sizeDelta,
                        p.sizeOut,
                        step
                    );

                    // sizeOut에 도달하면 비활성화 및 아이콘 복귀
                    if (Vector2.Distance(targetRT.sizeDelta, p.sizeOut) < 0.01f)
                    {
                        targetRT.sizeDelta = p.sizeOut;
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
                    targetRT.sizeDelta = p.sizeOut;
            }
        }
    }

    /// <summary>
    /// GameObject에 붙은 Image 컴포넌트의 알파만 설정
    /// </summary>
    static void SetAlpha(GameObject go, float a)
    {
        if (go == null) return;
        var img = go.GetComponent<Image>();
        if (img != null)
        {
            Color c = img.color;
            c.a = a;
            img.color = c;
        }
    }

    void OnApplicationQuit()
    {
        using (var sw = new StreamWriter(logFilePath, true))
        {
            sw.WriteLine($"{totalGazeTime:F3}");          // 총 머문 시간
            foreach (var kv in gazeDurations)
            {
                sw.WriteLine($"{kv.Key},{kv.Value:F3}");
            }
        }

        TobiiGameIntegrationApi.StopTracking();
        TobiiGameIntegrationApi.Shutdown();
    }
}
