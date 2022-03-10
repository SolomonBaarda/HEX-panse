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
    public int MaxPlayerMovementPerTurnInBase = 3;


    [Space]

    [Range(0, 1)]
    public float EnemyCityChance = 0.25f;
    public int InitialEnemyStrengthMin = 0;
    public int InitialEnemyStrengthMax = 3;
    [Min(1)]
    public int MaxEnemyStrengthMin = 4;
    [Min(1)]
    public int MaxEnemyStrengthMax = 10;

    [Space]
    [Min(1)]
    public int NumberSidedDice = 6;
    [Min(1)]
    public int MaxNumberDefendersPerFight = 2;
    [Min(1)]
    public int MaxNumberAttackersPerFight = 2;

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
