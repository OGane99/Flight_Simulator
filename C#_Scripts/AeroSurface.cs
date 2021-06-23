using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class AeroSurface : MonoBehaviour
{
    private float liftSlope = 6.28f;
    private float skinFriction = 0.02f;
    private float zeroLiftAOA;
    private float stallAngleHighBase = 15;
    private float stallAngleLowBase = -15;
    private float flapChord; //chord of the flap
    private float AR; //aspect ratio = span/chord
    private float wingSpan;
    private float Cl; //coefficient of lift
    private float Cd; //coefficient of drag
    private float Cm; //coefficient of pitching moment

    private static float liftCurveSlope;
    private static float theta;
    private static float flapEffectiveness;

    public float wingChord;
    public float wingArea;
    public float horizontalFlapAngle = 0; //for horizontal aero surfaces
    public float verticalFlapAngle = 0; //confusing but for vertical aerosurfaces
    public float flapLimiter = 0;

    public bool isSurfaceVertical; //checks if aerosurface is horizontal or vertical
    public bool isLeftRollController; //checks if the aerosurface controls roll of the aircraft (aileron) (AND IS ON LEFT SIDE OF AIRCRAFT)
    public bool isRightRollController;
    public bool isDragSurface; //no lift, only drag
    public bool isTrimSurface; //aerosurface that only adjusts trim

    public GameObject associatedFlap;
    public Vector3 aerodynamicCenter;
    public string associatedFlapString; //the flap associated with this aero-surface
    public string associatedFlapTransformString;

    public LineRenderer liftLine;

    // Start is called before the first frame update
    void Start()
    {
        if (isDragSurface != true) {
            //Initialize associated flap for wing
            associatedFlap = GameObject.Find(associatedFlapString);
            associatedFlap.transform.parent = GameObject.Find(associatedFlapTransformString).transform;

            if (isSurfaceVertical == true) {
                wingSpan = this.GetComponentInParent<Transform>().localScale.y;
                aerodynamicCenter = this.GetComponent<Transform>().localPosition;
            } else {
                wingSpan = this.GetComponentInParent<Transform>().localScale.x;
                aerodynamicCenter = this.GetComponent<Transform>().localPosition;
            }

            //Initialize wing parameters depending on wing and flap dimensions
            wingChord = this.GetComponentInParent<Transform>().localScale.z;
            flapChord = associatedFlap.GetComponentInChildren<Transform>().localScale.z;
            AR = wingSpan / wingChord;
            wingArea = wingSpan * wingChord;

            //Initialize constant parameters of wing and flap
            liftCurveSlope = liftSlope * AR / (AR + 2 * (AR + 4) / (AR + 2));
            theta = Mathf.Acos(2 * flapChord / wingChord - 1);
            flapEffectiveness = 1 - (theta - Mathf.Sin(theta)) / Mathf.PI;
        } else {
            wingArea = this.GetComponentInParent<Transform>().localScale.y * this.GetComponentInParent<Transform>().localScale.x;
        }
    }

    //calculates the 3 aerodynamics coefficients lift, drag and pitching moment (Cl, Cd, Cm)
    public Vector3 CalculateCoefficients(float angleOfAttack, float flapAngle) {
        //change in lift coefficient
        float deltaCL = liftCurveSlope * flapEffectiveness * Mathf.Lerp(0.8f, 0.4f, (Mathf.Abs(flapAngle) * Mathf.Rad2Deg - 10) / 50) * flapAngle;

        //zero lift angle of attack depending on delta lift coefficient
        zeroLiftAOA = 0 - deltaCL / liftCurveSlope;

        //max and min lift coefficients with corresponding stall angles
        float maxCLPos = liftCurveSlope * (stallAngleHighBase * Mathf.Deg2Rad - 0) + deltaCL;
        float maxCLNeg = liftCurveSlope * (stallAngleLowBase * Mathf.Deg2Rad - 0) + deltaCL;
        float stallAngleHigh = zeroLiftAOA + maxCLPos / liftCurveSlope;
        float stallAngleLow = zeroLiftAOA + maxCLNeg / liftCurveSlope;

        //depending on aerosurface AoA, do low or high AoA lift and drag equations
        //low AoA
        if (angleOfAttack > stallAngleLow && angleOfAttack < stallAngleHigh) {
            Cl = liftCurveSlope * (angleOfAttack - zeroLiftAOA); //lift coefficient
            float angleI = Cl / (Mathf.PI * AR);
            float angleEff = angleOfAttack - zeroLiftAOA - angleI;
            float Ct = skinFriction * Mathf.Cos(angleEff);
            float Cn = (Cl + Ct * Mathf.Sin(angleEff)) / (Mathf.Cos(angleEff));
            Cd = Cn * Mathf.Sin(angleEff)+ Ct * Mathf.Cos(angleEff); //drag coefficient
            Cm = -Cn * (0.25f - 0.175f * (1 - 2 * angleEff / Mathf.PI)); //pitching moment coefficient
        }
        //High AoA
        else if (angleOfAttack > stallAngleHigh || angleOfAttack < stallAngleLow) {
            float liftCoefficientLowAoA;
            if (angleOfAttack > stallAngleHigh) {
                liftCoefficientLowAoA = liftCurveSlope * (stallAngleHigh - zeroLiftAOA);
            }
            else {
                liftCoefficientLowAoA = liftCurveSlope * (stallAngleLow - zeroLiftAOA);
            }
            float inducedAngle = liftCoefficientLowAoA / (Mathf.PI * AR); //get induced angle for base linear interpolation
            
            float lerpParam;
            if (angleOfAttack > stallAngleHigh) {
                lerpParam = (Mathf.PI / 2 - Mathf.Clamp(angleOfAttack, -Mathf.PI / 2, Mathf.PI / 2))
                    / (Mathf.PI / 2 - stallAngleHigh);
            }
            else {
                lerpParam = (-Mathf.PI / 2 - Mathf.Clamp(angleOfAttack, -Mathf.PI / 2, Mathf.PI / 2))
                    / (-Mathf.PI / 2 - stallAngleLow);
            }
            float angleI = Mathf.Lerp(0, inducedAngle, lerpParam); //interpolate for induced angle
            //float angleEff = angleOfAttack - zeroLiftAOA - inducedAngle;
            float angleEff = angleOfAttack - zeroLiftAOA - angleI;

            float Cd90 = 1.98f - 4.26e-2f * flapAngle * flapAngle + 2.1e-1f * flapAngle;
            float Ct = 0.5f * skinFriction * Mathf.Cos(angleEff);
            float Cn = Cd90 * Mathf.Sin(angleEff) * (1 / (0.56f + 0.44f * Mathf.Sin(angleEff)) - 0.41f * (1 - Mathf.Exp(-17 / AR)));
            Cl = Cn * Mathf.Cos(angleEff) - Ct * Mathf.Sin(angleEff); //lift coefficient
            Cd = Cn * Mathf.Sin(angleEff) + Ct * Mathf.Cos(angleEff); //drag coefficient
            Cm = -Cn * (0.25f - 0.175f * (1 - 2 * angleEff / Mathf.PI)); //pitching moment coefficient
        }
        return new Vector3(Cl, Cd, Cm);
    }

    //calculate forces and pitching torque from aero coefficients
    public Vector3 CalculateLiftForces(Vector3 AeroCoefficients, float absVelocity, Vector3 liftDir) {
        Vector3 forceLift = liftDir * 0.5f * AeroCoefficients.x * 1.225f * absVelocity * absVelocity * wingArea;
        return forceLift;
    }
    public Vector3 CalculateDragForces(Vector3 AeroCoefficients, float absVelocity, Vector3 dragDir) {
        Vector3 forceDrag = dragDir * 0.5f * AeroCoefficients.y * 1.225f * absVelocity * absVelocity * wingArea;
        return forceDrag;
    }
    public float CalculatePitchingMoment(Vector3 AeroCoefficients, float absVelocity) {
        float pitchingMoment = 0.5f * AeroCoefficients.z * 1.225f * absVelocity * absVelocity * wingArea * wingSpan;
        return pitchingMoment;
    }

    //change horizontal flap angles
    public void AdjustHorizontalFlapAngle(float deltaFlapAngle) {
        horizontalFlapAngle += deltaFlapAngle * Mathf.Deg2Rad;

        if (horizontalFlapAngle < -flapLimiter * Mathf.Deg2Rad) {
            horizontalFlapAngle = -flapLimiter * Mathf.Deg2Rad;
        }
        if (horizontalFlapAngle > flapLimiter * Mathf.Deg2Rad) {
            horizontalFlapAngle = flapLimiter * Mathf.Deg2Rad;
        }
        associatedFlap.transform.RotateAround(associatedFlap.transform.position, associatedFlap.transform.right, -horizontalFlapAngle * Mathf.Rad2Deg);
    }

    //change vertical flap angles
    public void AdjustVerticalFlapAngle(float deltaFlapAngle) {
        verticalFlapAngle += deltaFlapAngle * Mathf.Deg2Rad;

        if (verticalFlapAngle < -flapLimiter * Mathf.Deg2Rad) {
            verticalFlapAngle = -flapLimiter * Mathf.Deg2Rad;
        }
        if (verticalFlapAngle > flapLimiter * Mathf.Deg2Rad) {
            verticalFlapAngle = flapLimiter * Mathf.Deg2Rad;
        }
        associatedFlap.transform.localEulerAngles = new Vector3(0, verticalFlapAngle * Mathf.Rad2Deg, 0);
    }

    //used for interpolating flap angles back to 0
    public void SetHorizontalFlapAngle(float lerpingHorAngle) {
        horizontalFlapAngle = lerpingHorAngle;
        associatedFlap.transform.localEulerAngles = new Vector3(-horizontalFlapAngle * Mathf.Rad2Deg, 0, 0);
    }
    public void SetVerticalFlapAngle(float lerpingVertAngle) {
        verticalFlapAngle = lerpingVertAngle;
        associatedFlap.transform.localEulerAngles = new Vector3(0, verticalFlapAngle * Mathf.Rad2Deg, 0);
    }
}
