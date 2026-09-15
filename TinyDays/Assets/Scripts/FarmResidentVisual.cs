using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace TinyDays.Review
{
    // Only presentation. Routes, clocks and review cameras belong to the director.
    public sealed class FarmResidentVisual : MonoBehaviour
    {
        public Animator animator;
        public AnimationClip idle, walk;
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable idlePlayable, walkPlayable;
        public void Sample(double time, float walkTime, float walkingWeight)
        {
            if (!graph.IsValid())
            {
                animator.enabled=true; animator.applyRootMotion=false;
                animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                graph=PlayableGraph.Create("Farm temporary resident");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                idlePlayable=AnimationClipPlayable.Create(graph,idle);
                walkPlayable=AnimationClipPlayable.Create(graph,walk);
                mixer=AnimationMixerPlayable.Create(graph,2);
                graph.Connect(idlePlayable,0,mixer,0); graph.Connect(walkPlayable,0,mixer,1);
                AnimationPlayableOutput.Create(graph,"Generic visual",animator).SetSourcePlayable(mixer);
                graph.Play();
            }
            idlePlayable.SetTime(time%idle.length);
            walkPlayable.SetTime(walkTime%walk.length);
            mixer.SetInputWeight(0,1-walkingWeight); mixer.SetInputWeight(1,walkingWeight);
            graph.Evaluate(0);
        }
        void OnDisable(){if(graph.IsValid())graph.Destroy();}
    }
}
