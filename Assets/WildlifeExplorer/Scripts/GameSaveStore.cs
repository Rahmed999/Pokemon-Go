using System;
using System.IO;
using UnityEngine;
using WildlifeExplorer;
public sealed class GameSaveStore {
    public GameSave Data {get;private set;}
    public string Warning {get;private set;}="";
    readonly string path;
    public GameSaveStore(string file){path=file;Load();}
    void Load(){
        Data=new GameSave();if(!File.Exists(path))return;
        try{var loaded=JsonUtility.FromJson<GameSave>(File.ReadAllText(path));
            if(loaded==null||(loaded.version!=1&&loaded.version!=2)||loaded.rewarded==null||loaded.consumedZones==null||loaded.xp<0||loaded.foxCount<0||loaded.sensitivity<0||loaded.sensitivity>2)throw new InvalidDataException("Invalid save");
            if(loaded.version==2&&(loaded.egg==null||loaded.collection==null||loaded.eggDiscoveryLocks==null||loaded.egg.target<1||loaded.egg.steps<0||loaded.egg.steps>loaded.egg.target||!Enum.IsDefined(typeof(EggState),loaded.egg.state)))throw new InvalidDataException("Invalid egg save");
            if(loaded.version==2){
                var ids=new System.Collections.Generic.HashSet<string>();
                foreach(var animal in loaded.collection)if(animal==null||string.IsNullOrWhiteSpace(animal.id)||animal.count<1||!ids.Add(animal.id))throw new InvalidDataException("Invalid collection record");
                var egg=loaded.egg;
                if(egg.state!=EggState.None&&(string.IsNullOrWhiteSpace(egg.id)||string.IsNullOrWhiteSpace(egg.discoveryZone)))throw new InvalidDataException("Missing egg identity");
                if((egg.state==EggState.Discovered&&egg.steps!=0)||(egg.state==EggState.Incubating&&egg.steps>=egg.target)||((egg.state==EggState.ReadyToHatch||egg.state==EggState.Hatching)&&egg.steps!=egg.target))throw new InvalidDataException("Inconsistent egg progress");
            }
            loaded.Upgrade();Data=loaded;
        }catch(Exception){
            try{File.Copy(path,path+".unreadable-"+DateTime.UtcNow.Ticks+".bak");}catch(Exception){ }
            Warning="Your save could not be read. A backup was attempted; a new collection is being used.";
        }
    }
    public bool TryCommit(Action<GameSave> change){
        var candidate=JsonUtility.FromJson<GameSave>(JsonUtility.ToJson(Data));change(candidate);
        if(!Write(candidate))return false;Data=candidate;return true;
    }
    public bool Save()=>Write(Data);
    bool Write(GameSave data){
        try{Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(data,true));
            if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);return true;
        }catch(Exception e){Warning="Could not save progress: "+e.GetType().Name;Debug.LogWarning(Warning);return false;}
    }
}
