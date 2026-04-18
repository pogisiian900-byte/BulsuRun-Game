using UnityEngine;

public class TurretCombatBridge : MonoBehaviour
{ 
       [SerializeField] private TurretShooting shooting;

    public void StartFiring() => shooting.StartFiring();
    public void StopFiring() => shooting.StopFiring();
}
