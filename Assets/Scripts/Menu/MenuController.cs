using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("Scene Names (exactly as in Build Settings)")] [SerializeField]
    string casualScene = "CasualScene";

    [SerializeField] string pvpScene = "PvPScene";
    [Header("Select first button on start (optional)")] [SerializeField]
    GameObject Selected;

    void Start()
    {
        if (Selected)
        {
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(Selected);
        }
    }

    public void PlayCasual() => SceneManager.LoadScene(casualScene, LoadSceneMode.Single);
    public void PlayPvP() => SceneManager.LoadScene(pvpScene, LoadSceneMode.Single);
    public void Quit() => Application.Quit();
}