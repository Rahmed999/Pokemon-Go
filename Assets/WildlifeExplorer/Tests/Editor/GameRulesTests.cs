using System;
#if UNITY_EDITOR
using NUnit.Framework;
#endif
namespace WildlifeExplorer.Tests {
public class GameRulesTests {
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
#if UNITY_EDITOR
[Test]
#endif
public void ThreeMissesEscapeExactlyOnce(){var r=new CatchRound();r.Begin("a");for(int i=0;i<3;i++){Check(r.Launch(),"launch");Check(!r.Launch(),"duplicate launch");r.Miss();}Check(r.Escaped&&r.Attempts==0,"third miss");Check(!r.Launch()&&!r.Capture(),"finished");}
#if UNITY_EDITOR
[Test]
#endif
public void CancellationRefundsAndCaptureResolvesOnce(){var r=new CatchRound();r.Begin("a");r.Launch();r.Cancel();Check(r.Attempts==3&&!r.InFlight,"refund");r.Launch();Check(r.Capture()&&!r.Capture(),"capture once");Check(!r.Miss(),"late miss");}
#if UNITY_EDITOR
[Test]
#endif
public void DuplicateRewardsAndConsumedZoneSurviveRestarts(){var s=new GameSave();Check(s.RecordCapture("a","fox"),"first reward");Check(!s.RecordCapture("a","fox"),"duplicate reward");Check(s.xp==50&&s.foxCount==1&&s.ZoneConsumed("zone" )==false,"reward values");s.Consume("zone");Check(s.ZoneConsumed("zone"),"consumed");s.ConfirmExit("different");Check(s.ZoneConsumed("zone"),"other zone");s.ConfirmExit("zone");Check(!s.ZoneConsumed("zone"),"exit clears");}
#if UNITY_EDITOR
[Test]
#endif
public void ShakeRejectsRestTiltAndSinglePeak(){var s=Calibrated();for(int i=51;i<100;i++)Check(!s.Sample(0,1,0,i*.02),"rest");for(int i=100;i<200;i++){double a=(i-100)*.005;Check(!s.Sample(Math.Sin(a),Math.Cos(a),0,i*.02),"slow tilt");}Check(!s.Sample(3,1,0,4.02),"one peak");}
#if UNITY_EDITOR
[Test]
#endif
public void ShakeNeedsTwoCrossingsAndFreshGestureAfterCooldown(){var s=Calibrated();Check(!s.Sample(3,1,0,1.1),"first peak");s.Sample(0,1,0,1.12);Check(s.Sample(-3,1,0,1.2),"second peak");for(int i=61;i<150;i++)Check(!s.Sample(3,1,0,i*.02),"held movement");}
#if UNITY_EDITOR
[Test]
#endif
public void ShakeRecoversAfterPhoneRotatesNinetyDegrees(){var s=Calibrated();for(int i=51;i<=150;i++)Check(!s.Sample(0,0,1,i*.02),"rotation must not throw");Check(!s.Sample(3,0,1,3.1),"first new peak");s.Sample(0,0,1,3.12);Check(s.Sample(-3,0,1,3.2),"shake after orientation change");}
static ShakeGesture Calibrated(){var s=new ShakeGesture();for(int i=0;i<=50;i++)s.Sample(0,1,0,i*.02);return s;}
}
#if !UNITY_EDITOR
public static class GameTestMain {public static int Main(){int n=0;foreach(var m in typeof(GameRulesTests).GetMethods(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.DeclaredOnly)){try{m.Invoke(new GameRulesTests(),null);Console.WriteLine("PASS "+m.Name);n++;}catch(Exception e){Console.WriteLine("FAIL "+m.Name+": "+e.InnerException?.Message);return 1;}}Console.WriteLine(n+" checks passed");return 0;}}
#endif
}
