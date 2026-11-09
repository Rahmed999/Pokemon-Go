using System;
using System.IO;
using NUnit.Framework;
using WildlifeExplorer;
public class EggIntegrationTests {
    string directory,path;
    [SetUp]public void Setup(){directory=Path.Combine(Path.GetTempPath(),"egg-tests-"+Guid.NewGuid());Directory.CreateDirectory(directory);path=Path.Combine(directory,"save.json");}
    [TearDown]public void Cleanup(){Directory.Delete(directory,true);}
    [Test]public void LegacySaveMigratesWithoutLosingFoxesXPOrSettings(){
        File.WriteAllText(path,"{\"version\":1,\"xp\":150,\"foxCount\":3,\"sound\":false,\"vibration\":true,\"sensitivity\":2,\"rewarded\":[\"capture\"],\"consumedZones\":[\"zone\"]}");
        var save=new GameSaveStore(path);Assert.That(save.Data.version,Is.EqualTo(2));Assert.That(save.Data.CountAnimal("fox"),Is.EqualTo(3));Assert.That(save.Data.xp,Is.EqualTo(150));Assert.That(save.Data.sound,Is.False);Assert.That(save.Data.ZoneConsumed("zone"),Is.True);Assert.That(save.Data.egg.state,Is.EqualTo(EggState.None));
    }
    [Test]public void ProgressAndReadyStateSurviveReloadButCheckpointIsRebased(){
        var store=new GameSaveStore(path);var eggs=new EggManager(store);eggs.Discover("zone");eggs.Collect(false);eggs.StartForeground(0);eggs.Reading(100,1);eggs.Reading(125,2);
        var reopened=new GameSaveStore(path);Assert.That(reopened.Data.egg.steps,Is.EqualTo(25));var resume=new EggManager(reopened);resume.StartForeground(10);resume.Reading(900,11);Assert.That(reopened.Data.egg.steps,Is.EqualTo(25));resume.Reading(975,12);Assert.That(new GameSaveStore(path).Data.egg.state,Is.EqualTo(EggState.ReadyToHatch));
    }
    [Test]public void HatchCompletionIsOneTransactionAndCannotDuplicateAfterReload(){
        var store=new GameSaveStore(path);var eggs=new EggManager(store);eggs.Discover("zone");eggs.Collect(true);eggs.Simulate(100);string id=eggs.Egg.id;Assert.That(eggs.BeginHatch(),Is.True);Assert.That(eggs.CompleteHatch(id,"zone"),Is.True);
        var reload=new GameSaveStore(path);var manager=new EggManager(reload);Assert.That(reload.Data.CountAnimal("baby_fox"),Is.EqualTo(1));Assert.That(manager.CompleteHatch(id),Is.False);Assert.That(reload.Data.egg.state,Is.EqualTo(EggState.None));Assert.That(manager.Discover("zone"),Is.False);manager.ConfirmExit("zone");Assert.That(manager.Discover("zone"),Is.True);
    }
    [Test]public void OnlyOneEggAndSimulationNeverCreditsRealReadings(){var store=new GameSaveStore(path);var eggs=new EggManager(store);Assert.That(eggs.Discover("zone"),Is.True);Assert.That(eggs.Discover("other"),Is.False);eggs.Collect(true);eggs.StartForeground(0);eggs.Reading(100,1);eggs.Reading(200,2);Assert.That(eggs.Egg.steps,Is.Zero);eggs.Simulate(10);Assert.That(eggs.Egg.steps,Is.EqualTo(10));}
    [Test]public void InterruptedHatchReloadsAsReadyWithoutGivingAnimal(){var store=new GameSaveStore(path);var eggs=new EggManager(store);eggs.Discover("zone");eggs.Collect(true);eggs.Simulate(100);eggs.BeginHatch();var reload=new GameSaveStore(path);Assert.That(reload.Data.egg.state,Is.EqualTo(EggState.ReadyToHatch));Assert.That(reload.Data.CountAnimal("baby_fox"),Is.Zero);}
    [TestCase("{\"id\":\"\",\"count\":1}")]
    [TestCase("{\"id\":\"baby_fox\",\"count\":-1}")]
    [TestCase("null")]
    public void InvalidCollectionGetsBackedUpInsteadOfBreakingInventory(string entry){
        File.WriteAllText(path,"{\"version\":2,\"rewarded\":[],\"consumedZones\":[],\"eggDiscoveryLocks\":[],\"collection\":["+entry+"],\"egg\":{\"state\":0,\"target\":100}}");
        var store=new GameSaveStore(path);Assert.That(store.Warning,Does.Contain("could not be read"));Assert.That(store.Data.TotalAnimals,Is.Zero);Assert.That(Directory.GetFiles(directory,"*.bak").Length,Is.EqualTo(1));
    }
    [Test]public void ActiveEggWithoutIdentityGetsBackedUp(){
        File.WriteAllText(path,"{\"version\":2,\"rewarded\":[],\"consumedZones\":[],\"eggDiscoveryLocks\":[],\"collection\":[],\"egg\":{\"state\":2,\"target\":100,\"steps\":10}}");
        Assert.That(new GameSaveStore(path).Warning,Does.Contain("could not be read"));
    }
    [Test]public void FailedCommitDoesNotConsumeReadyEggOrGrantReward(){
        var store=new GameSaveStore(path);var eggs=new EggManager(store);eggs.Discover("zone");eggs.Collect(true);eggs.Simulate(100);eggs.BeginHatch();string id=eggs.Egg.id;
        File.Delete(path);Directory.CreateDirectory(path);UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning,new System.Text.RegularExpressions.Regex("Could not save progress"));Assert.That(eggs.CompleteHatch(id),Is.False);Assert.That(eggs.Egg.id,Is.EqualTo(id));Assert.That(store.Data.CountAnimal("baby_fox"),Is.Zero);
    }
}
