using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AircraftCamera : MonoBehaviour
{
    private float xCirclePos;
    private float yCirclePos;
    private float zCirclePos;
    public float relPositionX;
    public float relPositionY;
    public float relPositionZ;
    private Vector3 startingCameraPosition = new Vector3(-16f, 67.4f, -16f);
    private float viewRadius;
    private float scrollPosition; //tracks scrollPosition in exponential scroll function
    private GameObject mainCamera;
    private AeroBody thisAeroBody;
    private Text SASActivated;
    private Aircraft_Controller aircraftControl;

    public static AircraftCamera InitializeSpacecraftCamera(GameObject iGivenObject) {
        AircraftCamera thisAircraftCamera = iGivenObject.AddComponent<AircraftCamera>();

        return thisAircraftCamera;
    }
    // Start is called before the first frame update
    void Start()
    {
        relPositionX = startingCameraPosition.x; //used for relative camera position and defining starting camera position
        relPositionY = startingCameraPosition.y;
        relPositionZ = startingCameraPosition.z;
        scrollPosition = 26f; //gives a starting radius ~= 150
        viewRadius = 0.005f * Mathf.Exp(0.15f * scrollPosition);
        mainCamera = GameObject.Find("PlaneCamera");
        thisAeroBody = GameObject.Find("PlaneObject").GetComponent<AeroBody>();
        SASActivated = GameObject.Find("SASColor").GetComponentInChildren<Text>();
        aircraftControl = GameObject.Find("PlaneObject").GetComponent<Aircraft_Controller>();

    }

    // Update is called once per frame
    void Update()
    {
        Vector3 currentPosition = thisAeroBody.ReturnAircraftPosition();

        //---------------------------------------scrolls the camera in and out depending on the scroll wheel input-------------------------------------------------------------
        if (Input.GetAxis("Mouse ScrollWheel") > 0f) {
            scrollPosition -= 1;
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0f) {
            scrollPosition += 1;
        }
        viewRadius = 0.1f * Mathf.Exp(0.2f * scrollPosition);

        //---------------------------------------Rotates camera around craft for the up, down, left and right arrow keys-----------------------------------------------------------------
        if (Input.GetKey(KeyCode.RightArrow)) {
            relPositionX += 0.04f;
            relPositionZ += 0.04f;
            xCirclePos = viewRadius * Mathf.Cos(0.1f * relPositionX);
            zCirclePos = viewRadius * Mathf.Sin(0.1f * relPositionZ);
            mainCamera.transform.position = new Vector3(currentPosition.x + xCirclePos, currentPosition.y + yCirclePos, currentPosition.z + zCirclePos);
        }
        if (Input.GetKey(KeyCode.LeftArrow)) {
            relPositionX -= 0.04f;
            relPositionZ -= 0.04f;
            xCirclePos = viewRadius * Mathf.Cos(0.1f * relPositionX);
            zCirclePos = viewRadius * Mathf.Sin(0.1f * relPositionZ);
            mainCamera.transform.position = new Vector3(currentPosition.x + xCirclePos, currentPosition.y + yCirclePos, currentPosition.z + zCirclePos);
        }
        if (Input.GetKey(KeyCode.UpArrow)) {
            relPositionY += 0.04f;
            yCirclePos = viewRadius * Mathf.Sin(0.1f * relPositionY);
            mainCamera.transform.position = new Vector3(currentPosition.x + xCirclePos, currentPosition.y + yCirclePos, currentPosition.z + zCirclePos);
        }
        if (Input.GetKey(KeyCode.DownArrow)) {
            relPositionY -= 0.04f;
            yCirclePos = viewRadius * Mathf.Sin(0.1f * relPositionY);
            mainCamera.transform.position = new Vector3(currentPosition.x + xCirclePos, currentPosition.y + yCirclePos, currentPosition.z + zCirclePos);
        }

        //Activate control system
        if (Input.GetKeyDown(KeyCode.T)) {
            if (thisAeroBody.activateSAS == true) {
                thisAeroBody.activateSAS = false;
                SASActivated.text = "S.A.S Control - <color=red>OFF</color>";
            }
            else {
                thisAeroBody.activateSAS = true;
                aircraftControl.ResetIntegral();
                SASActivated.text = "S.A.S Control - <color=green>ON</color>";
            }
        }

        //------------------------------------------------If no input from arrow keys keep camera pointed at craft and following------------------------------------------------------
        xCirclePos = viewRadius * Mathf.Cos(0.1f * relPositionX);
        yCirclePos = viewRadius * Mathf.Sin(0.1f * relPositionY);
        zCirclePos = viewRadius * Mathf.Sin(0.1f * relPositionZ);

        mainCamera.transform.position = new Vector3(currentPosition.x + xCirclePos, currentPosition.y + yCirclePos, currentPosition.z + zCirclePos);
        mainCamera.transform.position = new Vector3(currentPosition.x + xCirclePos, currentPosition.y + yCirclePos, currentPosition.z + zCirclePos);
        mainCamera.transform.LookAt(currentPosition);
        Vector3 eulers = mainCamera.transform.eulerAngles;
        eulers.z = 0;
        mainCamera.transform.eulerAngles = eulers;
    }
}
