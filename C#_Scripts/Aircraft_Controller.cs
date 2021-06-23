using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Aircraft_Controller : MonoBehaviour
{
    public float Kp_Roll = 100;
    public float Ki_Roll = 1;
    public float Kd_Roll = 0.1f;
    public float Kp_Pitch = 160;
    public float Ki_Pitch = 10;
    public float Kd_Pitch = 0.3f;
    public float Kp_Yaw = 300;
    public float Ki_Yaw = 1;
    public float Kd_Yaw = 0.05f;

    private float prevErrorRoll, prevErrorYaw, prevErrorPitch;
    private float P_Roll, I_Roll, D_Roll, P_Pitch, I_Pitch, D_Pitch, P_Yaw, I_Yaw, D_Yaw;

    //takes in relative angular velocities for pitch, yaw and roll and attempts to get 0 angular velocity
    public Vector3 GetSASOutput(float currentErrorPitch, float currentErrorYaw, float currentErrorRoll, float dt) {
        P_Roll = currentErrorRoll;
        I_Roll += P_Roll * dt;
        D_Roll = (P_Roll - prevErrorRoll) / dt;
        prevErrorRoll = currentErrorRoll;

        P_Yaw = currentErrorYaw;
        I_Yaw += P_Yaw * dt;
        D_Yaw = (P_Yaw - prevErrorYaw) / dt;
        prevErrorYaw = currentErrorYaw;

        P_Pitch = currentErrorPitch;
        I_Pitch += P_Pitch * dt;
        D_Pitch = (P_Pitch - prevErrorPitch) / dt;
        prevErrorPitch = currentErrorPitch;

        float rollOutput = P_Roll * Kp_Roll + I_Roll * Ki_Roll + D_Roll * Kd_Roll;
        float yawOutput = P_Yaw * Kp_Yaw + I_Yaw * Ki_Yaw + D_Yaw * Kd_Yaw;
        float pitchOutput = P_Pitch * Kp_Pitch + I_Pitch * Ki_Pitch + D_Pitch * Kd_Pitch;
        return new Vector3(pitchOutput, yawOutput, rollOutput);
    }

    public void ResetIntegral() {
        I_Roll = 0;
        I_Pitch = 0;
        I_Yaw = 0;
    }
}
