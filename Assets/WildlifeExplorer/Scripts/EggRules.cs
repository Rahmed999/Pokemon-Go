using System;
namespace WildlifeExplorer {
public enum EggState {None,Discovered,Incubating,ReadyToHatch,Hatching}
[Serializable] public sealed class EggSaveData {
    public string id="",discoveryZone="";
    public EggState state=EggState.None;
    public int steps,target=100;
    public bool simulated;
    public long lastRaw=-1;
    public float Percent=>target>0?Math.Min(1,Math.Max(0,steps/(float)target)):0;
    public void Recover(){if(state==EggState.Hatching)state=EggState.ReadyToHatch;}
}
[Serializable] public sealed class AnimalCount {public string id;public int count;public AnimalCount(string species,int total){id=species;count=total;}}
// Baselines are session-local. A serialized checkpoint is diagnostic, never a resume baseline.
public sealed class ForegroundSteps {
    long last=-1;double boundary,lastEvent=-1;
    public void StartSession(double time){last=-1;boundary=time;lastEvent=-1;}
    public int Apply(EggSaveData egg,long raw,double eventTime){
        if(egg==null||egg.state!=EggState.Incubating||raw<0||double.IsNaN(eventTime)||double.IsInfinity(eventTime)||eventTime<boundary||eventTime<=lastEvent)return 0;
        lastEvent=eventTime;egg.lastRaw=raw;
        if(last<0||raw<last){last=raw;return 0;}
        long delta=raw-last;last=raw;
        int gained=(int)Math.Min(delta,Math.Max(0,egg.target-egg.steps));egg.steps+=gained;
        if(egg.steps>=egg.target){egg.steps=egg.target;egg.state=EggState.ReadyToHatch;}
        return gained;
    }
}
}
