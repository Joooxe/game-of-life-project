using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LifeController : MonoBehaviour
{
    [SerializeField] LifeRenderer rendererRef;

    [Header("Grid")] [SerializeField] int width = 128;
    [SerializeField] int height = 128;

    [Header("Simulation")] [SerializeField, Range(0.01f, 1f)]
    float stepDelay = 0.08f;

    [SerializeField, Range(0f, 1f)] float randomFill = 0.15f;
    [SerializeField] bool running = true;

    [Header("Camera / Viewport")] [SerializeField]
    Camera targetCamera;

    [SerializeField] RawImage simViewport;
    [SerializeField] AspectRatioFitter simFitter;

    [SerializeField, Min(2)] int visibleCellsVertically = 72;
    [SerializeField, Min(1)] int zoomWheelStep = 6;
    [SerializeField, Min(2)] int minVisibleCells = 4;
    [SerializeField, Min(2)] int maxVisibleCells = 4096;

    [SerializeField, Min(0f)] float panSpeedCellsPerSec = 200f;
    [SerializeField] bool clampViewToGrid = true;

    [Header("UI Links")] [SerializeField] NeighborhoodEditor neighborhoodEditor;
    [SerializeField] RulesRange rulesRange;

    [Header("Scale (slider)")] [SerializeField]
    Slider scaleSlider;

    [SerializeField] bool scalePowersOfTwo = true;
    [SerializeField] int scaleMinStep = 0;
    [SerializeField] int scaleMaxStep = 5;
    [SerializeField] int scaleMinFactor = 1;
    [SerializeField] int scaleMaxFactor = 16;

    LifeGrid _grid;
    System.Random _rng = new System.Random();
    Coroutine _loop;

    int[] _kdx = System.Array.Empty<int>();
    int[] _kdy = System.Array.Empty<int>();
    int _kcount = 0;

    int _baseW, _baseH;

    bool _resizePending;
    int _pendingW, _pendingH;
    bool _fitCameraPending;

    void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        _grid = new LifeGrid(width, height);
        rendererRef.Init(_grid);
        _grid.Randomize(randomFill, _rng);
        rendererRef.Redraw();

        _baseW = width;
        _baseH = height;

        ApplyAbsoluteZoom();

        if (rulesRange)
        {
            rulesRange.OnValuesChanged += ApplyRulesFromUI;
        }

        if (neighborhoodEditor && neighborhoodEditor.Data != null)
        {
            neighborhoodEditor.Data.OnChanged += OnNeighborhoodChanged;
            OnNeighborhoodChanged();
        }
        else
        {
            _grid.SetKernel(System.Array.Empty<int>(), System.Array.Empty<int>(), 0);
            _kcount = 0;
        }

        if (scaleSlider)
        {
            scaleSlider.onValueChanged.AddListener(OnScaleSliderChanged);
        }

        _loop = StartCoroutine(SimLoop());
        ApplyRulesFromUI();
    }

    void OnDestroy()
    {
        if (rulesRange)
        {
            rulesRange.OnValuesChanged -= ApplyRulesFromUI;
        }

        if (neighborhoodEditor && neighborhoodEditor.Data != null)
        {
            neighborhoodEditor.Data.OnChanged -= OnNeighborhoodChanged;
        }

        if (scaleSlider)
        {
            scaleSlider.onValueChanged.RemoveListener(OnScaleSliderChanged);
        }
    }

    IEnumerator SimLoop()
    {
        while (true)
        {
            if (running)
            {
                _grid.Step();
                rendererRef.Redraw();
            }

            yield return new WaitForSeconds(stepDelay);
        }
    }

    void Update()
    {
        if (_resizePending && Application.isPlaying && _grid != null)
        {
            _resizePending = false;
            if (_pendingW != _grid.Width || _pendingH != _grid.Height)
                Resize(_pendingW, _pendingH);

            if (_fitCameraPending)
            {
                _fitCameraPending = false;
                ApplyAbsoluteZoom();
                ClampCameraToGrid();
            }
        }

        var kb = Keyboard.current;
        if (kb == null)
        {
            return;
        }

        if (kb.spaceKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)
        {
            running = !running;
        }

        if (kb.minusKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)
        {
            stepDelay = Mathf.Min(1f, stepDelay + 0.02f);
        }

        if (kb.equalsKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
        {
            stepDelay = Mathf.Max(0.01f, stepDelay - 0.02f);
        }
        if (kb.rKey.wasPressedThisFrame)
        {
            _grid.Randomize(randomFill, _rng);
            rendererRef.Redraw();
        }

        if (kb.cKey.wasPressedThisFrame)
        {
            _grid.Clear();
            rendererRef.Redraw();
        }

        var mouse = Mouse.current;
        if (mouse != null)
        {
            float wheel = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(wheel) > 0.01f)
            {
                int dir = wheel > 0 ? -1 : 1;
                visibleCellsVertically = Mathf.Clamp(
                    visibleCellsVertically + dir * zoomWheelStep,
                    minVisibleCells, maxVisibleCells);
                ApplyAbsoluteZoom();
                ClampCameraToGrid();
            }
        }

        {
            Vector2 pan = Vector2.zero;
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed)
            {
                pan.x -= 1f;
            }

            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed)
            {
                pan.x += 1f;
            }

            if (kb.downArrowKey.isPressed || kb.sKey.isPressed)
            {
                pan.y -= 1f;
            }

            if (kb.upArrowKey.isPressed || kb.wKey.isPressed)
            {
                pan.y += 1f;
            }

            if (pan.sqrMagnitude > 0f)
            {
                pan = pan.normalized;
                float dt = Time.deltaTime;
                var cam = targetCamera ? targetCamera : Camera.main;
                var pos = cam.transform.position;
                pos += new Vector3(pan.x, pan.y, 0f) * panSpeedCellsPerSec * dt;
                cam.transform.position = pos;
                ClampCameraToGrid();
            }
        }

        if (!running && mouse != null && simViewport != null)
        {
            Vector2 sp = mouse.position.ReadValue();

            if (RectTransformUtility.RectangleContainsScreenPoint(simViewport.rectTransform, sp, null))
            {
                if (mouse.leftButton.isPressed || mouse.rightButton.isPressed)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            simViewport.rectTransform, sp, null, out var local))
                    {
                        Rect r = simViewport.rectTransform.rect;
                        float nx = Mathf.InverseLerp(r.xMin, r.xMax, local.x);
                        float ny = Mathf.InverseLerp(r.yMin, r.yMax, local.y);

                        var cam = targetCamera ? targetCamera : Camera.main;
                        float halfH = cam.orthographicSize;
                        float halfW = halfH * cam.aspect;

                        float worldX = (nx - 0.5f) * 2f * halfW + cam.transform.position.x;
                        float worldY = (ny - 0.5f) * 2f * halfH + cam.transform.position.y;

                        if (rendererRef.WorldToCell(new Vector3(worldX, worldY, 0f), out int gx, out int gy))
                        {
                            if (mouse.leftButton.isPressed)
                            {
                                _grid.Set(gx, gy, true);
                            }

                            if (mouse.rightButton.isPressed)
                            {
                                _grid.Set(gx, gy, false);
                            }
                            rendererRef.Redraw();
                        }
                    }
                }
            }
        }
    }

    void OnValidate()
    {
        if (width < 1)
        {
            width = 1;
        }

        if (height < 1)
        {
            height = 1;
        }
        stepDelay = Mathf.Clamp(stepDelay, 0.01f, 1f);
        randomFill = Mathf.Clamp01(randomFill);

        if (Application.isPlaying)
        {
            _pendingW = width;
            _pendingH = height;
            _resizePending = true;
            _fitCameraPending = true;
        }
    }

    public void Resize(int newW, int newH)
    {
        if (newW < 1)
        {
            newW = 1;
        }

        if (newH < 1)
        {
            newH = 1;
        }

        if (_grid.Width == newW && _grid.Height == newH)
        {
            return;
        }

        _grid.Resize(newW, newH);
        rendererRef.OnGridResized();
        rendererRef.Redraw();

        ApplyAbsoluteZoom();
        ClampCameraToGrid();

        width = _grid.Width;
        height = _grid.Height;
    }

    void OnNeighborhoodChanged()
    {
        var nd = neighborhoodEditor?.Data;

        if (nd == null || !nd.Active)
        {
            _grid.SetKernel(System.Array.Empty<int>(), System.Array.Empty<int>(), 0);
            _kcount = 0;
            return;
        }

        nd.GetOffsets(out _kdx, out _kdy, out _kcount);
        _grid.SetKernel(_kdx, _kdy, _kcount);
    }

    void ApplyRulesFromUI()
    {
        if (!rulesRange)
        {
            _grid.SetRules(2, 3, 3, 3);
            return;
        }

        _grid.SetRules(rulesRange.SMin, rulesRange.SMax, rulesRange.BMin, rulesRange.BMax);
    }

    public void OnScaleSliderChanged(float value)
    {
        if (_grid == null) return;

        if (_baseW <= 0 || _baseH <= 0)
        {
            _baseW = width;
            _baseH = height;
        }

        int factor;
        if (scalePowersOfTwo)
        {
            float t = scaleSlider ? Mathf.InverseLerp(scaleSlider.minValue, scaleSlider.maxValue, value) : value;
            int step = Mathf.RoundToInt(Mathf.Lerp(scaleMinStep, scaleMaxStep, Mathf.Clamp01(t)));
            step = Mathf.Clamp(step, scaleMinStep, scaleMaxStep);
            factor = 1 << step;
        }
        else
        {
            float t = scaleSlider ? Mathf.InverseLerp(scaleSlider.minValue, scaleSlider.maxValue, value) : value;
            factor = Mathf.RoundToInt(Mathf.Lerp(scaleMinFactor, scaleMaxFactor, Mathf.Clamp01(t)));
            if (factor < 1)
            {
                factor = 1;
            }
        }

        int newW = Mathf.Max(1, _baseW * factor);
        int newH = Mathf.Max(1, _baseH * factor);

        if (newW == _grid.Width && newH == _grid.Height)
        {
            return;
        }

        Resize(newW, newH);

        _grid.Clear();
        _grid.Randomize(randomFill, _rng);
        rendererRef.Redraw();

        FitCameraToGrid();
        var cam = targetCamera ? targetCamera : Camera.main;
        if (cam)
        {
            visibleCellsVertically = Mathf.Max(minVisibleCells, Mathf.Min(maxVisibleCells, Mathf.RoundToInt(cam.orthographicSize * 2f)));
        }
    }

    void FitCameraToGrid()
    {
        var cam = targetCamera ? targetCamera : Camera.main;
        if (!cam)
        {
            return;
        }
        cam.orthographic = true;

        float gridW = width;
        float gridH = height;
        float aspect = cam.aspect;

        float sizeByHeight = gridH * 0.5f;
        float sizeByWidth = gridW * 0.5f / Mathf.Max(0.0001f, aspect);

        cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
        cam.transform.position = new Vector3(0f, 0f, cam.transform.position.z);
    }

    void ApplyAbsoluteZoom()
    {
        var cam = targetCamera ? targetCamera : Camera.main;
        if (!cam)
        {
            return;
        }
        cam.orthographic = true;

        int cells = Mathf.Clamp(visibleCellsVertically, minVisibleCells, maxVisibleCells);
        cam.orthographicSize = cells * 0.5f;
    }

    void ClampCameraToGrid()
    {
        if (!clampViewToGrid)
        {
            return;
        }

        var cam = targetCamera ? targetCamera : Camera.main;
        if (!cam)
        {
            return;
        }

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        float gx = width * 0.5f;
        float gy = height * 0.5f;

        if (halfW >= gx || halfH >= gy)
        {
            cam.transform.position = new Vector3(0f, 0f, cam.transform.position.z);
            return;
        }

        float minX = -gx + halfW;
        float maxX = gx - halfW;
        float minY = -gy + halfH;
        float maxY = gy - halfH;

        var p = cam.transform.position;
        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.y = Mathf.Clamp(p.y, minY, maxY);
        cam.transform.position = p;
    }
}