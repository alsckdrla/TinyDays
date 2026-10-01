using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TinyDays.Review {
// Explicit opt-in player diagnostics. Does not run in ordinary review sessions.
public sealed class AdultReviewPanelChecks : MonoBehaviour {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install(){if(Environment.GetCommandLineArgs().Contains("-review-panel-checks"))new GameObject("Panel diagnostic runner").AddComponent<AdultReviewPanelChecks>();}
    readonly List<string> report=new List<string>();
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    IEnumerator Start(){
        yield return null;
        var r=FindObjectOfType<AdultRabbitMotionReview>();
        string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../Docs/Captures/ReviewPanelV0115"));Directory.CreateDirectory(directory);
        var sizes=new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720),new Vector2Int(800,600)};
        foreach(var size in sizes){
            Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.7f);
            r.AutomaticIdle=false;r.StartBreathing(false);r.paused=true;r.PanelScrollY=0;yield return new WaitForEndOfFrame();
            try{
                Check(Screen.width==size.x&&Screen.height==size.y,"Requested resolution unavailable");
                Rect panel=AdultRabbitMotionReview.PanelBounds(Screen.width,Screen.height);
                Check(panel.width==300&&panel.x==12&&panel.y==12&&panel.yMax<=Screen.height,"Panel bounds");
                var items=r.PanelItems.ToArray();float contentWidth=panel.width-34;
                Check(items.Any(i=>i.Text=="서기 → 눕기")&&items.Any(i=>i.Text=="누움 → 서기"),"Lying transition buttons missing");
                Check(items.Any(i=>i.Text=="잠들기")&&items.Any(i=>i.Text=="깨어나기")&&!items.Any(i=>i.Text=="누운 호흡")&&items.Any(i=>i.Text.Contains("기본 호흡 자동 적용")),"Automatic sleep breathing UI");
                Check(items.Any(i=>i.Text=="서서 짝발 대기"),"Standing fidget button missing");
                items.First(i=>i.Text=="서서 짝발 대기").Click();Check(r.Idle.CurrentId=="서서 짝발 대기","Standing fidget callback");r.StartBreathing(false);r.paused=true;
                Check(items.All(i=>i.Bounds.xMin>=0&&i.Bounds.xMax<=contentWidth+.1f),"Horizontal overflow");
                Check(items.Where(i=>i.Button).All(i=>i.Bounds.height==24),"Button height");
                Check(items.Where(i=>i.Button).GroupBy(i=>i.Bounds.y).All(g=>g.Min(i=>i.Bounds.x)==0),"Rows not left aligned");
                Check(AdultRabbitMotionReview.IsPanelPoint(panel.center,Screen.width,Screen.height)&&!AdultRabbitMotionReview.IsPanelPoint(new Vector2(panel.xMax+1,50),Screen.width,Screen.height),"Input panel boundary");
                Vector2 outside=new Vector2(panel.xMax+80,150);var cameraStart=r.reviewCamera.transform.position;
                r.PointerInput(panel.center,Vector2.zero,0,0,1);Check(Vector3.Distance(cameraStart,r.reviewCamera.transform.position)<.0001,"UI wheel zoom leaked");
                for(int button=0;button<3;button++){
                    r.PointerInput(panel.center,Vector2.zero,1<<button,1<<button,0);r.PointerInput(outside,new Vector2(40,20),0,1<<button,0);
                    Check(!r.CameraDragging&&Vector3.Distance(cameraStart,r.reviewCamera.transform.position)<.0001,"UI-started drag leaked");
                    r.PointerInput(outside,Vector2.zero,1<<button,1<<button,0);r.PointerInput(outside,new Vector2(40,20),0,1<<button,0);
                    Check(Vector3.Distance(cameraStart,r.reviewCamera.transform.position)>.001,"Outside drag inactive");
                    r.PointerInput(outside,Vector2.zero,0,0,0);Check(!r.CameraDragging,"Release failed");r.Home();cameraStart=r.reviewCamera.transform.position;
                }
                r.PointerInput(outside,Vector2.zero,0,0,1);Check(Vector3.Distance(cameraStart,r.reviewCamera.transform.position)>.001,"Outside zoom inactive");r.Home();
                report.Add($"{Screen.width}x{Screen.height}: {items.Count(i=>i.Button)} buttons, content {r.PanelContentHeight:F1}px; bounds, wrap, row alignment PASS.");
            }catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,$"Panel{size.x}x{size.y}Top.png"));yield return new WaitForEndOfFrame();
            r.PanelScrollY=100000;yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();
            try{
                float viewport=Screen.height-40,max=Mathf.Max(0,r.PanelContentHeight-viewport);
                Check(Mathf.Abs(r.PanelScrollY-max)<.1f,"Bottom scroll clamp");
                Check(r.PanelItems.Last().Bounds.yMax-r.PanelScrollY<=viewport+.1f,"Last label inaccessible");
                var before=r.reviewCamera.transform.position;r.PanelItems.First(i=>i.Text=="앉기 기본 호흡").Click();
                Check(r.Idle!=null&&r.Idle.Seated&&Vector3.Distance(before,r.reviewCamera.transform.position)<.0001,"Panel action/camera preservation");
                r.paused=true;report.Add($"Bottom {r.PanelScrollY:F1}px reachable; direct button callback preserved camera (not OS input).");
            }catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            yield return new WaitForEndOfFrame();r.PanelScrollY=100000;yield return new WaitForEndOfFrame();
            try{r.PanelItems.First(i=>i.Text=="앉아서 발목 까딱").Click();Check(r.Idle.CurrentId=="앉아서 발목 까딱","Seated fidget callback");r.StartBreathing(true);r.PanelItems.First(i=>i.Text=="앉아서 낙서하기").Click();Check(r.Idle.CurrentId=="앉아서 낙서하기","Sandplay callback");r.StartBreathing(true);r.paused=true;report.Add("Fidget and sandplay callbacks PASS (not physical input).");}catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,$"Panel{size.x}x{size.y}Bottom.png"));yield return new WaitForEndOfFrame();
        }
        report.Add("Runtime GUI render/layout, shared pointer routing for three drags + wheel, and direct callbacks PASS. Physical OS wheel/drag/click interaction not verified.");
        r.StartBreathing(false);r.RequestLie();for(int i=0;i<240;i++)r.Advance(1f/60);r.paused=true;yield return new WaitForEndOfFrame();
        try{Check(r.Posture==AdultRabbitMotionReview.RestPosture.Supine,"Lying callback");r.PanelItems.First(i=>i.Text=="누움 → 앉기").Click();r.Advance(3);Check(r.Idle!=null&&r.Idle.Seated,"Lying return callback");r.ExitLyingReview();report.Add("Lying/return panel callbacks and review exit PASS (not physical input).");}catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
        r.Select(22);r.paused=true;yield return new WaitForEndOfFrame();
        try{r.PanelItems.First(i=>i.Text=="잠들기").Click();r.Advance(3.5f);Check(r.selected==24&&r.SleepEyeWeight>.99f,"Sleep callback");r.paused=true;}catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
        yield return new WaitForEndOfFrame();
        try{r.PanelItems.First(i=>i.Text=="깨어나기").Click();r.Advance(2);Check(r.selected==22&&r.SleepEyeWeight<.001f,"Wake callback");r.ExitLyingReview();report.Add("Sleep/wake callbacks, closed/open eye restoration PASS (not OS input).");}catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
        r.Select(22);r.paused=true;yield return new WaitForEndOfFrame();
        try{r.PanelItems.First(i=>i.Text=="왼쪽으로 돌아눕기").Click();r.Advance(2.5f);Check(r.selected==28&&r.Posture==AdultRabbitMotionReview.RestPosture.LeftSide,"Left roll callback");r.paused=true;}catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
        yield return new WaitForEndOfFrame();
        try{r.PanelItems.First(i=>i.Text=="바로 눕기로 복귀").Click();r.Advance(2.5f);Check(r.selected==22,"Side return callback");r.ExitLyingReview();report.Add("Left roll/return callbacks and awake side state PASS (not OS input).");}catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
        r.Select(28);r.paused=true;yield return new WaitForEndOfFrame();
        try{r.PanelItems.First(i=>i.Text=="잠들기").Click();r.Advance(3.5f);Check(r.selected==30&&r.SleepEyeWeight>.99f&&r.Posture==AdultRabbitMotionReview.RestPosture.LeftSide,"Side sleep shared button");r.paused=true;}catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"SideSleeping.png"));
        try{r.PanelItems.First(i=>i.Text=="깨어나기").Click();r.Advance(2);Check(r.selected==28&&r.SleepEyeWeight<.001f,"Side wake shared button");r.ExitLyingReview();report.Add("Shared sleep/wake buttons retain left-side posture PASS (not OS input).");}catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
        r.Select(28);r.paused=true;yield return new WaitForEndOfFrame();
        try{r.PanelItems.First(i=>i.Text=="옆누움 → 직접 앉기").Click();r.Advance(2.5f);Check(r.Idle!=null&&r.Idle.Seated,"Direct side-to-sit callback");r.Select(28);r.paused=true;report.Add("Direct side-to-sit panel callback PASS (not OS input).");}catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
        yield return new WaitForEndOfFrame();
        try{r.PanelItems.First(i=>i.Text=="옆누움 → 직접 서기").Click();r.Advance(3.3f);Check(r.Idle!=null&&!r.Idle.Seated,"Direct side-to-stand callback");r.ExitLyingReview();report.Add("Direct side-to-stand panel callback PASS (not OS input).");}catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
        foreach(bool seated in new[]{false,true}){
            r.StartBreathing(seated);r.paused=true;yield return new WaitForEndOfFrame();
            try{
                r.PanelItems.First(i=>i.Text==(seated?"앉기 → 왼쪽 눕기":"서기 → 왼쪽 눕기")).Click();
                r.paused=false;for(int i=0;i<300;i++)r.Advance(1f/60);
                Check(r.selected==28,"Direct left-side entry callback");r.ExitLyingReview();
                report.Add((seated?"Seated":"Standing")+" direct left-side entry button PASS (not physical input).");
            }catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
        }
        File.WriteAllLines(Path.Combine(directory,"Verification.txt"),report);Debug.Log("REVIEW_PANEL_OK");yield return new WaitForSecondsRealtime(.5f);Application.Quit(0);
    }
}}
