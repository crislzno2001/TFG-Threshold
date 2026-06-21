using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Sprout.Data;
using Sprout.Domain.Flowers;
using Sprout.Domain.Narrative;

namespace Sprout.Application
{
    /// <summary>
    /// Applies the emotional-flower generation rules and resolves bouquet gifting.
    /// Flowers are added to the director's FlowerInventory; the growing area UI
    /// reflects the inventory. Bouquet gifting returns a context string the
    /// dialogue system can feed to the AI so the NPC reacts in character.
    /// </summary>
    public class FlowerService : MonoBehaviour
    {
        [Header("Definitions (for icons / reactions)")]
        [SerializeField] private List<FlowerDefinitionSO> flowerDefs = new();
        [SerializeField] private List<BouquetDefinitionSO> bouquetDefs = new();

        [Header("Events")]
        public UnityEvent<FlowerKind> onFlowerGenerated;
        public UnityEvent<BouquetKind> onBouquetCrafted;

        private SproutGameDirector D => SproutGameDirector.Instance;

        // ── Generation rules (called by trackers / dialogue flag events) ────────
        public void OnHighFluency()        => Grant(FlowerKind.Sol);
        public void OnHighOriginality()    => Grant(FlowerKind.Acuariana);
        public void OnLiedKindlyToMochi()  => Grant(FlowerKind.Crisalida);
        public void OnPainfulHonesty()     => Grant(FlowerKind.Anima);
        public void OnGossiped()           => Grant(FlowerKind.Inquieta);
        public void OnHelpedMothLie()      => Grant(FlowerKind.Brasa);
        public void OnRefusedMothLie()     => Grant(FlowerKind.Anima);
        public void OnRixOpenedUp()        => Grant(FlowerKind.Acuariana);
        public void OnUnresolvedArgument() => Grant(FlowerKind.Velada);

        /// <summary>String-keyed entry point so dialogue flag events can trigger flowers.</summary>
        public void GrantByName(string flowerName)
        {
            if (System.Enum.TryParse(flowerName, true, out FlowerKind kind))
                Grant(kind);
        }

        public void Grant(FlowerKind kind)
        {
            if (D == null || kind == FlowerKind.None) return;
            D.Inventory.AddFlower(kind);
            onFlowerGenerated?.Invoke(kind);
            Debug.Log($"[FlowerService] grew a {kind} flower.");
        }

        // ── Crafting ────────────────────────────────────────────────────────────
        public BouquetKind Craft(FlowerKind a, FlowerKind b)
        {
            if (D == null) return BouquetKind.None;
            var result = D.Inventory.Craft(a, b);
            if (result != BouquetKind.None) onBouquetCrafted?.Invoke(result);
            return result;
        }

        // ── Gifting ───────────────────────────────────────────────────────────
        /// <summary>
        /// Gives a bouquet to an NPC. Applies the relationship delta and returns a
        /// context string for the AI (or null if the NPC has no defined reaction).
        /// </summary>
        public string GiveBouquetTo(BouquetKind bouquet, NpcId npc)
        {
            if (D == null) return null;
            if (!D.Inventory.GiveBouquet(bouquet)) return null;

            var def = bouquetDefs.Find(d => d != null && d.kind == bouquet);

            // Comfort bouquet spreads positive gossip (spec rule 6).
            if (bouquet == BouquetKind.Comfort)
                D.Flags.SetFlag("gave_comforting_bouquet", true);

            if (def == null || def.reactions == null) return "You receive a bouquet.";

            foreach (var r in def.reactions)
            {
                if (r.npc != npc) continue;
                int delta = r.affinityDelta;
                if (r.requiresTrust && D.Relationships.Get(npc) < 2)
                    delta = -System.Math.Abs(delta); // patronising if no trust
                D.Relationships.Add(npc, delta);
                return r.reactionContext;
            }
            return "You receive a bouquet.";
        }

        public FlowerDefinitionSO DefOf(FlowerKind k) => flowerDefs.Find(d => d != null && d.kind == k);
        public BouquetDefinitionSO DefOf(BouquetKind k) => bouquetDefs.Find(d => d != null && d.kind == k);
        public IReadOnlyList<FlowerDefinitionSO> FlowerDefs => flowerDefs;
        public IReadOnlyList<BouquetDefinitionSO> BouquetDefs => bouquetDefs;
    }
}
