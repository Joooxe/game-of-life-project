using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class EscToTitle : MonoBehaviour
{
    [SerializeField] string titleSceneName = "TitleScene";

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(titleSceneName, LoadSceneMode.Single);
        }
    }
}