using System;
using System.Collections.Generic;
namespace WildlifeExplorer {
public sealed class CatchRound {
    public string Id {get;private set;}
    public int Attempts {get;private set;}=3;
    public bool InFlight {get;private set;}
    public bool Captured {get;private set;}
    public bool Escaped {get;private set;}
    public void Begin(string id){Id=id;Attempts=3;InFlight=Captured=Escaped=false;}
    public bool Launch(){if(InFlight||Captured||Escaped||Attempts<=0)return false;Attempts--;InFlight=true;return true;}
    public bool Miss(){if(!InFlight||Captured||Escaped)return false;InFlight=false;Escaped=Attempts==0;return true;}
    public bool Capture(){if(!InFlight||Captured||Escaped)return false;InFlight=false;Captured=true;return true;}
    public void Cancel(){if(InFlight){InFlight=false;Attempts++;}}
    public void Escape(){InFlight=false;Escaped=true;}
}
[Serializable] public sealed class GameSave {
    public int version=2,xp,foxCount;
    public EggSaveData egg=new EggSaveData();
    public List<AnimalCount> collection=new List<AnimalCount>();
    public List<string> eggDiscoveryLocks=new List<string>();
    public bool sound=true,vibration=true;
    public int sensitivity=1;
    public List<string> rewarded=new List<string>();
    public List<string> consumedZones=new List<string>();
    public bool RecordCapture(string id,string animal){if(string.IsNullOrEmpty(id)||animal!="fox"||rewarded.Contains(id))return false;rewarded.Add(id);xp+=50;foxCount++;AddAnimal(animal);return true;}
    public void AddAnimal(string id){var item=collection.Find(x=>x.id==id);if(item==null)collection.Add(new AnimalCount(id,1));else item.count++;}
    public int CountAnimal(string id){var item=collection.Find(x=>x.id==id);return item==null?0:item.count;}
    public int TotalAnimals {get{int total=0;foreach(var item in collection)total+=item.count;return total;}}
    public void Upgrade(){
        if(collection==null)collection=new List<AnimalCount>();
        if(version==1){collection.Clear();if(foxCount>0)collection.Add(new AnimalCount("fox",foxCount));}
        if(egg==null)egg=new EggSaveData();if(eggDiscoveryLocks==null)eggDiscoveryLocks=new List<string>();
        egg.Recover();version=2;
    }
    public bool FinishEgg(string id){
        if(egg.state!=EggState.Hatching||egg.id!=id||rewarded.Contains("egg:"+id))return false;
        rewarded.Add("egg:"+id);AddAnimal("baby_fox");egg=new EggSaveData();return true;
    }
    public void Consume(string zone){if(!consumedZones.Contains(zone))consumedZones.Add(zone);}
    public bool ZoneConsumed(string zone)=>consumedZones.Contains(zone);
    public void ConfirmExit(string zone){consumedZones.Remove(zone);}
}
// Inputs are normalized to the measured resting magnitude: handles platform sensor units.
public sealed class ShakeGesture {
    double firstTime=-1,lastTime=-1,gx,gy,gz,baseline=1,calibrationMagnitude;
    double peak=-1,lastShake=-100,rx,ry,rz,stableSince=-1;int calibrationCount;
    bool below=true,needsRelease;
    public bool Calibrated {get;private set;}
    public double Threshold=1;
    public bool Sample(double x,double y,double z,double time){
        if(double.IsNaN(x+y+z+time)||double.IsInfinity(x+y+z+time))return false;
        if(firstTime<0)firstTime=time;
        double dt=lastTime<0?.02:Math.Max(0,Math.Min(.1,time-lastTime));lastTime=time;
        double magnitude=Math.Sqrt(x*x+y*y+z*z);
        if(!Calibrated){gx=x;gy=y;gz=z;calibrationMagnitude+=magnitude;calibrationCount++;if(time-firstTime>=1){baseline=Math.Max(.01,calibrationMagnitude/calibrationCount);Calibrated=true;}return false;}
        double motion=Math.Sqrt((x-gx)*(x-gx)+(y-gy)*(y-gy)+(z-gz)*(z-gz))/baseline;
        double rawChange=Math.Sqrt((x-rx)*(x-rx)+(y-ry)*(y-ry)+(z-rz)*(z-rz))/baseline;
        if(rawChange>.08)stableSince=time;else if(stableSince<0)stableSince=time;
        rx=x;ry=y;rz=z;
        // A stable new orientation is gravity, not a permanent shake. Do not adapt during impulses.
        if(motion<.5||time-stableSince>.25){double alpha=1-Math.Exp(-dt/.2);gx+=alpha*(x-gx);gy+=alpha*(y-gy);gz+=alpha*(z-gz);}
        if(motion<Threshold*.5){below=true;if(time-lastShake>=1.2)needsRelease=false;return false;}
        if(time-lastShake<1.2||needsRelease||motion<Threshold||!below)return false;
        below=false;
        if(peak>=0&&time-peak<=.6){peak=-1;lastShake=time;needsRelease=true;return true;}
        peak=time;return false;
    }
}
}
