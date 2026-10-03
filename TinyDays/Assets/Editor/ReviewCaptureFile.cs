using System;
using System.IO;
using UnityEngine;

// Keep a mapped/open previous render intact; emit a distinct current render.
public static class ReviewCaptureFile {
    public static void Write(string path,byte[] bytes){
        try{File.WriteAllBytes(path,bytes);}
        catch(IOException){
            string alternative=Path.Combine(Path.GetDirectoryName(path),Path.GetFileNameWithoutExtension(path)+"-"+Guid.NewGuid().ToString("N")+Path.GetExtension(path));
            File.WriteAllBytes(alternative,bytes);Debug.LogWarning("Previous capture locked; current render saved to "+alternative);
        }
    }
}
