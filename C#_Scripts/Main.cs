using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    void Start() {
        GameObject aircraftObject = GameObject.Find("PlaneObject");
        AeroBody.InitializeAeroBody(aircraftObject);
        AircraftCamera.InitializeSpacecraftCamera(aircraftObject);
    }
}
