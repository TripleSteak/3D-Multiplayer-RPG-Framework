using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsHUD : MonoBehaviour
{
    private Transform XPBar;
    private Transform HealthBar;
    private Transform StaminaBar;
     
    private Transform XPText;
    private Transform HealthText;
    private Transform StaminaText;

    private readonly float barWidthXP = 8.9F;
    private readonly float barWidthCombat = 11F;

    void Start()
    {
        XPBar = gameObject.transform.Find("XP Bar");
        HealthBar = gameObject.transform.Find("Health Bar");
        StaminaBar = gameObject.transform.Find("Stamina Bar");

        XPText = gameObject.transform.Find("XP Text").transform.Find("Text");
        HealthText = gameObject.transform.Find("Health Text").transform.Find("Text");
        StaminaText = gameObject.transform.Find("Stamina Text").transform.Find("Text");
    }

    public void UpdateLevel(int level)
    {
        gameObject.transform.Find("Player Level").transform.Find("Text").GetComponent<Text>().text = level.ToString();
    }

    public void UpdateXP(int currentXP, int maxXP)
    {
        XPBar.localScale = new Vector3(barWidthXP * ((float)currentXP) / ((float)maxXP), 1F, 1F);
        XPText.GetComponent<Text>().text = currentXP.ToString() + "/" + maxXP.ToString();
    }

    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        HealthBar.localScale = new Vector3(barWidthCombat * ((float)currentHealth) / ((float)maxHealth), 1F, 1F);
        HealthText.GetComponent<Text>().text = currentHealth.ToString() + "/" + maxHealth.ToString();
    }

    public void UpdateStamina(int currentStamina, int maxStamina)
    {
        StaminaBar.localScale = new Vector3(barWidthCombat * ((float)currentStamina) / ((float)maxStamina), 1F, 1F);
        StaminaText.GetComponent<Text>().text = currentStamina.ToString() + "/" + maxStamina.ToString();
    }
}
