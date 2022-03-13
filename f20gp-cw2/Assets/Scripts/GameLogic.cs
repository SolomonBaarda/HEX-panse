using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameLogic
{
    public static void Fight(ref int defendingStrength, ref int attackingStrength, int maxAttackers, int maxDefenders, out int defenderDamageTaken, out int attackerDamageTaken, int numberSidedDice = 6)
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

        defenderDamageTaken = 0;
        attackerDamageTaken = 0;

        for (int i = 0; i < Mathf.Min(numberOfAttackers, numberOfDefenders); i++)
        {
            // Attacker wins
            if (attackStrengths[i] > defendStrengths[i])
            {
                defenderDamageTaken--;
            }
            // Defenders win
            else
            {
                attackerDamageTaken--;
            }
        }

        defendingStrength += defenderDamageTaken;
        attackingStrength += attackerDamageTaken;

        Debug.Log($"Attackers took {attackerDamageTaken} damage and defenders took {defenderDamageTaken} damage");
    }





}
