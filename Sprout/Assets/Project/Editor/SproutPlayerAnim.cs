#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Crea un Animator Controller para la florista-player con animaciones KayKit (rig Medium),
    /// COMPATIBLE con tu PlayerAnimationDriver (parámetros Speed, Grounded, Jump, FreeFall,
    /// MotionSpeed). Locomoción Idle/Walk/Run por velocidad (umbrales 0 / 2 / 5.335), salto y
    /// caída automáticos, y acciones extra: Sit, Lie (bool) e Interact/UseItem/Throw/Pickup (trigger).
    ///
    /// Menú:  Tools/Sprout/Build Player Animator (full)
    /// </summary>
    public static class SproutPlayerAnim
    {
        private const string Dir = "Assets/Art/Characters/florista_final/";
        private const string Kay = "Assets/Art/Characters/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/";
        private const float WalkSpeed = 2.0f;     // moveSpeed de AnimalCrossingLocomotion
        private const float RunSpeed = 5.335f;    // sprintSpeed

        [MenuItem("Tools/Sprout/Build Player Animator (full)")]
        public static void Build()
        {
            string florista = Dir + "Angryç.fbx";
            if (AssetImporter.GetAtPath(florista) == null) florista = Dir + "Angry.fbx";
            var fImp = AssetImporter.GetAtPath(florista) as ModelImporter;
            if (fImp == null) { Dlg("No encuentro el FBX de la florista en\n" + Dir); return; }
            if (fImp.animationType != ModelImporterAnimationType.Human)
            {
                fImp.animationType = ModelImporterAnimationType.Human;
                fImp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                fImp.SaveAndReimport();
            }

            string[] files = {
                "Rig_Medium_MovementBasic.fbx", "Rig_Medium_MovementAdvanced.fbx",
                "Rig_Medium_General.fbx", "Rig_Medium_Simulation.fbx",
                "Rig_Medium_Special.fbx", "Rig_Medium_Tools.fbx",
            };
            var clips = new List<AnimationClip>();
            foreach (var f in files)
            {
                string path = Kay + f;
                if (AssetImporter.GetAtPath(path) is ModelImporter imp)
                {
                    if (imp.animationType != ModelImporterAnimationType.Human)
                    {
                        imp.animationType = ModelImporterAnimationType.Human;
                        imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    }
                    var cs = imp.defaultClipAnimations;
                    for (int i = 0; i < cs.Length; i++)
                    {
                        cs[i].loopTime = Looping(cs[i].name);
                        cs[i].lockRootHeightY = true;    // altura consistente: ni flota ni se hunde
                        cs[i].heightFromFeet = true;     // basado en los PIES (apoyados en el suelo)
                        cs[i].lockRootPositionXZ = true; // sin deriva horizontal de la raíz
                        cs[i].lockRootRotation = true;   // sin giro de raíz
                    }
                    imp.clipAnimations = cs;
                    imp.SaveAndReimport();
                    foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                        if (o is AnimationClip c && !c.name.StartsWith("__")) clips.Add(c);
                }
            }
            if (clips.Count == 0) { Dlg("No encontré clips en Rig_Medium."); return; }

            AnimationClip idle  = Pick(clips, "idle_b", "idle_a", "idle");
            AnimationClip walk  = Pick(clips, "walking_a", "walk");
            AnimationClip run   = Pick(clips, "running_a", "run");
            AnimationClip jStart= Pick(clips, "jump_start", "jump_full");
            AnimationClip jAir  = Pick(clips, "jump_idle", "jump_full_long", "fall");
            AnimationClip jLand = Pick(clips, "jump_land", "land");
            AnimationClip sit   = Pick(clips, "sit_chair_idle", "sit");
            AnimationClip lie   = Pick(clips, "lie_idle", "lie", "sleep");
            AnimationClip inter = Pick(clips, "interact");
            AnimationClip use   = Pick(clips, "use_item", "use");
            AnimationClip thr   = Pick(clips, "throw");
            AnimationClip pck   = Pick(clips, "pick", "grab") ?? inter;
            if (idle == null) idle = clips[0];
            if (walk == null) walk = idle;
            if (run == null) run = walk;
            if (jAir == null) jAir = jStart;

            string ctrlPath = Dir + "Player_Anim.controller";
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null)
                AssetDatabase.DeleteAsset(ctrlPath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("MotionSpeed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Jump", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("FreeFall", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Sit", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Lie", AnimatorControllerParameterType.Bool);
            foreach (var t in new[] { "Interact", "UseItem", "Throw", "Pickup" })
                ctrl.AddParameter(t, AnimatorControllerParameterType.Trigger);

            var sm = ctrl.layers[0].stateMachine;

            // Locomoción (Speed: 0 idle, 2 walk, 5.3 run)
            // Leer las velocidades REALES de la locomoción para que los umbrales encajen exactos
            // (así andar = moveSpeed -> Walking_A puro, y esprintar = sprintSpeed -> Running_A puro).
            float walkT = WalkSpeed, runT = RunSpeed;
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb == null || mb.GetType().Name != "AnimalCrossingLocomotion") continue;
                var so = new SerializedObject(mb);
                var mv = so.FindProperty("moveSpeed");
                var sp = so.FindProperty("sprintSpeed");
                if (mv != null) walkT = mv.floatValue;
                if (sp != null) runT = sp.floatValue;
                break;
            }
            if (runT <= walkT + 0.1f) runT = walkT + 1.5f; // por si no hay sprint distinto

            var bt = new BlendTree { name = "Locomotion", blendType = BlendTreeType.Simple1D, blendParameter = "Speed" };
            AssetDatabase.AddObjectToAsset(bt, ctrl);
            bt.AddChild(idle, 0f);
            bt.AddChild(walk, walkT);   // andar = moveSpeed real
            bt.AddChild(run, runT);     // correr = sprintSpeed real
            var loco = sm.AddState("Locomotion");
            loco.motion = bt;
            sm.defaultState = loco;

            // SIN salto/caída: AnimalCrossingLocomotion no salta, y el estado de caída era el que
            // dejaba a la florista en "free fall" constante. La locomoción (Idle/Walk/Run) es la base
            // y nunca se interrumpe por Grounded/FreeFall, así que ya no puede quedarse cayendo.

            // Sentarse / tumbarse (bool)
            Held(sm, loco, "Sit", sit);
            Held(sm, loco, "Lie", lie);

            // Acciones de un disparo (trigger)
            OneShot(sm, loco, "Interact", inter);
            OneShot(sm, loco, "UseItem", use);
            OneShot(sm, loco, "Throw", thr);
            OneShot(sm, loco, "Pickup", pck);

            // Avatar + asignar en escena
            Avatar avatar = null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(florista))
                if (o is Avatar a) avatar = a;

            int n = 0;
            foreach (var smr in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!NameChainContains(smr.transform, "angry", "florista")) continue;
                // usa el Animator que ya conduce a la florista (en el Player o en el modelo)
                var anim = smr.GetComponentInParent<Animator>();
                if (anim == null) anim = Undo.AddComponent<Animator>(smr.transform.root.gameObject);
                anim.runtimeAnimatorController = ctrl;
                if (avatar != null) anim.avatar = avatar;
                anim.applyRootMotion = false;
                EditorUtility.SetDirty(anim);
                n++; break;
            }

            AssetDatabase.SaveAssets();
            Dlg($"Player_Anim creado (compatible con tu driver).\n\n" +
                $"Idle={Nm(idle)} Walk={Nm(walk)} Run={Nm(run)}\nJump={Nm(jStart)}/{Nm(jAir)}/{Nm(jLand)}\n" +
                $"Sit={Nm(sit)} Lie={Nm(lie)} Interact={Nm(inter)} Use={Nm(use)} Throw={Nm(thr)} Pickup={Nm(pck)}\n\n" +
                (n > 0 ? "Asignado a la florista en la escena." : "No encontré la florista en la escena.") +
                "\n\nLocomoción y salto los mueve tu PlayerAnimationDriver solo. Sit/Lie/Interact/etc. los disparas tú.");
            Debug.Log("[Sprout] Player_Anim (full) construido y asignado a " + n);
        }

        private static void OneShot(AnimatorStateMachine sm, AnimatorState back, string trig, AnimationClip clip)
        {
            if (clip == null) return;
            var st = sm.AddState(trig);
            st.motion = clip;
            var enter = sm.AddAnyStateTransition(st);
            enter.AddCondition(AnimatorConditionMode.If, 0, trig);
            enter.duration = 0.06f; enter.hasExitTime = false; enter.canTransitionToSelf = false;
            var exit = st.AddTransition(back);
            exit.hasExitTime = true; exit.exitTime = 0.85f; exit.duration = 0.1f;
        }

        private static void Held(AnimatorStateMachine sm, AnimatorState back, string boolName, AnimationClip clip)
        {
            if (clip == null) return;
            var st = sm.AddState(boolName);
            st.motion = clip;
            var enter = sm.AddAnyStateTransition(st);
            enter.AddCondition(AnimatorConditionMode.If, 0, boolName);
            enter.duration = 0.15f; enter.hasExitTime = false; enter.canTransitionToSelf = false;
            var exit = st.AddTransition(back);
            exit.AddCondition(AnimatorConditionMode.IfNot, 0, boolName);
            exit.duration = 0.15f; exit.hasExitTime = false;
        }

        private static AnimationClip Pick(List<AnimationClip> clips, params string[] keys)
        {
            foreach (var k in keys)
            {
                var c = clips.FirstOrDefault(x => x.name.ToLowerInvariant() == k)
                     ?? clips.FirstOrDefault(x => x.name.ToLowerInvariant().Contains(k));
                if (c != null) return c;
            }
            return null;
        }

        private static bool Looping(string name)
        {
            string n = name.ToLowerInvariant();
            return n.Contains("idle") || n.Contains("walk") || n.Contains("run") ||
                   n.Contains("sit") || n.Contains("lie") || n.Contains("sleep") ||
                   n.Contains("crawl") || n.Contains("sneak") || n.Contains("crouch");
        }

        private static bool NameChainContains(Transform t, params string[] keys)
        {
            while (t != null)
            {
                string n = t.name.ToLowerInvariant();
                foreach (var k in keys) if (n.Contains(k)) return true;
                t = t.parent;
            }
            return false;
        }

        private static string Nm(AnimationClip c) => c != null ? c.name : "—";
        private static void Dlg(string m) => EditorUtility.DisplayDialog("Sprout", m, "OK");
    }
}
#endif
