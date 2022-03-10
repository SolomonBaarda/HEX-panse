using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameLogic
{
    public static void Fight(ref int defendingStrength, ref int attackingStrength, int maxAttackers, int maxDefenders, int numberSidedDice = 6)
    {
        int numberOfDefenders = Mathf.Min(defendingStrength, maxDefenders);
        int numberOfAttackers = Mathf.Min(attackingStrength, maxAttackers);

        List<int> attackStrengths = new List<int>();
        List<int> defendStrengths = new List<int>();

        for (int i = 0; i < numberOfAttackers; i++)
        {
            attackStrengths.Add(Random.Range(0, numberSidedDice + 1));
        }

        for (int i = 0; i < numberOfDefenders; i++)
        {
            defendStrengths.Add(Random.Range(0, numberSidedDice + 1));
        }

        attackStrengths.Sort((x, y) => -x.CompareTo(y));
        defendStrengths.Sort((x, y) => -x.CompareTo(y));

        int defendersKilled = 0, attackersKilled = 0;

        for (int i = 0; i < Mathf.Min(numberOfAttackers, numberOfDefenders); i++)
        {
            // Attacker wins
            if (attackStrengths[i] > defendStrengths[i])
            {
                defendersKilled++;
            }
            // Defenders win
            else
            {
                attackersKilled++;
            }
        }

        defendingStrength -= defendersKilled;
        attackingStrength -= attackersKilled;

        Debug.Log($"Attackers lost {attackersKilled} and defenders lost {defendersKilled}");
    }





}
