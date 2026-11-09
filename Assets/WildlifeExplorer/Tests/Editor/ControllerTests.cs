using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace WildlifeExplorer.Tests
{
    public class ControllerTests
    {
        [Test]
        public void ControllerUsesSameClockSkewPolicyAsZoneRules()
        {
            var go=new GameObject("Test controller");
            try
            {
                var gps=go.AddComponent<WildlifeExplorerGPS>();
                var rules=new ZoneRules(0,0,50,70,3);
                double now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()/1000.0;
                rules.Process(new ZoneFix(0,0,5,now+1),now);
                Set(gps,"rules",rules);Set(gps,"running",true);
                Assert.That(gps.HasFreshPosition,Is.True);
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test]
        public void CaptureIsRejectedWithoutARTracking()
        {
            var manager=new GameObject("Test manager");var animal=new GameObject("Test animal");
            try
            {
                var gps=manager.AddComponent<WildlifeExplorerGPS>();
                var spawner=manager.AddComponent<WildlifeAnimalSpawner>();spawner.explorer=gps;
                var target=animal.AddComponent<ExplorerAnimal>();target.Initialize(spawner);
                typeof(WildlifeAnimalSpawner).GetProperty("CurrentAnimal").GetSetMethod(true).Invoke(spawner,new object[]{animal});
                Assert.That(spawner.ConfirmCapture(target),Is.False);
            }
            finally{UnityEngine.Object.DestroyImmediate(manager);UnityEngine.Object.DestroyImmediate(animal);}
        }
        static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
    }
}
