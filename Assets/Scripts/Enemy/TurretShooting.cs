using System.Collections;
using UnityEngine;

public class TurretShooting : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.8f;
    [SerializeField] private float startDelay = 1.2f;

    [Header("Audio")]
    [SerializeField] private AudioClip fireSfx;
    [SerializeField, Range(0f, 1f)] private float fireSfxVolume = 0.8f;
    [SerializeField, Range(0.1f, 3f)] private float fireSfxPitch = 1f;

    private Coroutine fireRoutine;
    private bool firesStunRounds;

    private void Awake()
    {
        CacheProjectileAudioType();
    }

    public void StartFiring()
    {
        if (fireRoutine != null) return; // already firing
        fireRoutine = StartCoroutine(FireLoop());
    }

    public void StopFiring()
    {
        if (fireRoutine == null) return;
        StopCoroutine(fireRoutine);
        fireRoutine = null;
    }

    private IEnumerator FireLoop()
    {
        yield return new WaitForSeconds(startDelay);

        while (true)
        {
            if (bulletPrefab != null && firePoint != null)
            {
                Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
                PlayFireSfx();
            }

            yield return new WaitForSeconds(fireRate);
        }
    }

    private void CacheProjectileAudioType()
    {
        firesStunRounds = false;

        if (bulletPrefab == null)
            return;

        TurretProjectiles projectile = bulletPrefab.GetComponent<TurretProjectiles>();
        firesStunRounds = projectile != null && projectile.CanStunPlayer;
    }

    private void PlayFireSfx()
    {
        if (firesStunRounds)
        {
            SceneAudioManager.PlayStunTurretShotSfx(fireSfx, fireSfxVolume, fireSfxPitch);
            return;
        }

        SceneAudioManager.PlayTurretShotSfx(fireSfx, fireSfxVolume, fireSfxPitch);
    }
}
