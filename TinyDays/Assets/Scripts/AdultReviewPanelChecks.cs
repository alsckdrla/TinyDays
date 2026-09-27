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
        string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../Docs/Captures/ReviewPanelV0105"));Directory.CreateDirectory(directory);
        var sizes=new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720),new Vector2Int(800,600)};
        foreach(var size in sizes){
            Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.7f);
            r.AutomaticIdle=false;r.StartBreathing(false);r.paused=true;r.PanelScrollY=0;yield return new WaitForEndOfFrame();
            try{
                Check(Screen.width==size.x&&Screen.height==size.y,"Requested resolution unavailable");
                Rect panel=AdultRabbitMotionReview.PanelBounds(Screen.width,Screen.height);
                Check(panel.width==300&&panel.x==12&&panel.y==12&&panel.yMax<=Screen.height,"Panel bounds");
                var items=r.PanelItems.ToArray();float contentWidth=panel.width-34;
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
        File.WriteAllLines(Path.Combine(directory,"Verification.txt"),report);Debug.Log("REVIEW_PANEL_OK");yield return new WaitForSecondsRealtime(.5f);Application.Quit(0);
    }
}}
