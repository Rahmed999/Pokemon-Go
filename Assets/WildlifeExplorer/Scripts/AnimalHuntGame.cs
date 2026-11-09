using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;
using WildlifeExplorer;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
public enum HuntState {MainMenu,Exploring,EncounterAvailable,Scanning,ReadyToThrow,BallInFlight,Captured,Escaped,EggScanning,EggCollecting,EggHatching,EggResult}
public class AnimalHuntGame:MonoBehaviour {
    public bool menuOnly,localPreview;
    public int eggStepTarget=100;
    public bool eggDiscoveryEnabled=true;
    public HuntProgress Progress {get;private set;}
    public EggARFlow EggAR {get;private set;}
    public bool EggFlowActive=>State==HuntState.EggScanning||State==HuntState.EggCollecting||State==HuntState.EggHatching||State==HuntState.EggResult;
    public bool EggHatchMode {get;private set;}
    public bool CanCollectEgg=>!menuOnly&&gps!=null&&Progress.Eggs.Egg.state==EggState.Discovered&&Progress.Eggs.Egg.discoveryZone==ZoneKey&&gps.IsInsideZone&&gps.HasFreshPosition;
    static bool launchHatch;
    public WildlifeExplorerGPS gps;
    public WildlifeAnimalSpawner spawner;
    public GameObject sessionRoot,originRoot;
    public Camera overviewCamera;
    public CaptureBall ballPrefab;
    public HuntState State {get;private set;}=HuntState.MainMenu;
    public CatchRound Round {get;private set;}=new CatchRound();
    public GameSaveStore Save {get;private set;}
    public string Notice {get;private set;}="";
    public string ResultText {get;private set;}="";
    public bool Development=>Application.isEditor||Debug.isDebugBuild;
    public bool Ready=>State==HuntState.ReadyToThrow&&!modal&&!paused&&Tracking;
    public bool Tracking=>localPreview||ARSession.state==ARSessionState.SessionTracking;
    public bool HasSensor=>shake!=null&&shake.Available;
    public bool Calibrated=>shake!=null&&shake.Calibrated;
    public bool Modal=>modal;
    public bool Resuming=>resumeEncounter;
    public bool CameraStarting {get;private set;}
    public string ZoneKey=>gps==null?"":(gps.useSimulation?"simulation:":"gps:")+gps.zoneLatitude.ToString("F6",CultureInfo.InvariantCulture)+","+gps.zoneLongitude.ToString("F6",CultureInfo.InvariantCulture);
    public bool Consumed=>gps!=null&&Save.Data.ZoneConsumed(ZoneKey);
    public bool CanOpen=>gps!=null&&gps.CanPlaceAnimal&&!Consumed;
    ShakeDetector shake;CaptureBall ball;AnimalHuntView view;bool modal,paused,resumeEncounter;
    float lastThrowAt=-100;
    bool started,dragging;Vector2 dragPosition;
    int eggOutsideCount;double eggOutsideTime=-1;
    int outsideCount;double previousOutside=-1;float effectUntil;bool pulseSuccess;
    readonly List<ARRaycastHit> planeHits=new List<ARRaycastHit>();GameObject marker;AudioSource speaker;
    void Awake(){
        Progress=HuntProgress.Instance;Save=Progress.Save;
        Notice=Save.Warning;speaker=gameObject.AddComponent<AudioSource>();speaker.playOnAwake=false;
        shake=gameObject.AddComponent<ShakeDetector>();shake.enabled=false;shake.Shaken+=ThrowBall;
        view=gameObject.AddComponent<AnimalHuntView>();view.game=this;
        if(!menuOnly){
            EggAR=gameObject.AddComponent<EggARFlow>();EggAR.game=this;
            gps.ExternalPauseManagement=true;gps.AccuratePosition+=Position;gps.ExplorationReset+=ExplorationReset;
            spawner.onAnimalPlaced.AddListener(Placed);spawner.CaptureConfirmed+=Captured;
            if(originRoot!=null)originRoot.SetActive(false);if(sessionRoot!=null)sessionRoot.SetActive(false);
        }
    }
    IEnumerator Start(){started=true;if(menuOnly){view.ShowSplash();yield return new WaitForSecondsRealtime(1);view.ShowMenu();}else{view.ShowExplore();if(launchHatch){launchHatch=false;State=HuntState.Exploring;OpenEggCamera(true);}else StartExploring();}}
    void Update(){
        if(menuOnly)return;
        gps.ExternalZoneBlocked=Consumed;
        if(!modal&&(State==HuntState.Exploring||State==HuntState.EncounterAvailable))State=CanOpen?HuntState.EncounterAvailable:HuntState.Exploring;
        if(ball!=null&&!Tracking){CancelBall();Notice="Tracking lost. Scan the surface again; your throw was refunded.";}
        bool enable=Ready;
        if(shake.enabled!=enable){shake.enabled=enable;if(enable)shake.ResetCalibration(Save.Data.sensitivity);}
        if(State==HuntState.Scanning&&!modal&&Tracking&&!localPreview)UpdateMarkerAndTap();
        if(localPreview&&Ready)UpdateLocalAim();
        if(!localPreview&&(State==HuntState.ReadyToThrow||State==HuntState.BallInFlight))SetPlanes(false);
#if UNITY_EDITOR
        if(Keyboard.current!=null&&Keyboard.current.spaceKey.wasPressedThisFrame)ThrowBall();
#endif
        if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)Back();
        if(effectUntil>Time.unscaledTime)view.SetFlash(pulseSuccess?(effectUntil-Time.unscaledTime)*.4f:(effectUntil-Time.unscaledTime)*.12f,pulseSuccess);
        else view.SetFlash(0,false);
    }
    public void Play(){
        if(!menuOnly)return;
        string next=Application.CanStreamedLevelBeLoaded("ShakeCatch_Test")?"ShakeCatch_Test":"AnimalHunt_Game";
        SceneManager.LoadScene(next);
    }
    public void StartExploring(){
        modal=false;Notice=Save.Warning;outsideCount=0;previousOutside=-1;
        gps.ExternalZoneBlocked=Consumed;gps.StartExploration();State=HuntState.Exploring;view.ShowExplore();
    }
    void Position(double distance,double timestamp){
        if(eggDiscoveryEnabled){
            if(eggOutsideTime>0&&timestamp-eggOutsideTime>15)eggOutsideCount=0;eggOutsideTime=timestamp;
            if(distance>70){if(++eggOutsideCount>=3){Progress.Eggs.ConfirmExit(ZoneKey);eggOutsideCount=0;}}
            else eggOutsideCount=0;
            if(gps.IsInsideZone)Progress.Eggs.Discover(ZoneKey,eggStepTarget);
        }
        if(!Consumed){outsideCount=0;return;}
        if(previousOutside>0&&timestamp-previousOutside>15)outsideCount=0;previousOutside=timestamp;
        if(distance<=70){outsideCount=0;return;}
        if(++outsideCount>=3){Save.Data.ConfirmExit(ZoneKey);Save.Save();gps.ExternalZoneBlocked=false;Notice="Zone reset. Return within 50 m for another encounter.";outsideCount=0;}
    }
    public void ChangeSimulation(){
        if(State!=HuntState.Exploring&&State!=HuntState.EncounterAvailable)return;
        gps.SetSimulation(!gps.useSimulation);StartExploring();
    }
    public void OpenCamera(){if(!CanOpen||CameraStarting)return;StartCoroutine(OpenCameraRoutine(false));}
    public void ShowMyEgg(){modal=true;view.ShowEggInventory();}
    public void CollectEgg(){if(menuOnly){Play();return;}if(CanCollectEgg)OpenEggCamera(false);}
    public void HatchEgg(){
        if(Progress.Eggs.Egg.state!=EggState.ReadyToHatch)return;
        if(menuOnly){launchHatch=true;Play();return;}OpenEggCamera(true);
    }
    public void OpenEggCamera(bool hatch){
        if(CameraStarting||(!hatch&&!CanCollectEgg)||(hatch&&Progress.Eggs.Egg.state!=EggState.ReadyToHatch))return;
        modal=false;EggHatchMode=hatch;StartCoroutine(OpenCameraRoutine(true));
    }
    public void EggPlaced(){State=EggHatchMode?HuntState.EggHatching:HuntState.EggCollecting;SetPlanes(false);view.ShowEggEncounter();}
    public void EggCollected(){Notice="Mystery Egg collected! Walk "+Progress.Eggs.Egg.target+" steps to hatch it.";EggAR.Cancel();DisableCamera();State=HuntState.Exploring;modal=false;view.ShowExplore();if(!Progress.Eggs.Egg.simulated)Progress.Steps.RequestPermission();}
    public void EggHatched(){State=HuntState.EggResult;Notice="Congratulations! You hatched a Baby Fox!";Feedback(true,1100);view.ShowEggResult();}
    public void CancelEggFlow(){if(EggAR!=null)EggAR.Cancel();DisableCamera();if(!gps.Running){gps.ExternalZoneBlocked=Consumed;gps.StartExploration();}State=HuntState.Exploring;modal=false;view.ShowEggInventory();modal=true;}
    public void DeveloperSteps(int count){if(Development)Progress.Eggs.Simulate(count);}
    public void RequestSteps(){Progress.Steps.RequestPermission();}
    public void ToggleEggSimulation(){if(!Development||Progress.Eggs.Egg.state!=EggState.Discovered)return;Save.TryCommit(data=>data.egg.simulated=!data.egg.simulated);Progress.Notify();view.ShowEggInventory();}
    IEnumerator OpenCameraRoutine(bool eggFlow){
        CameraStarting=true;Notice="Starting camera...";
        if(!localPreview){
#if UNITY_ANDROID && !UNITY_EDITOR
            if(!Permission.HasUserAuthorizedPermission(Permission.Camera)){
                bool answered=false;var callbacks=new PermissionCallbacks();callbacks.PermissionGranted+=_=>answered=true;callbacks.PermissionDenied+=_=>answered=true;callbacks.PermissionDeniedAndDontAskAgain+=_=>answered=true;
                Permission.RequestUserPermission(Permission.Camera,callbacks);float until=Time.realtimeSinceStartup+60;
                while(!answered&&Time.realtimeSinceStartup<until)yield return null;
                if(!Permission.HasUserAuthorizedPermission(Permission.Camera)){Notice="Camera permission denied. Allow Camera in Android app settings to use AR.";CameraStarting=false;yield break;}
            }
#endif
            if(XRGeneralSettings.Instance==null||XRGeneralSettings.Instance.Manager==null){Notice="XR settings unavailable.";CameraStarting=false;yield break;}
            var xr=XRGeneralSettings.Instance.Manager;
            if(xr.activeLoader==null)yield return xr.InitializeLoader();
            if(xr.activeLoader==null){Notice="ARCore is unavailable. Use an ARCore-supported phone or the standalone test APK.";CameraStarting=false;yield break;}
            xr.StartSubsystems();
        }
        if(eggFlow){
            if((EggHatchMode&&Progress.Eggs.Egg.state!=EggState.ReadyToHatch)||(!EggHatchMode&&!CanCollectEgg)){DisableCamera();CameraStarting=false;yield break;}
            if(originRoot!=null)originRoot.SetActive(true);if(sessionRoot!=null)sessionRoot.SetActive(true);if(overviewCamera!=null)overviewCamera.gameObject.SetActive(false);
            State=HuntState.EggScanning;Notice=localPreview?"LOCAL TEST: place the egg to continue.":"Scan a horizontal surface, then tap to place the egg.";
            EggAR.Begin(EggHatchMode);CameraStarting=false;view.ShowEggEncounter();yield break;
        }
        if(!gps.Running)gps.StartExploration();
        float freshDeadline=Time.realtimeSinceStartup+20;
        while(!CanOpen&&!Consumed&&Time.realtimeSinceStartup<freshDeadline){Notice="Waiting for fresh, accurate location before entering AR...";yield return null;}
        if(!CanOpen){DisableCamera();Notice="Refresh your GPS position and enter the zone before opening the camera.";CameraStarting=false;yield break;}
        if(!resumeEncounter)Round.Begin(Guid.NewGuid().ToString("N"));
        if(originRoot!=null)originRoot.SetActive(true);if(sessionRoot!=null)sessionRoot.SetActive(true);
        if(overviewCamera!=null)overviewCamera.gameObject.SetActive(false);
        State=HuntState.Scanning;Notice=localPreview?"LOCAL TEST: tap Place fox. No AR tracking is used.":"Stand still and scan a textured floor. Tap a marked surface to place the fox.";
        CameraStarting=false;view.ShowEncounter();
    }
    void UpdateMarkerAndTap(){
        planeHits.Clear();bool found=spawner.raycastManager.Raycast(new Vector2(Screen.width*.5f,Screen.height*.5f),planeHits,TrackableType.PlaneWithinPolygon);
        if(marker==null){marker=GameObject.CreatePrimitive(PrimitiveType.Cylinder);marker.name="Placement Marker";Destroy(marker.GetComponent<Collider>());marker.transform.localScale=new Vector3(.3f,.004f,.3f);marker.GetComponent<Renderer>().material.color=new Color(.2f,1,.65f);}
        marker.SetActive(found);if(found)marker.transform.SetPositionAndRotation(planeHits[0].pose.position,planeHits[0].pose.rotation);
        bool tapped=false;Vector2 pos=default;
        if(Touchscreen.current!=null&&Touchscreen.current.primaryTouch.press.wasPressedThisFrame){tapped=true;pos=Touchscreen.current.primaryTouch.position.ReadValue();}
        else if(Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame){tapped=true;pos=Mouse.current.position.ReadValue();}
        if(tapped&&!OverUI(pos))spawner.PlaceAtScreenPoint(pos);
    }
    public static bool OverUI(Vector2 pos){if(EventSystem.current==null)return false;var results=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=pos},results);return results.Count>0;}
    public void PlaceLocal(){if(State==HuntState.Scanning&&localPreview)spawner.PlaceAnimal();}
    void Placed(GameObject animal){
        if(animal.GetComponent<FoxIdle>()==null)animal.AddComponent<FoxIdle>();
        if(marker!=null)marker.SetActive(false);SetPlanes(false);State=HuntState.ReadyToThrow;resumeEncounter=false;
        if(localPreview)spawner.arCamera.transform.LookAt(animal.GetComponent<Collider>().bounds.center);
        Notice="Aim at the fox, then shake!";view.ShowEncounter();
    }
    void SetPlanes(bool visible){if(spawner.raycastManager==null)return;var manager=spawner.raycastManager.GetComponent<ARPlaneManager>();if(manager!=null)foreach(var plane in manager.trackables)foreach(var renderer in plane.GetComponentsInChildren<Renderer>())renderer.enabled=visible;}
    public void ThrowBall(){
        if(!Ready||Time.unscaledTime-lastThrowAt<1.2f||spawner.CurrentAnimal==null||ballPrefab==null||!Round.Launch())return;
        lastThrowAt=Time.unscaledTime;
        var cam=spawner.arCamera;var ray=new Ray(cam.transform.position,cam.transform.forward);
        Vector3 aim=ray.GetPoint(3);
        if(Physics.Raycast(ray,out RaycastHit hit,5,~0,QueryTriggerInteraction.Ignore))aim=ray.GetPoint(Mathf.Clamp(hit.distance,1,5));
        Vector3 start=cam.transform.position+cam.transform.forward*.3f-cam.transform.up*.15f;
        float travel=Mathf.Max(.08f,Vector3.Distance(start,aim)/8);
        ball=Instantiate(ballPrefab,start,Quaternion.identity);ball.Resolved=BallResolved;
        ball.Launch((aim-start)/travel-.5f*Physics.gravity*travel);State=HuntState.BallInFlight;Notice="Ball in flight";Feedback(false,650);view.Refresh();
    }
    public void ApplyLocalAim(Vector2 delta){
        if(!localPreview||spawner==null||spawner.arCamera==null)return;
        var camera=spawner.arCamera.transform;Vector3 e=camera.eulerAngles;float pitch=Mathf.DeltaAngle(0,e.x);
        camera.rotation=Quaternion.Euler(Mathf.Clamp(pitch-delta.y*.12f,-80,80),e.y+delta.x*.12f,0);
    }
    void UpdateLocalAim(){
        bool pressed=false;Vector2 pos=default;
        if(Touchscreen.current!=null){pressed=Touchscreen.current.primaryTouch.press.isPressed;pos=Touchscreen.current.primaryTouch.position.ReadValue();}
        else if(Mouse.current!=null){pressed=Mouse.current.leftButton.isPressed;pos=Mouse.current.position.ReadValue();}
        if(!pressed){dragging=false;return;}
        if(!dragging){if(OverUI(pos))return;dragging=true;dragPosition=pos;return;}
        ApplyLocalAim((pos-dragPosition)*(1080f/Screen.width));dragPosition=pos;
    }
    void BallResolved(bool hit){
        ball=null;if(hit)return;if(State!=HuntState.BallInFlight)return;
        if(!Tracking||paused){Round.Cancel();State=HuntState.ReadyToThrow;Notice="Tracking interrupted. Throw refunded; scan the surface again.";return;}
        Round.Miss();Feedback(false,190);
        if(Round.Escaped){EndEncounterAsEscaped();return;}
        State=HuntState.ReadyToThrow;Notice="Miss - try again. Aim the centre reticle at the fox.";
    }
    void Captured(string animalId){
        if(!Round.Capture())return;
        Save.Data.RecordCapture(Round.Id,animalId);Save.Data.Consume(ZoneKey);Save.Save();
        State=HuntState.Captured;ResultText="Congratulations! Fox captured!\n+50 XP";
        Feedback(true,980);view.ShowResult();
    }
    public void EndEncounterAsEscaped(){
        if(State==HuntState.Captured||State==HuntState.Escaped)return;
        if(ball!=null){ball.Cancel();ball=null;}Round.Escape();gps.CompleteEncounter();spawner.ResetEncounter();
        Save.Data.Consume(ZoneKey);Save.Save();State=HuntState.Escaped;ResultText="The fox escaped!\nLeave the zone and return to try again.";resumeEncounter=false;
        view.ShowResult();
    }
    void CancelBall(){if(ball!=null){ball.Cancel();ball=null;}Round.Cancel();if(State==HuntState.BallInFlight)State=HuntState.ReadyToThrow;}
    public void ContinueExploring(){
        if(EggAR!=null)EggAR.Cancel();
        modal=false;DisableCamera();resumeEncounter=false;StartExploring();
    }
    void DisableCamera(){
        if(marker!=null)marker.SetActive(false);SetPlanes(true);
        if(sessionRoot!=null)sessionRoot.SetActive(false);if(originRoot!=null)originRoot.SetActive(false);
        if(overviewCamera!=null)overviewCamera.gameObject.SetActive(true);
        if(!localPreview&&XRGeneralSettings.Instance?.Manager?.activeLoader!=null)XRGeneralSettings.Instance.Manager.StopSubsystems();
    }
    public void ShowCollection(){modal=true;view.ShowCollection();}
    public void ShowSettings(){modal=true;view.ShowSettings();}
    public void CloseOverlay(){modal=false;if(menuOnly)view.ShowMenu();else if(EggFlowActive){if(State==HuntState.EggResult)view.ShowEggResult();else view.ShowEggEncounter();}else if(State==HuntState.Captured||State==HuntState.Escaped)view.ShowResult();else if(State==HuntState.Scanning||State==HuntState.ReadyToThrow||State==HuntState.BallInFlight)view.ShowEncounter();else view.ShowExplore();}
    public void Back(){
        if(modal){CloseOverlay();return;}if(EggFlowActive){CancelEggFlow();return;}if(menuOnly){view.ShowMenu();return;}
        if(State==HuntState.Scanning||State==HuntState.ReadyToThrow||State==HuntState.BallInFlight||resumeEncounter){CancelBall();modal=true;view.ShowAbandon();}
        else ReturnMenu();
    }
    public void Abandon(){modal=false;EndEncounterAsEscaped();}
    public void ReturnMenu(){if(EggAR!=null)EggAR.Cancel();if(!menuOnly){gps.StopExploration();DisableCamera();}Save.Save();SceneManager.LoadScene("AnimalHunt_Menu");}
    public void Quit(){Save.Save();Application.Quit();}
    public void ToggleSound(){Save.Data.sound=!Save.Data.sound;Save.Save();view.ShowSettings();}
    public void ToggleVibration(){Save.Data.vibration=!Save.Data.vibration;Save.Save();view.ShowSettings();}
    public void CycleSensitivity(){Save.Data.sensitivity=(Save.Data.sensitivity+1)%3;Save.Save();if(shake!=null)shake.ResetCalibration(Save.Data.sensitivity);view.ShowSettings();}
    void Feedback(bool success,int hz){
        pulseSuccess=success;effectUntil=Time.unscaledTime+(success?.6f:.25f);
        if(Save.Data.vibration&&Application.isMobilePlatform)Handheld.Vibrate();
        if(!Save.Data.sound)return;int samples=4410;var data=new float[samples];for(int i=0;i<samples;i++)data[i]=Mathf.Sin(2*Mathf.PI*hz*i/44100f)*.12f*(1-i/(float)samples);
        var clip=AudioClip.Create("Feedback",samples,1,44100,false);clip.SetData(data,0);speaker.PlayOneShot(clip);Destroy(clip,1);
    }
    void ExplorationReset(){if(State==HuntState.ReadyToThrow||State==HuntState.BallInFlight||State==HuntState.Scanning){CancelBall();resumeEncounter=true;}}
    void OnApplicationPause(bool pause){
        if(menuOnly||!started)return;
        // Permission prompts must not cancel camera initialization.
        if(CameraStarting||gps.PermissionPending||Progress.Steps.PermissionPending)return;
        if(pause&&EggFlowActive){EggAR.Cancel();State=HuntState.Exploring;}
        paused=pause;
        if(pause){bool unfinished=State==HuntState.ReadyToThrow||State==HuntState.BallInFlight||State==HuntState.Scanning;CancelBall();resumeEncounter|=unfinished;shake.enabled=false;gps.StopExploration();Save.Save();}
        else if(State!=HuntState.Captured&&State!=HuntState.Escaped){DisableCamera();modal=false;StartExploring();Notice=resumeEncounter?"Encounter paused. Re-enter the zone and reopen camera; remaining attempts are preserved.":"Location refreshed after resume.";}
    }
    void OnDestroy(){if(EggAR!=null)EggAR.Cancel();if(gps!=null){gps.AccuratePosition-=Position;gps.ExplorationReset-=ExplorationReset;}if(spawner!=null){spawner.onAnimalPlaced.RemoveListener(Placed);spawner.CaptureConfirmed-=Captured;}if(ball!=null)ball.Cancel();if(marker!=null)Destroy(marker);}
}

