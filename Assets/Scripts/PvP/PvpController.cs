using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PvpController : MonoBehaviour
{
    [Header("Match Timer")] [SerializeField]
    TMP_Text timerText;

    [SerializeField] TMP_Text winnerText;
    [SerializeField, Min(1f)] float matchDuration = 60f;
    float _timeLeft;

    [Header("References")] [SerializeField]
    PvPRenderer rendererRef;

    [SerializeField] Camera simCamera;
    [SerializeField] RawImage simViewport;

    [Header("Grid")] [SerializeField] int width = 64;
    [SerializeField] int height = 64;

    [Header("Conway Rules")] [SerializeField]
    int SMin = 2, SMax = 3, BMin = 3, BMax = 3;

    [SerializeField, Range(0.01f, 0.5f)] float stepDelay = 0.08f;

    [Header("UI - Players")] [SerializeField]
    TMP_Text leftPiecesText;

    [SerializeField] TMP_Text rightPiecesText;
    [SerializeField] Button leftReadyBtn;
    [SerializeField] Button rightReadyBtn;

    [Header("UI - Bottom")] [SerializeField]
    TMP_Text scoreText;

    [Header("Seeding")] [SerializeField] int startPiecesPerPlayer = 64;
    [SerializeField] bool disallowPlaceOnOccupied = true;

    [Header("Tie-break on birth")] [SerializeField]
    bool alternateTieAdvantage = true;

    LifeGrid _grid;
    Coroutine _loop;

    byte[] _owners;
    byte[] _ownersNext;
    bool[] _prevAlive;
    byte[] _prevOwners;
    int[] _kdx, _kdy;
    int _kCount;
    bool _leftReady, _rightReady;
    int _leftPieces, _rightPieces;

    PatternId _currentPattern = PatternId.Dot;

    int _score;
    int _redBornLast, _blueBornLast;
    bool _tieToLeft = true;

    enum Phase
    {
        Seeding,
        Running,
        Finished
    }

    Phase _phase = Phase.Seeding;

    void Start()
    {
        _grid = new LifeGrid(width, height);
        rendererRef.Init(_grid);

        int n = width * height;
        _owners = new byte[n];
        _ownersNext = new byte[n];
        _prevAlive = new bool[n];
        _prevOwners = new byte[n];
        rendererRef.BindOwners(_owners);
        rendererRef.Redraw();

        var dx = new List<int>(8);
        var dy = new List<int>(8);
        for (int oy = -1; oy <= 1; oy++)
        for (int ox = -1; ox <= 1; ox++)
            if (!(ox == 0 && oy == 0))
            {
                dx.Add(ox);
                dy.Add(oy);
            }

        _kdx = dx.ToArray();
        _kdy = dy.ToArray();
        _kCount = 8;
        _grid.SetKernel(_kdx, _kdy, _kCount);
        _grid.SetRules(SMin, SMax, BMin, BMax);

        _leftPieces = startPiecesPerPlayer;
        _rightPieces = startPiecesPerPlayer;
        UpdatePiecesUI();
        UpdateScoreUI();

        if (simViewport)
        {
            simViewport.raycastTarget = false;
        }

        _loop = StartCoroutine(SimLoop());
    }

    IEnumerator SimLoop()
    {
        var wait = new WaitForSeconds(stepDelay);
        while (true)
        {
            if (_phase == Phase.Running)
            {
                if (_timeLeft <= 0f)
                {
                    EndMatch();
                }
                SnapshotPrevious();
                _grid.Step();
                MajorityAndScore();
                rendererRef.Redraw();
            }

            yield return wait;
        }
    }

    void Update()
    {
        if (_phase == Phase.Running)
        {
            _timeLeft -= Time.deltaTime;
            if (_timeLeft < 0f)
            {
                _timeLeft = 0f;
            }
            UpdateTimerUI();
            if (_timeLeft <= 0f)
            {
                EndMatch();
            }
        }

        if (_phase != Phase.Seeding)
        {
            return;
        }

        var mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Vector2 sp = mouse.position.ReadValue();
        if (simViewport == null || !RectTransformUtility.RectangleContainsScreenPoint(simViewport.rectTransform, sp, null))
        {
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(simViewport.rectTransform, sp, null,
                out var local))
        {
            return;
        }

        Rect r = simViewport.rectTransform.rect;
        float nx = Mathf.InverseLerp(r.xMin, r.xMax, local.x);
        float ny = Mathf.InverseLerp(r.yMin, r.yMax, local.y);

        float halfH = simCamera.orthographicSize;
        float halfW = halfH * simCamera.aspect;
        float worldX = (nx - 0.5f) * 2f * halfW + simCamera.transform.position.x;
        float worldY = (ny - 0.5f) * 2f * halfH + simCamera.transform.position.y;

        if (!rendererRef.WorldToCell(new Vector3(worldX, worldY, 0f), out int gx, out int gy))
        {
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            TryPlacePattern(1, gx, gy);
        }

        if (mouse.rightButton.wasPressedThisFrame)
        {
            TryPlacePattern(2, gx, gy);
        }
    }

    public void SelectPattern(PatternId id) => _currentPattern = id;

    public void ToggleReady(bool isLeft)
    {
        if (_phase != Phase.Seeding)
        {
            return;
        }

        if (isLeft)
        {
            _leftReady = !_leftReady;
        }
        else _rightReady = !_rightReady;

        SetReadyVisual(isLeft ? _leftReady : _rightReady, isLeft);

        if (_leftReady && _rightReady)
        {
            StartMatch();
        }
    }

    void StartMatch()
    {
        _phase = Phase.Running;
        _tieToLeft = true;
        _timeLeft = matchDuration;

        if (leftReadyBtn)
        {
            leftReadyBtn.interactable = false;
        }

        if (rightReadyBtn)
        {
            rightReadyBtn.interactable = false;
        }

        if (winnerText)
        {
            winnerText.text = string.Empty;
        }
        UpdateTimerUI();
    }

    void EndMatch()
    {
        _phase = Phase.Finished;
        string winner = (_score > 0) ? "P1 (Red) wins!" : "P2 (Blue) wins!";
        if (winnerText)
        {
            winnerText.text = winner;
        }

        _timeLeft = 0f;
        UpdateTimerUI();
    }

    void UpdateTimerUI()
    {
        if (!timerText)
        {
            return;
        }
        int t = Mathf.CeilToInt(_timeLeft);
        int m = t / 60;
        int s = t % 60;
        timerText.text = $"{m}:{s:00}";
    }

    void TryPlacePattern(byte owner, int cx, int cy)
    {
        if (owner == 1 && _leftReady)
        {
            return;
        }

        if (owner == 2 && _rightReady)
        {
            return;
        }

        var cells = PatternLibrary.GetCells(_currentPattern);
        int cost = cells.Count;

        ref int pool = ref (owner == 1 ? ref _leftPieces : ref _rightPieces);
        if (pool < cost)
        {
            return;
        }

        if (disallowPlaceOnOccupied)
        {
            foreach (var off in cells)
            {
                int x = cx + off.x, y = cy + off.y;
                if ((uint)x >= (uint)width || (uint)y >= (uint)height)
                {
                    return;
                }

                if (_owners[y * width + x] != 0)
                {
                    return;
                }
            }
        }

        foreach (var off in cells)
        {
            int x = cx + off.x, y = cy + off.y;
            if ((uint)x >= (uint)width || (uint)y >= (uint)height)
            {
                continue;
            }
            _grid.Set(x, y, true);
            _owners[y * width + x] = owner;
        }

        pool -= cost;
        UpdatePiecesUI();
        rendererRef.Redraw();
    }

    void SnapshotPrevious()
    {
        int n = width * height;
        for (int y = 0, i = 0; y < height; y++)
        for (int x = 0; x < width; x++, i++)
            _prevAlive[i] = _grid.Get(x, y);

        System.Array.Copy(_owners, _prevOwners, n);
    }

    void MajorityAndScore()
    {
        _redBornLast = 0;
        _blueBornLast = 0;
        System.Array.Clear(_ownersNext, 0, _ownersNext.Length);

        for (int y = 0, i = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++, i++)
            {
                bool wasAlive = _prevAlive[i];
                bool nowAlive = _grid.Get(x, y);

                if (!nowAlive)
                {
                    _ownersNext[i] = 0;
                    continue;
                }

                if (wasAlive)
                {
                    _ownersNext[i] = _prevOwners[i];
                    continue;
                }

                int red = 0, blue = 0;
                for (int k = 0; k < _kCount; k++)
                {
                    int nx = x + _kdx[k];
                    int ny = y + _kdy[k];
                    if ((uint)nx >= (uint)width || (uint)ny >= (uint)height)
                    {
                        continue;
                    }

                    int j = ny * width + nx;
                    if (!_prevAlive[j])
                    {
                        continue;
                    }

                    byte own = _prevOwners[j];
                    if (own == 1)
                    {
                        red++;
                    }
                    else if (own == 2)
                    {
                        blue++;
                    }
                }

                byte owner;
                if (red > blue)
                {
                    owner = 1;
                    _redBornLast++;
                    _score++;
                }
                else if (blue > red)
                {
                    owner = 2;
                    _blueBornLast++;
                    _score--;
                }
                else
                {
                    if (alternateTieAdvantage)
                    {
                        owner = _tieToLeft ? (byte)1 : (byte)2;
                        if (_tieToLeft)
                        {
                            _redBornLast++;
                            _score++;
                        }
                        else
                        {
                            _blueBornLast++;
                            _score--;
                        }

                        _tieToLeft = !_tieToLeft;
                    }
                    else
                    {
                        owner = 0;
                    }
                }

                _ownersNext[i] = owner;
            }
        }

        var tmp = _owners;
        _owners = _ownersNext;
        _ownersNext = tmp;
        rendererRef.BindOwners(_owners);
        UpdateScoreUI();
    }

    void SetReadyVisual(bool ready, bool isLeft)
    {
        var btn = isLeft ? leftReadyBtn : rightReadyBtn;
        if (!btn)
        {
            return;
        }
        var txt = btn.GetComponentInChildren<TMP_Text>();
        if (txt)
        {
            txt.text = ready ? "GoGoGo" : "Ready";
        }
        btn.image.color = ready ? new Color(0.8f, 1f, 0.8f, 1f) : Color.white;
    }

    void UpdatePiecesUI()
    {
        if (leftPiecesText)
        {
            leftPiecesText.text = $"Pieces Left: {_leftPieces}";
        }

        if (rightPiecesText)
        {
            rightPiecesText.text = $"Pieces Left: {_rightPieces}";
        }
    }

    void UpdateScoreUI()
    {
        if (!scoreText)
        {
            return;
        }
        scoreText.text = $"Score: {_score}";
    }
}