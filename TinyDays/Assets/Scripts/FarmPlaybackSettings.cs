namespace TinyDays.Review
{
    // Review settings, reset to defaults when the application starts.
    public sealed class FarmPlaybackSettings
    {
        public static readonly float[] Rates={.5f,1f,2f,4f};
        public static readonly int[] SuggestedMinutes={5,10,20,30};
        public int DayMinutes {get;private set;}=5;
        public float Rate {get;private set;}=1;
        public bool SetDayMinutes(string input)
        {
            if(!int.TryParse(input,out int minutes)||minutes<1||minutes>120)return false;
            DayMinutes=minutes;return true;
        }
        public void SetRate(int index){if(index>=0&&index<Rates.Length)Rate=Rates[index];}
        public float ScaledSeconds(float seconds,bool paused)=>paused?0:System.Math.Max(0,seconds)*Rate;
    }
}
