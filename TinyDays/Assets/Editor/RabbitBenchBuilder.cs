using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TinyDays.Review;
public static class RabbitBenchBuilder {
    public const string Folder="Assets/Art/Generated/RabbitBench";
    public static void Attach(RabbitHomeLifeReview home){
        AssetDatabase.Refresh();
        var path=Folder+"/AdultRabbitBench.fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);
        if(importer==null)throw new Exception("Generate bench source before building scene");
        importer.isReadable=true;importer.animationType=ModelImporterAnimationType.Generic;importer.importAnimation=true;importer.animationCompression=ModelImporterAnimationCompression.Off;
        importer.clipAnimations=Array.Empty<ModelImporterClipAnimation>();importer.SaveAndReimport();
        var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        var task=home.gameObject.AddComponent<RabbitBenchReview>();home.benchRest=task;task.home=home;
        task.sitClip=clips.Single(c=>c.name.EndsWith("Adult_Bench_Sit"));task.breatheClip=clips.Single(c=>c.name.EndsWith("Adult_Bench_Breathe"));task.standClip=clips.Single(c=>c.name.EndsWith("Adult_Bench_Stand"));
        var shader=Shader.Find("Universal Render Pipeline/Lit");string matPath=Folder+"/BenchWood.mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!mat){mat=new Material(shader);AssetDatabase.CreateAsset(mat,matPath);}mat.color=new Color(.43f,.27f,.13f);mat.SetFloat("_Smoothness",.18f);
        var bi=(ModelImporter)AssetImporter.GetAtPath(Folder+"/WoodBench.fbx");bi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"BenchWood"),mat);bi.SaveAndReimport();
        var visual=new GameObject("House left wooden bench");visual.transform.SetParent(home.transform,false);
        UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/WoodBench.fbx"),visual.transform);
        visual.transform.localPosition=new Vector3(-2.4f,.275f,-3.5f);visual.transform.localRotation=Quaternion.Euler(0,180,0);task.bench=visual.transform;
    }
}
