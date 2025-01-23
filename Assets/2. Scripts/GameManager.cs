using UnityEngine;

namespace UnityDraw
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Singleton;
        public static GameManager It => Singleton;

        [SerializeField] private GameObject title_text;
        [SerializeField] private GameObject success_ui;
        [SerializeField] private DrawOnImage draw;
        public bool Success { get; private set; } = false;

        void Awake()
        {
            Singleton = this;
        }

        public void Restart()
        {
            Success = false;
            draw.Restart();
            success_ui.SetActive(false);
            title_text.SetActive(true);
            AudioManager.It.PlayRestartSfx();
        }

        public void Win()
        {
            if (Success) return;
            Success = true;
            success_ui.SetActive(true);
            title_text.SetActive(false);
            AudioManager.It.PlayWinSfx();
        }
    }
}