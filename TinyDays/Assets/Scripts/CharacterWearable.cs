using UnityEngine;

namespace TinyDays.Characters
{
    public enum BodyAge { Infant, Child, Adult }
    public enum WearSlot { Top, Bottom, Shoes, Neckwear, Backpack }

    // Same-family assets share rest proportions, bone names AND bind transforms.
    // Infant/Child are identifiers only; they do not claim implemented models.
    [CreateAssetMenu(menuName="Tiny Days/Character wearable")]
    public sealed class CharacterWearable : ScriptableObject
    {
        public BodyAge age = BodyAge.Adult;
        public string bodyFamily = "AdultStandard_v1";
        public WearSlot slot;
        public Mesh mesh;
        public Material[] materials;
        public string[] boneNames;
        public string rootBoneName;
        public Vector3 localPosition;
        public Quaternion localRotation = Quaternion.identity;
        public Vector3 localScale = Vector3.one;
        public Bounds localBounds;
        public string[] coveredBodyParts;
    }
}
