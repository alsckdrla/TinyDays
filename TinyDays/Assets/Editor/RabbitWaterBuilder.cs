using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Review;
public static class RabbitWaterBuilder {
    const string Folder="Assets/Art/Generated/RabbitWater";
    static Material Mat(string name,Color color){
        string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Smoothness",.22f);EditorUtility.SetDirty(m);return m;
    }
    public static void Attach(RabbitHomeLifeReview home){
        string fbx=Folder+"/AdultRabbitWater.fbx";
        if(!File.Exists(fbx))return;
        var importer=(ModelImporter)AssetImporter.GetAtPath(fbx);importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation=true;importer.optimizeGameObjects=false;importer.isReadable=true;importer.animationCompression=ModelImporterAnimationCompression.Off;
        importer.clipAnimations=Array.Empty<ModelImporterClipAnimation>();importer.SaveAndReimport();
        var clips=AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        var task=home.gameObject.AddComponent<RabbitWaterReview>();home.watering=task;task.home=home;
        task.carryIdle=clips.Single(c=>c.name.EndsWith("Adult_Water_Carry_Idle"));task.carryWalk=clips.Single(c=>c.name.EndsWith("Adult_Water_Carry_Walk"));task.pour=clips.Single(c=>c.name.EndsWith("Adult_Water_Pour"));
        task.pickup=clips.SingleOrDefault(c=>c.name.EndsWith("Adult_Water_Pickup"));task.putdown=clips.SingleOrDefault(c=>c.name.EndsWith("Adult_Water_Putdown"));
        task.standingRest=clips.Single(c=>c.name.EndsWith("Adult_Breathe_Stand"));
        var blue=Mat("WaterBlue",new Color(.13f,.40f,.65f));var dark=Mat("WaterDark",new Color(.045f,.15f,.26f));
        var ci=(ModelImporter)AssetImporter.GetAtPath(Folder+"/WateringCan.fbx");ci.isReadable=true;
        ci.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"WaterBlue"),blue);ci.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"WaterDark"),dark);ci.SaveAndReimport();
        var can=new GameObject("Watering can task root");can.transform.SetParent(home.transform,false);task.can=can.transform;
        var visual=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/WateringCan.fbx"),can.transform);visual.name="Blue watering can";
        task.grips=new Transform[2];for(int i=0;i<2;i++){var grip=new GameObject("Grip_"+(i==0?"L":"R")).transform;grip.SetParent(can.transform,false);grip.localPosition=new Vector3(i==0?.12f:-.12f,.305f,-.08f);task.grips[i]=grip;}
        task.spout=new GameObject("Spout").transform;task.spout.SetParent(can.transform,false);task.spout.localPosition=new Vector3(0,.16f,.455f);
        var drops=new GameObject("Water drops");drops.transform.SetParent(home.transform,false);task.water=drops.AddComponent<ParticleSystem>();
        var main=task.water.main;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=.65f;main.startSize=.018f;main.maxParticles=96;main.startColor=new Color(.55f,.83f,1,1);main.gravityModifier=1;
        var emission=task.water.emission;emission.enabled=false;var shape=task.water.shape;shape.enabled=false;
        drops.GetComponent<ParticleSystemRenderer>().sharedMaterial=Mat("WaterDrops",new Color(.55f,.83f,1));task.water.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        string dropPath=Folder+"/WaterDrop.asset";var dropMesh=AssetDatabase.LoadAssetAtPath<Mesh>(dropPath);
        if(!dropMesh){dropMesh=new Mesh();AssetDatabase.CreateAsset(dropMesh,dropPath);}
        dropMesh.Clear();dropMesh.vertices=new[]{Vector3.up*.65f,Vector3.down*.65f,Vector3.left*.4f,Vector3.forward*.4f,Vector3.right*.4f,Vector3.back*.4f};
        dropMesh.triangles=new[]{0,3,2,0,4,3,0,5,4,0,2,5,1,2,3,1,3,4,1,4,5,1,5,2};dropMesh.RecalculateNormals();dropMesh.RecalculateBounds();EditorUtility.SetDirty(dropMesh);
        var dropRenderer=drops.GetComponent<ParticleSystemRenderer>();dropRenderer.renderMode=ParticleSystemRenderMode.Mesh;dropRenderer.mesh=dropMesh;
        var mats=new Dictionary<string,Material>();foreach(string n in new[]{"Soil","Stone","Pink","FlowerGold","Leaf","LeafLight"})mats[n]=AssetDatabase.LoadAssetAtPath<Material>(HouseVillageAssets.Folder+"/"+n+".mat");
        var bed=new GameObject("Watering flower bed");bed.transform.SetParent(home.transform,false);bed.transform.localPosition=new Vector3(2.15f,.275f,-3.7f);bed.transform.localRotation=Quaternion.Euler(0,90,0);
        foreach(var pair in mats){
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(HouseVillageAssets.Folder+"/PinkBed-"+pair.Key+".asset");if(!mesh)continue;
            var part=new GameObject(pair.Key);part.transform.SetParent(bed.transform,false);part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterial=pair.Value;
        }
        can.SetActive(false);
        if(task.pickup&&task.putdown){
            var flow=home.gameObject.AddComponent<RabbitWaterLifeFlow>();flow.home=home;home.lifeFlow=flow;
            Func<string,Vector3,Transform> anchor=(name,position)=>{var t=new GameObject(name).transform;t.SetParent(home.transform,false);t.position=position;return t;};
            float lowest=visual.GetComponentsInChildren<MeshFilter>().SelectMany(f=>f.sharedMesh.vertices.Select(v=>can.transform.InverseTransformPoint(f.transform.TransformPoint(v)).y)).Min();
            float x=Mathf.Max(.70f,home.insideWait.x+home.BodyClearanceRadius+.32f);
            flow.storage=anchor("Can floor storage",new Vector3(x,home.groundHeight-lowest+.0025f,-3.7f));
            flow.approach=anchor("Can pickup approach",new Vector3(x,home.groundHeight,-4.06f));
            flow.work=anchor("Flower work position",task.destination);flow.work.rotation=Quaternion.Euler(0,90,0);
        }
    }
    public static void Build(){RabbitHomeLifeBuilder.Build();Debug.Log("WATER_SCENE_OK");}
}
