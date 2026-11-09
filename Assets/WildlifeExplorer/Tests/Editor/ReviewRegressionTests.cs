using System.Reflection;
using NUnit.Framework;
using UnityEngine;
public class ReviewRegressionTests {
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    [Test]public void ControllerManagedPauseDoesNotStopGPSDuringCameraPermission(){
        var go=new GameObject("GPS permission regression");try{
            var gps=go.AddComponent<WildlifeExplorerGPS>();typeof(WildlifeExplorerGPS).GetField("running",Private).SetValue(gps,true);typeof(WildlifeExplorerGPS).GetField("polling",Private).SetValue(gps,true);
            var flag=typeof(WildlifeExplorerGPS).GetField("ExternalPauseManagement");Assert.That(flag,Is.Not.Null,"GPS must allow coordinated lifecycle ownership");flag.SetValue(gps,true);
            typeof(WildlifeExplorerGPS).GetMethod("OnApplicationPause",Private).Invoke(gps,new object[]{true});Assert.That(gps.Running,Is.True);
        }finally{Object.DestroyImmediate(go);}
    }
    [Test]public void TrackingRefusalOnLastBallRefundsInsteadOfEscape(){
        var go=new GameObject("tracking collision regression");try{
            var game=go.AddComponent<AnimalHuntGame>();game.localPreview=false;game.Round.Begin("test");game.Round.Launch();game.Round.Miss();game.Round.Launch();game.Round.Miss();game.Round.Launch();
            typeof(AnimalHuntGame).GetField("<State>k__BackingField",Private).SetValue(game,HuntState.BallInFlight);
            typeof(AnimalHuntGame).GetMethod("BallResolved",Private).Invoke(game,new object[]{false});Assert.That(game.Round.Attempts,Is.EqualTo(1));Assert.That(game.Round.Escaped,Is.False);Assert.That(game.State,Is.EqualTo(HuntState.ReadyToThrow));
        }finally{Object.DestroyImmediate(go);}
    }
    [Test]public void VisibleReticleMatchesTheScreenCentre(){
        var go=new GameObject("reticle regression");try{var game=go.AddComponent<AnimalHuntGame>();var view=go.AddComponent<AnimalHuntView>();view.game=game;view.ShowEncounter();Canvas.ForceUpdateCanvases();view.Refresh();
            var field=typeof(AnimalHuntView).GetField("reticle",Private);var rect=(RectTransform)field.GetValue(view);var point=RectTransformUtility.WorldToScreenPoint(null,rect.position);
            Assert.That(point.x,Is.EqualTo(Screen.width*.5f).Within(2));Assert.That(point.y,Is.EqualTo(Screen.height*.5f).Within(2));
        }finally{Object.DestroyImmediate(go);}
    }
    [Test]public void StandaloneDragChangesAimButDoesNotRotateARCamera(){
        var go=new GameObject("aim regression");var camera=new GameObject("test camera");try{
            var game=go.AddComponent<AnimalHuntGame>();game.spawner=go.AddComponent<WildlifeAnimalSpawner>();game.spawner.arCamera=camera.AddComponent<Camera>();
            var method=typeof(AnimalHuntGame).GetMethod("ApplyLocalAim");Assert.That(method,Is.Not.Null,"Standalone must offer touch aiming");game.localPreview=true;method.Invoke(game,new object[]{new Vector2(100,0)});Assert.That(Quaternion.Angle(camera.transform.rotation,Quaternion.identity),Is.GreaterThan(1));
            var aimed=camera.transform.rotation;game.localPreview=false;method.Invoke(game,new object[]{new Vector2(100,0)});Assert.That(camera.transform.rotation,Is.EqualTo(aimed));
        }finally{Object.DestroyImmediate(go);Object.DestroyImmediate(camera);}
    }
}
