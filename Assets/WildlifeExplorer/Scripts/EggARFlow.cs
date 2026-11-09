using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using WildlifeExplorer;
// Reuses the game's camera, raycast manager and anchor manager. No second AR session.
public class EggARFlow:MonoBehaviour {
    public AnimalHuntGame game;
    public GameObject EggObject {get;private set;}
    public GameObject BabyObject {get;private set;}
    public bool Placing {get;private set;}
    public string Message {get;private set;}="";
    bool hatch;int generation;ARAnchor anchor;GameObject marker;Coroutine animation;
    readonly List<GameObject> fragments=new List<GameObject>();
    readonly List<ARRaycastHit> hits=new List<ARRaycastHit>();
    public void Begin(bool hatching){Cancel();hatch=hatching;Message=hatch?"Tap a surface to hatch your ready egg.":"Place the egg, then tap the egg itself to collect it.";}
    void Update(){
        if(game==null||!game.EggFlowActive||game.State==HuntState.EggResult)return;
        if(!game.Tracking){if(marker!=null)marker.SetActive(false);return;}
        if(game.State==HuntState.EggScanning&&!game.localPreview){
            hits.Clear();bool found=game.spawner.raycastManager.Raycast(new Vector2(Screen.width/2f,Screen.height/2f),hits,TrackableType.PlaneWithinPolygon);
            if(marker==null){marker=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Destroy(marker.GetComponent<Collider>());marker.name="Egg placement marker";marker.transform.localScale=new Vector3(.3f,.004f,.3f);marker.GetComponent<Renderer>().material.color=new Color(.2f,1,.7f);}
            marker.SetActive(found);if(found)marker.transform.SetPositionAndRotation(hits[0].pose.position,hits[0].pose.rotation);
        }
        if(Tapped(out Vector2 point)&&!AnimalHuntGame.OverUI(point)){
            if(game.State==HuntState.EggScanning&&!game.localPreview)Place(point);
            else if(game.State==HuntState.EggCollecting&&EggObject!=null){var ray=game.spawner.arCamera.ScreenPointToRay(point);if(Physics.Raycast(ray,out RaycastHit hit,20)&&hit.collider.transform.IsChildOf(EggObject.transform))TryCollect();}
        }
    }
    static bool Tapped(out Vector2 position){position=default;
        if(Touchscreen.current!=null&&Touchscreen.current.primaryTouch.press.wasPressedThisFrame){position=Touchscreen.current.primaryTouch.position.ReadValue();return true;}
        if(Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame){position=Mouse.current.position.ReadValue();return true;}return false;
    }
    public void PlaceLocal(){if(game.localPreview)Place(Vector2.zero);}
    public async void Place(Vector2 screen){
        if(Placing||EggObject!=null||game.State!=HuntState.EggScanning||!game.Tracking)return;
        if(!hatch&&!game.CanCollectEgg){Message="Return to the egg's discovery zone with accurate GPS.";return;}
        var prefab=Resources.Load<GameObject>("WalkHatch/MysteryEgg");if(prefab==null){Message="Egg prefab missing. Run Walk & Hatch > Prepare egg assets.";return;}
        int request=generation;Placing=true;ARAnchor created=null;
        try{
            Vector3 position;
            if(game.localPreview){position=game.spawner.arCamera.transform.position+game.spawner.arCamera.transform.forward*2;position.y=0;}
            else{
                hits.Clear();if(!game.spawner.raycastManager.Raycast(screen,hits,TrackableType.PlaneWithinPolygon)){Message="Tap a detected horizontal surface.";return;}
                var result=await game.spawner.anchorManager.TryAddAnchorAsync(hits[0].pose);
                if(!result.status.IsSuccess()||result.value==null){Message="Anchor failed. Tap another surface.";return;}created=result.value;position=created.transform.position;
            }
            if(this==null||request!=generation||!game.EggFlowActive||!game.Tracking||(!hatch&&!game.CanCollectEgg)){if(created!=null)Destroy(created.gameObject);return;}
            anchor=created;EggObject=anchor!=null?Instantiate(prefab,anchor.transform):Instantiate(prefab,position,Quaternion.identity);
            if(game.localPreview)game.spawner.arCamera.transform.LookAt(position+Vector3.up*.22f);
            if(marker!=null)marker.SetActive(false);game.EggPlaced();
            if(hatch){if(!game.Progress.Eggs.BeginHatch()){Message="Egg is not ready, or progress could not be saved.";Cancel();game.CancelEggFlow();return;}animation=StartCoroutine(HatchSequence(game.Progress.Eggs.Egg.id,request));}
            else Message="Tap the glowing Mystery Egg to collect it.";
        }catch(Exception e){Debug.LogException(e);if(created!=null&&created!=anchor)Destroy(created.gameObject);Message="Placement failed. Return to My Egg and retry.";}
        finally{if(this!=null&&generation==request)Placing=false;}
    }
    public bool TryCollect(){
        if(game.State!=HuntState.EggCollecting||EggObject==null||!game.Tracking||!game.CanCollectEgg)return false;
        if(!game.Progress.Eggs.Collect(game.Development&&game.Progress.Eggs.Egg.simulated))return false;
        game.EggCollected();return true;
    }
    IEnumerator HatchSequence(string id,int request){
        Message="Your egg is hatching...";Vector3 initial=EggObject.transform.localScale;float start=Time.unscaledTime;
        while(Time.unscaledTime-start<1.6f){
            if(request!=generation||!game.Tracking){Interrupt();yield break;}
            float t=Time.unscaledTime-start;EggObject.transform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*32)*Mathf.Lerp(3,14,t/1.6f));EggObject.transform.localScale=initial*(1+.04f*Mathf.Sin(t*40));yield return null;
        }
        var baby=Resources.Load<GameObject>("WalkHatch/BabyFox");if(baby==null){Message="Baby Fox prefab missing. Prepare egg assets and retry.";Interrupt();yield break;}
        if(!game.Tracking||request!=generation){Interrupt();yield break;}
        Vector3 position=EggObject.transform.position;
        // Spawn the presentation first, then commit reward+egg consumption in a single save transaction.
        BabyObject=anchor!=null?Instantiate(baby,anchor.transform):Instantiate(baby,position,Quaternion.identity);
        if(!game.Progress.Eggs.CompleteHatch(id,game.gps.IsInsideZone?game.ZoneKey:"")){Destroy(BabyObject);BabyObject=null;Message="Hatch could not be saved. Your egg remains ready; retry.";Interrupt();yield break;}
        Crack(position);Destroy(EggObject);EggObject=null;
        Vector3 facing=game.spawner.arCamera.transform.position-position;facing.y=0;if(facing.sqrMagnitude>.01f)BabyObject.transform.rotation=Quaternion.LookRotation(facing);
        Message="Your Baby Fox has hatched!";StartCoroutine(Reveal(BabyObject));
        // Keep the AR presentation visible before the opaque results panel.
        float revealUntil=Time.unscaledTime+1.3f;while(Time.unscaledTime<revealUntil){if(request!=generation)yield break;yield return null;}
        animation=null;game.EggHatched();
    }
    void Crack(Vector3 position){
        var material=EggObject.GetComponentInChildren<Renderer>().sharedMaterial;
        for(int i=0;i<6;i++){var piece=GameObject.CreatePrimitive(PrimitiveType.Sphere);piece.name="Egg shell fragment";Destroy(piece.GetComponent<Collider>());piece.transform.localScale=new Vector3(.08f,.025f,.06f);piece.GetComponent<Renderer>().sharedMaterial=material;piece.transform.position=position+Vector3.up*.22f;fragments.Add(piece);StartCoroutine(Fragment(piece,position,i));}
    }
    IEnumerator Fragment(GameObject piece,Vector3 centre,int index){float start=Time.unscaledTime;float angle=index*Mathf.PI/3;Vector3 direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));while(piece!=null&&Time.unscaledTime-start<.7f){float t=Time.unscaledTime-start;piece.transform.position=centre+direction*t*.6f+Vector3.up*Mathf.Max(.02f,.22f+t*.6f-t*t*1.4f);piece.transform.Rotate(160*Time.unscaledDeltaTime,80*Time.unscaledDeltaTime,0);yield return null;}if(piece!=null)Destroy(piece);}
    IEnumerator Reveal(GameObject baby){Vector3 scale=baby.transform.localScale;float start=Time.unscaledTime;while(baby!=null&&Time.unscaledTime-start<.45f){baby.transform.localScale=scale*Mathf.SmoothStep(.1f,1,(Time.unscaledTime-start)/.45f);yield return null;}if(baby!=null)baby.transform.localScale=scale;}
    void Interrupt(){game.Progress.Eggs.InterruptHatch();Cancel();game.CancelEggFlow();}
    public void Cancel(){generation++;if(animation!=null)StopCoroutine(animation);animation=null;Placing=false;
        if(game!=null&&game.Progress!=null)game.Progress.Eggs.InterruptHatch();
        if(EggObject!=null)Destroy(EggObject);if(BabyObject!=null)Destroy(BabyObject);if(anchor!=null)Destroy(anchor.gameObject);if(marker!=null)marker.SetActive(false);
        foreach(var piece in fragments)if(piece!=null)Destroy(piece);fragments.Clear();EggObject=BabyObject=null;anchor=null;
    }
    void OnDestroy(){Cancel();if(marker!=null)Destroy(marker);}
}
