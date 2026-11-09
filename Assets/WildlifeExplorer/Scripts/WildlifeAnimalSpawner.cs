using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class WildlifeAnimalSpawner : MonoBehaviour
{
    public WildlifeExplorerGPS explorer;
    public ARRaycastManager raycastManager;
    public ARAnchorManager anchorManager;
    public Camera arCamera;
    public GameObject animalPrefab;
    public TMP_Text arStatusText;
    [Tooltip("GPS_Test only: renders a local preview rather than actual AR.")]
    public bool previewWithoutAR;
    public UnityEvent<GameObject> onAnimalPlaced = new UnityEvent<GameObject>();
    public UnityEvent onAnimalCaptured = new UnityEvent();
    public GameObject CurrentAnimal {get;private set;}
    public bool Placing {get;private set;}
    public event Action<string> CaptureConfirmed;
    Vector2? placementPoint;
    public void PlaceAtScreenPoint(Vector2 point){placementPoint=point;PlaceAnimal();}
    public string Message {get;private set;}="Start exploration, then enter the zone.";
    public string Status => previewWithoutAR ? "LOCAL PREVIEW - not AR tracking\n"+Message :
        ARSession.state==ARSessionState.Unsupported ? "AR unsupported on this phone. Use GPS_Test instead." :
        ARSession.state!=ARSessionState.SessionTracking ? "AR: "+ARSession.state+". Scan a textured surface.\n"+Message : Message;
    readonly List<ARRaycastHit> hits=new List<ARRaycastHit>();
    ARAnchor anchor;
    int generation;
    bool subscribed;
    void Start(){Subscribe();}
    void OnEnable(){Subscribe();}
    void Subscribe(){if(!subscribed&&explorer!=null){explorer.ExplorationReset+=ResetEncounter;subscribed=true;}}
    void Update(){if(arStatusText!=null)arStatusText.text=Status;}
    void OnDisable()
    {
        if(subscribed&&explorer!=null)explorer.ExplorationReset-=ResetEncounter;
        subscribed=false;ResetEncounter();
    }
    public async void PlaceAnimal()
    {
        if(explorer==null||animalPrefab==null||arCamera==null){Message="Assign spawner references.";return;}
        if(Placing||CurrentAnimal!=null)return;
        if(!explorer.CanPlaceAnimal){Message="Enter the zone with fresh accurate GPS first.";return;}
        if(previewWithoutAR)
        {
            if(!explorer.TryMarkAnimalPlaced())return;
            Vector3 pos=arCamera.transform.position+arCamera.transform.forward*4.5f;pos.y=0;
            CurrentAnimal=Instantiate(animalPrefab,pos,Quaternion.Euler(0,180,0));
            CurrentAnimal.GetComponent<ExplorerAnimal>().Initialize(this);
            Message="Preview ready. Real AR is tested in WildlifeExplorer_AR.";
            onAnimalPlaced.Invoke(CurrentAnimal);return;
        }
        if(ARSession.state!=ARSessionState.SessionTracking||anchorManager==null||raycastManager==null)
        {Message="Wait for AR tracking and scan the ground.";return;}
        Vector2 screenPoint=placementPoint??new Vector2(Screen.width/2f,Screen.height/2f);placementPoint=null;
        hits.Clear();
        if(!raycastManager.Raycast(screenPoint,hits,TrackableType.PlaneWithinPolygon))
        {Message="Aim the centre reticle at a detected horizontal surface.";return;}
        Placing=true;int request=generation;ARAnchor created=null;Message="Creating anchor...";
        try
        {
            var result=await anchorManager.TryAddAnchorAsync(hits[0].pose);
            if(!result.status.IsSuccess()||result.value==null){Message="Anchor failed. Try another surface.";return;}
            created=result.value;
            if(this==null||!isActiveAndEnabled||request!=generation||!explorer.CanPlaceAnimal||ARSession.state!=ARSessionState.SessionTracking)
            {Destroy(created.gameObject);return;}
            if(!explorer.TryMarkAnimalPlaced()){Destroy(created.gameObject);return;}
            anchor=created;CurrentAnimal=Instantiate(animalPrefab,anchor.transform);
            CurrentAnimal.transform.localPosition=Vector3.zero;
            Vector3 facing=arCamera.transform.position-CurrentAnimal.transform.position;facing.y=0;
            if(facing.sqrMagnitude>.001f)CurrentAnimal.transform.rotation=Quaternion.LookRotation(facing);
            CurrentAnimal.GetComponent<ExplorerAnimal>().Initialize(this);
            Message="Animal placed. Your friend's ball can capture it.";onAnimalPlaced.Invoke(CurrentAnimal);
        }
        catch(Exception ex)
        {
            Debug.LogException(ex);
            if(created!=null&&created!=anchor)Destroy(created.gameObject);
            if(request==generation){explorer.CompleteEncounter();RemoveAnimal();Message="Placement failed. Restart exploration and retry.";}
        }
        finally{if(this!=null&&request==generation)Placing=false;}
    }
    public bool ConfirmCapture(ExplorerAnimal target)
    {
        if(!previewWithoutAR && ARSession.state!=ARSessionState.SessionTracking)return false;
        if(CurrentAnimal==null||target==null||target.gameObject!=CurrentAnimal||!explorer.TryCompleteEncounter())return false;
        string animalId=target.animalId;RemoveAnimal();
        Message="Captured "+animalId+". Exit beyond 70 m, then return within 50 m.";
        CaptureConfirmed?.Invoke(animalId);onAnimalCaptured.Invoke();return true;
    }
    public void TestCapture()
    {
        if(!(Application.isEditor||Debug.isDebugBuild))return;
        if(CurrentAnimal!=null)CurrentAnimal.GetComponent<ExplorerAnimal>().TryCapture();
    }
    public void ResetEncounter(){generation++;Placing=false;RemoveAnimal();Message="Start exploration, then enter the zone.";}
    void RemoveAnimal()
    {
        if(CurrentAnimal!=null)Destroy(CurrentAnimal);
        if(anchor!=null)Destroy(anchor.gameObject);
        CurrentAnimal=null;anchor=null;
    }
}
