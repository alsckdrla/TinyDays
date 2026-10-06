using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Life;
using TinyDays.Review;

public static class AutonomousLifeChecks
{
    static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    public static void Run()
    {
        try
        {
            EditorSceneManager.OpenScene(AutonomousLifeBuilder.ScenePath);
            var world=UnityEngine.Object.FindObjectOfType<AutonomousLifeWorld>();
            var rows=new List<string>{"v0.139 integration/failure checks. Accelerated tests, not real wall-clock or user approval."};
            Func<AutonomousSimulation> create=()=>new AutonomousSimulation(new LifeTuning(),world.definitions,world.starts,new AutonomousNavigation(world.land,world.obstacles));
            foreach(int fps in new[]{30,60,120})foreach(float rate in new[]{.5f,1,4})
            {
                var sim=create();float dt=rate/fps;int steps=Mathf.CeilToInt(1800/dt);
                for(int i=0;i<steps;i++){sim.Tick(dt,Mathf.Repeat(12+i*dt*24/600,24));if(i%fps==0)sim.Validate();}
                sim.Validate();Check(sim.consumed>=9&&sim.homeArrivals>=9&&sim.harvested>0&&sim.gathered>0,"Incomplete rate/fps loop");
                rows.Add($"{fps}fps {rate}x: food={sim.food} harvested={sim.harvested} gathered={sim.gathered} consumed={sim.consumed} home={sim.homeArrivals} recovered={sim.recovered}: PASS");
            }
            var blocked=create();blocked.navigation.TemporaryBlock=new Bounds(new Vector3(-2,0,-5),new Vector3(26,3,7));
            for(int i=0;i<2400;i++){blocked.Tick(.05f,12);if(i%20==0)blocked.Validate();}
            Check(blocked.produced==0,"Blocked production must not yield food remotely");
            blocked.navigation.TemporaryBlock=null;
            for(int i=0;i<12000;i++){blocked.Tick(.05f,12);if(i%20==0)blocked.Validate();}
            Check(blocked.produced>0&&blocked.food>=4,"Production failed to recover after opening path");rows.Add("Blocked production / release / food recovery: PASS");
            var carrying=create();
            for(int i=0;i<6000&&!carrying.residents.Any(r=>r.cargo>0&&r.Moving);i++)carrying.Tick(.05f,12);
            Check(carrying.residents.Any(r=>r.cargo>0),"Missing carrier for interruption test");
            carrying.navigation.TemporaryBlock=new Bounds(new Vector3(11.4f,0,1),new Vector3(3.8f,3,1.6f));
            for(int i=0;i<1200;i++){carrying.Tick(.05f,12);if(i%20==0)carrying.Validate();}
            Check(carrying.bundles.Count>0,"Blocked delivery must preserve dropped produce");
            carrying.navigation.TemporaryBlock=null;
            for(int i=0;i<12000;i++){carrying.Tick(.05f,12);if(i%20==0)carrying.Validate();}
            Check(carrying.bundles.Count==0&&carrying.food>0,"Dropped produce was not recovered");rows.Add("Interrupted delivery / bundle ownership / conservation / recovery: PASS");
            world.Initialize();var adapter=world.GetComponent<FarmLifeDirector>();var light=world.GetComponent<FarmLightingStudy>();
            adapter.Playback.SetRate(0);world.Advance(8);Check(Mathf.Abs(world.Simulation.elapsed-4)<.01f,"Half rate failed");
            light.Apply(3);float previous=world.Simulation.elapsed;world.Advance(2);Check(light.Hour==0&&world.Simulation.elapsed>previous,"Fixed light should not pause life");
            light.ResumeClock();adapter.paused=true;previous=world.Simulation.elapsed;world.Advance(2);Check(world.Simulation.elapsed==previous&&light.Hour==0,"Pause must freeze both clocks");
            rows.Add("0.5x / fixed-time life / common pause: PASS");
            // Preserve an actual serialized manual object across two rebuilds.
            var manual=GameObject.Find("ManualEdits");var probe=new GameObject("Life139ManualPreservationProbe");probe.transform.SetParent(manual.transform);probe.transform.localPosition=new Vector3(2,3,4);
            AutonomousLifeBuilder.Build();Check(GameObject.Find("Life139ManualPreservationProbe"),"Unsaved manual object lost");
            AutonomousLifeBuilder.Build();probe=GameObject.Find("Life139ManualPreservationProbe");Check(probe&&probe.transform.localPosition==new Vector3(2,3,4),"Manual object changed");
            UnityEngine.Object.DestroyImmediate(probe);EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            rows.Add("Two regenerations preserve ManualEdits: PASS");
            File.WriteAllLines("Docs/AutonomousLife139Integration.txt",rows);Debug.Log("LIFE139_INTEGRATION_OK\n"+string.Join("\n",rows));
        }
        catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
    }
}
