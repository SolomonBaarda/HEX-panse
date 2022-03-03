using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu]
public class BiomeSettings : ScriptableObject
{
    public List<BiomeSetting> Biomes;

    [System.Serializable]
    public class BiomeSetting
    {
        public Biome Biome;
        public Color BiomeColour;
    }

    public Color GetBiomeColour(Biome biome)
    {
        try
        {
            return Biomes.First(x => x.Biome == biome).BiomeColour;
        }
        catch (System.Exception)
        {
            return Color.white;
        }
    }
}
