using UnityEngine;

[DisallowMultipleComponent]
public class ExplosionSpawnAudio : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip explosionSfx;
    [SerializeField, Range(0f, 1f)] private float explosionSfxVolume = 1f;
    [SerializeField, Range(0.1f, 3f)] private float explosionSfxPitch = 1f;

    private void Start()
    {
        SceneAudioManager.PlayExplosionSfx(explosionSfx, explosionSfxVolume, explosionSfxPitch);
    }
}
