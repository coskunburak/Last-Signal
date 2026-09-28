using UnityEngine;
using UnityEngine.UI;

public class StanceHUD : MonoBehaviour
{
    [Tooltip("Image component for the stance sprites")]
    public Image stanceImage;
    [Tooltip("Sprite to display when standing")]
    public Sprite standingSprite;
    [Tooltip("Sprite to display when crouching")]
    public Sprite crouchingSprite;
    [Tooltip("Sprite to display when sprinting")]
    public Sprite sprintingSprite;

    private void Start()
    {
        PlayerCharacterController character = FindObjectOfType<PlayerCharacterController>();
        DebugUtility.HandleErrorIfNullFindObject<PlayerCharacterController, StanceHUD>(character, this);
        character.onStanceChanged += OnStanceChanged;

        OnStanceChanged(character.isCrouching, character.isSprinting);
    }

    void OnStanceChanged(bool crouched, bool isSprinting)
    {
        if (isSprinting)
        {
            stanceImage.sprite = sprintingSprite;
        }
        else
        {
            stanceImage.sprite = crouched ? crouchingSprite : standingSprite;
        }
    }
}
