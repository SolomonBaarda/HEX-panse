using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu]
public class PlayerSettings : ScriptableObject
{
    public List<PlayerSetting> Players;

    [System.Serializable]
    public class PlayerSetting
    {
        public uint ID;
        public Color Colour;
    }

    public Color GetPlayerColour(uint ID)
    {
        try
        {
            return Players.First(x => x.ID == ID).Colour;
        }
        catch (System.Exception)
        {
            return Color.white;
        }
    }
}
