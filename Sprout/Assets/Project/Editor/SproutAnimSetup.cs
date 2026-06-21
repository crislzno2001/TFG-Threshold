#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único: deja la florista (Mixamo) animada con las animaciones de KayKit (rig MEDIUM).
    /// 1) Pone el FBX de la florista en Humanoid (si no lo está).
    /// 2) Pone los FBX de animaciones Rig_Medium en Humanoid Y activa Loop en sus clips
    ///    (sin loop la animación hace un frame y se queda tiesa).
    /// 3) Crea un Animator Controller "Florista_Anim" con Idle + Walk (bool "Moving").
    /// 4) Intenta asignarlo a la florista en la escena (Animator + avatar, root motion off).
    ///
    /// Menú:  Tools/Sprout/Setup Florista Animations (KayKit Medium)
    /// </summary>
    public static class SproutAnimSetup
    {
        private const string Dir = "Assets/Art/Characters/florista_final/";
        private const string Kay = "Assets/Art/Characters/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/";

        [MenuItem("Tools/Sprout/Setup Florista Animations (KayKit Medium)")]
        public static void Setup()
        {
            // 1. FBX de la florista -> Humanoid
            string florista = Dir + "Angryç.fbx";
            if (AssetImporter.GetAtPath(florista) == null) florista = Dir + "Angry.fbx";
            var fImp = AssetImporter.GetAtPath(florista) as ModelImporter;
            if (fImp == null) { Dialog("No encuentro el FBX de la florista (Angryç.fbx / Angry.fbx) en\n" + Dir); return; }
            ToHumanoid(fImp, false);

            // 2. Animaciones KayKit Rig_Medium -> Humanoid + Loop + recoger clips
            string[] files = {
                "Rig_Medium_MovementBasic.fbx", "Rig_Medium_MovementAdvanced.fbx",
                "Rig_Medium_General.fbx", "Rig_Medium_Simulation.fbx",
            };
            var clips = new List<AnimationClip>();
            foreach (var f in files)
            {
                string path = Kay + f;
                if (AssetImporter.GetAtPath(path) is ModelImporter imp)
                {
                    ToHumanoid(imp, true); // true = activar loop en todos los clips
                    foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                        if (o is AnimationClip c && !c.name.StartsWith("__"))
                            clips.Add(c);
                }
            }
            if (clips.Count == 0) { Dialog("No encontré clips de animación en Rig_Medium."); return; }

            AnimationClip idle =
                clips.FirstOrDefault(c => c.name.ToLowerInvariant() == "idle")
                ?? clips.FirstOrDefault(c => c.name.ToLowerInvariant().StartsWith("idle"))
                ?? clips.FirstOrDefault(c => c.name.ToLowerInvariant().Contains("idle") && !c.name.ToLowerInvariant().Contains("jump"))
                ?? clips[0];
            AnimationClip walk =
                clips.FirstOrDefault(c => c.name.ToLowerInvariant().StartsWith("walk"))
                ?? clips.FirstOrDefault(c => c.name.ToLowerInvariant().Contains("walk"))
                ?? idle;

            // 3. Animator Controller
            string ctrlPath = Dir + "Florista_Anim.controller";
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null)
                AssetDatabase.DeleteAsset(ctrlPath); // sobrescribir, no duplicar
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            ctrl.AddParameter("Moving", AnimatorControllerParameterType.Bool);
            var sm = ctrl.layers[0].stateMachine;
            var sIdle = sm.AddState("Idle"); sIdle.motion = idle;
            var sWalk = sm.AddState("Walk"); sWalk.motion = walk;
            sm.defaultState = sIdle;
            var toWalk = sIdle.AddTransition(sWalk); toWalk.hasExitTime = false; toWalk.duration = 0.1f;
            toWalk.AddCondition(AnimatorConditionMode.If, 0, "Moving");
            var toIdle = sWalk.AddTransition(sIdle); toIdle.hasExitTime = false; toIdle.duration = 0.1f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "Moving");

            // 4. Avatar de la florista
            Avatar avatar = null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(florista))
                if (o is Avatar a) avatar = a;

            // 5. Asignar en la escena
            int n = 0;
            foreach (var smr in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string rn = smr.transform.root.name.ToLowerInvariant();
                if (!rn.Contains("angry") && !rn.Contains("florista")) continue;
                var go = smr.transform.root.gameObject;
                var anim = go.GetComponent<Animator>() ?? Undo.AddComponent<Animator>(go);
                anim.runtimeAnimatorController = ctrl;
                if (avatar != null) anim.avatar = avatar;
                anim.applyRootMotion = false;
                EditorUtility.SetDirty(anim);
                n++;
                break;
            }

            AssetDatabase.SaveAssets();
            Dialog($"Animaciones (KayKit Medium) listas.\n\nIdle: {idle.name}\nWalk: {walk.name}\n\n" +
                   (n > 0 ? "Asignado a la florista (Animator + avatar, root motion off)."
                          : "No encontré la florista en la escena: arrastra Florista_Anim a su Animator a mano.") +
                   "\n\nDale a Play: debería respirar en Idle. Si sigue tiesa, abre el clip Idle y comprueba que tiene 'Loop Time'.");
            Debug.Log("[Sprout] Animaciones de la florista (KayKit Medium) configuradas.");
        }

        private static void ToHumanoid(ModelImporter imp, bool loopClips)
        {
            bool dirty = false;
            if (imp.animationType != ModelImporterAnimationType.Human)
            {
                imp.animationType = ModelImporterAnimationType.Human;
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                dirty = true;
            }
            if (loopClips)
            {
                var cs = imp.defaultClipAnimations;
                for (int i = 0; i < cs.Length; i++) cs[i].loopTime = true;
                imp.clipAnimations = cs;
                dirty = true;
            }
            if (dirty) imp.SaveAndReimport();
        }

        private static void Dialog(string m) => EditorUtility.DisplayDialog("Sprout", m, "OK");
    }
}
#endif
