using System;
#if UNITY_EDITOR
using NUnit.Framework;
#endif
namespace WildlifeExplorer.Tests {
public class EggRulesTests {
static void Check(bool condition,string text){if(!condition)throw new Exception(text);}
#if UNITY_EDITOR
[Test]
#endif
public void NewEggNeedsFreshBaselineAndOnlyCountsNewSteps(){var egg=Egg();var steps=new ForegroundSteps();steps.StartSession(10);Check(steps.Apply(egg,1000,9)==0,"stale");Check(steps.Apply(egg,1000,10)==0,"baseline");Check(steps.Apply(egg,1000,11)==0,"repeat");Check(steps.Apply(egg,1025,12)==25&&egg.steps==25,"new steps");Check(steps.Apply(egg,1030,11)==0,"out of order");}
#if UNITY_EDITOR
[Test]
#endif
public void ResumeExcludesClosedAppStepsAndRebootNeverErasesProgress(){var e=Egg();var s=new ForegroundSteps();s.StartSession(0);s.Apply(e,500,1);s.Apply(e,525,2);s.StartSession(10);s.Apply(e,900,11);Check(e.steps==25,"background excluded");s.Apply(e,905,12);Check(e.steps==30,"foreground resumed");s.Apply(e,2,13);Check(e.steps==30,"reset kept progress");s.Apply(e,7,14);Check(e.steps==35,"after reset");}
#if UNITY_EDITOR
[Test]
#endif
public void ProgressCapsAtTargetAndReadyIsStable(){var e=Egg();var s=new ForegroundSteps();s.StartSession(0);s.Apply(e,100,1);s.Apply(e,500,2);Check(e.steps==100&&e.state==EggState.ReadyToHatch&&e.Percent==1,"cap");Check(s.Apply(e,600,3)==0,"ready ignores more");}
#if UNITY_EDITOR
[Test]
#endif
public void InvalidCounterCannotInventSteps(){var e=Egg();var s=new ForegroundSteps();s.StartSession(0);Check(s.Apply(e,-1,1)==0,"negative");s.Apply(e,100,2);s.Apply(e,101,3);Check(e.steps==1,"valid count");}
#if UNITY_EDITOR
[Test]
#endif
public void HatchRewardIsIdempotentAndSeparateFromCaptureXP(){var save=new GameSave();save.egg=Egg();save.egg.steps=100;save.egg.state=EggState.Hatching;Check(save.FinishEgg("egg-1"),"complete");Check(save.CountAnimal("baby_fox")==1&&save.xp==0,"baby reward");Check(!save.FinishEgg("egg-1"),"duplicate");Check(save.egg.state==EggState.None,"slot freed");}
#if UNITY_EDITOR
[Test]
#endif
public void InterruptedHatchingReturnsToReady(){var e=Egg();e.steps=100;e.state=EggState.Hatching;e.Recover();Check(e.state==EggState.ReadyToHatch&&e.steps==100,"recover");}
static EggSaveData Egg()=>new EggSaveData{id="egg-1",discoveryZone="zone",target=100,state=EggState.Incubating};
}
#if !UNITY_EDITOR
public static class EggTestMain {public static int Main(){int n=0;foreach(var m in typeof(EggRulesTests).GetMethods(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.DeclaredOnly)){try{m.Invoke(new EggRulesTests(),null);Console.WriteLine("PASS "+m.Name);n++;}catch(Exception e){Console.WriteLine("FAIL "+m.Name+": "+(e.InnerException??e).Message);return 1;}}Console.WriteLine(n+" egg checks passed");return 0;}}
#endif
}
