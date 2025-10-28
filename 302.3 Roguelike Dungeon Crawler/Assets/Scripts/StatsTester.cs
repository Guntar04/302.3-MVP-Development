using UnityEngine;

public class PlayerStatsTester : MonoBehaviour
{
    public PlayerStats playerStats;

   private void Update()
{
    if (Input.GetKeyDown(KeyCode.H))
        playerStats.Heal(25);

    if (Input.GetKeyDown(KeyCode.J))
        playerStats.TakeDamage(25);

    if (Input.GetKeyDown(KeyCode.K))
        playerStats.AddShield(25);

    if (Input.GetKeyDown(KeyCode.L))
        playerStats.RemoveShield(25);
}

}
