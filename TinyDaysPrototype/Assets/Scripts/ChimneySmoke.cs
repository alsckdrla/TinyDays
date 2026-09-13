using UnityEngine;

namespace TinyDays
{
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class ChimneySmoke : MonoBehaviour
    {
        public void Configure()
        {
            var smoke = GetComponent<ParticleSystem>();
            smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = smoke.main;
            main.loop = true; main.playOnAwake = true; main.prewarm = true; main.duration = 5f; main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 3.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.22f, .38f); main.startSize = new ParticleSystem.MinMaxCurve(.16f, .28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(.68f,.68f,.64f,.52f), new Color(.84f,.81f,.74f,.35f));
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 18;
            var emission = smoke.emission; emission.rateOverTime = 1.3f;
            var shape = smoke.shape; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = .10f;
            var velocity = smoke.velocityOverLifetime; velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-.05f,.05f);
            velocity.y = new ParticleSystem.MinMaxCurve(.45f,.45f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f,0f);
            var color = smoke.colorOverLifetime; color.enabled = true;
            var gradient = new Gradient(); gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.22f,0),new GradientAlphaKey(.12f,.55f),new GradientAlphaKey(0,1)}); color.color = gradient;
            var size = smoke.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, 2.3f);
            var renderer = smoke.GetComponent<ParticleSystemRenderer>(); renderer.renderMode = ParticleSystemRenderMode.Mesh; renderer.sortingFudge = -1f;
            smoke.Play(true);
        }
        void Awake() { Configure(); }
    }
}
