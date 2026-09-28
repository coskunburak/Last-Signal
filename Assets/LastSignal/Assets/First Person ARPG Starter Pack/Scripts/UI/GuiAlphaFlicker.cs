using UnityEngine.UI;
using UnityEngine;

public class GuiAlphaFlicker : MonoBehaviour
{
    public bool shouldShow { private get; set; }
    
    private Image image;

    public float flickerRate = 0.4f;

    private float lastFlickerSwapTime = 0f;

    public float maxFlickerAlpha = 0.4f;
    // Start is called before the first frame update
    void Start()
    {
        image = GetComponent<Image>();
        shouldShow = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time > lastFlickerSwapTime + flickerRate)
        {
            lastFlickerSwapTime = Time.time;
        }

        float targetAlpha = (lastFlickerSwapTime + flickerRate / 2) < Time.time || !shouldShow ? 0 : maxFlickerAlpha;
        
        Color imageColor = image.color;
        imageColor.a = Mathf.Lerp(targetAlpha, imageColor.a, flickerRate / 2);
        image.color = imageColor;
    }
}
