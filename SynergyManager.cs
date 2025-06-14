namespace Game
{
    using UnityEngine;
    using System.Collections.Generic;
    using System.Linq;

    // Applies bonuses to units based on their traits
    public class SynergyManager : MonoBehaviour
    {
        // Reference to unit manager
        private UnitManger unitManger;

        // Initialize reference
        private void Awake()
        {
            unitManger = FindObjectOfType<UnitManger>();
            if (unitManger == null) Debug.LogError("UnitManger not found in scene.");
        }

        // Apply synergy bonuses to player units
        public void ApplySynergies()
        {
            if (unitManger == null) return;

            // Count traits of player units
            var units = unitManger.GetAllUnits().Where(u => u != null && !u.isEnemy).Select(u => u.GetComponent<Unit>()).ToList();
            Dictionary<Trait, int> traitCounts = new Dictionary<Trait, int>();
            foreach (var unit in units)
                if (unit != null)
                    foreach (var trait in unit.traits)
                        traitCounts[trait] = traitCounts.GetValueOrDefault(trait, 0) + 1;

            // Log trait counts
            foreach (var kvp in traitCounts)
                Debug.Log($"Trait {kvp.Key}: {kvp.Value} units");

            // Apply bonuses for traits with 2+ units
            foreach (var unit in units)
            {
                if (unit != null)
                {
                    int hpBonus = 0, attackBonus = 0;
                    foreach (var trait in unit.traits)
                    {
                        if (traitCounts.GetValueOrDefault(trait, 0) >= 2)
                        {
                            switch (trait)
                            {
                                case Trait.King:
                                case Trait.Warrior:
                                case Trait.Knight:
                                case Trait.Missionary:
                                    hpBonus += 10; // Health boost
                                    break;
                                case Trait.Adventurer:
                                case Trait.Rogue:
                                case Trait.Royal:
                                    attackBonus += 5; // Attack boost
                                    break;
                            }
                        }
                    }
                    unit.ApplySynergyBonus(hpBonus, attackBonus);
                }
            }
        }

        // Remove all synergy bonuses
        public void ClearSynergies()
        {
            if (unitManger == null) return;
            var units = unitManger.GetAllUnits().Where(u => u != null && !u.isEnemy).Select(u => u.GetComponent<Unit>()).ToList();
            foreach (var unit in units)
                if (unit != null)
                    unit.RemoveSynergyBonus();
            Debug.Log("Cleared all synergy bonuses.");
        }
    }
}