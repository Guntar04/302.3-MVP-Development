using UnityEngine;
using UnityEngine.UI;

public class PotionTimerBar : MonoBehaviour
{
    public SpriteStatBar potionBar;
    public float potionDuration = 15f;
    private float remainingTime = 0f;
    private bool potionActive = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) // test trigger
        {
            StartPotionEffect();
        }

        if (potionActive)
        {
            remainingTime -= Time.deltaTime;
            potionBar.SetNormalizedValue(remainingTime / potionDuration);

            if (remainingTime <= 0f)
            {
                potionActive = false;
                remainingTime = 0f;
                potionBar.SetNormalizedValue(0);
                Debug.Log("Potion effect ended!");
            }
        }
    }

    public void StartPotionEffect()
    {
        potionActive = true;
        remainingTime = potionDuration;
        potionBar.SetNormalizedValue(1);
    }
}