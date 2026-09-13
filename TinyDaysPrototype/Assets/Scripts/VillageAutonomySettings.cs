using UnityEngine;

namespace TinyDays
{
    public sealed class VillageAutonomySettings : MonoBehaviour
    {
        [Header("Residents")]
        public int residentCount = 6;
        public float residentRadius=.45f, obstacleMargin=.1f, lookAhead=.8f;
        public float acceleration=2.5f, turnDegrees=150f;
        public float detectionDistance=3.5f, predictionSeconds=1.8f;
        public float stuckSeconds=2f, failureSeconds=8f, failedSlotCooldown=10f;
        [Range(.5f, 2.5f)] public float moveSpeedMultiplier = 1.2f;
        [Range(0f, 1f)] public float restThreshold = .58f;
        [Header("Fatigue per second")]
        public float walkingFatigue = .010f;
        public float workFatigue = .035f;
        public float carryFatigue = .024f;
        public float restRecovery = .090f;
        [Header("Action timing")]
        public Vector2 actionSeconds = new Vector2(4f, 8f);
        // The farthest safe route crosses the whole 28-unit village at the slowest resident speed.
        public float destinationTimeout = 45f;
        public float retryDelay = 1.2f;
        [Header("Balanced daytime weights")]
        public float workWeight = 1f, carryWeight = .9f, restWeight = .42f, appreciateWeight = .68f;
    }
}
