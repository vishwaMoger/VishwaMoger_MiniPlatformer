using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace MiniPlatformer.EditorTools
{
    /// <summary>
    /// One-click project configuration: Tools > Fruit Quest > Run Full Setup.
    /// Also callable from the Unity CLI: unity command eval "MiniPlatformer.EditorTools.ProjectSetup.RunAll();"
    /// </summary>
    public static class ProjectSetup
    {
        const string Root = "Assets/_Game";
        const string Art = Root + "/Art";
        const string Audio = Root + "/Audio";
        const string Rendering = Root + "/Rendering";
        const string AssetsPath = Root + "/Resources/GameAssets.asset";
        const string ScenePath = Root + "/Scenes/Game.unity";
        const string FontPath = Art + "/Fonts/LilitaOne SDF.asset";

        static readonly Regex SizeRx = new Regex(@"\((\d+)x(\d+)\)");

        [MenuItem("Tools/Fruit Quest/Run Full Setup")]
        public static void RunAll()
        {
            ConfigureLayers();
            ConfigurePlayer();
            ImportTextures();
            ImportAudio();
            CreateFont();
            CreateMaterials();
            SetupURP();
            BuildGameAssets();
            CreateScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[FruitQuest] Full setup complete.");
        }

        [MenuItem("Tools/Fruit Quest/Rebuild Asset Registry")]
        public static void RebuildRegistry()
        {
            BuildGameAssets();
            AssetDatabase.SaveAssets();
            Debug.Log("[FruitQuest] GameAssets rebuilt.");
        }

        // ------------------------------------------------------------------ layers & player

        static void ConfigureLayers()
        {
            var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tm.FindProperty("layers");
            for (int i = 0; i < Layers.Names.Length; i++)
                if (!string.IsNullOrEmpty(Layers.Names[i])) layers.GetArrayElementAtIndex(i).stringValue = Layers.Names[i];
            tm.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.productName = "Fruit Quest";
            if (PlayerSettings.colorSpace != ColorSpace.Linear) PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.runInBackground = false;
        }

        // ------------------------------------------------------------------ textures

        static void ImportTextures()
        {
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Art });
            int n = 0;
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;
                if (path.Contains("/Branding/")) continue;
                ConfigureTexture(ti, path, factory);
                n++;
            }
            Debug.Log($"[FruitQuest] Configured {n} textures.");
        }

        static void ConfigureTexture(TextureImporter ti, string path, SpriteDataProviderFactories factory)
        {
            bool isBg = path.Contains("/Backgrounds/");
            bool isUi = path.Contains("/UI/");
            ti.textureType = TextureImporterType.Sprite;
            ti.spritePixelsPerUnit = K.PPU;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.isReadable = false;
            ti.npotScale = TextureImporterNPOTScale.None;

            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteGenerateFallbackPhysicsShape = false;
            s.spriteAlignment = (int)SpriteAlignment.Center;
            s.spriteExtrude = 0;
            ti.SetTextureSettings(s);

            if (isBg)
            {
                ti.wrapModeU = TextureWrapMode.Repeat;
                ti.wrapModeV = TextureWrapMode.Clamp;
            }
            else ti.wrapMode = TextureWrapMode.Clamp;

            ti.GetSourceTextureWidthAndHeight(out int w, out int h);
            string file = Path.GetFileNameWithoutExtension(path);
            int fw = 0, fh = 0;
            var m = SizeRx.Match(file);
            if (m.Success && !isBg && !isUi) { fw = int.Parse(m.Groups[1].Value); fh = int.Parse(m.Groups[2].Value); }
            else if (path.Contains("/Fruits/") && h > 0 && w % h == 0 && w > h) { fw = h; fh = h; }

            if (fw > 0 && fh > 0 && (fw < w || fh < h))
            {
                ti.spriteImportMode = SpriteImportMode.Multiple;
                var dp = factory.GetSpriteEditorDataProviderFromObject(ti);
                dp.InitSpriteEditorDataProvider();
                var old = new Dictionary<string, SpriteRect>();
                foreach (var r in dp.GetSpriteRects()) old[r.name] = r;
                int cols = w / fw, rows = h / fh;
                var rects = new List<SpriteRect>(cols * rows);
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                    {
                        string name = $"{file}_{r * cols + c}";
                        rects.Add(new SpriteRect
                        {
                            name = name,
                            rect = new Rect(c * fw, h - (r + 1) * fh, fw, fh),
                            alignment = SpriteAlignment.Center,
                            pivot = new Vector2(0.5f, 0.5f),
                            spriteID = old.TryGetValue(name, out var prev) ? prev.spriteID : GUID.Generate(),
                        });
                    }
                dp.SetSpriteRects(rects.ToArray());
                var ids = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
                ids?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
                dp.Apply();
            }
            else
            {
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePivot = new Vector2(0.5f, 0.5f);
                ti.spriteBorder = isUi ? new Vector4(6, 6, 6, 6) : Vector4.zero;
            }
            ti.SaveAndReimport();
        }

        // ------------------------------------------------------------------ audio

        static void ImportAudio()
        {
            foreach (var g in AssetDatabase.FindAssets("t:AudioClip", new[] { Audio }))
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                if (!(AssetImporter.GetAtPath(path) is AudioImporter ai)) continue;
                bool music = path.Contains("/Music/");
                var st = ai.defaultSampleSettings;
                st.compressionFormat = AudioCompressionFormat.Vorbis;
                st.quality = music ? 0.45f : 0.6f;
                st.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                st.sampleRateSetting = music ? AudioSampleRateSetting.PreserveSampleRate : AudioSampleRateSetting.OverrideSampleRate;
                st.sampleRateOverride = 22050;
                st.preloadAudioData = !music;
                ai.defaultSampleSettings = st;
                ai.forceToMono = !music;
                ai.loadInBackground = music;
                ai.SaveAndReimport();
            }
        }

        // ------------------------------------------------------------------ font

        [MenuItem("Tools/Fruit Quest/Rebuild Font")]
        public static void RebuildFont()
        {
            AssetDatabase.DeleteAsset(FontPath);
            CreateFont();
            BuildGameAssets();
            AssetDatabase.SaveAssets();
        }

        static void CreateFont()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) != null) return;
            var ttf = AssetDatabase.LoadAssetAtPath<Font>(Art + "/Fonts/LilitaOne-Regular.ttf");
            if (ttf == null) { Debug.LogError("Lilita One font missing"); return; }
            var fa = TMP_FontAsset.CreateFontAsset(ttf, 72, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            fa.name = "LilitaOne SDF";
            AssetDatabase.CreateAsset(fa, FontPath);
            fa.atlasTextures[0].name = "LilitaOne Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            fa.material.name = "LilitaOne Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            fa.TryAddCharacters(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~");
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ materials

        static Material Mat(string name, string shader, System.Action<Material> init = null)
        {
            string path = $"{Rendering}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var sh = Shader.Find(shader);
            if (sh == null) { Debug.LogError("Shader not found: " + shader); return null; }
            if (m == null)
            {
                m = new Material(sh) { name = name };
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = sh;
            init?.Invoke(m);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void CreateMaterials()
        {
            Directory.CreateDirectory(Rendering);
            Mat("M_SpriteLit", "MiniPlatformer/SpriteLitFX", m => m.SetFloat("_Emission", 0f));
            Mat("M_SpriteGlow", "MiniPlatformer/SpriteLitFX", m => m.SetFloat("_Emission", 0.55f));
            Mat("M_Parallax", "MiniPlatformer/ParallaxLayer");
            Mat("M_Additive", "MiniPlatformer/Additive");
            Mat("M_Water", "MiniPlatformer/Water");
            Mat("M_IrisWipe", "MiniPlatformer/UI/IrisWipe");
        }

        // ------------------------------------------------------------------ URP 2D

        static void SetupURP()
        {
            string rendererPath = Rendering + "/URP_2DRenderer.asset";
            string pipePath = Rendering + "/URP_2D.asset";
            var data = AssetDatabase.LoadAssetAtPath<Renderer2DData>(rendererPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(data, rendererPath);
            }
            var so = new SerializedObject(data);
            var pp = so.FindProperty("m_PostProcessData");
            if (pp != null && pp.objectReferenceValue == null)
            {
                pp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(data);

            var pipe = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipePath);
            if (pipe == null)
            {
                pipe = UniversalRenderPipelineAsset.Create(data);
                AssetDatabase.CreateAsset(pipe, pipePath);
            }
            pipe.supportsHDR = true;
            pipe.msaaSampleCount = 1;
            pipe.renderScale = 1f;
            EditorUtility.SetDirty(pipe);

            GraphicsSettings.defaultRenderPipeline = pipe;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipe;
            }
            QualitySettings.SetQualityLevel(current, false);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ registry

        static string KeyFor(string path, string baseFolder)
        {
            string rel = path.Substring(baseFolder.Length + 1);
            rel = rel.Substring(0, rel.Length - Path.GetExtension(rel).Length);
            rel = rel.Replace("PixelAdventure1/", "PA1/").Replace("PixelAdventure2/", "PA2/").Replace("Backgrounds/", "BG/");
            rel = SizeRx.Replace(rel, "");
            rel = Regex.Replace(rel, @"\s+", " ").Replace(" /", "/").Trim();
            return rel;
        }

        static int FrameIndex(Sprite s)
        {
            int u = s.name.LastIndexOf('_');
            return u >= 0 && int.TryParse(s.name.Substring(u + 1), out int i) ? i : 0;
        }

        static void BuildGameAssets()
        {
            Directory.CreateDirectory(Root + "/Resources");
            var ga = AssetDatabase.LoadAssetAtPath<GameAssets>(AssetsPath);
            if (ga == null)
            {
                ga = ScriptableObject.CreateInstance<GameAssets>();
                AssetDatabase.CreateAsset(ga, AssetsPath);
            }
            ga.sprites.Clear();
            ga.textures.Clear();
            ga.clips.Clear();
            ga.levels.Clear();

            foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { Art }))
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                if (path.Contains("/Branding/")) continue;
                string key = KeyFor(path, Art);
                var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(FrameIndex).ToArray();
                if (sprites.Length > 0) ga.sprites.Add(new GameAssets.SpriteSet { key = key, frames = sprites });
                if (path.Contains("/Backgrounds/"))
                    ga.textures.Add(new GameAssets.TextureEntry { key = key, texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path) });
            }
            foreach (var g in AssetDatabase.FindAssets("t:AudioClip", new[] { Audio }))
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                ga.clips.Add(new GameAssets.ClipEntry { key = KeyFor(path, Audio), clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path) });
            }
            // puzzles are generated from their level number; no level files any more
            if (AssetDatabase.IsValidFolder(Root + "/Resources/Levels"))
                ga.levels.AddRange(AssetDatabase.FindAssets("t:TextAsset", new[] { Root + "/Resources/Levels" })
                    .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).Select(AssetDatabase.LoadAssetAtPath<TextAsset>));

            ga.spriteLit = AssetDatabase.LoadAssetAtPath<Material>(Rendering + "/M_SpriteLit.mat");
            ga.spriteGlow = AssetDatabase.LoadAssetAtPath<Material>(Rendering + "/M_SpriteGlow.mat");
            ga.parallax = AssetDatabase.LoadAssetAtPath<Material>(Rendering + "/M_Parallax.mat");
            ga.additive = AssetDatabase.LoadAssetAtPath<Material>(Rendering + "/M_Additive.mat");
            ga.water = AssetDatabase.LoadAssetAtPath<Material>(Rendering + "/M_Water.mat");
            ga.irisWipe = AssetDatabase.LoadAssetAtPath<Material>(Rendering + "/M_IrisWipe.mat");
            ga.trekPath = AssetDatabase.LoadAssetAtPath<Material>(Rendering + "/M_TrekPath.mat");
            ga.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            ga.logo = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Branding/logo.png");
            EditorUtility.SetDirty(ga);
            Debug.Log($"[FruitQuest] Registry: {ga.sprites.Count} sprite sets, {ga.textures.Count} textures, {ga.clips.Count} clips, {ga.levels.Count} levels.");
        }

        // ------------------------------------------------------------------ scene

        static void CreateScene()
        {
            Directory.CreateDirectory(Root + "/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = K.CameraSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(40, 30, 50, 255);
            camGo.transform.position = new Vector3(0, 0, -10);
            var urp = camGo.AddComponent<UniversalAdditionalCameraData>();
            urp.renderPostProcessing = true;
            camGo.AddComponent<AudioListener>();

            var gameGo = new GameObject("Game");
            var game = gameGo.AddComponent<Game>();
            var so = new SerializedObject(game);
            so.FindProperty("assets").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameAssets>(AssetsPath);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}
