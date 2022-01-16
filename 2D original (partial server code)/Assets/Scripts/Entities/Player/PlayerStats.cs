using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    // Level data
    private int CurrentMainLevel { get; set; }
    private int CurrentMainExp { get; set; }

    // Health stats
    private int CurrentHealth { get; set; }
    private int MaxHealth { get; set; }

    // Stamina stats
    private int CurrentStamina { get; set; }
    private int MaxStamina { get; set; }

    public GameObject StatBarsObject;
    private PlayerStatsHUD PlayerStatsHUD; // reference to HUD element used to update screen

    void Start()
    {
        PlayerStatsHUD = StatBarsObject.GetComponent<PlayerStatsHUD>();

        CurrentMainLevel = 20;
        CurrentMainExp = 37;

        CurrentHealth = 100;
        MaxHealth = 120;

        CurrentStamina = 50;
        MaxStamina = 120;

        Thread updateStatsThread = new Thread(() => {
            Thread.Sleep(1000);
            UnityThread.executeInUpdate(() =>
            {
                PlayerStatsHUD.UpdateLevel(CurrentMainLevel);
                PlayerStatsHUD.UpdateXP(CurrentMainExp, 100);
                PlayerStatsHUD.UpdateHealth(CurrentHealth, MaxHealth);
                PlayerStatsHUD.UpdateStamina(CurrentStamina, MaxStamina);
            });
        });
        updateStatsThread.Start();
    }
}
