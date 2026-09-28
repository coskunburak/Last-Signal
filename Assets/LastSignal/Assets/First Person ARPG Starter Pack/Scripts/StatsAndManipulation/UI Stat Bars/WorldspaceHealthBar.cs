using FPS.Scripts.Characters;
using FPS.Scripts.Characters.UI;
using Main.StatsAndManipulation;
using UnityEngine;

public class WorldspaceHealthBar : StatsBar
{
    [Tooltip("Health component to track")]
    public Health health;
    [Tooltip("The floating healthbar pivot transform")]
    public Transform healthBarPivot;
    [Tooltip("Whether the health bar is visible when at full health or not")]
    public bool hideFullHealthBar = true;

    protected override void UpdateStatBar()
    {
        // rotate health bar to face the camera/player
        healthBarPivot.LookAt(Camera.main.transform.position);

        // hide health bar if needed
        if (hideFullHealthBar)
            healthBarPivot.gameObject.SetActive(fillImage.fillAmount != 1);
    }

    protected override void InitialiseStatBar()
    {
        health.onDamaged += OnDamaged;
        health.onHealed += OnHealed;
    }

    protected override float GetStatFillPercent()
    {
        return health.currentHealth / health.maxHealth;
    }
    
    void OnDamaged(DamageInfo damage)
    {
        OnStatValueDeducted();
    }

    void OnHealed(float healAmount)
    {
        OnStatValueIncremented();
    }
}
