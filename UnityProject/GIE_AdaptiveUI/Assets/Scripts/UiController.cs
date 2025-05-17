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
public class UiController : MonoBehaviour
{
    [Header("필수 레퍼런스")]
    public GraphicRaycaster raycaster;
    public Camera canvasCamera;

    [Header("시선 반응 파라미터")]
    [Range(1f, 3f)] public float focusScale = 1.2f;
    [Range(.01f, .5f)] public float dwellTime = .15f;
    public float lerpSpeed = 15f;

    private bool trackerAvailable;
    private float logTimer;

    [Header("시선 커서")]
    /// Canvas 위에 움직일 UI 오브젝트 (UI Image 등)
    public RectTransform gazeCursor;

    [Header("영향 대상 UI 리스트")]
    public List<RectTransform> uiTargets = new List<RectTransform>();
    [Header("영향 반경 (픽셀)")]
    public float influenceRadius = 50f;

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
        float mappedY = gaze.Y * 300f;
        Vector2 pos = new Vector2(mappedX, mappedY);

        // 시선 커서가 있으면 해당 좌표로 이동
        if (gazeCursor != null)
        {
            // Canvas 로컬 좌표로 변환 없이 바로 앵커드 포지션에 할당
            gazeCursor.anchoredPosition = pos;
        }

        // ── 이하 기존 로직 유지 ──
        logTimer += Time.unscaledDeltaTime;
        if (logTimer >= 1f)
        {
            logTimer = 0f;
            Debug.Log($"[Gaze] norm=({gaze.X:F3},{gaze.Y:F3}) → mapped px=({pos.x:F0},{pos.y:F0})");
        }

        // UI Raycast & 확대/축소 등…
        // (필요하다면 raycaster.Raycast에 사용할 포지션도 pos로 교체)
        foreach (var target in uiTargets)
        {
            if (target == null)
                continue;

            // RectTransform의 월드 좌표를 화면 좌표로 변환 (Overlay 모드일 때 카메라 파라미터는 무시됨)
            Vector2 targetScreenPos = RectTransformUtility.WorldToScreenPoint(null, target.position);

            // 시선과 대상 간 거리 계산
            float dist = Vector2.Distance(pos, targetScreenPos);
            Debug.Log(dist);

            // 반경 이내면 focusScale, 아니면 1
            float goalScale = dist <= influenceRadius ? focusScale : 1f;

            // 부드러운 크기 보간
            Vector3 baseScale = target.localScale;
            target.localScale = Vector3.Lerp(
                target.localScale,
                baseScale * goalScale,
                Time.deltaTime * lerpSpeed
            );
        }
    }

    void OnApplicationQuit()
    {
        TobiiGameIntegrationApi.StopTracking();
        TobiiGameIntegrationApi.Shutdown();
    }
}
