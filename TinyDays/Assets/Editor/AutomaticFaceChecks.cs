using System;
using System.IO;
using TinyDays.Review;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
using UnityEngine;

public static class AutomaticFaceChecks
{
    public static void Execute(){
        try{
            foreach(int fps in new[]{30,60,120})foreach(double speed in new[]{.5,1.0}){
                var face=new AdultFaceMotion(null,1701);double previous=face.Next;int last=-1;
                for(int i=0;i<fps*120;i++){
                    face.Advance(speed/fps,false);
                    if(face.Next!=previous){double interval=face.Next-previous;int choice=Array.FindIndex(AdultFaceMotion.Intervals,x=>Math.Abs(x-interval)<1e-8);
                        if(choice<0||choice==last)throw new Exception("Blink interval/repetition failure");last=choice;previous=face.Next;}
                    if(face.Blink<0||face.Blink>1)throw new Exception("Blink range");
                }
                double clock=face.Clock;face.Advance(0,false);if(face.Clock!=clock)throw new Exception("Pause failure");
                face.Advance(1,true);if(face.Blink>.001)throw new Exception("Sleep suppression failure");
                face.Advance(0,false);if(face.Next-face.Clock<3.19)throw new Exception("Wake interval failure");
            }
            if(AdultFaceMotion.Curve(0)!=0||AdultFaceMotion.Curve(.24)!=0||AdultFaceMotion.Curve(.09)!=1)throw new Exception("Blink curve endpoints");
            EditorSceneManager.OpenScene("Assets/Scenes/AdultRabbitMotionStudy.unity");
            var motion=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();motion.Select(1);motion.paused=false;
            var skin=motion.resident.GetComponentsInChildren<SkinnedMeshRenderer>().First(s=>s.sharedMesh.GetBlendShapeIndex("SleepEyesClosed")>=0);
            int shape=skin.sharedMesh.GetBlendShapeIndex("SleepEyesClosed");bool seen=false;
            for(int i=0;i<240*9;i++){motion.Advance(1f/240);float blink=motion.AutomaticBlinkWeight;seen|=blink>.9f;if(Math.Abs(skin.GetBlendShapeWeight(shape)-100*blink)>.01)throw new Exception("Sample overwrote blink");}
            if(!seen)throw new Exception("Awake blink absent");
            double saved=motion.AutomaticBlinkClock;motion.Select(24);motion.paused=false;if(motion.AutomaticBlinkClock!=saved)throw new Exception("Clip change reset blink clock");
            for(int i=0;i<240;i++){motion.Advance(1f/240);if(skin.GetBlendShapeWeight(shape)<99.9)throw new Exception("Sleep eyes opened");}
            EditorSceneManager.OpenScene("Assets/Scenes/RabbitHomeLifeStudy.unity");
            var home=UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();home.ResetStudy();home.Advance(.5f);saved=home.AutomaticBlinkClock;
            home.SelectWaterMode(true);if(home.AutomaticBlinkClock!=saved)throw new Exception("Water mode reset face clock");
            home.SelectBenchMode();if(home.AutomaticBlinkClock!=saved)throw new Exception("Bench mode reset face clock");
            File.WriteAllText("Docs/AutomaticFaceVerification.txt","PASS: four intervals/no consecutive repeats; 30/60/120fps x 0.5/1; pause; sleep suppression and fresh wake wait; curve endpoints; actual imported shape application during walking and sleep; clip/life-mode clock preservation. Body/FBX untouched. OS input and visual quality not verified.\n");
            RabbitHomeGroundChecks.Execute();RabbitHomeLifeBuilder.Stress();RabbitWaterChecks.Verify();RabbitBenchChecks.Verify();
            AdultRabbitBuilder.BuildPlayer();AdultRabbitMotionBuilder.BuildPlayer();RabbitHomeLifeBuilder.BuildPlayer();
            Debug.Log("AUTOMATIC_FACE_OK");
        }catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
    }
}
