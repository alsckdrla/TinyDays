using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Life;
using TinyDays.Review;

public static class AutonomousLife144SupplementaryChecks
{
    static void Need(bool value,string message){if(!value)throw new Exception(message);}
    public static void Execute()
    {
        try{
            EditorSceneManager.OpenScene(AutonomousLifeBuilder.ScenePath);
            var world=UnityEngine.Object.FindObjectOfType<AutonomousLifeWorld>();world.Initialize();world.GetComponent<FarmStudyReview>().ResetCameraToPreset();
            var light=world.GetComponent<FarmLightingStudy>();light.Apply(3);
            // In-memory edits only: no LoadPreferences/Save calls and no player settings access.
            light.Colors.Set(3,1,new Color(.4f,.6f,.8f));light.RefreshEnvironment();
            var windows=world.GetComponentsInChildren<MeshRenderer>().Where(r=>r.sharedMaterial.name.Contains("Glass Lighting study")).ToArray();
            Need(windows.Length>0,"No window materials");
            var emissions=windows.Select(r=>r.sharedMaterial.GetColor("_EmissionColor")).ToArray();
            float clear=light.sun.intensity;var sky=RenderSettings.ambientSkyColor;
            world.SelectWeather(2);Need(light.sun.intensity==clear,"Manual transition snapped");
            world.Advance(2.5f);Need(light.sun.intensity<clear&&light.sun.intensity>clear*.57f,"Missing smooth weather fade");
            world.Advance(3);Need(light.sun.color==light.Colors.Get(3,1),"Sun color changed");
            for(int i=0;i<windows.Length;i++)Need(windows[i].sharedMaterial.GetColor("_EmissionColor")==emissions[i],"Window glow changed");
            world.SelectWeather(0);world.Advance(6);Need(Mathf.Abs(light.sun.intensity-clear)<.0001f&&RenderSettings.ambientSkyColor==sky,"Clear light did not restore");
            var good=world.CaptureSnapshot();var bad=AutonomousSaveCodec.Decode(AutonomousSaveCodec.Encode(good));bad.weather.elapsed=-1;
            bool rejected=false;try{world.RestoreSnapshot(bad);}catch{rejected=true;}
            Need(rejected&&JsonUtility.ToJson(good)==JsonUtility.ToJson(world.CaptureSnapshot()),"Invalid weather mutated active world");
            // This is the isolated v143 test farm, never the user's persistent data folder.
            string fixture="Logs/Save143UI/AutonomousLife142.json";
            if(File.Exists(fixture)){
                string original=File.ReadAllText(fixture);var old=AutonomousSaveCodec.Decode(original);Need(old.sourceVersion==142,"Unexpected legacy fixture");
                world.RestoreSnapshot(old);var migrated=world.CaptureSnapshot();
                var oldSites=migrated.simulation.sites.Take(old.simulation.sites.Length).ToArray();
                migrated.simulation.sites=oldSites;
                Need(JsonUtility.ToJson(old.simulation)==JsonUtility.ToJson(migrated.simulation),"Real legacy simulation changed");
                Need(JsonUtility.ToJson(old.camera)==JsonUtility.ToJson(migrated.camera)&&old.hour==migrated.hour&&old.day==migrated.day&&old.paused==migrated.paused,"Real legacy view/time changed");
                Need(File.ReadAllText(fixture)==original,"Legacy fixture modified");
            }
            File.WriteAllText("Docs/AutonomousLife144SupplementaryVerification.txt","Manual weather fade and clear lighting restore: PASS\nCustom sunlight and window emission preserved: PASS\nInvalid weather rejected without active state mutation: PASS\nIsolated v143 save exact simulation/view/time migration: "+(File.Exists(fixture)?"PASS":"SKIPPED (fixture absent)")+"\n");
            Debug.Log("LIFE144_SUPPLEMENTARY_OK");
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);throw;}
    }
}
