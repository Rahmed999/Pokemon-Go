using System;
#if UNITY_EDITOR
using NUnit.Framework;
#endif
namespace WildlifeExplorer.Tests
{
    public class ZoneRulesTests
    {
        static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        static ZoneFix Fix(double metres, double time, double accuracy = 5) => new ZoneFix(metres / 111194.92664455874, 0, accuracy, time);
        static ZoneRules Rules() => new ZoneRules(0,0,50,70,3);
#if UNITY_EDITOR
        [Test]
#endif
        public void DistanceInMetresAndAcrossDateline()
        {
            Check(ZoneRules.DistanceMetres(0,0,0,0)<.001,"same point");
            Check(Math.Abs(ZoneRules.DistanceMetres(0,0,1,0)-111194.9266)<.1,"one degree");
            Check(ZoneRules.DistanceMetres(0,179.999,0,-179.999)<225,"dateline");
        }
#if UNITY_EDITOR
        [Test]
#endif
        public void ThreeDistinctFixesEnterOnce()
        {
            var r=Rules(); r.Process(Fix(10,100),100);
            for(int i=0;i<10;i++)r.Process(Fix(10,100),100);
            Check(!r.Inside && r.Confirmations==1,"duplicate sample");
            r.Process(Fix(10,101),101);
            Check(r.Process(Fix(10,102),102)==ZoneTransition.Entered,"third fix");
            Check(r.Process(Fix(10,103),103)==ZoneTransition.None,"repeated enter");
        }
#if UNITY_EDITOR
        [Test]
#endif
        public void InaccurateFixBreaksConfirmation()
        {
            var r=Rules();r.Process(Fix(10,100),100);r.Process(Fix(10,101),101);
            r.Process(Fix(10,102,100),102);Check(r.Confirmations==0,"bad accuracy");
            r.Process(Fix(10,103),103);Check(!r.Inside,"bridged bad sample");
        }
#if UNITY_EDITOR
        [Test]
#endif
        public void StaleAndLongGapCannotCompleteEntry()
        {
            var r=Rules();r.Process(Fix(10,100),100);r.Process(Fix(10,101),101);
            r.Process(Fix(10,102),120);Check(!r.Inside && r.Confirmations==0,"stale entry");
            r.Process(Fix(10,121),121);r.Process(Fix(10,140),140);
            Check(r.Confirmations==1,"long gap");
        }
#if UNITY_EDITOR
        [Test]
#endif
        public void FutureTimestampCannotPoisonLaterValidFix()
        {
            var r=Rules();r.Process(Fix(10,10000),100);
            r.Process(Fix(10,101),101);r.Process(Fix(10,102),102);
            r.Process(Fix(10,103),103);Check(r.Inside,"future sample locked timestamp");
        }
#if UNITY_EDITOR
        [Test]
#endif
        public void HysteresisRequiresThreeOutsideReadings()
        {
            var r=Rules();for(int i=0;i<3;i++)r.Process(Fix(49,100+i),100+i);
            for(int i=0;i<4;i++)r.Process(Fix(60,103+i),103+i);
            Check(r.Inside,"hysteresis band");
            r.Process(Fix(80,107),107);r.Process(Fix(80,108),108);
            Check(r.Process(Fix(80,109),109)==ZoneTransition.Exited,"exit");
            for(int i=0;i<3;i++)r.Process(Fix(60,110+i),110+i);
            Check(!r.Inside,"entered in band");
        }
#if UNITY_EDITOR
        [Test]
#endif
        public void InvalidValuesAndZeroAccuracyRejected()
        {
            var r=Rules();r.Process(new ZoneFix(double.NaN,0,5,100),100);
            r.Process(new ZoneFix(91,0,5,101),101);
            r.Process(new ZoneFix(0,181,5,102),102);
            r.Process(Fix(0,103,0),103);
            r.Process(Fix(0,104,-1),104);
            Check(!r.Inside && r.Confirmations==0,"invalid coordinates or accuracy");
        }
#if UNITY_EDITOR
        [Test]
#endif
        public void BadFixRetainsInsideButBlocksNewPlacement()
        {
            var r=Rules();for(int i=0;i<3;i++)r.Process(Fix(10,100+i),100+i);
            Check(r.HasFreshFix(103),"valid fresh fix");
            r.Process(Fix(500,103,100),103);
            Check(r.Inside && !r.HasFreshFix(103),"poor fix removed inside or stayed usable");
        }
#if UNITY_EDITOR
        [Test]
#endif
        public void FreshnessExpiresWithoutNewFix()
        {
            var r=Rules();r.Process(Fix(10,100),100);
            Check(!r.HasFreshFix(116),"stale usable fix");
        }
#if UNITY_EDITOR
        [Test]
#endif
        public void CompletedEncounterRequiresExitAndCannotDoubleAward()
        {
            var e=new EncounterState();Check(e.CanPlace(true,true),"initial encounter");
            e.Placed();Check(!e.CanPlace(true,true),"second animal");
            Check(e.Complete(true),"first completion");Check(!e.Complete(true),"double completion");
            Check(!e.CanPlace(true,true),"repeat reward inside");
            e.Exited();Check(e.CanPlace(true,true),"reentry after exit");
        }
#if UNITY_EDITOR
        [Test]
#endif
        public void CompletionAfterLeavingAllowsNextEntry()
        {
            var e=new EncounterState();e.Placed();e.Exited();e.Complete(false);
            Check(e.CanPlace(true,true),"completion outside needs unnecessary second exit");
        }
#if !UNITY_EDITOR
        public static int Main()
        {
            int count=0,failed=0;var tests=new ZoneRulesTests();
            foreach(var method in typeof(ZoneRulesTests).GetMethods(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.DeclaredOnly))
            {try{method.Invoke(tests,null);Console.WriteLine("PASS "+method.Name);count++;}catch(Exception e){Console.WriteLine("FAIL "+method.Name+": "+(e.InnerException??e).Message);failed++;}}
            Console.WriteLine($"{count} passed, {failed} failed");return failed==0?0:1;
        }
#endif
    }
}
