# Flight_Simulator
 
Hello! This is a ‘realistic’ flight simulator with PID control I developed to gain experience coding in C#. 

If you want to try it out, clone this repository and drag the scene MainScene into your Unity game located at Flight_Simulator/Assets/GameScenes/MainScene. For game controls:
W, A, S, D, E and Q control aircraft rotation, LShift and LControl control engine thrust, and T activates the PID Control System termed S.A.S (Stability Augmentation System).

![](Images/Aircraft1.JPG) 

# Aircraft Physics

Aircraft physics are modelled using [1], which presents equations for the coefficients of lift, drag and pitching moment for an aerodynamic surface given the angles of attack of both the airfoil and it’s associated flap.

[1] W. Khan and M. Nahon, "Real-time modeling of agile fixed-wing UAV aerodynamics," 2015 International Conference on Unmanned Aircraft Systems (ICUAS), 2015, pp. 1188-1195, doi: 10.1109/ICUAS.2015.7152411

# PID Control

By pressing 'T', a proportional (P), integral (I) and derivative (D) controller or stability augmentation system (S.A.S) is activated for each of the aircrafts primary axes: roll, pitch, and yaw. This tuned S.A.S. attempts to limit the crafts angular velocity to 0 using experimentally chosen gain values for Kp, Ki, Kd in each axis. 
