using System.Collections.Generic;
using UnityEngine;

namespace TinyDays
{
    public sealed class ResidentWanderer : MonoBehaviour
    {
        public int residentIndex;
        public float walkSpeed = 1.3f;
        public float turnSharpness = 8f;
        public float initialDelay = 1f;
        public Vector3[] route = new Vector3[0];
        public int MovementCount { get; private set; }
        public float WaitingSeconds { get; private set; }
        public float PeakStrideAngle { get; private set; }
        public Vector3 CurrentTarget => route != null && route.Length > 0 ? route[routeIndex] : transform.position;

        Transform visual, leftLeg, rightLeg, leftArm, rightArm;
        Vector3 visualRest;
        Quaternion leftLegRest, rightLegRest, leftArmRest, rightArmRest;
        int routeIndex;
        float waitRemaining, phase;
        bool walking;

        void Awake()
        {
            visual = FindDeep(transform, "Visual") ?? transform;
            leftLeg = FindDeep(transform, "Leg_L"); rightLeg = FindDeep(transform, "Leg_R");
            leftArm = FindDeep(transform, "Arm_L"); rightArm = FindDeep(transform, "Arm_R");
            visualRest = visual.localPosition;
            if (leftLeg) leftLegRest = leftLeg.localRotation;
            if (rightLeg) rightLegRest = rightLeg.localRotation;
            if (leftArm) leftArmRest = leftArm.localRotation;
            if (rightArm) rightArmRest = rightArm.localRotation;
            routeIndex = route == null || route.Length == 0 ? 0 : residentIndex % route.Length;
            waitRemaining = initialDelay;
            phase = residentIndex * .91f;
        }

        void Update() { Simulate(Time.deltaTime); }

        public bool DriveTowards(Vector3 target, float delta, Vector3 separation)
        {
            if (delta <= 0f) return Vector3.Distance(transform.position, target) <= .15f;
            phase += delta * walkSpeed * 7f;
            var flat = target - transform.position; flat.y = 0f;
            if (flat.sqrMagnitude <= .025f) { Idle(delta); return true; }
            var direction = separation.sqrMagnitude>1f ? separation.normalized : (flat.normalized + separation).normalized;
            transform.position += direction * Mathf.Min(walkSpeed * delta, flat.magnitude);
            var desired = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desired, 1f - Mathf.Exp(-turnSharpness * delta));
            Animate(true);
            return false;
        }

        public bool DriveTowards(Vector3 target, float delta) { return DriveTowards(target,delta,Vector3.zero); }

        public void Idle(float delta)
        {
            phase += delta * 1.4f;
            Animate(false);
        }

        public void AnimateMotion(float speed,float delta)
        {
            phase+=delta*(speed>.03f?speed*7f:1.4f);
            Animate(speed>.03f);
        }

        public void Simulate(float delta)
        {
            if (route == null || route.Length == 0 || delta <= 0f) return;
            phase += delta * (walking ? walkSpeed * 7f : 1.4f);
            if (waitRemaining > 0f)
            {
                waitRemaining -= delta; WaitingSeconds += delta; walking = false;
                Animate(false);
                return;
            }
            var target = route[routeIndex];
            var flat = target - transform.position; flat.y = 0f;
            if (flat.sqrMagnitude <= .025f)
            {
                transform.position = new Vector3(target.x, transform.position.y, target.z);
                MovementCount++;
                routeIndex = (routeIndex + 1) % route.Length;
                waitRemaining = 2f + Mathf.Repeat(residentIndex * 1.37f + MovementCount * 1.91f, 5f);
                walking = false;
                Animate(false);
                return;
            }
            walking = true;
            var direction = flat.normalized;
            transform.position += direction * Mathf.Min(walkSpeed * delta, flat.magnitude);
            var desired = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desired, 1f - Mathf.Exp(-turnSharpness * delta));
            Animate(true);
        }

        void Animate(bool isWalking)
        {
            float stride = isWalking ? Mathf.Sin(phase) : 0f;
            float breathe = Mathf.Sin(phase * .62f) * (isWalking ? .025f : .012f);
            visual.localPosition = visualRest + Vector3.up * (breathe + (isWalking ? Mathf.Abs(stride) * .045f : 0f));
            Rotate(leftLeg, leftLegRest, stride * 24f); Rotate(rightLeg, rightLegRest, -stride * 24f);
            Rotate(leftArm, leftArmRest, -stride * 16f); Rotate(rightArm, rightArmRest, stride * 16f);
            PeakStrideAngle = Mathf.Max(PeakStrideAngle, Mathf.Abs(stride * 24f));
        }

        static void Rotate(Transform limb, Quaternion rest, float degrees)
        {
            if (limb) limb.localRotation = rest * Quaternion.Euler(degrees, 0f, 0f);
        }
        static Transform FindDeep(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                var found = FindDeep(child, name);
                if (found) return found;
            }
            return null;
        }
    }
}
