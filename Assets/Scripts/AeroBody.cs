using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AeroBody : MonoBehaviour {

    private Aircraft_Controller aircraftControl;
    private Component[] aeroSurfaces;
    private Text Speedometer;
    private Text thrustTracker;
    private Text angularVelocity;
    private Text rcsTorque;
    private Slider trimSlider;


    private Vector3 forcesThisTick;
    private Vector3 torqueThisTick;
    private Vector3 forces; //net forces applied
    private Vector3 torque; //net torque applied
    private Vector3 predictedForces;
    private Vector3 predictedTorque;
    public Vector3 relativePosition;
    public Vector3 liftDirection;
    public Vector3 AeroCoefficients;

    private GameObject playerAircraft;
    private Rigidbody rb;

    private float pitchingMomentThisTick;
    private float angleOfAttack;
    private float thrustPercentage;
    private float maxThrust = 4000;
    public bool activateSAS = false;

    public static AeroBody InitializeAeroBody(GameObject iGivenObject) {
        AeroBody thisBody = iGivenObject.AddComponent<AeroBody>();
        return thisBody;
    }

    // Start is called before the first frame update
    void Start()
    {
        //Get a reference to text components which are updated every tick
        Speedometer = GameObject.Find("Speedometer").GetComponentInChildren<Text>();
        thrustTracker = GameObject.Find("ThrustTracker").GetComponentInChildren<Text>();
        angularVelocity = GameObject.Find("AngularVelX").GetComponentInChildren<Text>();
        rcsTorque = GameObject.Find("RCSTORQUE").GetComponentInChildren<Text>();
        trimSlider = GameObject.Find("TrimSlider").GetComponent<Slider>();

        //initialize aero body stuff
        playerAircraft = GameObject.Find("PlaneObject");
        rb = playerAircraft.GetComponent<Rigidbody>();
        aeroSurfaces = playerAircraft.GetComponentsInChildren<AeroSurface>();
        aircraftControl = playerAircraft.GetComponent<Aircraft_Controller>();
        rb.angularDrag = 6;
        Time.fixedDeltaTime = 0.005f;
        activateSAS = false;
    }


    void FixedUpdate() {
        //Calculate net forces and torque from each aerosurface
        CalculateNetForceAndTorque(rb.velocity, rb.angularVelocity, true, out forces, out torque);

        //calculate velocity and angular velocity half a timestep ahead and take the average of those values to calculate lift and drag (reduces craft jitter)
        Vector3 predictedVelocity = rb.velocity + 0.5f * Time.fixedDeltaTime * (forces + new Vector3(0, -9.81f * rb.mass, 0) + rb.transform.forward*maxThrust*thrustPercentage/100) / rb.mass;
        Vector3 predictedAngularVelocity = PredictAngularVelocity(torque);
        CalculateNetForceAndTorque(predictedVelocity, predictedAngularVelocity, false, out predictedForces, out predictedTorque);

        //apply smoothed forces and torque
        Vector3 smoothedForces = (forces + predictedForces) * 0.5f;
        Vector3 smoothedTorque = (torque + predictedTorque) * 0.5f;
        rb.AddForce(smoothedForces);
        rb.AddTorque(smoothedTorque);

        //control system aims to have 0 angular velocity for aircraft
        if (activateSAS == true) {
                Vector3 angVelAdjustment = aircraftControl.GetSASOutput(rb.angularVelocity.x, rb.angularVelocity.y, rb.angularVelocity.z, Time.fixedDeltaTime);
                rb.AddTorque(-angVelAdjustment * rb.velocity.magnitude);
                rcsTorque.text = "X: " + angVelAdjustment.x.ToString("F2") + ", Y: " + angVelAdjustment.y.ToString("F2") + ", Z: " + angVelAdjustment.z.ToString("F2");
        }
        else {
            rcsTorque.text = "X: 0.000, Y: 0.000, Z:0.000";
        }
        rcsTorque.text.Substring(0, 4).ToString();

        //add gravity and forward thrust
        rb.AddForce(new Vector3(0, -9.81f * rb.mass, 0));
        rb.AddForce(rb.transform.forward * maxThrust * thrustPercentage / 100);

        //Handle pitch for aircraft and flap angles
        if (Input.GetKey(KeyCode.W)) {
            //rb.AddRelativeTorque(new Vector3(0.1f, 0, 0));
            foreach (AeroSurface surface in aeroSurfaces) {
                if (surface.isSurfaceVertical != true && surface.isDragSurface != true && surface.isTrimSurface == false) {
                    if (surface.isLeftRollController != true) {
                        if (surface.isRightRollController != true) { 
                            surface.AdjustHorizontalFlapAngle(-0.125f * 2);
                        }
                    }
                }
            }
        }

        if (Input.GetKey(KeyCode.S)) {
            //rb.AddRelativeTorque(new Vector3(-0.1f, 0, 0));
            foreach (AeroSurface surface in aeroSurfaces) {
                if (surface.isSurfaceVertical != true && surface.isDragSurface != true && surface.isTrimSurface == false) {
                    if (surface.isLeftRollController != true) {
                        if (surface.isRightRollController != true) {
                            surface.AdjustHorizontalFlapAngle(0.125f * 2);
                        }
                    }
                }
            }
        }

        //Handle roll for aircraft
        if (Input.GetKey(KeyCode.Q)) {
            foreach (AeroSurface surface in aeroSurfaces) {
                if (surface.isSurfaceVertical != true && surface.isDragSurface != true) {
                    if (surface.isLeftRollController == true) {
                        surface.AdjustHorizontalFlapAngle(-0.125f * 2);
                    } else if(surface.isRightRollController == true) {
                        surface.AdjustHorizontalFlapAngle(0.125f * 2);
                    }
                }
            }
        }

        if (Input.GetKey(KeyCode.E)) {
            foreach (AeroSurface surface in aeroSurfaces) {
                if (surface.isSurfaceVertical != true && surface.isDragSurface != true) {
                    if (surface.isLeftRollController == true) {
                        surface.AdjustHorizontalFlapAngle(0.125f * 2);
                    }
                    else if (surface.isRightRollController == true) {
                        surface.AdjustHorizontalFlapAngle(-0.125f * 2);
                    }
                }
            }
        }

        //Handle yaw and flap adjustments for aircraft
        if (Input.GetKey(KeyCode.A)) {
            foreach (AeroSurface surface in aeroSurfaces) {
                if (surface.isSurfaceVertical == true && surface.isDragSurface != true) {
                    surface.AdjustVerticalFlapAngle(0.125f * 5);
                }
            }
        }
        if (Input.GetKey(KeyCode.D)) {
            foreach (AeroSurface surface in aeroSurfaces) {
                if (surface.isSurfaceVertical == true && surface.isDragSurface != true) {
                    surface.AdjustVerticalFlapAngle(-0.125f * 5);
                }
            }
        }

        //Handles thrust on aircraft
        if (Input.GetKey(KeyCode.LeftShift)) {
            if (thrustPercentage < 100) {
                thrustPercentage += 0.25f;
            }
        }

        if (Input.GetKey(KeyCode.LeftControl)) {
            thrustPercentage -= 0.25f;
            if (thrustPercentage < 0) { 
                thrustPercentage = 0;
            }
        }

        //update text on screen with forces
        Speedometer.text = "SPEED: " + rb.velocity.magnitude.ToString();
        thrustTracker.text = "THRUST %: " + thrustPercentage.ToString();
        angularVelocity.text = "X: " + rb.angularVelocity.x.ToString("F3") + ", Y: " + rb.angularVelocity.y.ToString("F3") + ", Z: " + rb.angularVelocity.z.ToString("F3"); 

        //slowly interpolates the angle of flaps back to 0
        foreach (AeroSurface surface in aeroSurfaces) {
            if (surface.isDragSurface == false && surface.isTrimSurface == false) {
                if (surface.isSurfaceVertical == true) {
                    float returningVerticalFlapAngle = Mathf.Lerp(surface.verticalFlapAngle, 0, 2f * Time.fixedDeltaTime);
                    surface.SetVerticalFlapAngle(returningVerticalFlapAngle);
                }
                else {
                    float returningHorizontalFlapAngle = Mathf.Lerp(surface.horizontalFlapAngle, 0, 2f * Time.fixedDeltaTime);
                    surface.SetHorizontalFlapAngle(returningHorizontalFlapAngle);
                }
            }
        }

        //sets a new zero angle of attack for the back wing for trim adjustment
        foreach (AeroSurface surface in aeroSurfaces) {
           if (surface.isTrimSurface == true) {
                float mappedAngle = -1 + trimSlider.value * 2;
                surface.SetHorizontalFlapAngle(mappedAngle * surface.flapLimiter * Mathf.Deg2Rad);
            }
        }
    }

    public void CalculateNetForceAndTorque(Vector3 rbVelocity, Vector3 rbAngVelocity, bool drawRays, out Vector3 returnNetForce, out Vector3 returnNetTorque) {
        torqueThisTick = Vector3.zero;
        forcesThisTick = Vector3.zero;
        foreach (AeroSurface surface in aeroSurfaces) {
            if (surface.isDragSurface == true) {
                Vector3 relativePosition = new Vector3(surface.transform.position.x, surface.transform.position.y, surface.transform.position.z) - rb.worldCenterOfMass;
                Vector3 airVelocity = transform.InverseTransformDirection(-rbVelocity - Vector3.Cross(rbAngVelocity, relativePosition));
                Vector3 dragDirection = transform.TransformDirection(airVelocity.normalized);
                float absVelocity = airVelocity.magnitude;
                Vector3 DragForces = surface.CalculateDragForces(new Vector3(0,3f,0), absVelocity, dragDirection);
                forcesThisTick += DragForces;

                //Draw the red (DRAG) force lines
                if (drawRays == true) {
                    Debug.DrawRay(new Vector3(surface.transform.position.x, surface.transform.position.y, surface.transform.position.z), 0.01f * DragForces.magnitude * dragDirection, Color.red);
                }
            }
            else {
                //find relative wind velocity giving drag and lift vector directions
                Vector3 relativePosition = new Vector3(surface.transform.position.x, surface.transform.position.y, surface.transform.position.z) - rb.worldCenterOfMass;
                Vector3 airVelocity = transform.InverseTransformDirection(-rbVelocity - Vector3.Cross(rbAngVelocity, relativePosition));
                Vector3 dragDirection = transform.TransformDirection(airVelocity.normalized);
                float absVelocity = airVelocity.magnitude;

                //Finds angle of attack for vertical and horizontal aero surfaces, and given liftDirection
                //Calculates aerocoefficients lift, drag, moment => Cl, Cd, Cm
                if (surface.isSurfaceVertical == true) {
                    angleOfAttack = Mathf.Atan2(airVelocity.x, -airVelocity.z);
                    liftDirection = Vector3.Cross(dragDirection, surface.transform.up);
                    AeroCoefficients = surface.CalculateCoefficients(angleOfAttack, surface.verticalFlapAngle);
                }
                else {
                    liftDirection = Vector3.Cross(dragDirection, -surface.transform.right);
                    //liftDirection = Vector3.Cross(dragDirection, -surface.transform.forward);
                    angleOfAttack = Mathf.Atan2(airVelocity.y, -airVelocity.z);
                    AeroCoefficients = surface.CalculateCoefficients(angleOfAttack, surface.horizontalFlapAngle);
                }

                //Calculate lift forces, drag forces, and pitching moment from this aerosurface
                Vector3 LiftForces = surface.CalculateLiftForces(AeroCoefficients, absVelocity, liftDirection);
                Vector3 DragForces = surface.CalculateDragForces(AeroCoefficients, absVelocity, dragDirection);
                pitchingMomentThisTick = surface.CalculatePitchingMoment(AeroCoefficients, absVelocity);

                //sum forces to be added after looping through aerosurface components, and for tracking magnitudes
                forcesThisTick += LiftForces + DragForces;

                //calculate and apply torque every loop (though only updates every fixed deltaT update)
                Vector3 torqueFromForces = Vector3.Cross(relativePosition, LiftForces + DragForces);
                torqueThisTick += torqueFromForces;


                //Correctly apply pitching moment depending on vertical surface AOA
                if (surface.isSurfaceVertical == true) {
                    if (angleOfAttack > 0) {
                        torqueThisTick += new Vector3(0, pitchingMomentThisTick, 0);
                    }
                    else {
                        torqueThisTick += new Vector3(0, -pitchingMomentThisTick, 0);
                    }
                }
                else {
                    torqueThisTick += new Vector3(-pitchingMomentThisTick, 0, 0);
                }


                //Draw the blue (LIFT) and red (DRAG) force lines for each aerosurface
                if (drawRays == true) {
                    if (surface.isSurfaceVertical) {
                        if (angleOfAttack > 0) {
                            Debug.DrawRay(new Vector3(surface.transform.position.x, surface.transform.position.y, surface.transform.position.z), 0.01f * LiftForces.magnitude * liftDirection, Color.blue);
                        } else {
                            Debug.DrawRay(new Vector3(surface.transform.position.x, surface.transform.position.y, surface.transform.position.z), -0.01f * LiftForces.magnitude * liftDirection, Color.blue);
                        }
                    } else {
                        if (angleOfAttack > 0) {
                            Debug.DrawRay(new Vector3(surface.transform.position.x, surface.transform.position.y, surface.transform.position.z), 0.01f * LiftForces.magnitude * liftDirection, Color.blue);
                        } else {
                            Debug.DrawRay(new Vector3(surface.transform.position.x, surface.transform.position.y, surface.transform.position.z), -0.01f * LiftForces.magnitude * liftDirection, Color.blue);
                        }
                    }
                    Debug.DrawRay(new Vector3(surface.transform.position.x, surface.transform.position.y, surface.transform.position.z), 0.01f * DragForces.magnitude * dragDirection, Color.red);
                }
            }
        }
        returnNetForce = forcesThisTick;
        returnNetTorque = torqueThisTick;
    }

    private Vector3 PredictAngularVelocity(Vector3 givenTorque) {
        Quaternion inertiaTensorWorldRotation = rb.rotation * rb.inertiaTensorRotation;
        Vector3 torqueInDiagonalSpace = Quaternion.Inverse(inertiaTensorWorldRotation) * givenTorque;
        Vector3 angularVelocityChangeInDiagonalSpace;
        angularVelocityChangeInDiagonalSpace.x = torqueInDiagonalSpace.x / rb.inertiaTensor.x;
        angularVelocityChangeInDiagonalSpace.y = torqueInDiagonalSpace.y / rb.inertiaTensor.y;
        angularVelocityChangeInDiagonalSpace.z = torqueInDiagonalSpace.z / rb.inertiaTensor.z;

        return rb.angularVelocity + Time.fixedDeltaTime * 0.5f
            * (inertiaTensorWorldRotation * angularVelocityChangeInDiagonalSpace);
    }

    //Used for camera to track position of plane
    public Vector3 ReturnAircraftPosition() {
        return rb.transform.position;
    }

}
