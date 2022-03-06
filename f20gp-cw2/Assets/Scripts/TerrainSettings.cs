using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu]
public class TerrainSettings : ScriptableObject
{
    public List<TerrainLayerSetting> Biomes;

    [Min(0)]
    public int InitialPlayerStrength = 10;
    [Min(0)]
    public int ReinforcementStrengthPerCityPerTurn = 1;
    [Min(1)]
    public int MaxPlayerMovementPerTurn = 2;

    [Space]

    [Range(0, 1)]
    public float EnemyCityChance = 0.25f;
    public int InitialEnemyStrengthMin = 3;
    public int InitialEnemyStrengthMax = 7;

    [System.Serializable]
    public class TerrainLayerSetting
    {
        public Biome Biome;
        [Range(0, 1)]
        public float MaximumHeightThreshold;
    }

    public void ValidateValues()
    {
        Biomes.Sort((x, y) => x.MaximumHeightThreshold.CompareTo(y.MaximumHeightThreshold));
    }

    public Biome GetBiomeForHeight(float normalisedHeight)
    {
        Biome best = Biome.None;
        for (int i = 0; i < Biomes.Count; i++)
        {
            if (normalisedHeight >= Biomes[i].MaximumHeightThreshold)
            {
                best = Biomes[i].Biome;
            }
        }

        return best;
    }
}
