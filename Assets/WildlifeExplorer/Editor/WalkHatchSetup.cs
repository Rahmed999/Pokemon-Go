using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class WalkHatchSetup {
    const string Folder="Assets/WildlifeExplorer/Resources/WalkHatch";
    [MenuItem("Walk & Hatch/1 Prepare egg assets (preserve scenes)")]
    public static void Prepare(){
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        if(!File.Exists(Folder+"/MysteryEgg.prefab")){
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=new Color(.98f,.94f,.74f);material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",new Color(.14f,.22f,.1f));AssetDatabase.CreateAsset(material,Folder+"/Egg Shell.mat");
            var egg=new GameObject("Mystery Egg");var shell=GameObject.CreatePrimitive(PrimitiveType.Sphere);shell.name="Glowing Shell";shell.transform.SetParent(egg.transform,false);shell.transform.localPosition=Vector3.up*.21f;shell.transform.localScale=new Vector3(.29f,.42f,.29f);shell.GetComponent<Renderer>().sharedMaterial=material;
            PrefabUtility.SaveAsPrefabAsset(egg,Folder+"/MysteryEgg.prefab");UnityEngine.Object.DestroyImmediate(egg);
        }
        if(!File.Exists(Folder+"/BabyFox.prefab")){
            var baby=PrefabUtility.LoadPrefabContents("Assets/WildlifeExplorer/Prefabs/Fox Animal.prefab");
            try{baby.name="Baby Fox (original placeholder)";baby.transform.localScale=Vector3.one*.4f;var target=baby.GetComponent<ExplorerAnimal>();if(target!=null)UnityEngine.Object.DestroyImmediate(target);PrefabUtility.SaveAsPrefabAsset(baby,Folder+"/BabyFox.prefab");}
            finally{PrefabUtility.UnloadPrefabContents(baby);}
        }
        PlayerSettings.Android.bundleVersionCode=2;PlayerSettings.bundleVersion="1.1.0";
        AssetDatabase.SaveAssets();Debug.Log("WALK_HATCH_ASSETS_OK: existing scenes/prefabs preserved; egg and baby resources ready.");
    }
    [MenuItem("Walk & Hatch/2 Run all automated checks")]public static void Checks(){ExplorerChecks.Run();}
    [MenuItem("Walk & Hatch/3 Build updated APKs")]
    public static void Build(){Prepare();AnimalHuntSetup.BuildTest();File.Copy("Builds/ShakeCatch_Test.apk","Builds/WalkHatch_Test.apk",true);AnimalHuntSetup.BuildGame();File.Copy("Builds/AnimalHunt_FullAR.apk","Builds/AnimalHunt_WalkHatch_AR.apk",true);Debug.Log("WALK_HATCH_BUILDS_OK");}
}
