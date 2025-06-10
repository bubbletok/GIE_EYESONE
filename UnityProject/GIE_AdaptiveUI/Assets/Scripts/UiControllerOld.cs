using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Tobii.GameIntegration.Net;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI 오브젝트에 시선 응시 시 확대/축소, 1초 로그,
/// TrackWindow 실패 시 TrackTracker URL 구독으로 폴백하는 컨트롤러
/// </summary>
[System.Obsolete("This class is deprecated. Use UiController instead.")]
public class UiControllerOld : MonoBehaviour
{
    [Header("필수 레퍼런스")]
    public GraphicRaycaster raycaster;
    public Camera canvasCamera;
    public EventSystem eventSystem;

    [Header("시선 반응 파라미터")]
    public float focusScale = 2f;
    public float stayTime = 0.15f;
    public float lerpSpeed = 5f;

    private Dictionary<GameObject, float> gazeStayTimers = new();
    private HashSet<GameObject> currentlyFocused = new();

    private bool trackerAvailable;
    private float logTimer;

    [Header("시선 커서")]
    /// Canvas 위에 움직일 UI 오브젝트 (UI Image 등)
    public RectTransform gazeCursor;

    [Header("감지 대상 UI 리스트")]
    public List<RectTransform> uiTargets = new List<RectTransform>();
    [Header("감지 반경")]
    public float detectionRadius = 50f;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    private class Slot
    {
        public RectTransform rt;
        public Vector3 baseScale;
        public float curScale = 1f;
        public float gazeEnterTime = -999f;
    }
    private readonly Dictionary<GameObject, Slot> _slots = new Dictionary<GameObject, Slot>();

    void Awake()
    {
        TobiiGameIntegrationApi.PrelinkAll();
        TobiiGameIntegrationApi.SetApplicationName(Application.productName);
        if (raycaster == null)
            raycaster = GetComponentInParent<GraphicRaycaster>();
    }

    IEnumerator Start()
    {
        yield return null;
        RefreshTrackWindow();
    }

    private void RefreshTrackWindow()
    {
        TobiiGameIntegrationApi.Update();
        TobiiGameIntegrationApi.UpdateTrackerInfos();
        var infos = TobiiGameIntegrationApi.GetTrackerInfos();

        if (infos == null || infos.Count == 0)
        {
            Debug.LogWarning("[TGI] 트래커 목록이 비어 있습니다.");
            trackerAvailable = false;
            return;
        }

        Debug.Log($"[TGI] 발견된 트래커 수: {infos.Count}");
        foreach (var ti in infos)
            Debug.Log($"    · {ti.FriendlyName} (URL={ti.Url}), Caps={ti.Capabilities}");

        // 1) TrackWindow 시도
        IntPtr hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            Debug.LogWarning("[TGI] 활성 윈도우 핸들 얻기 실패");
        }
        else
        {
            trackerAvailable = TobiiGameIntegrationApi.TrackWindow(hwnd);
            if (trackerAvailable)
            {
                Debug.Log("[TGI] TrackWindow 기반 구독 성공");
                return;
            }
            Debug.LogWarning("[TGI] TrackWindow 기반 구독 실패");
        }

        // 2) TrackWindow 실패 시 URL 구독 폴백
        var first = infos[0];
        trackerAvailable = TobiiGameIntegrationApi.TrackTracker(first.Url);
        if (trackerAvailable)
            Debug.Log($"[TGI] URL 기반 구독 성공: {first.FriendlyName}");
        else
            Debug.LogWarning($"[TGI] URL 기반 구독도 실패: {first.FriendlyName}");
    }

    void Update()
    {
        if (!trackerAvailable) return;

        TobiiGameIntegrationApi.Update();
        if (!TobiiGameIntegrationApi.IsTrackerConnected())
        {
            Debug.LogWarning("[TGI] 트래커 연결 끊김, 재시도");
            RefreshTrackWindow();
            return;
        }

        if (!TobiiGameIntegrationApi.IsPresent())
        {
            Debug.Log("[TGI] 사용자 부재 감지");
            return;
        }

        if (TobiiGameIntegrationApi.TryGetLatestGazePoint(out var gaze))
            ProcessGaze(gaze);
    }

    private void ProcessGaze(GazePoint gaze)
    {
        // ── 수정된 부분: 정규화된 gaze(0~1)를 800×600 좌표계로 매핑
        float mappedX = gaze.X * 400f;
        float mappedY = gaze.Y * 225f;
        Vector2 screenPos = new Vector2(mappedX, mappedY);

        // 시선 커서가 있으면 해당 좌표로 이동
        if (gazeCursor != null)
        {
            // Canvas 로컬 좌표로 변환 없이 바로 앵커드 포지션에 할당
            gazeCursor.anchoredPosition = screenPos;
        }

        // ── 이하 기존 로직 유지 ──
        logTimer += Time.unscaledDeltaTime;
        if (logTimer >= 1f)
        {
            logTimer = 0f;
            Debug.Log($"[Gaze] norm=({gaze.X:F3},{gaze.Y:F3}) → mapped px=({screenPos.x:F0},{screenPos.y:F0})");
        }


        DetectUI(screenPos);
    }

    private void DetectUI(Vector2 screenPos)
    {
        if (gazeCursor != null)
        {
            gazeCursor.anchoredPosition = screenPos;

            // ── 커서 주변 UI 감지
            Vector2 cursorScreenPos = RectTransformUtility.WorldToScreenPoint(raycaster.eventCamera, gazeCursor.position);

            Vector2[] offsets = new Vector2[]
            {
            Vector2.zero,
            Vector2.left * detectionRadius,
            Vector2.right * detectionRadius,
            Vector2.up * detectionRadius,
            Vector2.down * detectionRadius,
            new Vector2(-detectionRadius, -detectionRadius),
            new Vector2(detectionRadius, -detectionRadius),
            new Vector2(-detectionRadius, detectionRadius),
            new Vector2(detectionRadius, detectionRadius),
            };

            HashSet<GameObject> detectedThisFrame = new();

            foreach (var offset in offsets)
            {
                Vector2 samplePos = cursorScreenPos + offset;
                PointerEventData eventData = new PointerEventData(eventSystem) { position = samplePos };
                List<RaycastResult> results = new List<RaycastResult>();
                raycaster.Raycast(eventData, results);

                foreach (var result in results)
                {
                    GameObject go = result.gameObject;
                    if (go != gazeCursor.gameObject)
                    {
                        detectedThisFrame.Add(go);
                    }
                }
            }

            // 감지된 UI 로그 출력
            foreach (var go in detectedThisFrame)
            {
                Debug.Log($"[UI 감지] 커서 주변 UI: {go.name}");
            }

            // 감지된 UI 처리
            HandleDetectedUI(detectedThisFrame);
        }
    }

    private void HandleDetectedUI(HashSet<GameObject> detected)
    {
        foreach (var go in detected)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null) continue;

            // 감지되었으므로 타이머 갱신
            gazeStayTimers[go] = stayTime;

            // 즉시 확대
            rt.localScale = Vector3.Lerp(rt.localScale, Vector3.one * focusScale, Time.unscaledDeltaTime * lerpSpeed);

            // 이미지 컴포넌트가 있으면 불투명하게
            var img = go.GetComponent<Image>();
            if (img != null)
            {
                Color c = img.color;
                c.a = 1f;
                img.color = c;
            }

            currentlyFocused.Add(go);
        }

        // 감지되지 않은 UI 처리
        var keys = new List<GameObject>(gazeStayTimers.Keys);
        foreach (var go in keys)
        {
            if (!detected.Contains(go))
            {
                gazeStayTimers[go] -= Time.unscaledDeltaTime;

                RectTransform rt = go.GetComponent<RectTransform>();
                if (rt == null) continue;

                // 스케일 되돌리기
                rt.localScale = Vector3.Lerp(rt.localScale, Vector3.one, Time.unscaledDeltaTime * lerpSpeed);

                // 스케일 거의 1이면 → 반투명 처리 & 정리
                if (Vector3.Distance(rt.localScale, Vector3.one) < 0.01f)
                {
                    rt.localScale = Vector3.one;

                    var img = go.GetComponent<Image>();
                    if (img != null)
                    {
                        Color c = img.color;
                        c.a = 0.1f;
                        img.color = c;
                    }

                    gazeStayTimers.Remove(go);
                    currentlyFocused.Remove(go);
                }
            }
        }
    }



    void OnApplicationQuit()
    {
        TobiiGameIntegrationApi.StopTracking();
        TobiiGameIntegrationApi.Shutdown();
    }
}
