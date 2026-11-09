using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using WildlifeExplorer;
[InitializeOnLoad]public static class WalkHatchSmoke {
    static AnimalHuntGame game;static StepCounter sensor;static int stage;static double deadline;static string eggId;static double revealAt;
    static string PathName=>Path.Combine(Application.persistentDataPath,"animal-hunt-v1.json");
    static WalkHatchSmoke(){EditorApplication.playModeStateChanged+=Changed;}
    [MenuItem("Walk & Hatch/Run egg and pedometer smoke test")]
    public static void Run(){
        if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        WalkHatchSetup.Prepare();SessionState.SetBool("EggSmoke.hadSave",File.Exists(PathName));SessionState.SetString("EggSmoke.save",File.Exists(PathName)?File.ReadAllText(PathName):"");SessionState.SetBool("EggSmoke.run",true);
        EditorSceneManager.OpenScene(AnimalHuntSetup.Test);EditorApplication.isPlaying=true;
    }
    static void Changed(PlayModeStateChange change){
        if(!SessionState.GetBool("EggSmoke.run",false))return;
        if(change==PlayModeStateChange.EnteredPlayMode){stage=0;deadline=EditorApplication.timeSinceStartup+75;EditorApplication.update+=Tick;}
        if(change==PlayModeStateChange.EnteredEditMode){EditorApplication.update-=Tick;SessionState.SetBool("EggSmoke.run",false);if(SessionState.GetBool("EggSmoke.hadSave",false))File.WriteAllText(PathName,SessionState.GetString("EggSmoke.save",""));else if(File.Exists(PathName))File.Move(PathName,PathName+".egg-smoke-"+DateTime.UtcNow.Ticks);}
    }
    static void Tick(){try{
        if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Egg smoke timed out at stage "+stage);
        if(game==null)game=UnityEngine.Object.FindFirstObjectByType<AnimalHuntGame>();if(game==null)return;
        switch(stage){
        case 0:
            game.Save.TryCommit(s=>{s.egg=new EggSaveData();s.eggDiscoveryLocks.Clear();s.ConfirmExit(game.ZoneKey);});game.StartExploring();game.gps.SimulateInside();stage=1;break;
        case 1:if(game.CanCollectEgg){game.CollectEgg();stage=2;}break;
        case 2:if(game.State==HuntState.EggScanning){game.EggAR.PlaceLocal();stage=3;}break;
        case 3:if(game.State==HuntState.EggCollecting){if(!game.EggAR.TryCollect())throw new Exception("Collect failed");sensor=InputSystem.AddDevice<StepCounter>();stage=4;}break;
        case 4:if(sensor.enabled&&game.Progress.Steps.Status.StartsWith("Pedometer active")){InputSystem.QueueDeltaStateEvent(sensor.stepCounter,1000);stage=5;}break;
        case 5:if(game.Progress.Eggs.Egg.lastRaw==1000){if(game.Progress.Eggs.Egg.steps!=0)throw new Exception("Baseline counted old steps");InputSystem.QueueDeltaStateEvent(sensor.stepCounter,1025);stage=6;}break;
        case 6:if(game.Progress.Eggs.Egg.steps==25){InputSystem.QueueDeltaStateEvent(sensor.stepCounter,1025);stage=7;}break;
        case 7:if(game.Progress.Eggs.Egg.steps!=25)throw new Exception("Repeated sample counted");InputSystem.QueueDeltaStateEvent(sensor.stepCounter,1100);stage=8;break;
        case 8:if(game.Progress.Eggs.Egg.state==EggState.ReadyToHatch){eggId=game.Progress.Eggs.Egg.id;game.HatchEgg();stage=9;}break;
        case 9:if(game.State==HuntState.EggScanning){
            // Menu-launched hatching has not started GPS. Cancelling must restore exploration.
            game.gps.StopExploration();game.CancelEggFlow();if(!game.gps.Running)throw new Exception("Cancelling menu hatch left GPS stopped");game.CloseOverlay();game.HatchEgg();stage=11;
        }break;
        case 11:if(game.State==HuntState.EggScanning){game.EggAR.PlaceLocal();revealAt=0;stage=12;}break;
        case 12:if(game.EggAR.BabyObject!=null){revealAt=EditorApplication.timeSinceStartup;stage=10;}break;
        case 10:if(game.State==HuntState.EggResult){
            if(EditorApplication.timeSinceStartup-revealAt<1)throw new Exception("Opaque result hid the baby reveal immediately");
            if(game.Save.Data.CountAnimal("baby_fox")<1||game.Progress.Eggs.Egg.state!=EggState.None||!game.Save.Data.rewarded.Contains("egg:"+eggId))throw new Exception("Hatch reward not saved");
            if(game.Progress.Eggs.CompleteHatch(eggId))throw new Exception("Repeated hatch reward");
            if(game.spawner.CurrentAnimal!=null)throw new Exception("Egg flow spawned a capturable animal");
            Debug.Log("WALK_HATCH_SMOKE_OK: zone discovery, local egg collection, real StepCounter API event path baseline/25/duplicate/100, hatch animation, one Baby Fox reward, cleared slot.");Finish();
        }break;
        }
    }catch(Exception e){Debug.LogException(e);Debug.LogError("WALK_HATCH_SMOKE_FAILED");Finish();}}
    static void Finish(){if(sensor!=null&&sensor.added)InputSystem.RemoveDevice(sensor);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
}
