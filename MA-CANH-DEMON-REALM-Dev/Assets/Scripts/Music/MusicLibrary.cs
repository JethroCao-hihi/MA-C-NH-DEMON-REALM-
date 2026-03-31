using UnityEngine;

[System.Serializable]
public struct MusicTrack
{
    public string trackName;
    public AudioClip clip;
}

public class MusicLibrary : MonoBehaviour
{
    [SerializeField] private MusicTrack[] tracks;

    public MusicTrack[] Tracks => tracks;

    public AudioClip GetAudioClip(string trackName)
    {
        if (string.IsNullOrEmpty(trackName) || tracks == null)
            return null;

        for (int i = 0; i < tracks.Length; i++)
        {
            if (tracks[i].trackName == trackName)
                return tracks[i].clip;
        }

        return null;
    }
}
