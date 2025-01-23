using UnityEngine;
using UnityEngine.Serialization;

namespace UnityDraw
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Singleton;
        public static AudioManager It => Singleton; 
        
        [SerializeField] private AudioSource bgm_source;
        [SerializeField] private AudioSource sfx_source;
        [SerializeField] private AudioSource drawing_sfx_source;
        [SerializeField] private AudioClip win_clip;
        [SerializeField] private AudioClip restart_clip;

        private bool drawing_started = false;
        
        void Awake()
        {
            Singleton = this;
        }
        
        public void PlaySfx(AudioClip clip, float volume = 1f)
        {
            sfx_source.PlayOneShot(clip, volume);
        }

        public void PlayWinSfx()
        {
            PlaySfx(win_clip);
        }
        
        public void PlayRestartSfx()
        {
            PlaySfx(restart_clip, 0.6f);
        }

        public void PlayDrawingSfx()
        {
            if (drawing_sfx_source.isPlaying) return;
            
            if (!drawing_started)
            {
                drawing_sfx_source.Play();
                drawing_started = true;
            }
            else drawing_sfx_source.UnPause();
        }

        public void PauseDrawingSfx()
        {
            if (!drawing_sfx_source.isPlaying) return;
            drawing_sfx_source.Pause();
        }
        
    }
}