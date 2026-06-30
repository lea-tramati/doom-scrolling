using UnityEngine;
using UnityEngine.SceneManagement;

// Cadre smartphone désactivé — le labyrinthe occupe toute la fenêtre.
// Ce script remet simplement Camera.rect en plein écran au cas où
// une version précédente l'aurait restreint.
public class SmartphoneFrame : MonoBehaviour
{
    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ResetCamera();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        ResetCamera();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ResetCamera();

    static void ResetCamera()
    {
        if (Camera.main != null)
            Camera.main.rect = new Rect(0f, 0f, 1f, 1f);
    }
}
