using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public enum eTypeSound
{
    Music,
    Sound,
}

public enum eAudioName
{
    Audio_Button,
    Audio_Music,
    Audio_Click,
    Audio_Drop,
    Audio_SellectBox,
}
public class SoundAsset : MonoBehaviour
{
    public List<AudioClipAsset> audioClipAssets = new List<AudioClipAsset>();

    public AudioClipAsset GetAudioClipAsset(eAudioName type)
    {
        AudioClipAsset asset = new AudioClipAsset();
        for (int i = 0; i < audioClipAssets.Count; i++)
        {
            if (type == audioClipAssets[i].typeName)
            {
                asset = audioClipAssets[i];
            }
        }
        return asset;
    }
}

[Serializable]
public struct AudioClipAsset
{
    public eTypeSound type;

    public eAudioName typeName;

    public AudioClip clip;

    public float lenght => clip.length;
}