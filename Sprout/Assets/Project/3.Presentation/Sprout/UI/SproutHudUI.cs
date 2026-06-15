using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sprout.Application;
using Sprout.Domain.Flowers;
using Sprout.Domain.Narrative;

namespace Sprout.Presentation.UI
{
    /// <summary>
    /// Combined HUD: day/phase indicator, flower inventory readout, night summary,
    /// and subtle relationship feedback. Self-subscribes to the services.
    /// </summary>
    public class SproutHudUI : MonoBehaviour
    {
        [Header("Day / phase")]
        [SerializeField] private TMP_Text dayPhaseText;

        [Header("Flower inventory")]
        [SerializeField] private TMP_Text inventoryText;

        [Header("Night summary")]
        [SerializeField] private GameObject nightPanel;
        [SerializeField] private TMP_Text nightText;
        [SerializeField] private Button nightContinueButton;

        [Header("Relationship feedback")]
        [SerializeField] private TMP_Text relationshipText;

        [Header("Auto-subscribe sources (optional)")]
        [SerializeField] private DayCycleService dayCycle;
        [SerializeField] private NightGossipService gossip;

        /// <summary>The night-summary panel (so the cursor guard can free the mouse).</summary>
        public GameObject NightPanel => nightPanel;

        private SproutGameDirector D => SproutGameDirector.Instance;

        private void Start()
        {
            if (nightPanel != null) nightPanel.SetActive(false);
            if (nightContinueButton != null) nightContinueButton.onClick.AddListener(HideNightSummary);
            if (D != null) D.Inventory.OnChanged += RefreshInventory;
            if (dayCycle != null) dayCycle.onPhaseChanged.AddListener(OnPhaseChanged);
            if (gossip != null) gossip.onNightSummary.AddListener(ShowNightSummary);
            RefreshInventory();
            RefreshRelationships();
        }

        private void OnDestroy()
        {
            if (D != null) D.Inventory.OnChanged -= RefreshInventory;
        }

        // Wire to DayCycleService.onPhaseChanged
        public void OnPhaseChanged(int day, string phase)
        {
            if (dayPhaseText != null)
                dayPhaseText.text = $"Day {day} — {phase}";
            RefreshRelationships();
        }

        // Wire to NightGossipService.onNightSummary
        public void ShowNightSummary(List<string> lines)
        {
            if (nightText != null) nightText.text = string.Join("\n\n", lines);
            if (nightPanel != null) nightPanel.SetActive(true);
        }

        public void HideNightSummary()
        {
            if (nightPanel != null) nightPanel.SetActive(false);
        }

        private void RefreshInventory()
        {
            if (inventoryText == null || D == null) return;
            var sb = new StringBuilder("Flowers: ");
            bool any = false;
            foreach (var kv in D.Inventory.Flowers)
            {
                if (kv.Value <= 0) continue;
                sb.Append($"{kv.Key} x{kv.Value}  ");
                any = true;
            }
            if (!any) sb.Append("(none yet)");

            bool anyBouquet = false;
            var bs = new StringBuilder("\nBouquets: ");
            foreach (var kv in D.Inventory.Bouquets)
            {
                if (kv.Value <= 0) continue;
                bs.Append($"{kv.Key} x{kv.Value}  ");
                anyBouquet = true;
            }
            if (anyBouquet) sb.Append(bs);
            inventoryText.text = sb.ToString();
        }

        private void RefreshRelationships()
        {
            if (relationshipText == null || D == null) return;
            var sb = new StringBuilder();
            foreach (NpcId npc in System.Enum.GetValues(typeof(NpcId)))
                sb.AppendLine($"{npc} {D.Relationships.MoodLabel(npc)}.");
            relationshipText.text = sb.ToString();
        }
    }
}
