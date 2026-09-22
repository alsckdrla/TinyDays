using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TinyDays.Characters
{
    public sealed class ModularCharacter : MonoBehaviour
    {
        public BodyAge age = BodyAge.Adult;
        public string bodyFamily = "AdultStandard_v1";
        public CharacterWearable[] wardrobe;
        public SkinnedMeshRenderer[] bodyParts;
        readonly Dictionary<WearSlot, SkinnedMeshRenderer> equipped = new Dictionary<WearSlot, SkinnedMeshRenderer>();
        readonly Dictionary<WearSlot, CharacterWearable> items = new Dictionary<WearSlot, CharacterWearable>();
        public bool IsEquipped(WearSlot slot) => items.ContainsKey(slot);
        void Start(){Dress();}
        public void Dress(){foreach(var item in wardrobe)Equip(item);}
        public bool Equip(CharacterWearable item)
        {
            if(!item || item.age!=age || item.bodyFamily!=bodyFamily || !item.mesh)return false;
            var map=bodyParts.Where(s=>s).SelectMany(s=>s.bones).Where(b=>b).Distinct().ToDictionary(b=>b.name,b=>b);
            if(item.boneNames==null || item.boneNames.Length!=item.mesh.bindposes.Length || item.boneNames.Any(n=>!map.ContainsKey(n)) || !map.ContainsKey(item.rootBoneName))return false;
            // Validate fully before replacing the currently equipped item.
            Unequip(item.slot);
            var go=new GameObject("Wear_"+item.slot);go.transform.SetParent(transform,false);
            go.transform.localPosition=item.localPosition;go.transform.localRotation=item.localRotation;go.transform.localScale=item.localScale;
            var skin=go.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=item.mesh;skin.sharedMaterials=item.materials;
            skin.bones=item.boneNames.Select(n=>map[n]).ToArray();skin.rootBone=map[item.rootBoneName];skin.localBounds=item.localBounds;
            skin.updateWhenOffscreen=true;equipped[item.slot]=skin;items[item.slot]=item;RefreshBody();return true;
        }
        public void Unequip(WearSlot slot)
        {
            if(equipped.TryGetValue(slot,out var skin)&&skin){skin.gameObject.SetActive(false);if(Application.isPlaying)Destroy(skin.gameObject);else DestroyImmediate(skin.gameObject);}
            equipped.Remove(slot);items.Remove(slot);RefreshBody();
        }
        public void Undress(){foreach(WearSlot s in Enum.GetValues(typeof(WearSlot)))Unequip(s);}
        void RefreshBody()
        {
            var hidden=new HashSet<string>(items.Values.SelectMany(i=>i.coveredBodyParts??Array.Empty<string>()));
            foreach(var p in bodyParts)if(p)p.enabled=!hidden.Contains(p.name);
        }
    }
}
