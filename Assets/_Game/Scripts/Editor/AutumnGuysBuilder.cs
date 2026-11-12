using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace AutumnGuys.EditorTools
{
    /// <summary>
    /// Generates the whole game skeleton: materials, prefabs, Splash / MainMenu / Level01 scenes and build settings.
    /// Menu: Autumn Guys/...
    /// WARNING: rebuilding a scene overwrites manual changes made to it.
    /// </summary>
    public static class AutumnGuysBuilder
    {
        const string Root = "Assets/_Game";
        const string ScenesDir = Root + "/Scenes";
        const string PrefabsDir = Root + "/Prefabs";
        const string MaterialsDir = Root + "/Materials";
        const string MeshesDir = Root + "/Meshes";

        const string CharacterPrefab = "Assets/Asset Store/PartyGameCharacters/Pack_FREE_PartyCharacters/Prefabs/Character_6dd688.prefab";
        const string PumpkinPrefab = "Assets/Asset Store/Halloween/Poly Halloween/Prefabs/Halloween/Pumpkin_A.prefab";
        const string PumpkinCarvedPrefab = "Assets/Asset Store/Halloween/Poly Halloween/Prefabs/Halloween/Pumpkin_Carved_Lit_A.prefab";
        const string AmanitaPrefab = "Assets/Asset Store/Halloween/Poly Halloween/Prefabs/Halloween/Mushroom_Magical_Amanita_Mature.prefab";
        static readonly string[] TreePrefabs =
        {
            "Assets/Asset Store/ForestPack/Prefabs/Tree_Autumn.prefab",
            "Assets/Asset Store/ForestPack/Prefabs/Tree_AutumnLight.prefab",
            "Assets/Asset Store/ForestPack/Prefabs/CircleTree_Autumn.prefab",
            "Assets/Asset Store/ForestPack/Prefabs/CircleTree_AutumnLight.prefab",
            "Assets/Asset Store/ForestPack/Prefabs/Tree_Autumn_Small.prefab",
        };
        static readonly string[] StandardShaderFolders =
        {
            "Assets/Asset Store/ForestPack",
            "Assets/Asset Store/Halloween",
        };

        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

        // ───────────────────────────── Menu ─────────────────────────────

        [MenuItem("Autumn Guys/Build Everything (overwrites scenes)", priority = 0)]
        public static void BuildAll()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Autumn Guys",
                    "Regenerate materials, prefabs and the Splash, MainMenu, Level01, LeafPile and HarvestMemory scenes?\n\nManual changes in these scenes will be overwritten!",
                    "Yes, regenerate", "Cancel"))
                return;

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Prepare();
            BuildSplashScene();
            BuildMenuScene();
            BuildLevelScene();
            BuildLeafPileScene();
            BuildMemoryScene();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();

            if (!Application.isBatchMode)
                EditorSceneManager.OpenScene($"{ScenesDir}/{SceneNames.Splash}.unity");
            Debug.Log("[AutumnGuys] Build Everything finished.");
        }

        [MenuItem("Autumn Guys/Rebuild Level01 only", priority = 20)]
        public static void BuildLevelOnly()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Autumn Guys", "Overwrite the Level01 scene?", "Yes", "Cancel")) return;
            Prepare();
            BuildLevelScene();
            ConfigureBuildSettings();
        }

        [MenuItem("Autumn Guys/Rebuild LeafPile only", priority = 21)]
        public static void BuildLeafPileOnly()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Autumn Guys", "Overwrite the LeafPile scene?", "Yes", "Cancel")) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Prepare();
            BuildLeafPileScene();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[AutumnGuys] LeafPile scene built.");
        }

        [MenuItem("Autumn Guys/Rebuild HarvestMemory only", priority = 22)]
        public static void BuildMemoryOnly()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Autumn Guys", "Overwrite the HarvestMemory scene?", "Yes", "Cancel")) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Prepare();
            BuildMemoryScene();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[AutumnGuys] HarvestMemory scene built.");
        }

        [MenuItem("Autumn Guys/Convert Standard materials to URP", priority = 40)]
        public static void ConvertStandardMaterialsMenu() => ConvertStandardMaterials();

        [MenuItem("Autumn Guys/Reset best time", priority = 60)]
        public static void ResetBestTime()
        {
            PlayerPrefs.DeleteAll();
            Debug.Log("[AutumnGuys] PlayerPrefs cleared.");
        }

        static void Prepare()
        {
            EnsureFolder(ScenesDir);
            EnsureFolder(PrefabsDir);
            EnsureFolder(MaterialsDir);
            ConfigureSprites();
            ConvertStandardMaterials();
            CreateMaterials();
            BuildPumpkinPrefab();
        }

        // ───────────────────────────── Assets ─────────────────────────────

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void ConfigureSprites()
        {
            foreach (var path in new[] { Root + "/Resources/TeamLogo.png", Root + "/Resources/GameTitle.png" })
            {
                if (AssetImporter.GetAtPath(path) is not TextureImporter ti) continue;
                if (ti.textureType == TextureImporterType.Sprite) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.SaveAndReimport();
            }
        }

        /// <summary>ForestPack and Poly Halloween ship with Built-in "Standard" materials which render pink in URP.</summary>
        static void ConvertStandardMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", StandardShaderFolders))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || mat.shader == null || !mat.shader.name.StartsWith("Standard")) continue;

                Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                Texture tex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                Vector2 tiling = mat.HasProperty("_MainTex") ? mat.GetTextureScale("_MainTex") : Vector2.one;
                bool emission = mat.IsKeywordEnabled("_EMISSION");
                Color emissionColor = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;
                Texture emissionMap = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap") : null;
                float smooth = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.2f;

                mat.shader = lit;
                mat.SetColor("_BaseColor", color);
                mat.SetTexture("_BaseMap", tex);
                mat.SetTextureScale("_BaseMap", tiling);
                mat.SetFloat("_Smoothness", Mathf.Min(smooth, 0.4f));
                if (emission)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", emissionColor);
                    mat.SetTexture("_EmissionMap", emissionMap);
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                EditorUtility.SetDirty(mat);
                count++;
            }
            if (count > 0) Debug.Log($"[AutumnGuys] Converted {count} Standard materials to URP Lit.");
        }

        static Material Mat(string name, Color color, float smoothness = 0.15f, Color? emission = null)
        {
            string path = $"{MaterialsDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
            }
            EditorUtility.SetDirty(mat);
            mats[name] = mat;
            return mat;
        }

        static void CreateMaterials()
        {
            Mat("Platform", new Color(0.93f, 0.52f, 0.18f));
            Mat("PlatformAlt", new Color(0.98f, 0.73f, 0.25f));
            Mat("PlatformDark", new Color(0.62f, 0.29f, 0.12f));
            Mat("Moving", new Color(0.85f, 0.25f, 0.15f));
            Mat("Falling", new Color(1f, 0.85f, 0.35f));
            Mat("Wood", new Color(0.45f, 0.27f, 0.13f));
            Mat("WoodDark", new Color(0.28f, 0.16f, 0.08f));
            Mat("Ground", new Color(0.42f, 0.24f, 0.12f), 0.05f);
            Mat("Checkpoint", new Color(0.9f, 0.3f, 0.2f));
            Mat("Finish", new Color(1f, 0.9f, 0.4f), 0.3f, new Color(0.4f, 0.3f, 0.05f));
            Mat("MushroomCap", new Color(0.85f, 0.12f, 0.1f), 0.4f);
            Mat("MushroomStem", new Color(0.95f, 0.9f, 0.8f));

            // Leaf Pile minigame
            Mat("LeafOrange", new Color(0.95f, 0.5f, 0.12f));
            Mat("LeafOrangeMound", new Color(1f, 0.62f, 0.2f));
            Mat("LeafRed", new Color(0.72f, 0.2f, 0.1f));
            Mat("LeafRedMound", new Color(0.85f, 0.3f, 0.14f));
            Mat("LeafGold", new Color(0.92f, 0.72f, 0.18f));
            Mat("LeafGoldMound", new Color(1f, 0.82f, 0.3f));
            Mat("AcornBody", new Color(0.6f, 0.36f, 0.14f), 0.35f);
            Mat("AcornCap", new Color(0.32f, 0.2f, 0.09f));
            Mat("GoldenAcorn", new Color(1f, 0.82f, 0.2f), 0.7f, new Color(0.6f, 0.42f, 0.05f));
            Mat("AcornMarker", new Color(0.12f, 0.04f, 0.02f), 0f, new Color(0.45f, 0.06f, 0.02f));

            // Harvest Memory
            Mat("MemoryTileA", new Color(0.55f, 0.36f, 0.2f));
            Mat("MemoryTileB", new Color(0.66f, 0.45f, 0.26f));
            Mat("SignBoard", new Color(0.95f, 0.88f, 0.7f));
            string leafIconPath = $"{MaterialsDir}/LeafIcon.mat";
            var leafIcon = AssetDatabase.LoadAssetAtPath<Material>(leafIconPath);
            if (leafIcon == null)
            {
                leafIcon = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(leafIcon, leafIconPath);
            }
            leafIcon.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/Leaf.png"));
            leafIcon.SetColor("_BaseColor", new Color(1f, 0.78f, 0.12f));
            leafIcon.SetFloat("_AlphaClip", 1f);
            leafIcon.SetFloat("_Cutoff", 0.5f);
            leafIcon.EnableKeyword("_ALPHATEST_ON");
            leafIcon.SetFloat("_Cull", 0f);
            leafIcon.SetFloat("_Smoothness", 0.2f);
            leafIcon.EnableKeyword("_EMISSION");
            leafIcon.SetColor("_EmissionColor", new Color(0.25f, 0.18f, 0.02f));
            leafIcon.SetTexture("_EmissionMap", leafIcon.GetTexture("_BaseMap"));
            EditorUtility.SetDirty(leafIcon);
            mats["LeafIcon"] = leafIcon;

            // Falling leaves particle material (alpha-clipped, double sided).
            string leafPath = $"{MaterialsDir}/Leaf.mat";
            var leaf = AssetDatabase.LoadAssetAtPath<Material>(leafPath);
            if (leaf == null)
            {
                leaf = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(leaf, leafPath);
            }
            leaf.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/Leaf.png"));
            leaf.SetFloat("_AlphaClip", 1f);
            leaf.SetFloat("_Cutoff", 0.5f);
            leaf.EnableKeyword("_ALPHATEST_ON");
            leaf.SetFloat("_Cull", 0f);
            EditorUtility.SetDirty(leaf);
            mats["Leaf"] = leaf;

            string confPath = $"{MaterialsDir}/Confetti.mat";
            var conf = AssetDatabase.LoadAssetAtPath<Material>(confPath);
            if (conf == null)
            {
                conf = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(conf, confPath);
            }
            conf.SetFloat("_Cull", 0f);
            mats["Confetti"] = conf;

            // Warm autumn sky.
            string skyPath = $"{MaterialsDir}/AutumnSky.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(sky, skyPath);
            }
            sky.SetColor("_SkyTint", new Color(0.75f, 0.55f, 0.45f));
            sky.SetColor("_GroundColor", new Color(0.35f, 0.22f, 0.15f));
            sky.SetFloat("_AtmosphereThickness", 1.4f);
            sky.SetFloat("_Exposure", 1.2f);
            mats["Sky"] = sky;
        }

        static GameObject pumpkinPrefabAsset;

        static void BuildPumpkinPrefab()
        {
            var go = new GameObject("RollingPumpkin");
            var col = go.AddComponent<SphereCollider>();
            col.radius = 0.9f;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 4f;
            rb.angularDamping = 0.05f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            var kb = go.AddComponent<KnockbackOnHit>();
            kb.force = 11f;
            kb.upwardForce = 5f;
            kb.stunDuration = 0.7f;

            var visual = SpawnVisual(PumpkinPrefab, go.transform);
            if (visual != null)
            {
                FitToSize(visual, 1.9f, false);
                CenterAt(visual, go.transform.position);
            }
            else
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(s.GetComponent<Collider>());
                s.transform.SetParent(go.transform, false);
                s.transform.localScale = Vector3.one * 1.8f;
                s.GetComponent<Renderer>().sharedMaterial = mats["Platform"];
            }

            pumpkinPrefabAsset = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabsDir}/RollingPumpkin.prefab");
            Object.DestroyImmediate(go);
        }

        static GameObject BuildPlayer(Vector3 position, Quaternion rotation)
        {
            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.SetPositionAndRotation(position, Quaternion.identity);

            var rb = player.AddComponent<Rigidbody>();
            rb.mass = 1f;
            var capsule = player.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.9f, 0f);
            capsule.height = 1.8f;
            capsule.radius = 0.4f;
            var pc = player.AddComponent<PlayerController>();

            var visual = SpawnVisual(CharacterPrefab, player.transform);
            if (visual != null)
            {
                visual.name = "Visual";
                FitToSize(visual, 1.75f, true);
                PlaceOnGround(visual, player.transform.position);
                var anim = visual.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.applyRootMotion = false;
                    anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    pc.animator = anim;
                }
            }
            else
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Object.DestroyImmediate(body.GetComponent<Collider>());
                body.transform.SetParent(player.transform, false);
                body.transform.localPosition = new Vector3(0, 0.9f, 0);
                body.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
                body.GetComponent<Renderer>().sharedMaterial = mats["Moving"];
            }

            SetLayerRecursive(player, 2); // Ignore Raycast → camera collision ignores the player
            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(player, $"{PrefabsDir}/Player.prefab", InteractionMode.AutomatedAction);
            player.transform.rotation = rotation;
            return player;
        }

        // ───────────────────────────── Scenes ─────────────────────────────

        static Scene NewScene(string name)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = name;
            return scene;
        }

        static void Save(Scene scene, string name) =>
            EditorSceneManager.SaveScene(scene, $"{ScenesDir}/{name}.unity");

        static void BuildSplashScene()
        {
            var scene = NewScene(SceneNames.Splash);
            var cam = CreateCamera(Vector3.zero, Quaternion.identity);
            var c = cam.GetComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = Color.black;
            new GameObject("Splash").AddComponent<SplashScreen>();
            Save(scene, SceneNames.Splash);
        }

        static void BuildMenuScene()
        {
            var scene = NewScene(SceneNames.MainMenu);
            SetupLighting();
            var env = new GameObject("Environment").transform;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ground.name = "Island";
            ground.transform.SetParent(env);
            ground.transform.position = new Vector3(0, -0.5f, 0);
            ground.transform.localScale = new Vector3(30, 0.5f, 30);
            ground.GetComponent<Renderer>().sharedMaterial = mats["Ground"];

            var character = SpawnVisual(CharacterPrefab, env);
            if (character != null)
            {
                character.name = "MenuCharacter";
                FitToSize(character, 1.75f, true);
                PlaceOnGround(character, new Vector3(1.6f, 0f, 0f));
                character.transform.rotation = Quaternion.Euler(0, 200f, 0);
            }

            SpawnDecor(PumpkinCarvedPrefab, env, new Vector3(3.2f, 0, 1.5f), 0.9f, 150f);
            SpawnDecor(PumpkinPrefab, env, new Vector3(-0.5f, 0, 2.5f), 0.7f, 30f);
            SpawnDecor(PumpkinPrefab, env, new Vector3(4.5f, 0, 4f), 1.1f, 80f);
            var rng = new System.Random(7);
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2f;
                float r = 9f + (float)rng.NextDouble() * 4f;
                var p = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r + 4f);
                if (p.z < 2f) continue; // keep the front open for the camera
                SpawnTree(env, p, 6f + (float)rng.NextDouble() * 5f, rng);
            }

            var cam = CreateCamera(new Vector3(0f, 1.6f, -6f), Quaternion.Euler(6f, 0f, 0f));
            AddFallingLeaves(cam.transform);

            new GameObject("MainMenu").AddComponent<MainMenu>();
            Save(scene, SceneNames.MainMenu);
        }

        static void BuildLevelScene()
        {
            var scene = NewScene(SceneNames.Level01);
            SetupLighting();

            var course = new GameObject("Course").transform;
            var decor = new GameObject("Decor").transform;

            // ── Start ──
            Box("Start_Platform", new Vector3(0, -0.5f, 5), new Vector3(14, 1, 16), mats["Platform"], course);
            var start = new GameObject("StartPoint").transform;
            start.position = new Vector3(0, 0.05f, 0);
            BuildBouncer(course, new Vector3(-5f, 0f, 8f));
            SpawnDecor(PumpkinCarvedPrefab, decor, new Vector3(5.5f, 0, -1.5f), 1f, 200f);
            SpawnDecor(PumpkinPrefab, decor, new Vector3(-5.5f, 0, -1.5f), 0.8f, 20f);

            // ── Section 1: moving platforms over a gap ──
            MovingBox("Moving_1", new Vector3(-4, -0.3f, 18), new Vector3(4, 0.6f, 4), new Vector3(8, 0, 0), 4f, 0f, course);
            MovingBox("Moving_2", new Vector3(4, -0.3f, 24), new Vector3(4, 0.6f, 4), new Vector3(-8, 0, 0), 4f, 0.25f, course);
            Box("Island_1", new Vector3(0, -0.5f, 30), new Vector3(5, 1, 4), mats["PlatformAlt"], course);
            MovingBox("Moving_3", new Vector3(-3, -0.3f, 36), new Vector3(4, 0.6f, 4), new Vector3(6, 0, 0), 3f, 0.5f, course);

            // ── Checkpoint 1 ──
            Box("CP1_Platform", new Vector3(0, -0.5f, 45), new Vector3(12, 1, 10), mats["Platform"], course);
            BuildCheckpoint(course, 1, new Vector3(0, 0.05f, 44));

            // ── Section 2: sweeping logs ──
            Box("Sweepers_Platform", new Vector3(0, -0.5f, 70), new Vector3(12, 1, 40), mats["PlatformAlt"], course);
            BuildSweeper(course, new Vector3(0, 0, 58), 70f);
            BuildSweeper(course, new Vector3(0, 0, 70), -95f);
            BuildSweeper(course, new Vector3(0, 0, 82), 120f);

            // ── Section 2b: rolling log bridge ──
            BuildRollingLog(course, new Vector3(0, -1.2f, 97), 14f, 1.2f, 35f);

            // ── Checkpoint 2 ──
            Box("CP2_Platform", new Vector3(0, -0.5f, 109), new Vector3(12, 1, 10), mats["Platform"], course);
            BuildCheckpoint(course, 2, new Vector3(0, 0.05f, 108));

            // ── Section 3: falling tiles ──
            var tiles = new GameObject("FallingTiles").transform;
            tiles.SetParent(course);
            for (int row = 0; row < 6; row++)
            for (int col = -1; col <= 1; col++)
            {
                var tile = Box($"Tile_{row}_{col + 1}", new Vector3(col * 3.6f, -0.25f, 117.5f + row * 3.6f),
                    new Vector3(3f, 0.5f, 3f), mats["Falling"], tiles);
                var rb = tile.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.mass = 5f;
                tile.AddComponent<FallingPlatform>();
            }

            // ── Checkpoint 3 ──
            Box("CP3_Platform", new Vector3(0, -0.5f, 144), new Vector3(14, 1, 10), mats["Platform"], course);
            BuildCheckpoint(course, 3, new Vector3(0, 0.05f, 143));

            // ── Section 4: pumpkin ramp ──
            const float rampLen = 40f, rampRise = 7f;
            float angle = Mathf.Atan2(rampRise, rampLen) * Mathf.Rad2Deg;
            float slopeLen = Mathf.Sqrt(rampLen * rampLen + rampRise * rampRise);
            var rampRot = Quaternion.Euler(-angle, 0, 0);
            Vector3 rampTop = new Vector3(0, rampRise * 0.5f, 149 + rampLen * 0.5f);
            Box("Ramp", rampTop - rampRot * Vector3.up * 0.5f, new Vector3(12, 1, slopeLen + 0.4f), mats["PlatformAlt"], course, rampRot);
            Box("Ramp_WallL", rampTop + rampRot * new Vector3(-6.25f, 0.6f, 0), new Vector3(0.5f, 1.2f, slopeLen), mats["WoodDark"], course, rampRot);
            Box("Ramp_WallR", rampTop + rampRot * new Vector3(6.25f, 0.6f, 0), new Vector3(0.5f, 1.2f, slopeLen), mats["WoodDark"], course, rampRot);

            var spawner = new GameObject("PumpkinSpawner").AddComponent<PumpkinSpawner>();
            spawner.transform.SetParent(course);
            spawner.transform.SetPositionAndRotation(new Vector3(0, rampRise + 2f, 149 + rampLen - 1f), Quaternion.Euler(0, 180, 0));
            spawner.pumpkinPrefab = pumpkinPrefabAsset;
            spawner.lateralSpread = 4.5f;

            // ── Finish ──
            Box("Finish_Platform", new Vector3(0, rampRise - 0.5f, 196), new Vector3(14, 1, 14), mats["Platform"], course);
            BuildFinish(course, new Vector3(0, rampRise, 194));
            SpawnDecor(PumpkinCarvedPrefab, decor, new Vector3(-5f, rampRise, 200f), 1.2f, 160f);
            SpawnDecor(PumpkinCarvedPrefab, decor, new Vector3(5f, rampRise, 200f), 1.2f, 200f);

            // ── Environment ──
            var ground = Box("ForestFloor", new Vector3(0, -15f, 100), new Vector3(400, 1, 500), mats["Ground"], decor);
            var rng = new System.Random(42);
            for (int i = 0; i < 70; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * (14f + (float)rng.NextDouble() * 35f);
                float z = -20f + (float)rng.NextDouble() * 240f;
                SpawnTree(decor, new Vector3(x, -14.5f, z), 14f + (float)rng.NextDouble() * 10f, rng);
            }

            // ── Player, camera, game manager ──
            var player = BuildPlayer(start.position, start.rotation);
            var camGo = CreateCamera(new Vector3(0, 3, -7), Quaternion.Euler(15, 0, 0));
            var cam = camGo.AddComponent<ThirdPersonCamera>();
            cam.target = player.transform;
            camGo.GetComponent<Camera>().farClipPlane = 600f;
            player.GetComponent<PlayerController>().cameraTransform = camGo.transform;
            AddFallingLeaves(camGo.transform);

            var gm = new GameObject("GameManager").AddComponent<GameManager>();
            gm.player = player.GetComponent<PlayerController>();
            gm.playerCamera = cam;
            gm.startPoint = start;
            gm.killHeight = -10f;

            Save(scene, SceneNames.Level01);
        }

        // ───────────────────────────── Leaf Pile minigame ─────────────────────────────

        const float HexLayoutRadius = 1.5f;   // centre spacing
        const float HexGap = 0.12f;           // gap between neighbouring tiles
        const float HexHeight = 0.6f;
        const int HexRings = 5;               // 1 + 3*N*(N+1) = 91 tiles per floor
        static readonly float[] LeafFloorHeights = { 0f, -8f, -16f };

        static void BuildLeafPileScene()
        {
            var scene = NewScene(SceneNames.LeafPile);
            SetupLighting();

            var hexMesh = BuildHexMeshAsset();
            var acornPrefab = BuildAcornPrefab("FallingAcorn", 1.4f, mats["AcornBody"], mats["AcornCap"], true);
            var goldenPrefab = BuildAcornPrefab("GoldenAcorn", 0.8f, mats["GoldenAcorn"], mats["GoldenAcorn"], false);

            var arenaRoot = new GameObject("Arena").transform;
            var decor = new GameObject("Decor").transform;

            // ── Floors ──
            string[] tileMats = { "LeafOrange", "LeafRed", "LeafGold" };
            var rng = new System.Random(1234);
            for (int f = 0; f < LeafFloorHeights.Length; f++)
            {
                var floorRoot = new GameObject($"Floor_{f}").transform;
                floorRoot.SetParent(arenaRoot, false);
                string m = tileMats[f % tileMats.Length];
                BuildHexFloor(floorRoot, f, LeafFloorHeights[f], hexMesh, mats[m], mats[m + "Mound"], rng);
            }

            var start = new GameObject("StartPoint").transform;
            start.position = new Vector3(0f, LeafFloorHeights[0] + 0.05f, 0f);

            // ── Game logic ──
            var arenaGo = new GameObject("LeafPileArena");
            var arena = arenaGo.AddComponent<LeafPileArena>();
            arena.floorHeights = (float[])LeafFloorHeights.Clone();

            var dropper = arenaGo.AddComponent<AcornDropper>();
            dropper.acornPrefab = acornPrefab.GetComponent<FallingAcorn>();
            dropper.markerMaterial = mats["AcornMarker"];

            var wind = arenaGo.AddComponent<WindGust>();
            wind.leafBurst = BuildLeafBurst(arenaGo.transform, wind.gustDuration);

            var golden = arenaGo.AddComponent<GoldenAcornSpawner>();
            golden.goldenAcornPrefab = goldenPrefab.GetComponent<GoldenAcorn>();

            // ── Environment ──
            float groundY = LeafFloorHeights[LeafFloorHeights.Length - 1] - 16f;
            Box("ForestFloor", new Vector3(0, groundY - 0.5f, 0), new Vector3(400, 1, 400), mats["Ground"], decor);
            for (int i = 0; i < 60; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = 24f + (float)rng.NextDouble() * 50f;
                SpawnTree(decor, new Vector3(Mathf.Cos(a) * r, groundY, Mathf.Sin(a) * r), 22f + (float)rng.NextDouble() * 14f, rng);
            }
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f + 0.3f;
                float r = 16f + (float)rng.NextDouble() * 6f;
                SpawnDecor(i % 3 == 0 ? PumpkinCarvedPrefab : PumpkinPrefab, decor,
                    new Vector3(Mathf.Cos(a) * r, groundY, Mathf.Sin(a) * r), 1.5f + (float)rng.NextDouble() * 1.5f, (float)rng.NextDouble() * 360f);
            }

            // ── Player, camera, game manager ──
            var player = BuildPlayer(start.position, start.rotation);
            var camGo = CreateCamera(start.position + new Vector3(0, 3, -7), Quaternion.Euler(15, 0, 0));
            var cam = camGo.AddComponent<ThirdPersonCamera>();
            cam.target = player.transform;
            camGo.GetComponent<Camera>().farClipPlane = 600f;
            player.GetComponent<PlayerController>().cameraTransform = camGo.transform;
            AddFallingLeaves(camGo.transform);

            var gm = new GameObject("GameManager").AddComponent<GameManager>();
            gm.mode = GameManager.LevelMode.Survival;
            gm.player = player.GetComponent<PlayerController>();
            gm.playerCamera = cam;
            gm.startPoint = start;
            gm.killHeight = LeafFloorHeights[LeafFloorHeights.Length - 1] - 8f;

            Save(scene, SceneNames.LeafPile);
        }

        static void BuildHexFloor(Transform parent, int floor, float topY, Mesh hexMesh, Material tileMat, Material moundMat, System.Random rng)
        {
            float sqrt3 = Mathf.Sqrt(3f);
            for (int q = -HexRings; q <= HexRings; q++)
            {
                int rMin = Mathf.Max(-HexRings, -q - HexRings);
                int rMax = Mathf.Min(HexRings, -q + HexRings);
                for (int r = rMin; r <= rMax; r++)
                {
                    // Pointy-top axial layout.
                    var pos = new Vector3(sqrt3 * HexLayoutRadius * (q + r * 0.5f), topY - HexHeight * 0.5f, 1.5f * HexLayoutRadius * r);

                    var tile = new GameObject($"Tile_{floor}_{q}_{r}");
                    tile.transform.SetParent(parent, false);
                    tile.transform.position = pos;
                    tile.AddComponent<MeshFilter>().sharedMesh = hexMesh;
                    tile.AddComponent<MeshRenderer>().sharedMaterial = tileMat;
                    var mc = tile.AddComponent<MeshCollider>();
                    mc.sharedMesh = hexMesh;
                    mc.convex = true;
                    var rb = tile.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                    rb.mass = 5f;
                    tile.AddComponent<LeafTile>().floor = floor;

                    // Soft leaf mound on top (visual only).
                    var mound = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    mound.name = "LeafMound";
                    Object.DestroyImmediate(mound.GetComponent<Collider>());
                    mound.transform.SetParent(tile.transform, false);
                    float s = 2.1f + (float)rng.NextDouble() * 0.3f;
                    mound.transform.localPosition = new Vector3(0f, HexHeight * 0.5f, 0f);
                    mound.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                    mound.transform.localScale = new Vector3(s, 0.22f, s * 0.92f);
                    mound.GetComponent<Renderer>().sharedMaterial = moundMat;
                }
            }
        }

        /// <summary>Hexagonal prism (pointy-top) with flat-shaded sides, saved as a mesh asset.</summary>
        static Mesh BuildHexMeshAsset()
        {
            EnsureFolder(MeshesDir);
            float radius = HexLayoutRadius - HexGap / Mathf.Sqrt(3f);
            float h = HexHeight * 0.5f;

            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            Vector3 Corner(int i, float y)
            {
                float a = Mathf.Deg2Rad * (60f * i - 30f);
                return new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius);
            }

            // Caps
            foreach (bool top in new[] { true, false })
            {
                float y = top ? h : -h;
                Vector3 n = top ? Vector3.up : Vector3.down;
                int c = verts.Count;
                verts.Add(new Vector3(0, y, 0)); normals.Add(n); uvs.Add(new Vector2(0.5f, 0.5f));
                for (int i = 0; i < 6; i++)
                {
                    var p = Corner(i, y);
                    verts.Add(p); normals.Add(n); uvs.Add(new Vector2(0.5f + p.x / (2f * radius), 0.5f + p.z / (2f * radius)));
                }
                for (int i = 0; i < 6; i++)
                {
                    int a = c + 1 + i, b = c + 1 + (i + 1) % 6;
                    if (top) tris.AddRange(new[] { c, b, a });
                    else tris.AddRange(new[] { c, a, b });
                }
            }

            // Sides
            for (int i = 0; i < 6; i++)
            {
                int j = (i + 1) % 6;
                float mid = Mathf.Deg2Rad * (60f * i);
                var n = new Vector3(Mathf.Cos(mid), 0f, Mathf.Sin(mid));
                int c = verts.Count;
                verts.Add(Corner(i, h)); verts.Add(Corner(j, h)); verts.Add(Corner(j, -h)); verts.Add(Corner(i, -h));
                for (int k = 0; k < 4; k++) normals.Add(n);
                uvs.Add(new Vector2(0, 1)); uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(1, 0)); uvs.Add(new Vector2(0, 0));
                tris.AddRange(new[] { c, c + 1, c + 2, c, c + 2, c + 3 });
            }

            var mesh = new Mesh { name = "HexTile" };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            string path = $"{MeshesDir}/HexTile.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }
            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        /// <summary>Acorn from primitives. falling = hazard (FallingAcorn), otherwise bonus pickup (GoldenAcorn).</summary>
        static GameObject BuildAcornPrefab(string name, float size, Material bodyMat, Material capMat, bool falling)
        {
            var go = new GameObject(name);
            AddAcornVisual(go.transform, Vector3.zero, size, bodyMat, capMat);

            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            if (falling)
            {
                col.radius = 0.5f * size;
                var rb = go.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                go.AddComponent<FallingAcorn>();
            }
            else
            {
                col.radius = 1.1f; // generous pickup radius
                go.AddComponent<GoldenAcorn>();
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabsDir}/{name}.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>Acorn made of primitives (no colliders), centred on <paramref name="center"/>.</summary>
        static void AddAcornVisual(Transform parent, Vector3 center, float size, Material bodyMat, Material capMat)
        {
            void Part(PrimitiveType type, string partName, Vector3 localPos, Vector3 scale, Material mat)
            {
                var p = GameObject.CreatePrimitive(type);
                p.name = partName;
                Object.DestroyImmediate(p.GetComponent<Collider>());
                p.transform.SetParent(parent, false);
                p.transform.localPosition = center + localPos * size;
                p.transform.localScale = scale * size;
                p.GetComponent<Renderer>().sharedMaterial = mat;
            }

            Part(PrimitiveType.Sphere, "Body", new Vector3(0, -0.08f, 0), new Vector3(0.78f, 0.95f, 0.78f), bodyMat);
            Part(PrimitiveType.Sphere, "Cap", new Vector3(0, 0.25f, 0), new Vector3(0.88f, 0.45f, 0.88f), capMat);
            Part(PrimitiveType.Cylinder, "Stem", new Vector3(0, 0.52f, 0), new Vector3(0.1f, 0.1f, 0.1f), capMat);
        }

        /// <summary>Gust of leaves blowing along the local +Z axis (WindGust positions and plays it).</summary>
        static ParticleSystem BuildLeafBurst(Transform parent, float duration)
        {
            var go = new GameObject("WindLeafBurst");
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = duration;
            main.startLifetime = 2.2f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 16f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.95f, 0.45f, 0.1f), new Color(0.75f, 0.15f, 0.08f));
            var emission = ps.emission;
            emission.rateOverTime = 90f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(16f, 5f, 1f);
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.separateAxes = true;
            rot.x = new ParticleSystem.MinMaxCurve(-4f, 4f);
            rot.y = new ParticleSystem.MinMaxCurve(-4f, 4f);
            rot.z = new ParticleSystem.MinMaxCurve(-4f, 4f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 1.2f;
            noise.frequency = 0.4f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mats["Leaf"];
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            r.alignment = ParticleSystemRenderSpace.World;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return ps;
        }

        // ───────────────────────────── Harvest Memory minigame ─────────────────────────────

        const int MemoryGridSize = 5;
        const float MemoryTileSize = 3f;
        const float MemoryTileGap = 0.35f;
        const float MemoryTileHeight = 0.6f;

        static void BuildMemoryScene()
        {
            var scene = NewScene(SceneNames.HarvestMemory);
            SetupLighting();

            var arenaRoot = new GameObject("Arena").transform;
            var decor = new GameObject("Decor").transform;

            // ── Icon prefabs ──
            var iconPumpkin = BuildIconPrefab("Icon_Pumpkin", go =>
            {
                var v = SpawnVisual(PumpkinPrefab, go.transform);
                if (v == null) return;
                FitToSize(v, 1.4f, false);
                PlaceOnGround(v, Vector3.zero);
            });
            var iconMushroom = BuildIconPrefab("Icon_Mushroom", go =>
            {
                var v = SpawnVisual(AmanitaPrefab, go.transform);
                if (v == null) return;
                FitToSize(v, 1.7f, true);
                PlaceOnGround(v, Vector3.zero);
            });
            var iconAcorn = BuildIconPrefab("Icon_Acorn", go =>
                AddAcornVisual(go.transform, new Vector3(0f, 0.56f * 1.3f, 0f), 1.3f, mats["AcornBody"], mats["AcornCap"]));
            var iconLeaf = BuildIconPrefab("Icon_Leaf", go =>
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "Leaf";
                Object.DestroyImmediate(q.GetComponent<Collider>());
                q.transform.SetParent(go.transform, false);
                q.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                q.transform.localScale = Vector3.one * 1.8f;
                q.GetComponent<Renderer>().sharedMaterial = mats["LeafIcon"];
            });

            // ── Tile grid ──
            float step = MemoryTileSize + MemoryTileGap;
            float half = (MemoryGridSize - 1) * 0.5f;
            for (int x = 0; x < MemoryGridSize; x++)
            for (int z = 0; z < MemoryGridSize; z++)
            {
                var tile = new GameObject($"MemoryTile_{x}_{z}");
                tile.transform.SetParent(arenaRoot, false);
                tile.transform.position = new Vector3((x - half) * step, -MemoryTileHeight * 0.5f, (z - half) * step);
                var box = tile.AddComponent<BoxCollider>();
                box.size = new Vector3(MemoryTileSize, MemoryTileHeight, MemoryTileSize);
                var rb = tile.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.mass = 5f;

                var visual = Box("Visual", Vector3.zero, box.size, mats[(x + z) % 2 == 0 ? "MemoryTileA" : "MemoryTileB"], tile.transform);
                visual.transform.localPosition = Vector3.zero;
                Object.DestroyImmediate(visual.GetComponent<Collider>());

                var anchor = new GameObject("IconAnchor").transform;
                anchor.SetParent(tile.transform, false);
                anchor.localPosition = new Vector3(0f, MemoryTileHeight * 0.5f, 0f);
                tile.AddComponent<MemoryTile>().iconAnchor = anchor;
            }

            // ── Sign that shows the target icon ──
            float signZ = (half + 0.5f) * step + 3.5f;
            var sign = new GameObject("TargetSign").transform;
            sign.SetParent(arenaRoot, false);
            sign.position = new Vector3(0f, 0f, signZ);
            var board = Box("Board", Vector3.zero, new Vector3(6.5f, 5f, 0.3f), mats["SignBoard"], sign);
            board.transform.localPosition = new Vector3(0f, 5.8f, 0.4f);
            var frame = Box("Frame", Vector3.zero, new Vector3(7.1f, 5.6f, 0.25f), mats["WoodDark"], sign);
            frame.transform.localPosition = new Vector3(0f, 5.8f, 0.6f);
            foreach (float px in new[] { -3.3f, 3.3f })
            {
                var post = Box("Post", Vector3.zero, new Vector3(0.45f, 14f, 0.45f), mats["Wood"], sign);
                post.transform.localPosition = new Vector3(px, 1f, 0.6f);
            }
            var displayAnchor = new GameObject("DisplayAnchor").transform;
            displayAnchor.SetParent(sign, false);
            displayAnchor.localPosition = new Vector3(0f, 3.9f, -0.6f);

            // ── Game logic ──
            var arena = new GameObject("MemoryArena").AddComponent<MemoryArena>();
            arena.displayAnchor = displayAnchor;
            arena.icons = new[]
            {
                new MemoryArena.Icon { name = "PUMPKIN", prefab = iconPumpkin, color = new Color(1f, 0.55f, 0.1f) },
                new MemoryArena.Icon { name = "MUSHROOM", prefab = iconMushroom, color = new Color(1f, 0.3f, 0.25f) },
                new MemoryArena.Icon { name = "ACORN", prefab = iconAcorn, color = new Color(0.85f, 0.6f, 0.35f) },
                new MemoryArena.Icon { name = "LEAF", prefab = iconLeaf, color = new Color(1f, 0.85f, 0.2f) },
            };

            // ── Environment ──
            const float groundY = -20f;
            Box("ForestFloor", new Vector3(0, groundY - 0.5f, 0), new Vector3(400, 1, 400), mats["Ground"], decor);
            var rng = new System.Random(77);
            for (int i = 0; i < 55; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = 22f + (float)rng.NextDouble() * 50f;
                SpawnTree(decor, new Vector3(Mathf.Cos(a) * r, groundY, Mathf.Sin(a) * r), 20f + (float)rng.NextDouble() * 12f, rng);
            }
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f;
                float r = 14f + (float)rng.NextDouble() * 6f;
                SpawnDecor(i % 3 == 0 ? PumpkinCarvedPrefab : PumpkinPrefab, decor,
                    new Vector3(Mathf.Cos(a) * r, groundY, Mathf.Sin(a) * r), 1.5f + (float)rng.NextDouble() * 1.5f, (float)rng.NextDouble() * 360f);
            }

            // ── Player, camera, game manager ──
            var start = new GameObject("StartPoint").transform;
            start.position = new Vector3(0f, 0.05f, 0f);
            var player = BuildPlayer(start.position, start.rotation);
            var camGo = CreateCamera(start.position + new Vector3(0, 3, -7), Quaternion.Euler(15, 0, 0));
            var cam = camGo.AddComponent<ThirdPersonCamera>();
            cam.target = player.transform;
            camGo.GetComponent<Camera>().farClipPlane = 600f;
            player.GetComponent<PlayerController>().cameraTransform = camGo.transform;
            AddFallingLeaves(camGo.transform);

            var gm = new GameObject("GameManager").AddComponent<GameManager>();
            gm.mode = GameManager.LevelMode.Survival;
            gm.scoreType = GameManager.ScoreType.Rounds;
            gm.player = player.GetComponent<PlayerController>();
            gm.playerCamera = cam;
            gm.startPoint = start;
            gm.killHeight = -8f;

            Save(scene, SceneNames.HarvestMemory);
        }

        /// <summary>Visual-only icon prefab; <paramref name="build"/> adds the model under the root (base at the origin).</summary>
        static GameObject BuildIconPrefab(string name, System.Action<GameObject> build)
        {
            var go = new GameObject(name);
            build(go);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.On;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabsDir}/{name}.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ───────────────────────────── Level pieces ─────────────────────────────

        static GameObject Box(string name, Vector3 pos, Vector3 size, Material mat, Transform parent, Quaternion? rot = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, rot ?? Quaternion.identity);
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static GameObject Cylinder(string name, Transform parent, Vector3 localPos, Quaternion localRot, Vector3 scale, Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static void MovingBox(string name, Vector3 pos, Vector3 size, Vector3 offset, float period, float phase, Transform parent)
        {
            var go = Box(name, pos, size, mats["Moving"], parent);
            go.AddComponent<Rigidbody>().isKinematic = true;
            var mp = go.AddComponent<MovingPlatform>();
            mp.offset = offset;
            mp.period = period;
            mp.phase = phase;
        }

        static void BuildSweeper(Transform parent, Vector3 pos, float speed)
        {
            var root = new GameObject("Sweeper");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            root.AddComponent<Rigidbody>().isKinematic = true;
            root.AddComponent<Rotator>().speed = speed;
            var kb = root.AddComponent<KnockbackOnHit>();
            kb.force = 13f;
            kb.upwardForce = 6f;

            Cylinder("Post", root.transform, new Vector3(0, 1f, 0), Quaternion.identity, new Vector3(1.2f, 1f, 1.2f), mats["WoodDark"]);
            Cylinder("Log", root.transform, new Vector3(0, 0.55f, 0), Quaternion.Euler(0, 0, 90), new Vector3(0.7f, 5.6f, 0.7f), mats["Wood"]);
        }

        static void BuildRollingLog(Transform parent, Vector3 center, float length, float radius, float speed)
        {
            var log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.name = "RollingLog";
            log.transform.SetParent(parent, false);
            log.transform.SetPositionAndRotation(center, Quaternion.Euler(90, 0, 0));
            log.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);
            log.GetComponent<Renderer>().sharedMaterial = mats["Wood"];

            // Flat-ended collider instead of the rounded capsule.
            var mesh = log.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(log.GetComponent<Collider>());
            var mc = log.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = true;

            log.AddComponent<Rigidbody>().isKinematic = true;
            var rot = log.AddComponent<Rotator>();
            rot.localAxis = Vector3.up;
            rot.speed = speed;

            // Bark stripes so the rotation is visible.
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = "Bark";
                Object.DestroyImmediate(stripe.GetComponent<Collider>());
                stripe.transform.SetParent(log.transform, false);
                stripe.transform.localPosition = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.49f;
                stripe.transform.localRotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0);
                stripe.transform.localScale = new Vector3(0.04f, 0.98f, 0.12f);
                stripe.GetComponent<Renderer>().sharedMaterial = mats["WoodDark"];
            }
        }

        static void BuildCheckpoint(Transform parent, int index, Vector3 pos)
        {
            var cp = new GameObject($"Checkpoint_{index}");
            cp.transform.SetParent(parent, false);
            cp.transform.position = pos;
            var box = cp.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0, 2f, 0);
            box.size = new Vector3(12f, 4f, 3f);
            var c = cp.AddComponent<Checkpoint>();
            c.index = index;

            var flags = new List<Renderer>();
            foreach (float x in new[] { -5.6f, 5.6f })
            {
                Cylinder("Pole", cp.transform, new Vector3(x, 1.5f, 0), Quaternion.identity, new Vector3(0.15f, 1.5f, 0.15f), mats["WoodDark"], false);
                var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                flag.name = "Flag";
                Object.DestroyImmediate(flag.GetComponent<Collider>());
                flag.transform.SetParent(cp.transform, false);
                flag.transform.localPosition = new Vector3(x - Mathf.Sign(x) * 0.6f, 2.6f, 0);
                flag.transform.localScale = new Vector3(1.1f, 0.7f, 0.05f);
                flag.GetComponent<Renderer>().sharedMaterial = mats["Checkpoint"];
                flags.Add(flag.GetComponent<Renderer>());
            }
            c.indicators = flags.ToArray();
        }

        static void BuildFinish(Transform parent, Vector3 pos)
        {
            var finish = new GameObject("Finish");
            finish.transform.SetParent(parent, false);
            finish.transform.position = pos;
            var box = finish.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0, 2.5f, 0);
            box.size = new Vector3(12f, 5f, 1.5f);
            var fl = finish.AddComponent<FinishLine>();

            foreach (float x in new[] { -6.2f, 6.2f })
            {
                var pillar = Box("Pillar", Vector3.zero, new Vector3(1f, 6f, 1f), mats["Finish"], finish.transform);
                pillar.transform.localPosition = new Vector3(x, 3f, 0);
            }
            var banner = Box("Banner", Vector3.zero, new Vector3(13.4f, 1.4f, 0.6f), mats["Moving"], finish.transform);
            banner.transform.localPosition = new Vector3(0, 6.2f, 0);

            // Confetti
            var confGo = new GameObject("Confetti");
            confGo.transform.SetParent(finish.transform, false);
            confGo.transform.localPosition = new Vector3(0, 6f, 1f);
            confGo.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            var ps = confGo.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = 4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(8f, 16f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.gravityModifier = 0.6f;
            main.startRotation3D = true;
            main.maxParticles = 500;
            var grad = new Gradient();
            grad.SetKeys(new[]
            {
                new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0f),
                new GradientColorKey(new Color(1f, 0.35f, 0.1f), 0.33f),
                new GradientColorKey(new Color(0.3f, 0.8f, 0.3f), 0.66f),
                new GradientColorKey(new Color(0.9f, 0.15f, 0.2f), 1f),
            }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(grad) { mode = ParticleSystemGradientMode.RandomColor };
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 300) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 4f;
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.separateAxes = true;
            rot.x = new ParticleSystem.MinMaxCurve(-6f, 6f);
            rot.y = new ParticleSystem.MinMaxCurve(-6f, 6f);
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = mats["Confetti"];
            fl.confetti = ps;
        }

        static void BuildBouncer(Transform parent, Vector3 pos)
        {
            var root = new GameObject("MushroomBouncer");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            Cylinder("Stem", root.transform, new Vector3(0, 0.5f, 0), Quaternion.identity, new Vector3(0.8f, 0.5f, 0.8f), mats["MushroomStem"]);
            var cap = Cylinder("Cap", root.transform, new Vector3(0, 1.1f, 0), Quaternion.identity, new Vector3(2.6f, 0.15f, 2.6f), mats["MushroomCap"]);
            cap.AddComponent<Bouncer>();
        }

        // ───────────────────────────── Environment helpers ─────────────────────────────

        static void SetupLighting()
        {
            var lightGo = new GameObject("Sun");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.86f, 0.68f);
            light.intensity = 1.4f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            RenderSettings.sun = light;

            RenderSettings.skybox = mats["Sky"];
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.85f, 0.68f, 0.55f);
            RenderSettings.ambientEquatorColor = new Color(0.6f, 0.45f, 0.35f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.2f, 0.14f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.92f, 0.7f, 0.52f);
            RenderSettings.fogDensity = 0.006f;
        }

        static GameObject CreateCamera(Vector3 pos, Quaternion rot)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.SetPositionAndRotation(pos, rot);
            go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            return go;
        }

        static void AddFallingLeaves(Transform cam)
        {
            var go = new GameObject("FallingLeaves");
            go.transform.SetParent(cam, false);
            go.transform.localPosition = new Vector3(0, 8f, 8f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.prewarm = true;
            main.startLifetime = 7f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.4f);
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0.04f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 600;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.95f, 0.45f, 0.1f), new Color(0.75f, 0.15f, 0.08f));
            var emission = ps.emission;
            emission.rateOverTime = 45f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(40f, 1f, 40f);
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.separateAxes = true;
            rot.x = new ParticleSystem.MinMaxCurve(-2f, 2f);
            rot.y = new ParticleSystem.MinMaxCurve(-2f, 2f);
            rot.z = new ParticleSystem.MinMaxCurve(-2f, 2f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.8f;
            noise.frequency = 0.3f;
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
            vel.y = new ParticleSystem.MinMaxCurve(-1.2f, -0.6f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mats["Leaf"];
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            r.alignment = ParticleSystemRenderSpace.World;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }

        static void SpawnTree(Transform parent, Vector3 groundPos, float height, System.Random rng)
        {
            var tree = SpawnPrefab(TreePrefabs[rng.Next(TreePrefabs.Length)], parent);
            if (tree == null) return;
            tree.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);
            FitToSize(tree, height, true);
            PlaceOnGround(tree, groundPos);
        }

        static void SpawnDecor(string prefabPath, Transform parent, Vector3 groundPos, float size, float yaw)
        {
            var go = SpawnVisual(prefabPath, parent);
            if (go == null) return;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            FitToSize(go, size, true);
            PlaceOnGround(go, groundPos);
        }

        static GameObject SpawnPrefab(string path, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[AutumnGuys] Prefab not found: {path}");
                return null;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            return go;
        }

        /// <summary>Instantiates a prefab as pure visual: unpacked, without colliders, rigidbodies or scripts.</summary>
        static GameObject SpawnVisual(string path, Transform parent)
        {
            var go = SpawnPrefab(path, parent);
            if (go == null) return null;
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(mb);
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rb);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            go.transform.localPosition = Vector3.zero;
            return go;
        }

        static Bounds GetBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        static void FitToSize(GameObject go, float size, bool byHeight)
        {
            var b = GetBounds(go);
            float current = byHeight ? b.size.y : Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (current < 0.0001f) return;
            go.transform.localScale *= size / current;
        }

        static void PlaceOnGround(GameObject go, Vector3 groundPoint)
        {
            var b = GetBounds(go);
            go.transform.position += groundPoint - new Vector3(b.center.x, b.min.y, b.center.z);
        }

        static void CenterAt(GameObject go, Vector3 point)
        {
            var b = GetBounds(go);
            go.transform.position += point - b.center;
        }

        static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursive(t.gameObject, layer);
        }

        // ───────────────────────────── Build settings ─────────────────────────────

        static void ConfigureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var name in new[] { SceneNames.Splash, SceneNames.MainMenu, SceneNames.Level01, SceneNames.LeafPile, SceneNames.HarvestMemory })
            {
                string path = $"{ScenesDir}/{name}.unity";
                if (File.Exists(path)) scenes.Add(new EditorBuildSettingsScene(path, true));
                else Debug.LogWarning($"[AutumnGuys] Scene {path} not built yet – not added to Build Settings.");
            }
            EditorBuildSettings.scenes = scenes.ToArray();

            PlayerSettings.companyName = "Gin & Tonic";
            PlayerSettings.productName = "Autumn Guys";
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Resources/TeamLogo.png");
            if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            AssetDatabase.SaveAssets();
        }
    }
}
