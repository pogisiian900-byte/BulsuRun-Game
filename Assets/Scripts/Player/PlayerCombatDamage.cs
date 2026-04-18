using System.Collections.Generic;
using UnityEngine;

public static class PlayerCombatDamage
{
    public static bool TryDamage(Collider2D hit, int damage, Vector2 hitSource, Transform ownerRoot = null)
    {
        if (hit == null || damage <= 0)
        {
            return false;
        }

        if (ownerRoot != null && hit.transform.IsChildOf(ownerRoot))
        {
            return false;
        }

        EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            return true;
        }

        BossHealth boss = hit.GetComponentInParent<BossHealth>();
        if (boss != null)
        {
            boss.TakeDamage(damage, hitSource);
            return true;
        }

        return false;
    }

    public static bool DamageUniqueTargets(IEnumerable<Collider2D> hits, int damage, Vector2 hitSource, Transform ownerRoot = null)
    {
        if (hits == null || damage <= 0)
        {
            return false;
        }

        bool damagedAny = false;
        HashSet<EnemyHealth> hitEnemies = new HashSet<EnemyHealth>();
        HashSet<BossHealth> hitBosses = new HashSet<BossHealth>();

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            if (ownerRoot != null && hit.transform.IsChildOf(ownerRoot))
            {
                continue;
            }

            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy != null)
            {
                if (hitEnemies.Add(enemy))
                {
                    enemy.TakeDamage(damage);
                    damagedAny = true;
                }

                continue;
            }

            BossHealth boss = hit.GetComponentInParent<BossHealth>();
            if (boss != null && hitBosses.Add(boss))
            {
                boss.TakeDamage(damage, hitSource);
                damagedAny = true;
            }
        }

        return damagedAny;
    }
}
