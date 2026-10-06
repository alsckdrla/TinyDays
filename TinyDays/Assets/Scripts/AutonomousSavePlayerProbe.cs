using System;
using System.Collections;
using System.IO;
using UnityEngine;
using TinyDays.Review;

namespace TinyDays.Life
{
    // Opt-in player integration check. Always requires an explicitly isolated save directory.
    sealed class AutonomousSavePlayerProbe : MonoBehaviour
    {
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int arg=Array.IndexOf(args,"-verifySave142");
            if(Array.IndexOf(args,"-lifeSaveDirectory")<0||arg<0||arg+1>=args.Length){Debug.LogError("LIFE142_PROBE_REQUIRES_ISOLATED_DIRECTORY");Application.Quit(1);yield break;}
            var world=GetComponent<AutonomousLifeWorld>();var save=GetComponent<AutonomousLifeSave>();
            string expected=Path.Combine(Path.GetDirectoryName(save.SavePath),"ExpectedPlayerState.json");
            if(args[arg+1]=="create"){
                world.GetComponent<FarmLifeDirector>().paused=false;world.Advance(2);
                var playback=world.GetComponent<FarmLifeDirector>();playback.Playback.SetDayMinutes("20");playback.Playback.SetRate(3);
                world.GetComponent<FarmLightingStudy>().RestoreClock(5,18.25f,false);world.Advance(.01f);playback.paused=true;
                var camera=world.GetComponent<FarmStudyReview>();camera.FocusResident(2);camera.KeyboardMove(new Vector2(.3f,-.2f),.4f);
                yield return null;
                if(!save.SaveNow()){Application.Quit(1);yield break;}
                world.Simulation.residents[0].fatigue+=.01f;
                string changed=JsonUtility.ToJson(world.CaptureSnapshot());
                yield return new WaitForSecondsRealtime(61);
                try{
                    var written=AutonomousSaveCodec.Decode(File.ReadAllText(save.SavePath));
                    if(JsonUtility.ToJson(written)!=changed)throw new InvalidOperationException("Player autosave mismatch");
                    Debug.Log("LIFE142_PLAYER_AUTOSAVE_PAUSED_OK");
                    // This final change must be written by the real application's quit callback.
                    world.Simulation.residents[0].fatigue+=.01f;
                    File.WriteAllText(expected,AutonomousSaveCodec.Encode(world.CaptureSnapshot()));
                    Debug.Log("LIFE142_PLAYER_QUIT_EXPECTED_READY");Application.Quit(0);
                }catch(Exception error){Debug.LogException(error);Application.Quit(1);}
            }else{
                yield return new WaitForSecondsRealtime(1);
                try{
                    var prior=AutonomousSaveCodec.Decode(File.ReadAllText(expected));
                    if(JsonUtility.ToJson(prior)!=JsonUtility.ToJson(world.CaptureSnapshot()))throw new InvalidOperationException("Player restart did not restore exact saved state");
                    Debug.Log("LIFE142_PLAYER_RESTART_EXACT_OK");Application.Quit(0);
                }catch(Exception error){Debug.LogException(error);Application.Quit(1);}
            }
        }
    }
}
