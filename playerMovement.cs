using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerMovement : MonoBehaviour
{
    public bool start = false;    
    CharacterController cc;
    private Vector3 playerVelocity;
    private bool groundedPlayer;
    public float playerSpeed = 2.0f;
    public float jumpHeight = 1.0f;
    public float gravityValue = -9.81f;
    float mX = 0f;
    float mY = 0f;
    void Start()
    {
        cc = GetComponent<CharacterController>();
        Debug.Log("Press Q to unlock mouse");
    }

    void Update()
    {   
        if (!start) return;

        Camera.main.transform.position = transform.position;

        // https://docs.unity3d.com/ScriptReference/CharacterController.Move.html
        groundedPlayer = cc.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        if (Input.GetKey("w")) cc.Move(transform.forward * Time.deltaTime * playerSpeed);
        if (Input.GetKey("s")) cc.Move(-transform.forward * Time.deltaTime * playerSpeed);
        if (Input.GetKey("d")) cc.Move(transform.right * Time.deltaTime * playerSpeed);
        if (Input.GetKey("a")) cc.Move(-transform.right * Time.deltaTime * playerSpeed);

        if (Input.GetKeyDown("space") && groundedPlayer)
        {
            playerVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
        }

        playerVelocity.y += gravityValue * Time.deltaTime;
        cc.Move(playerVelocity * Time.deltaTime);

        // ------
        Transform t = Camera.main.transform;

        // https://answers.unity.com/questions/172728/transfromrotate-changes-z-axis-incorrectly.html
        mY -= Input.GetAxis("Mouse Y") * 500 * Time.deltaTime;
        mY = Mathf.Clamp(mY, -80, 80);
        mX += Input.GetAxis("Mouse X") * 500 * Time.deltaTime;
        t.localEulerAngles = new Vector3(mY, mX, 0);
        // ------

        transform.eulerAngles = new Vector3(0, t.eulerAngles.y, 0);
    }
}
