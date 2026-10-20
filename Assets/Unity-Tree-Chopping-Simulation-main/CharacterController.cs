using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TreeChoppingSimulation
{
public class CharacterController : MonoBehaviour
{

    public GameObject ik_target;
    public GameObject ik_hint;
    public Slider powerSlider;
    private float rotation_size = 20f;
    private float duration = 1f;
    public float force_modifier = 10f;
    private Coroutine lerpCoroutine;
    public Camera playerCamera;
    public Camera sideCamera;

    //Power levels
    public float maxPower = 10f;
    public float currentPower = 1f;
    public float chargeRate = 4f;
    public float minPower = 1f;

    public float lastPower = 0f;

    // Start is called before the first frame update
    void Start()
    {
        powerSlider.minValue = minPower;
        powerSlider.maxValue = maxPower;
        powerSlider.value = currentPower;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 old_position = ik_target.transform.position;
        Vector3 move = new Vector3();
        if (Input.GetKey(KeyCode.W) && old_position.z <= -0.2f)
            move.z += Time.deltaTime;
        if (Input.GetKey(KeyCode.S) && old_position.z >= -1f)
            move.z -= Time.deltaTime;
        if (Input.GetKey(KeyCode.D) && old_position.x <= 1.5f)
            move.x += Time.deltaTime;
        if (Input.GetKey(KeyCode.A) && old_position.x >= 0.0)
            move.x -= Time.deltaTime;
        if (Input.GetKey(KeyCode.Q) && old_position.y >= 2.23f)
            move.y -= Time.deltaTime;
        if (Input.GetKey(KeyCode.E) && old_position.y <= 2.85f)
            move.y += Time.deltaTime;

        ik_target.transform.position = old_position + move;

        Vector3 rotation = new Vector3();
        if (Input.GetKey(KeyCode.U))
            rotation.x += rotation_size;
        if (Input.GetKey(KeyCode.J))
            rotation.x -= rotation_size;
        if (Input.GetKey(KeyCode.I))
            rotation.y += rotation_size;
        if (Input.GetKey(KeyCode.K))
            rotation.y -= rotation_size;
        if (Input.GetKey(KeyCode.O))
            rotation.z += rotation_size;
        if (Input.GetKey(KeyCode.L))
            rotation.z -= rotation_size;


        //Camera
        if (Input.GetKeyDown(KeyCode.V))
        {
            playerCamera.enabled = !playerCamera.enabled;
            sideCamera.enabled = !sideCamera.enabled;
        }


        //Max and mins
        ik_target.transform.rotation *= Quaternion.Euler(rotation * Time.deltaTime);
        Debug.Log(ik_target.transform.rotation.eulerAngles);

        Vector3 myVec = (ik_target.transform.up*1) + ik_target.transform.position;
        Debug.DrawLine(ik_target.transform.position, myVec, Color.white);
        if (Input.GetKey(KeyCode.Space) && currentPower < maxPower)
        {
            currentPower += Time.deltaTime * chargeRate;
            powerSlider.value = currentPower;
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            slashAxe();
            lastPower = currentPower;
            powerSlider.value = currentPower;
            currentPower = 1f;
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            Application.Quit(0);
        }
    }

    void slashAxe()
    {
        Vector3 myVec = (ik_target.transform.up*1) + ik_target.transform.position;
        duration = 1 / currentPower;
        lerpCoroutine = StartCoroutine(LerpAxe(ik_target.transform.position, myVec));
    }

    public void stopLerp()
    {
        if (lerpCoroutine != null)
        {
            StopCoroutine(lerpCoroutine);
        }
    }

    IEnumerator LerpAxe(Vector3 start, Vector3 end)
    {
        float timeElapsed = 0;
        while (timeElapsed < duration)
        {
            float t = timeElapsed / duration;
            ik_target.transform.position = Vector3.Lerp(start, end, t);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        ik_target.transform.position = end;
    }
}
}
