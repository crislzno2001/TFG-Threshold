using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sprout.Application;
using Sprout.Domain.Flowers;

namespace Sprout.Presentation
{
    /// <summary>
    /// Macetero/huerto: cuando la florista consigue una flor (se añade al FlowerInventory), BROTA su modelo
    /// 3D en un slot libre del macetero, con animación de crecer. Al cargar partida, muestra las que ya
    /// tenías. Ponlo en un objeto del macetero, asigna los slots y el modelo de cada tipo de flor.
    /// </summary>
    public sealed class FlowerGarden : MonoBehaviour
    {
        [Serializable]
        public class FlowerModel
        {
            public FlowerKind kind;
            [Tooltip("Tu modelo 3D de esta flor (prefab).")]
            public GameObject prefab;
        }

        [Header("Slots del macetero (posiciones donde brotan, en orden)")]
        [SerializeField] private Transform[] slots;

        [Header("Modelo 3D de cada tipo de flor")]
        [SerializeField] private List<FlowerModel> models = new();

        [Header("Crecer")]
        [SerializeField] private float growSeconds = 1.2f;
        [Tooltip("Escala final del modelo al brotar (multiplica la del prefab).")]
        [SerializeField] private float finalScale = 1f;
        [SerializeField] private bool randomYRotation = true;

        private FlowerInventory _inv;
        private readonly Dictionary<FlowerKind, int> _planted = new();
        private int _nextSlot;

        private void OnEnable() => StartCoroutine(BindWhenReady());

        private IEnumerator BindWhenReady()
        {
            float t = 0f;
            while ((SproutGameDirector.Instance == null || SproutGameDirector.Instance.Inventory == null) && t < 5f)
            { t += Time.unscaledDeltaTime; yield return null; }

            var d = SproutGameDirector.Instance;
            if (d == null || d.Inventory == null) yield break;
            _inv = d.Inventory;
            _inv.OnChanged += Refresh;
            yield return null;          // deja que el guardado cargue el inventario
            Refresh();                  // muestra las que ya tuvieras (continuar)
        }

        private void OnDisable()
        {
            if (_inv != null) _inv.OnChanged -= Refresh;
        }

        /// <summary>Brota las flores nuevas que haya en el inventario y aún no estén plantadas.</summary>
        private void Refresh()
        {
            if (_inv == null || slots == null || slots.Length == 0) return;

            foreach (var kv in _inv.Flowers)
            {
                _planted.TryGetValue(kv.Key, out int already);
                int toPlant = kv.Value - already;
                for (int i = 0; i < toPlant && _nextSlot < slots.Length; i++)
                {
                    Plant(kv.Key, slots[_nextSlot]);
                    _nextSlot++;
                }
                _planted[kv.Key] = kv.Value; // marca este tipo como ya plantado (aunque falten slots)
            }
        }

        private void Plant(FlowerKind kind, Transform slot)
        {
            var model = models.Find(m => m != null && m.kind == kind && m.prefab != null);
            if (model == null || slot == null) return;

            var go = Instantiate(model.prefab, slot.position, slot.rotation, slot);
            if (randomYRotation)
                go.transform.Rotate(0f, UnityEngine.Random.Range(0f, 360f), 0f, Space.Self);

            Vector3 target = go.transform.localScale * finalScale;
            StartCoroutine(Grow(go.transform, target));
        }

        private IEnumerator Grow(Transform t, Vector3 target)
        {
            float e = 0f;
            while (t != null && e < growSeconds)
            {
                e += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / growSeconds));
                t.localScale = Vector3.LerpUnclamped(Vector3.zero, target, k);
                yield return null;
            }
            if (t != null) t.localScale = target;
        }
    }
}
