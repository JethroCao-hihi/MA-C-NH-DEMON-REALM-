using UnityEngine;

[System.Serializable]
public struct SoundTrack
{
    public string sfxName;
    public AudioClip clip;
}

public class SoundLibrary : MonoBehaviour
{
    [SerializeField] private SoundTrack[] tracks;

    public SoundTrack[] Tracks => tracks;

    public AudioClip GetAudioClip(string sfxName)
    {
        if (string.IsNullOrEmpty(sfxName) || tracks == null)
            return null;

        for (int i = 0; i < tracks.Length; i++)
        {
            if (tracks[i].sfxName == sfxName)
                return tracks[i].clip;
        }

        return null;
    }
}
