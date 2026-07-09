using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using OpenAI.Dialogue;
using Sprout.Application;
using Sprout.Domain.Flowers;
using Sprout.Domain.Narrative;
using Sprout.Domain.DayCycle;

namespace Sprout.Persistence
{
    /// <summary>
    /// Local JSON save/load. Persists day, phase, flags, counters, flower + bouquet
    /// inventory, relationships AND each NPC's conversation memory, so what the
    /// neighbours remember about you survives between days and game restarts.
    /// Auto-saves each night; enable loadOnStart for a "continue" flow.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        [SerializeField] private List<NPCBrain> npcBrains = new();
        [SerializeField] private DayCycleService dayCycle;
        [SerializeField] private string playerTag = "Player";
        [Tooltip("Cargar la última partida al arrancar (continuar). El guardado se hace SOLO al dormir.")]
        [SerializeField] private bool loadOnStart = false;

        [Serializable] private class Pair { public string k; public int v; }
        [Serializable] private class FlagPair { public string k; public bool v; }
        [Serializable] private class MemEntry { public string npc; public string key; public string val; }
        [Serializable] private class NodeEntry { public string npc; public string guid; }

        [Serializable]
        private class SaveData
        {
            public int day = 1;
            public string phase = "Morning";
            public List<FlagPair> flags = new();
            public List<Pair> counters = new();
            public List<Pair> flowers = new();
            public List<Pair> bouquets = new();
            public List<Pair> relationships = new();
            public List<MemEntry> memories = new();
            public List<NodeEntry> currentNodes = new();
            public List<string> phaseGoals = new();   // NPCs ya hablados en la fase actual

            public bool hasPlayer;                     // posición del jugador al guardar
            public Vector3 playerPos;
            public Quaternion playerRot = Quaternion.identity;
        }

        public string SavePath => Path.Combine(UnityEngine.Application.persistentDataPath, "sprout_save.json");

        private SproutGameDirector D => SproutGameDirector.Instance;

        public bool HasSave => File.Exists(SavePath);

        /// <summary>Lo activa el botón "Continuar" del menú principal para que la GameScene cargue la partida.</summary>
        public static bool ContinueRequested;

        private void Start()
        {
            // SOLO "Continuar" carga la partida. Nueva partida (o sin datos) → estado limpio SIEMPRE,
            // así se resetean todos los flags (incluida la intro) y no reapareces en el pueblo.
            if (ContinueRequested && HasSave) Load();
            else NewGameReset();
            ContinueRequested = false;
            if (dayCycle != null) dayCycle.onPhaseChanged.AddListener(OnPhase);
        }

        /// <summary>Reinicia el estado del director a una partida nueva (flags, inventario, relaciones y día).
        /// Necesario porque el director persiste entre escenas: sin esto, el flag de la intro (y todo el
        /// progreso) sobreviviría y la intro del coche se saltaría al empezar de nuevo.</summary>
        private void NewGameReset()
        {
            var d = D;
            if (d == null) return;
            d.Flags.Clear();
            d.Inventory.Clear();
            d.Relationships.LoadFrom(new Dictionary<NpcId, int>());
            d.Day.Load(1, DayPhase.Morning);
            Debug.Log("[SaveSystem] Nueva partida: estado reiniciado.");
        }

        private void OnDestroy()
        {
            if (dayCycle != null) dayCycle.onPhaseChanged.RemoveListener(OnPhase);
        }

        private void OnPhase(int day, string phase)
        {
            // Ya NO se autoguarda de noche: el guardado se hace SOLO al dormir (BedSleepPoint llama a Save()).
        }

        public void Save()
        {
            if (D == null) return;
            var data = new SaveData { day = D.Day.Day, phase = D.Day.Phase.ToString() };

            // Progreso de la fase actual (qué NPCs ya has hablado), para continuar sin reiniciarlo.
            if (dayCycle != null) data.phaseGoals.AddRange(dayCycle.ExportPhaseGoals());

            // Posición del jugador, para retomar donde lo dejaste (y no reaparecer en el coche).
            var playerGo = GameObject.FindGameObjectWithTag(playerTag);
            if (playerGo != null)
            {
                data.hasPlayer = true;
                data.playerPos = playerGo.transform.position;
                data.playerRot = playerGo.transform.rotation;
            }

            foreach (var kv in D.Flags.AllFlags) data.flags.Add(new FlagPair { k = kv.Key, v = kv.Value });
            foreach (var kv in D.Flags.AllCounters) data.counters.Add(new Pair { k = kv.Key, v = kv.Value });
            foreach (var kv in D.Inventory.Flowers) data.flowers.Add(new Pair { k = kv.Key.ToString(), v = kv.Value });
            foreach (var kv in D.Inventory.Bouquets) data.bouquets.Add(new Pair { k = kv.Key.ToString(), v = kv.Value });
            foreach (var kv in D.Relationships.All) data.relationships.Add(new Pair { k = kv.Key.ToString(), v = kv.Value });

            // NPC conversation memory (what each neighbour remembers about the player).
            foreach (var brain in npcBrains)
            {
                if (brain == null) continue;
                foreach (var kv in brain.ExportMemory())
                    data.memories.Add(new MemEntry { npc = brain.npcName, key = kv.Key, val = kv.Value });
            }

            // Nodo actual de cada conversación (para retomar donde lo dejaste al cargar).
            foreach (var brain in npcBrains)
            {
                if (brain == null) continue;
                var runner = brain.GetComponent<DialogueRunner>();
                var cur = runner != null ? runner.Current : null;
                if (cur != null && !string.IsNullOrEmpty(cur.nodeGuid))
                    data.currentNodes.Add(new NodeEntry { npc = brain.npcName, guid = cur.nodeGuid });
            }

            try
            {
                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
                Debug.Log($"[SaveSystem] Saved (with NPC memory) to {SavePath}");
            }
            catch (Exception e) { Debug.LogError($"[SaveSystem] Save failed: {e.Message}"); }
        }

        public bool Load()
        {
            if (D == null || !HasSave) return false;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));

                var flags = new Dictionary<string, bool>();
                foreach (var f in data.flags) flags[f.k] = f.v;
                var counters = new Dictionary<string, int>();
                foreach (var c in data.counters) counters[c.k] = c.v;
                D.Flags.LoadFrom(flags, counters);

                var fl = new Dictionary<FlowerKind, int>();
                foreach (var f in data.flowers)
                    if (Enum.TryParse(f.k, out FlowerKind fk)) fl[fk] = f.v;
                var bq = new Dictionary<BouquetKind, int>();
                foreach (var b in data.bouquets)
                    if (Enum.TryParse(b.k, out BouquetKind bk)) bq[bk] = b.v;
                D.Inventory.LoadFrom(fl, bq);

                var rel = new Dictionary<NpcId, int>();
                foreach (var r in data.relationships)
                    if (Enum.TryParse(r.k, out NpcId n)) rel[n] = r.v;
                D.Relationships.LoadFrom(rel);

                if (Enum.TryParse(data.phase, out DayPhase phase))
                    D.Day.Load(data.day, phase);

                // Restaurar el progreso de la fase (DESPUÉS de Load, que dispara el clear).
                if (dayCycle != null) dayCycle.ImportPhaseGoals(data.phaseGoals);

                // Restore each NPC's memory.
                var byNpc = new Dictionary<string, Dictionary<string, string>>();
                foreach (var m in data.memories)
                {
                    if (!byNpc.TryGetValue(m.npc, out var d)) { d = new Dictionary<string, string>(); byNpc[m.npc] = d; }
                    d[m.key] = m.val;
                }
                foreach (var brain in npcBrains)
                {
                    if (brain == null) continue;
                    if (byNpc.TryGetValue(brain.npcName, out var mem)) brain.ImportMemory(mem);
                }

                // Restaurar el nodo actual de cada conversación (retomar donde lo dejaste).
                if (data.currentNodes != null)
                    foreach (var ne in data.currentNodes)
                    {
                        var brain = npcBrains.Find(b => b != null && b.npcName == ne.npc);
                        if (brain == null || brain.dialogueGraph == null) continue;
                        var node = brain.dialogueGraph.nodes.Find(n => n != null && n.nodeGuid == ne.guid);
                        var runner = brain.GetComponent<DialogueRunner>();
                        if (node != null && runner != null) runner.RestoreCurrent(node);
                    }

                // Restaurar la posición del jugador (desactivando el CharacterController para no colisionar).
                if (data.hasPlayer)
                {
                    var playerGo = GameObject.FindGameObjectWithTag(playerTag);
                    if (playerGo != null)
                    {
                        var cc = playerGo.GetComponent<CharacterController>();
                        if (cc != null) cc.enabled = false;
                        playerGo.transform.SetPositionAndRotation(data.playerPos, data.playerRot);
                        if (cc != null) cc.enabled = true;
                    }
                }

                Debug.Log("[SaveSystem] Loaded (with NPC memory).");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Load failed: {e.Message}");
                return false;
            }
        }

        public void DeleteSave()
        {
            if (HasSave) File.Delete(SavePath);
        }
    }
}
