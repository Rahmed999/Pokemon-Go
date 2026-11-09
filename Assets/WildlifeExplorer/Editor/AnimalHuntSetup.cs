using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
public static class AnimalHuntSetup {
    public const string Root="Assets/WildlifeExplorer";
    public const string Menu=Root+"/Scenes/AnimalHunt_Menu.unity",Game=Root+"/Scenes/AnimalHunt_Game.unity",Test=Root+"/Scenes/ShakeCatch_Test.unity";
    [MenuItem("Animal Hunt/1 Create full game scenes (new files only)")]
    public static void Create(){
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        if(File.Exists(Menu)||File.Exists(Game)||File.Exists(Test))throw new Exception("New game scenes already exist. Open them; creation will not overwrite them.");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere.name="Capture Ball";sphere.transform.localScale=Vector3.one*.12f;
        var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=new Color(.95f,.28f,.11f);AssetDatabase.CreateAsset(mat,Root+"/Materials/Capture Ball.mat");sphere.GetComponent<Renderer>().sharedMaterial=mat;
        var rb=sphere.AddComponent<Rigidbody>();rb.mass=.1f;rb.useGravity=true;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;sphere.AddComponent<CaptureBall>();
        var prefab=PrefabUtility.SaveAsPrefabAsset(sphere,Root+"/Prefabs/Capture Ball.prefab").GetComponent<CaptureBall>();UnityEngine.Object.DestroyImmediate(sphere);
        var menuManager=new GameObject("Animal Hunt Menu").AddComponent<AnimalHuntGame>();menuManager.menuOnly=true;
        Camera overview=Overview();EventSystem();EditorSceneManager.SaveScene(scene,Menu);
        CreateGameplay(ExplorerSetup.GPSScene,Test,true,prefab);CreateGameplay(ExplorerSetup.ARScene,Game,false,prefab);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Menu,true),new EditorBuildSettingsScene(Game,true),new EditorBuildSettingsScene(Test,true)};
        AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(Menu);Debug.Log("ANIMAL_HUNT_SETUP_OK");
    }
    static void CreateGameplay(string original,string destination,bool local,CaptureBall ball){
        var scene=EditorSceneManager.OpenScene(original,OpenSceneMode.Single);
        var gps=UnityEngine.Object.FindFirstObjectByType<WildlifeExplorerGPS>();var spawner=UnityEngine.Object.FindFirstObjectByType<WildlifeAnimalSpawner>();
        foreach(var ui in UnityEngine.Object.FindObjectsByType<ExplorerUI>(FindObjectsSortMode.None))ui.enabled=false;
        foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.gameObject.SetActive(false);
        spawner.arStatusText=null;
        var game=gps.gameObject.AddComponent<AnimalHuntGame>();game.gps=gps;game.spawner=spawner;game.localPreview=local;game.ballPrefab=ball;
        game.overviewCamera=Overview();gps.useSimulation=local;
        if(local){game.originRoot=spawner.arCamera.gameObject;game.originRoot.SetActive(false);}
        else{game.originRoot=spawner.raycastManager.gameObject;game.sessionRoot=UnityEngine.Object.FindFirstObjectByType<ARSession>().gameObject;game.originRoot.SetActive(false);game.sessionRoot.SetActive(false);}
        EditorSceneManager.SaveScene(scene,destination);
    }
    static Camera Overview(){var go=new GameObject("Overview Camera",typeof(Camera),typeof(AudioListener));var camera=go.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.08f,.105f);camera.cullingMask=0;return camera;}
    static void EventSystem(){new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule)).GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
    [MenuItem("Animal Hunt/Open Main Menu")]public static void OpenMenu(){if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(Menu);}
    [MenuItem("Animal Hunt/Open Standalone Test")]public static void OpenTest(){if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(Test);}
    [MenuItem("Animal Hunt/2 Run all automated checks")]public static void Checks(){ExplorerChecks.Run();}
    [MenuItem("Animal Hunt/3 Build both APKs")]public static void BuildBoth(){Build(true);Build(false);}
    [MenuItem("Animal Hunt/Build standalone test APK")]public static void BuildTest(){Build(true);}
    [MenuItem("Animal Hunt/Build full AR game APK")]public static void BuildGame(){Build(false);}
    static void Build(bool test){
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var settings=AssetDatabase.LoadAllAssetsAtPath("Assets/XR/XRGeneralSettings.asset").FirstOrDefault(o=>o.name=="Android Settings");
        var serial=new SerializedObject(settings);var init=serial.FindProperty("m_InitManagerOnStart");bool previous=init.boolValue;
        string id=PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android),name=PlayerSettings.productName;
        var core=new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/XR/Settings/AR Core Settings.asset"));core.FindProperty("m_Depth").intValue=1;core.FindProperty("m_Requirement").intValue=1;core.ApplyModifiedPropertiesWithoutUndo();
        Directory.CreateDirectory("Builds");
        try{
            init.boolValue=false;serial.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,test?"com.student.animalhunt.test":"com.student.animalhunt.game");PlayerSettings.productName=test?"Animal Hunt Test":"AR Animal Hunt";
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Menu,test?Test:Game},target=BuildTarget.Android,locationPathName=test?"Builds/ShakeCatch_Test.apk":"Builds/AnimalHunt_FullAR.apk",options=BuildOptions.Development});
            if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Build failed: "+result.summary.result);
            Debug.Log("ANIMAL_HUNT_BUILD_OK: "+result.summary.outputPath);
        }finally{init.boolValue=previous;serial.ApplyModifiedPropertiesWithoutUndo();PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,id);PlayerSettings.productName=name;AssetDatabase.SaveAssets();}
    }
}
