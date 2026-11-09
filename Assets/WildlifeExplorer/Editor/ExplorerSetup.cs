using System;
using System.IO;
using System.Linq;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>Creates NEW scenes. Never rewrites or deletes the user's manually built scenes.</summary>
public static class ExplorerSetup
{
    const string Root="Assets/WildlifeExplorer";
    public const string ARScene=Root+"/Scenes/WildlifeExplorer_AR_Complete.unity";
    public const string GPSScene=Root+"/Scenes/GPS_Test_Complete.unity";
    static readonly Color Ink=new Color(.045f,.1f,.14f,.94f),Teal=new Color(.08f,.48f,.4f),White=new Color(.93f,.97f,.96f);
    static TMP_FontAsset Font=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

    [MenuItem("Wildlife Explorer/Create completed demo scenes (new files only)")]
    public static void FinishScenes()
    {
        if(File.Exists(ARScene)||File.Exists(GPSScene))throw new Exception("Completed scenes already exist. Open them; setup will not overwrite your work.");
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        Directory.CreateDirectory(Root+"/Scenes");Directory.CreateDirectory(Root+"/Prefabs");Directory.CreateDirectory(Root+"/Materials");AssetDatabase.Refresh();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        GameObject fox=CreateAnimal();
        CreateScene(GPSScene,false,fox);CreateScene(ARScene,true,fox);
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,"com.student.pokemongo.wildlife");
        PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel29;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});
        var core=AssetDatabase.LoadAllAssetsAtPath("Assets/XR/Settings/AR Core Settings.asset").FirstOrDefault();
        if(core!=null){var s=new SerializedObject(core);s.FindProperty("m_Requirement").intValue=1;s.ApplyModifiedPropertiesWithoutUndo();}
        AssetDatabase.SaveAssets();ValidateScenes();Debug.Log("WILDLIFE_SETUP_OK");
    }
    static void CreateScene(string path,bool ar,GameObject fox)
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var manager=new GameObject("ExplorerManager");var gps=manager.AddComponent<WildlifeExplorerGPS>();var spawn=manager.AddComponent<WildlifeAnimalSpawner>();
        gps.useSimulation=true;spawn.explorer=gps;spawn.animalPrefab=fox;spawn.previewWithoutAR=!ar;
        var light=new GameObject("Directional Light",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(45,-35,0);
        if(ar)
        {
            EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (Mobile AR)");EditorApplication.ExecuteMenuItem("GameObject/XR/AR Session");
            var origin=UnityEngine.Object.FindFirstObjectByType<XROrigin>();if(origin==null)throw new Exception("XR Origin menu failed.");
            var plane=origin.GetComponent<ARPlaneManager>()??origin.gameObject.AddComponent<ARPlaneManager>();plane.requestedDetectionMode=PlaneDetectionMode.Horizontal;
            plane.planePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/AR Default Plane.prefab");
            if(plane.planePrefab==null)throw new Exception("Existing plane prefab was not found.");
            spawn.raycastManager=origin.GetComponent<ARRaycastManager>()??origin.gameObject.AddComponent<ARRaycastManager>();
            spawn.anchorManager=origin.GetComponent<ARAnchorManager>()??origin.gameObject.AddComponent<ARAnchorManager>();spawn.arCamera=origin.Camera??origin.GetComponentInChildren<Camera>();
        }
        else
        {
            var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();camera.tag="MainCamera";
            camera.transform.SetPositionAndRotation(new Vector3(0,1.3f,-3),Quaternion.Euler(15,0,0));camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.17f,.2f);spawn.arCamera=camera;
            var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.name="Preview Ground";ground.transform.localScale=Vector3.one*2;ground.GetComponent<Renderer>().sharedMaterial=Material("Ground",new Color(.28f,.36f,.34f));
        }
        manager.name="ExplorerManager";light.name="Directional Light";
        CreateUI(manager,gps,spawn,ar);
        new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule)).GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        EditorSceneManager.SaveScene(scene,path);
    }
    static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 position,Vector2 size)
    {
        var rt=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rt.SetParent(parent,false);rt.anchorMin=min;rt.anchorMax=max;rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=position;rt.sizeDelta=size;return rt;
    }
    static RectTransform Top(string name,Transform parent,float y,float h)=>Rect(name,parent,new Vector2(0,1),new Vector2(1,1),new Vector2(0,-y-h/2),new Vector2(-64,h));
    static RectTransform Bottom(string name,Transform parent,float y,float h)=>Rect(name,parent,Vector2.zero,new Vector2(1,0),new Vector2(0,y+h/2),new Vector2(-64,h));
    static TMP_Text Text(string name,Transform parent,string value,int size)
    {
        var rt=Rect(name,parent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);var t=rt.gameObject.AddComponent<TextMeshProUGUI>();t.font=Font;t.fontSize=size;t.color=White;t.text=value;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;t.alignment=TextAlignmentOptions.TopLeft;return t;
    }
    static Button Button(string name,Transform rect,string label,UnityAction action)
    {
        rect.name=name;var image=rect.gameObject.AddComponent<Image>();image.color=Teal;var b=rect.gameObject.AddComponent<Button>();b.targetGraphic=image;Text("Label",rect,label,30).alignment=TextAlignmentOptions.Center;UnityEventTools.AddPersistentListener(b.onClick,action);return b;
    }
    static void CreateUI(GameObject manager,WildlifeExplorerGPS gps,WildlifeAnimalSpawner spawn,bool ar)
    {
        var canvas=new GameObject("Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var scale=canvas.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1080,1920);scale.matchWidthOrHeight=.5f;
        var safe=Rect("Safe Area",canvas.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);Text("Title",Top("Title Area",safe,24,64),"WILDLIFE EXPLORER",42);
        var modeArea=Top("Mode",safe,104,68);modeArea.gameObject.AddComponent<Image>().color=Ink;var toggle=modeArea.gameObject.AddComponent<Toggle>();
        var box=Rect("Box",modeArea,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(36,0),new Vector2(36,36));box.gameObject.AddComponent<Image>().color=Color.white;
        var tick=Rect("Check",box,Vector2.zero,Vector2.one,Vector2.zero,new Vector2(-10,-10));var check=tick.gameObject.AddComponent<Image>();check.color=Teal;toggle.targetGraphic=box.GetComponent<Image>();toggle.graphic=check;toggle.isOn=true;
        var mode=Text("Mode Label",modeArea,"SIMULATION",30);mode.rectTransform.offsetMin=new Vector2(74,0);mode.alignment=TextAlignmentOptions.MidlineLeft;
        var info=Top("GPS Information",safe,194,420);info.gameObject.AddComponent<Image>().color=Ink;gps.statusText=Text("GPSStatusText",info,"Press Start exploration.",29);gps.statusText.rectTransform.offsetMin=new Vector2(20,14);gps.statusText.rectTransform.offsetMax=new Vector2(-20,-14);
        var reticle=Rect("Reticle",safe,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(70,70));Text("Cross",reticle,"+",52).alignment=TextAlignmentOptions.Center;
        var detail=Bottom("Encounter Status",safe,660,174);detail.gameObject.AddComponent<Image>().color=Ink;spawn.arStatusText=Text("ARStatusText",detail,ar?"Scan a surface after entering the zone.":"LOCAL PREVIEW - not AR tracking",28);spawn.arStatusText.rectTransform.offsetMin=new Vector2(16,12);spawn.arStatusText.rectTransform.offsetMax=new Vector2(-16,-12);
        var row=Bottom("Start Stop",safe,546,86);
        Button("StartButton",Rect("Start",row,Vector2.zero,new Vector2(.5f,1),new Vector2(-7,0),new Vector2(-14,0)),"Start exploration",gps.StartExploration);
        Button("StopButton",Rect("Stop",row,new Vector2(.5f,0),Vector2.one,new Vector2(7,0),new Vector2(-14,0)),"Stop",gps.StopExploration);
        var place=Button("PlaceAnimalButton",Bottom("Place",safe,434,86),ar?"Place animal on surface":"Show animal preview",spawn.PlaceAnimal);
        var sim=Bottom("Simulation Controls",safe,238,170);
        Button("InsideButton",Rect("Inside",sim,new Vector2(0,.5f),new Vector2(.5f,1),new Vector2(-7,7),new Vector2(-14,-14)),"Inside: 10 m",gps.SimulateInside);
        Button("OutsideButton",Rect("Outside",sim,new Vector2(.5f,.5f),Vector2.one,new Vector2(7,7),new Vector2(-14,-14)),"Outside: 100 m",gps.SimulateOutside);
        Button("EdgeButton",Rect("Edge",sim,Vector2.zero,new Vector2(.5f,.5f),new Vector2(-7,-7),new Vector2(-14,-14)),"Edge: 60 m",gps.SimulateEdge);
        Button("BadGPSButton",Rect("Bad",sim,new Vector2(.5f,0),new Vector2(1,.5f),new Vector2(7,-7),new Vector2(-14,-14)),"Poor accuracy",gps.SimulatePoorGPS);
        var zone=Button("SetZoneButton",Bottom("Set zone",safe,136,76),"Set test zone here (real GPS)",gps.SetTestZoneHere);
        var capture=Button("TestCaptureButton",Bottom("Test capture",safe,42,72),"Test capture - development only",spawn.TestCapture);
        var ui=manager.AddComponent<ExplorerUI>();ui.explorer=gps;ui.spawner=spawn;ui.encounter=spawn.arStatusText;ui.modeLabel=mode;ui.simulationToggle=toggle;ui.placeButton=place;ui.setZoneButton=zone;ui.captureButton=capture;ui.simulationControls=sim.gameObject;ui.safeArea=safe;
    }
    static Material Material(string name,Color colour)
    {
        string path=Root+"/Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}mat.color=colour;return mat;
    }
    static GameObject CreateAnimal()
    {
        string path=Root+"/Prefabs/Fox Animal.prefab";
        if(File.Exists(path))throw new Exception("Fox Animal.prefab already exists. Setup will not overwrite it.");
        var orange=Material("Fox Orange",new Color(.85f,.31f,.08f));var cream=Material("Fox Cream",new Color(.96f,.9f,.72f));var dark=Material("Fox Dark",new Color(.08f,.06f,.05f));
        var animal=new GameObject("Fox Animal");animal.AddComponent<ExplorerAnimal>().animalId="fox";var hit=animal.AddComponent<CapsuleCollider>();hit.center=new Vector3(0,.52f,.05f);hit.radius=.35f;hit.height=1.04f;
        Part(animal,"Body",PrimitiveType.Sphere,new Vector3(0,.5f,0),new Vector3(.6f,.45f,.85f),orange);
        Part(animal,"Chest",PrimitiveType.Sphere,new Vector3(0,.54f,.3f),new Vector3(.42f,.42f,.38f),cream);
        Part(animal,"Head",PrimitiveType.Sphere,new Vector3(0,.83f,.42f),Vector3.one*.47f,orange);
        Part(animal,"Muzzle",PrimitiveType.Sphere,new Vector3(0,.74f,.65f),new Vector3(.29f,.22f,.3f),cream);
        Part(animal,"Nose",PrimitiveType.Sphere,new Vector3(0,.77f,.81f),Vector3.one*.08f,dark);
        for(int i=-1;i<=1;i+=2){Part(animal,"Ear",PrimitiveType.Capsule,new Vector3(i*.15f,1.08f,.4f),new Vector3(.13f,.17f,.12f),orange);Part(animal,"Eye",PrimitiveType.Sphere,new Vector3(i*.17f,.88f,.6f),Vector3.one*.055f,dark);for(int j=-1;j<=1;j+=2)Part(animal,"Leg",PrimitiveType.Capsule,new Vector3(i*.2f,.22f,j*.28f),new Vector3(.12f,.22f,.12f),dark);}
        var tail=Part(animal,"Tail",PrimitiveType.Capsule,new Vector3(0,.62f,-.65f),new Vector3(.19f,.4f,.19f),orange);tail.transform.localRotation=Quaternion.Euler(-48,0,0);
        Part(animal,"Tail Tip",PrimitiveType.Sphere,new Vector3(0,.86f,-.95f),Vector3.one*.22f,cream);
        animal.transform.localScale=Vector3.one*.65f;
        var prefab=PrefabUtility.SaveAsPrefabAsset(animal,path);UnityEngine.Object.DestroyImmediate(animal);return prefab;
    }
    static GameObject Part(GameObject parent,string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat)
    {
        var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent.transform,false);go.transform.localPosition=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
    }
    [MenuItem("Wildlife Explorer/Validate completed scene wiring")]
    public static void ValidateScenes()
    {
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        foreach(string path in new[]{GPSScene,ARScene})
        {
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
            foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new Exception("Missing script: "+t.name);
            var gps=UnityEngine.Object.FindFirstObjectByType<WildlifeExplorerGPS>();var sp=UnityEngine.Object.FindFirstObjectByType<WildlifeAnimalSpawner>();
            if(gps==null||sp==null||gps.statusText==null||sp.animalPrefab==null||sp.arCamera==null)throw new Exception("Unwired scene: "+path);
            if(path==ARScene&&(sp.anchorManager==null||sp.raycastManager==null||sp.previewWithoutAR))throw new Exception("AR setup incomplete.");
            foreach(var b in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None))if(b.onClick.GetPersistentEventCount()==0)throw new Exception("Unbound button: "+b.name);
            Debug.Log("WILDLIFE_VALIDATION_OK: "+path);
        }
    }
    [MenuItem("Wildlife Explorer/Repair generated scene object names")]
    public static void RepairGeneratedNames()
    {
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        string foxPath=Root+"/Prefabs/Fox Animal.prefab";
        var fox=PrefabUtility.LoadPrefabContents(foxPath);
        try{fox.transform.localScale=Vector3.one*.65f;PrefabUtility.SaveAsPrefabAsset(fox,foxPath);}
        finally{PrefabUtility.UnloadPrefabContents(fox);}
        foreach(string path in new[]{GPSScene,ARScene})
        {
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
            UnityEngine.Object.FindFirstObjectByType<WildlifeExplorerGPS>().gameObject.name="ExplorerManager";
            if(path==GPSScene)Camera.main.transform.rotation=Quaternion.Euler(15,0,0);
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional)light.gameObject.name="Directional Light";
            EditorSceneManager.SaveScene(scene);
        }
        ValidateScenes();
    }
    [MenuItem("Wildlife Explorer/Build GPS Test APK (A12 or A16)")]public static void BuildGPS(){Build(false);}
    [MenuItem("Wildlife Explorer/Build AR APK (supported phone)")]public static void BuildAR(){Build(true);}
    static void Build(bool ar)
    {
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var arSettings = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/XR/Settings/AR Core Settings.asset");
        if(arSettings != null){var arSerial = new SerializedObject(arSettings); arSerial.FindProperty("m_Depth").intValue = 1; arSerial.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets();}
        Directory.CreateDirectory("Builds");
        var settings=AssetDatabase.LoadAllAssetsAtPath("Assets/XR/XRGeneralSettings.asset").FirstOrDefault(o=>o.name=="Android Settings");var serial=settings==null?null:new SerializedObject(settings);var init=serial?.FindProperty("m_InitManagerOnStart");bool previous=init!=null&&init.boolValue;
        try
        {
            if(init!=null){init.boolValue=ar;serial.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();}
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ar?ARScene:GPSScene},target=BuildTarget.Android,locationPathName=ar?"Builds/WildlifeExplorer_AR.apk":"Builds/WildlifeExplorer_GPS_Test.apk",options=BuildOptions.Development});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Android build failed: "+report.summary.result);
            Debug.Log("WILDLIFE_BUILD_OK: "+report.summary.outputPath);
        }
        finally{if(init!=null){init.boolValue=previous;serial.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();}}
    }
}
