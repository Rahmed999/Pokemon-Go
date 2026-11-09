using System;
using System.IO;
using UnityEngine;
// One owner survives menu/game transitions. All gameplay writes use this same store.
public class HuntProgress:MonoBehaviour {
    static HuntProgress instance;
    public static HuntProgress Instance {get{if(instance==null){var go=new GameObject("Shared Hunt Progress");instance=go.AddComponent<HuntProgress>();DontDestroyOnLoad(go);}return instance;}}
    public GameSaveStore Save {get;private set;}
    public EggManager Eggs {get;private set;}
    public StepCounterService Steps {get;private set;}
    public int Revision {get;private set;}
    public string EggNotice {get;private set;}="";
    public event Action Changed;
    void Awake(){
        if(instance!=null&&instance!=this){Destroy(gameObject);return;}instance=this;
        Save=new GameSaveStore(Path.Combine(Application.persistentDataPath,"animal-hunt-v1.json"));Eggs=new EggManager(Save);Eggs.Changed+=EggChanged;
        Steps=gameObject.AddComponent<StepCounterService>();Steps.Initialize(this);Save.Save();
    }
    void EggChanged(){EggNotice=Eggs.Egg.state==WildlifeExplorer.EggState.ReadyToHatch?"Your egg is ready to hatch!":Eggs.Egg.state==WildlifeExplorer.EggState.Discovered?"A Mystery Egg is nearby!":"";Notify();}
    public void Notify(){Revision++;Changed?.Invoke();}
    void OnApplicationPause(bool paused){if(paused)Save.Save();}
    void OnApplicationQuit(){Save.Save();}
    void OnDestroy(){if(instance==this)instance=null;}
}
