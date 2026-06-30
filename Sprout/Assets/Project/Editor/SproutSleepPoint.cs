#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Sprout.Presentation;
using Sprout.Application;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Monta un punto para DORMIR de un clic. Selecciona el objeto (la cama, el pozo, un cartel…) y dale a
    /// Tools/Sprout/Make Sleep Point. Crea un hijo "SleepZone" con un BoxCollider TRIGGER amplio + el
    /// componente BedSleepPoint, sin tocar el collider sólido del objeto. Te acercas y pulsas E para dormir.
    /// </summary>
    public static class SproutSleepPoint
    {
        [MenuItem("Tools/Sprout/Make Sleep Point (selected)")]
        public static void Make()
        {
            var go = Selection.activeGameObject;
            if (go == null)
            {
                EditorUtility.DisplayDialog("Sprout · Dormir",
                    "Selecciona primero el objeto en la jerarquía (la cama o el pozo) y vuelve a darle.", "OK");
                return;
            }

            // Zona de dormir como hijo: así no tocamos el collider físico del objeto.
            var existing = go.transform.Find("SleepZone");
            GameObject zone = existing != null ? existing.gameObject : new GameObject("SleepZone");
            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(zone, "Sleep Zone");
                zone.transform.SetParent(go.transform, false);
                zone.transform.localPosition = Vector3.zero;
            }

            // IMPORTANTE: el collider trigger va ANTES del BedSleepPoint (RequireComponent(Collider)).
            var box = zone.GetComponent<BoxCollider>();
            if (box == null) box = Undo.AddComponent<BoxCollider>(zone);
            box.isTrigger = true;
            box.size = new Vector3(3.5f, 3f, 3.5f); // amplio, fácil de pisar
            box.center = new Vector3(0f, 1f, 0f);

            if (zone.GetComponent<BedSleepPoint>() == null)
                Undo.AddComponent<BedSleepPoint>(zone);

            // Avisos útiles
            string warn = "";
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) warn += "\n⚠ No encuentro ningún objeto con tag 'Player'. Pon ese tag a tu florista.";
            if (Object.FindFirstObjectByType<DayCycleService>() == null)
                warn += "\n⚠ No hay 'DayCycleService' en la escena: sin él no avanza el día al dormir.";

            EditorUtility.SetDirty(zone);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
            Selection.activeGameObject = zone;

            EditorUtility.DisplayDialog("Sprout · Dormir",
                $"Listo: '{go.name}' ya tiene una SleepZone (trigger + BedSleepPoint).\n\n" +
                "Acércate hasta tocarla y pulsa E para dormir.\n" +
                "Si la zona es muy pequeña/grande, ajusta el tamaño del BoxCollider del hijo 'SleepZone'." +
                (string.IsNullOrEmpty(warn) ? "" : "\n" + warn), "OK");
        }
    }
}
#endif
