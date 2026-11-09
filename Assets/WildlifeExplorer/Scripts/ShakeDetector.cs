using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using WildlifeExplorer;
public class ShakeDetector:MonoBehaviour {
    public event Action Shaken;
    public bool Available=>Accelerometer.current!=null;
    public bool Calibrated=>gesture!=null&&gesture.Calibrated;
    ShakeGesture gesture;Accelerometer sensor;bool enabledByUs;
    public void SetSensitivity(int level){if(gesture!=null)gesture.Threshold=level==0?1.35:level==2?.75:1;}
    public void ResetCalibration(int sensitivity){gesture=new ShakeGesture();SetSensitivity(sensitivity);}
    void OnEnable(){
        ResetCalibration(1);sensor=Accelerometer.current;
        if(sensor!=null){enabledByUs=!sensor.enabled;if(enabledByUs)InputSystem.EnableDevice(sensor);try{sensor.samplingFrequency=50;}catch(NotSupportedException){ }}
        InputSystem.onEvent+=OnSensorEvent;
    }
    void OnDisable(){InputSystem.onEvent-=OnSensorEvent;if(sensor!=null&&enabledByUs&&sensor.added)InputSystem.DisableDevice(sensor);sensor=null;}
    void OnSensorEvent(InputEventPtr evt,InputDevice device){
        if(device!=sensor||sensor==null||(!evt.IsA<StateEvent>()&&!evt.IsA<DeltaStateEvent>()))return;
        if(sensor.acceleration.ReadValueFromEvent(evt,out Vector3 a)&&gesture.Sample(a.x,a.y,a.z,evt.time))Shaken?.Invoke();
    }
}
