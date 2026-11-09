using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
// Uses an isolated persistent-data file and restores it; no player's collection is changed.
[InitializeOnLoad] public static class AnimalHuntSmoke {
    static double deadline;static int stage;static AnimalHuntGame game;static int initialFox,initialXP;static string originalSave;static bool hadSave;
    static string SavePath=>System.IO.Path.Combine(Application.persistentDataPath,"animal-hunt-v1.json");
    static AnimalHuntSmoke(){EditorApplication.playModeStateChanged+=Changed;}
    [MenuItem("Animal Hunt/Run end-to-end physics smoke test")]
    public static void Run(){
        if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        hadSave=System.IO.File.Exists(SavePath);originalSave=hadSave?System.IO.File.ReadAllText(SavePath):null;
        // Backup survives domain reload through SessionState, without modifying player progress.
        SessionState.SetString("HuntSmoke.save",originalSave??"");SessionState.SetBool("HuntSmoke.hadSave",hadSave);SessionState.SetBool("HuntSmoke.run",true);
        EditorSceneManager.OpenScene(AnimalHuntSetup.Test);EditorApplication.isPlaying=true;
    }
    static void Changed(PlayModeStateChange state){
        if(!SessionState.GetBool("HuntSmoke.run",false))return;
        if(state==PlayModeStateChange.EnteredPlayMode){stage=0;deadline=EditorApplication.timeSinceStartup+40;EditorApplication.update+=Tick;}
        if(state==PlayModeStateChange.EnteredEditMode){
            EditorApplication.update-=Tick;SessionState.SetBool("HuntSmoke.run",false);
            if(SessionState.GetBool("HuntSmoke.hadSave",false))System.IO.File.WriteAllText(SavePath,SessionState.GetString("HuntSmoke.save",""));
            else if(System.IO.File.Exists(SavePath))System.IO.File.Move(SavePath,SavePath+".smoke-"+DateTime.UtcNow.Ticks);
        }
    }
    static void Tick(){try{
        if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Smoke stage timed out: "+stage);
        if(game==null)game=UnityEngine.Object.FindFirstObjectByType<AnimalHuntGame>();if(game==null)return;
        switch(stage){
            case 0:
                game.Save.Data.ConfirmExit(game.ZoneKey);game.Save.Save();initialFox=game.Save.Data.foxCount;initialXP=game.Save.Data.xp;
                game.StartExploring();game.gps.SimulateInside();stage=1;break;
            case 1:if(game.CanOpen){game.OpenCamera();stage=2;}break;
            case 2:if(game.State==HuntState.Scanning){game.PlaceLocal();stage=3;}break;
            case 3:if(game.Ready){game.spawner.arCamera.transform.LookAt(game.spawner.CurrentAnimal.GetComponent<Collider>().bounds.center);game.ThrowBall();stage=4;}break;
            case 4:if(game.State==HuntState.Captured){
                if(game.Save.Data.foxCount!=initialFox+1||game.Save.Data.xp!=initialXP+50||!game.Consumed)throw new Exception("Capture reward/persistence mismatch");
                game.ContinueExploring();game.gps.SimulateOutside();stage=5;
            }break;
            case 5:if(!game.Consumed){game.gps.SimulateInside();stage=6;}break;
            case 6:if(game.CanOpen){game.OpenCamera();stage=7;}break;
            case 7:if(game.State==HuntState.Scanning){game.PlaceLocal();stage=8;}break;
            case 8:if(game.Ready){game.spawner.arCamera.transform.rotation=Quaternion.Euler(-75,0,0);game.ThrowBall();stage=9;}break;
            case 9:if(game.State==HuntState.ReadyToThrow){stage=8;}else if(game.State==HuntState.Escaped){
                if(game.Round.Attempts!=0||game.Save.Data.foxCount!=initialFox+1||!game.Consumed)throw new Exception("Escape/attempt mismatch");
                Debug.Log("ANIMAL_HUNT_SMOKE_OK: physical collision capture +50 XP, persisted zone, confirmed exit/reentry, three timeout misses escape.");Finish();
            }break;
        }
    }catch(Exception e){Debug.LogException(e);Debug.LogError("ANIMAL_HUNT_SMOKE_FAILED");Finish();}}
    static void Finish(){EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
}
