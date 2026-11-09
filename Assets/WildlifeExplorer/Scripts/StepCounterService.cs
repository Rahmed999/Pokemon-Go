using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using WildlifeExplorer;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
public class StepCounterService:MonoBehaviour {
    const string ActivityPermission="android.permission.ACTIVITY_RECOGNITION";
    HuntProgress owner;StepCounter device;bool enabledByUs,foreground=true,permissionPending,subscribed,dirty=true,lastSimulation,lastPermission;
    EggState lastState;string lastId;float nextPermissionCheck;
    public string Status {get;private set;}="Collect an egg to start incubation.";
    public bool PermissionPending=>permissionPending;
    public bool HasPermission {
        get {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Permission.HasUserAuthorizedPermission(ActivityPermission);
#else
            return Application.isEditor;
#endif
        }
    }
    public void Initialize(HuntProgress progress){owner=progress;InputSystem.onDeviceChange+=DeviceChanged;subscribed=true;Refresh();}
    void DeviceChanged(InputDevice d,InputDeviceChange change){if(d is StepCounter&&(change==InputDeviceChange.Added||change==InputDeviceChange.Removed||change==InputDeviceChange.Disconnected||change==InputDeviceChange.Reconnected))dirty=true;}
    public void RequestPermission(){
        if(owner==null||owner.Eggs.Egg.state!=EggState.Incubating||owner.Eggs.Egg.simulated||permissionPending)return;
#if UNITY_ANDROID && !UNITY_EDITOR
        if(!HasPermission){
            permissionPending=true;SetStatus("Allow Physical activity to count steps.");var callbacks=new PermissionCallbacks();
            callbacks.PermissionGranted+=_=>PermissionAnswered();callbacks.PermissionDenied+=_=>PermissionAnswered();callbacks.PermissionDeniedAndDontAskAgain+=_=>PermissionAnswered();
            Permission.RequestUserPermission(ActivityPermission,callbacks);return;
        }
#endif
        Refresh();
    }
    void PermissionAnswered(){permissionPending=false;Refresh();}
    void Update(){
        if(owner==null)return;var egg=owner.Eggs.Egg;
        if(lastState!=egg.state||lastId!=egg.id||lastSimulation!=egg.simulated){lastState=egg.state;lastId=egg.id;lastSimulation=egg.simulated;dirty=true;}
        if(Time.unscaledTime>=nextPermissionCheck){nextPermissionCheck=Time.unscaledTime+1;bool granted=HasPermission;if(granted!=lastPermission){lastPermission=granted;dirty=true;}}
        if(dirty){dirty=false;Refresh();}
    }    void Refresh(){
        if(owner==null)return;StopSensor();var egg=owner.Eggs.Egg;
        if(egg.state!=EggState.Incubating){SetStatus(egg.state==EggState.ReadyToHatch?"Ready to hatch.":"Collect an egg to start incubation.");return;}
        if(egg.simulated){SetStatus("SIMULATED STEPS - developer test egg.");return;}
        if(!foreground){SetStatus("Paused. Only foreground steps count.");return;}
        if(!HasPermission){SetStatus("Physical activity permission is needed. Tap Enable step counter.");return;}
        device=StepCounter.current;
        if(device==null){SetStatus("No pedometer detected. Exploring and catching remain available.");return;}
        owner.Eggs.StartForeground(Time.realtimeSinceStartupAsDouble);
        enabledByUs=!device.enabled;if(enabledByUs)InputSystem.EnableDevice(device);
        InputSystem.onEvent+=Reading;SetStatus("Pedometer active. Waiting for a fresh baseline; updates can take 10 seconds.");
    }
    void Reading(InputEventPtr evt,InputDevice source){
        if(source!=device||device==null||!foreground||!HasPermission||owner.Eggs.Egg.state!=EggState.Incubating||owner.Eggs.Egg.simulated)return;
        if(!evt.IsA<StateEvent>()&&!evt.IsA<DeltaStateEvent>())return;
        if(device.stepCounter.ReadValueFromEvent(evt,out int raw)){owner.Eggs.Reading(raw,evt.time);SetStatus("Pedometer active. Foreground steps only; updates may arrive in batches.");}
    }
    void StopSensor(){InputSystem.onEvent-=Reading;if(device!=null&&enabledByUs&&device.added)InputSystem.DisableDevice(device);device=null;enabledByUs=false;}
    void SetStatus(string message){if(Status==message)return;Status=message;owner?.Notify();}
    void OnApplicationPause(bool paused){if(permissionPending)return;foreground=!paused;Refresh();}
    void OnDisable(){StopSensor();}
    void OnDestroy(){StopSensor();if(subscribed)InputSystem.onDeviceChange-=DeviceChanged;}
}

