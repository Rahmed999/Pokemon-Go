using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
public class GameIntegrationTests {
    string directory;
    [SetUp]public void Setup(){directory=Path.Combine(Path.GetTempPath(),"animalhunt-tests-"+Guid.NewGuid());Directory.CreateDirectory(directory);}
    [TearDown]public void Cleanup(){Directory.Delete(directory,true);}
    [Test]public void SaveReloadPreservesCollectionSettingsAndZone(){
        string path=Path.Combine(directory,"save.json");var store=new GameSaveStore(path);store.Data.RecordCapture("id","fox");store.Data.Consume("zone");store.Data.sound=false;store.Data.sensitivity=2;Assert.That(store.Save(),Is.True);
        var loaded=new GameSaveStore(path);Assert.That(loaded.Data.xp,Is.EqualTo(50));Assert.That(loaded.Data.foxCount,Is.EqualTo(1));Assert.That(loaded.Data.ZoneConsumed("zone"),Is.True);Assert.That(loaded.Data.RecordCapture("id","fox"),Is.False);Assert.That(loaded.Data.sound,Is.False);Assert.That(loaded.Data.sensitivity,Is.EqualTo(2));
    }
    [Test]public void CorruptedSaveKeepsBackupAndStartsEmpty(){string path=Path.Combine(directory,"save.json");File.WriteAllText(path,"broken-json");var store=new GameSaveStore(path);Assert.That(store.Data.xp,Is.Zero);Assert.That(store.Warning,Is.Not.Empty);Assert.That(Directory.GetFiles(directory,"*.bak").Length,Is.EqualTo(1));}
    [Test]public void ExternalConsumedGateCannotBeBypassedByFreshGPS(){var go=new GameObject("test");try{var gps=go.AddComponent<WildlifeExplorerGPS>();gps.ExternalZoneBlocked=true;Assert.That(gps.CanPlaceAnimal,Is.False);}finally{UnityEngine.Object.DestroyImmediate(go);}}
    [Test]public void CollectionArtworkHasCanvasRenderer(){var go=new GameObject("fox artwork");try{go.AddComponent<FoxArtwork>();Assert.That(go.GetComponent<CanvasRenderer>(),Is.Not.Null);}finally{UnityEngine.Object.DestroyImmediate(go);}}
    [Test]public void BallPrefabUsesPhysicsAndCorrectScale(){var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WildlifeExplorer/Prefabs/Capture Ball.prefab");if(prefab==null)Assert.Ignore("Create game scenes before prefab validation");Assert.That(prefab.GetComponent<CaptureBall>(),Is.Not.Null);Assert.That(prefab.GetComponent<SphereCollider>(),Is.Not.Null);Assert.That(prefab.GetComponent<Rigidbody>().isKinematic,Is.False);Assert.That(prefab.GetComponent<Rigidbody>().collisionDetectionMode,Is.EqualTo(CollisionDetectionMode.ContinuousDynamic));Assert.That(prefab.transform.localScale.x,Is.EqualTo(.12f).Within(.001f));}
}

