using UnityEngine;
using System.Collections;

public class BossClosingLaserAttack : MonoBehaviour
{
    [Header("Laser References")]
    public Transform leftLaser;
    public Transform rightLaser;

    [Header("Positions")]
    public Transform leftStartPoint;
    public Transform rightStartPoint;
    public Transform centerPoint; // base position where lasers close in

    [Header("Settings")]
    public float moveSpeed = 5f;
    public float holdDuration = 1.5f;
    public float cooldown = 3f;
    [SerializeField] private float centerSafeGap = 2f;

    private bool isAttacking = false;
    private Coroutine attackRoutine;

    private void OnEnable()
    {
        BeginAutoLoop();
    }

    private void OnDisable()
    {
        StopAttack();
    }

    public void BeginAutoLoop()
    {
        if (!isActiveAndEnabled || attackRoutine != null)
            return;

        attackRoutine = StartCoroutine(AttackLoop());
    }

    public void StopAttack()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        isAttacking = false;

        if (leftLaser != null)
            leftLaser.gameObject.SetActive(false);

        if (rightLaser != null)
            rightLaser.gameObject.SetActive(false);
    }

    private IEnumerator AttackLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(cooldown);

            yield return StartCoroutine(DoClosingAttack());
        }
    }

    private IEnumerator DoClosingAttack()
    {
        if (leftLaser == null || rightLaser == null || leftStartPoint == null || rightStartPoint == null || centerPoint == null)
            yield break;

        isAttacking = true;

        Vector3 leftTargetPosition = centerPoint.position + Vector3.left * (centerSafeGap * 0.5f);
        Vector3 rightTargetPosition = centerPoint.position + Vector3.right * (centerSafeGap * 0.5f);

        // Reset positions
        leftLaser.position = leftStartPoint.position;
        rightLaser.position = rightStartPoint.position;

        leftLaser.gameObject.SetActive(true);
        rightLaser.gameObject.SetActive(true);

        // Move lasers toward center
        while (Vector3.Distance(leftLaser.position, leftTargetPosition) > 0.1f ||
               Vector3.Distance(rightLaser.position, rightTargetPosition) > 0.1f)
        {
            leftLaser.position = Vector3.MoveTowards(
                leftLaser.position,
                leftTargetPosition,
                moveSpeed * Time.deltaTime
            );

            rightLaser.position = Vector3.MoveTowards(
                rightLaser.position,
                rightTargetPosition,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        // Hold in center (danger moment)
        yield return new WaitForSeconds(holdDuration);

        // Disable lasers
        leftLaser.gameObject.SetActive(false);
        rightLaser.gameObject.SetActive(false);

        isAttacking = false;
    }
}
