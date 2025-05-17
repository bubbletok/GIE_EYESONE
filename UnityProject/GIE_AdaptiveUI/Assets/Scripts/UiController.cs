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
        Vector2 pos = new Vector2(gaze.X * Screen.width, (1f - gaze.Y) * Screen.height);
        logTimer += Time.unscaledDeltaTime;
        if (logTimer >= 1f)
        {
            logTimer = 0f;
            Debug.Log($"[Gaze] norm=({gaze.X:F3},{gaze.Y:F3}) px=({pos.x:F0},{pos.y:F0})");
        }

        var results = new List<RaycastResult>();
        raycaster.Raycast(new PointerEventData(EventSystem.current) { position = pos }, results);
        var hits = new HashSet<GameObject>();
        foreach (var rr in results)
        {
            var go = rr.gameObject;
            hits.Add(go);
            if (!_slots.TryGetValue(go, out var slot))
            {
                slot = new Slot { rt = go.GetComponent<RectTransform>(), baseScale = go.transform.localScale };
                _slots[go] = slot;
            }
            if (slot.gazeEnterTime < 0f)
                slot.gazeEnterTime = Time.unscaledTime;

            float t = Time.unscaledTime - slot.gazeEnterTime;
            float goal = t >= dwellTime ? focusScale : 1f;
            slot.curScale = Mathf.Lerp(slot.curScale, goal, Time.unscaledDeltaTime * lerpSpeed);
            slot.rt.localScale = slot.baseScale * slot.curScale;
        }
        foreach (var kv in _slots)
        {
            if (hits.Contains(kv.Key)) continue;
            var slot = kv.Value;
            slot.gazeEnterTime = -1f;
            slot.curScale = Mathf.Lerp(slot.curScale, 1f, Time.unscaledDeltaTime * lerpSpeed);
            slot.rt.localScale = slot.baseScale * slot.curScale;
        }
    }

    void OnApplicationQuit()
    {
        TobiiGameIntegrationApi.StopTracking();
        TobiiGameIntegrationApi.Shutdown();
    }
}
