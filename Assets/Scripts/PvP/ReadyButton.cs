using UnityEngine;
using UnityEngine.UI;

public class ReadyButton : MonoBehaviour
{
    [SerializeField] PvpController controller;
    [SerializeField] bool isLeftPlayer = true;

    void Awake() => GetComponent<Button>()?.onClick.AddListener(() => controller?.ToggleReady(isLeftPlayer));
}