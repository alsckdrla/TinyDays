using System;
using System.IO;
using UnityEngine;

namespace TinyDays
{
    public sealed class PerformanceMonitor : MonoBehaviour
    {
        const int SampleCount=600;
        readonly float[] frameMs=new float[SampleCount];
        int count,write;
        int totalFrames;
        float realSeconds,gameSeconds,measuredReal,measuredGame,longestFrame;
        bool automated16x,reportWritten;
        public float AverageFps => measuredReal>.001f?totalFrames/measuredReal:0f;
        public float GameRate => measuredReal>.001f?measuredGame/measuredReal:0f;
        public float P95FrameMs { get { if(count==0) return 0; var copy=new float[count]; Array.Copy(frameMs,copy,count); Array.Sort(copy); return copy[Mathf.Clamp(Mathf.CeilToInt(count*.95f)-1,0,count-1)]; } }
        public float LongestFrameMs => longestFrame;
        public string Summary => "성능 FPS "+AverageFps.ToString("0.0")+" · P95 "+P95FrameMs.ToString("0.0")+"ms · 최대 "+LongestFrameMs.ToString("0.0")+"ms · 진행 "+GameRate.ToString("0.0")+"×";
        void Update()
        {
            float real=Mathf.Max(0.00001f,Time.unscaledDeltaTime), frame=real*1000f;
            realSeconds+=real; gameSeconds+=Time.deltaTime; longestFrame=Mathf.Max(longestFrame,frame);
            if(realSeconds>=10f) { frameMs[write]=frame; write=(write+1)%SampleCount; count=Mathf.Min(SampleCount,count+1); totalFrames++; measuredReal+=real; measuredGame+=Time.deltaTime; }
            if(automated16x&&realSeconds>=300f) { WriteReport("performance-16x.txt"); Application.Quit(); }
        }
        void Start()
        {
            automated16x=Array.IndexOf(Environment.GetCommandLineArgs(),"-tinyDaysPerf")>=0;
            if(automated16x) GetComponent<VillageTimeControls>().SetSpeed(4);
        }
        void OnApplicationQuit()
        {
            WriteReport("performance-last.txt");
        }
        void WriteReport(string file)
        {
            if(reportWritten) return; reportWritten=true;
            try { File.WriteAllText(Path.Combine(Application.persistentDataPath,file),DateTime.UtcNow.ToString("O")+"\n"+Summary+"\n화면 "+Screen.width+"x"+Screen.height+"\n실시간 "+realSeconds.ToString("0.0")+"초 · 게임시간 "+gameSeconds.ToString("0.0")+"초"); }
            catch(Exception) { }
        }
    }
}
