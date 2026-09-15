using System;
using System.Collections.Generic;

namespace TinyDays
{
    [Serializable] public sealed class VillageSaveData
    {
        public int version=1, food, foodCapacity, totalHarvested, totalConsumed, priority, speedIndex;
        public bool paused, hudVisible, expansionDecided, expansionComplete;
        public float dayProgress, ambientVolume, cameraX, cameraY, cameraZ, cameraYaw, cameraPitch, cameraZoom, expansionShortage, expansionBuild;
        public List<CropSaveData> crops=new List<CropSaveData>();
        public List<ResidentSaveData> residents=new List<ResidentSaveData>();
    }
    [Serializable] public sealed class CropSaveData { public string id; public float growth; public bool harvest; public int carrierId; }
    [Serializable] public sealed class ResidentSaveData
    {
        public int id, action;
        public string slotId;
        public float x,y,z,yaw,fatigue,stateSeconds,travelSeconds,cooldown,elapsed;
        public bool traveling,carrying,restedSinceLastWork;
    }
}
