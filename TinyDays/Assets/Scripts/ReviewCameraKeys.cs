using System;
using UnityEngine;

namespace TinyDays.Review
{
    // Keyboard movement is independent of pointer-panel hit testing and animation time.
    public static class ReviewCameraKeys
    {
        public static Vector2 Axes(Func<KeyCode,bool> held)
        {
            return new Vector2(
                (held(KeyCode.D)||held(KeyCode.RightArrow)?1:0)-(held(KeyCode.A)||held(KeyCode.LeftArrow)?1:0),
                (held(KeyCode.W)||held(KeyCode.UpArrow)?1:0)-(held(KeyCode.S)||held(KeyCode.DownArrow)?1:0));
        }
        public static Vector2 Read()=>Axes(Input.GetKey);
        public static float HeightAxis(Func<KeyCode,bool> held)=>(held(KeyCode.E)?1:0)-(held(KeyCode.Q)?1:0);
        public static float ReadHeight()=>HeightAxis(Input.GetKey);
        public static float Speed(float targetDistance)=>Mathf.Max(1f,targetDistance)*.5f;
        public static bool Allowed(bool focused,bool modal=false,bool editing=false)=>focused&&!modal&&!editing;
        public static bool EditingText=>GUI.GetNameOfFocusedControl()=="DayMinutesInput";
    }
}
