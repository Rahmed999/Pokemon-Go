using System;
using WildlifeExplorer;
public sealed class EggManager {
    readonly GameSaveStore store;readonly ForegroundSteps steps=new ForegroundSteps();
    public event Action Changed;
    public EggSaveData Egg=>store.Data.egg;
    public EggManager(GameSaveStore save){store=save;}
    public bool Discover(string zone,int target=100){
        if(Egg.state!=EggState.None||target<1||store.Data.eggDiscoveryLocks.Contains(zone))return false;
        if(!store.TryCommit(s=>{s.egg=new EggSaveData{id=Guid.NewGuid().ToString("N"),discoveryZone=zone,target=target,state=EggState.Discovered};s.eggDiscoveryLocks.Add(zone);}))return false;
        Changed?.Invoke();return true;
    }
    public bool Collect(bool simulation){
        if(Egg.state!=EggState.Discovered)return false;
        if(!store.TryCommit(s=>{s.egg.state=EggState.Incubating;s.egg.steps=0;s.egg.lastRaw=-1;s.egg.simulated=simulation;}))return false;
        Changed?.Invoke();return true;
    }
    public void StartForeground(double now){steps.StartSession(now);}
    public void Reading(long raw,double time){if(Egg.simulated)return;if(steps.Apply(Egg,raw,time)>0){store.Save();Changed?.Invoke();}}
    public void Simulate(int count){if(!Egg.simulated||Egg.state!=EggState.Incubating||count<=0)return;Egg.steps=Math.Min(Egg.target,Egg.steps+Math.Min(count,Egg.target));if(Egg.steps==Egg.target)Egg.state=EggState.ReadyToHatch;store.Save();Changed?.Invoke();}
    public bool BeginHatch(){if(Egg.state!=EggState.ReadyToHatch)return false;if(!store.TryCommit(s=>s.egg.state=EggState.Hatching))return false;Changed?.Invoke();return true;}
    public bool CompleteHatch(string id,string insideZone=""){
        if(Egg.state!=EggState.Hatching||Egg.id!=id||store.Data.rewarded.Contains("egg:"+id))return false;
        if(!store.TryCommit(s=>{s.FinishEgg(id);if(!string.IsNullOrEmpty(insideZone)&&!s.eggDiscoveryLocks.Contains(insideZone))s.eggDiscoveryLocks.Add(insideZone);} ))return false;
        Changed?.Invoke();return true;
    }
    public void InterruptHatch(){if(Egg.state!=EggState.Hatching)return;Egg.Recover();store.Save();Changed?.Invoke();}
    public void ConfirmExit(string zone){if(store.Data.eggDiscoveryLocks.Remove(zone)){store.Save();Changed?.Invoke();}}
}
