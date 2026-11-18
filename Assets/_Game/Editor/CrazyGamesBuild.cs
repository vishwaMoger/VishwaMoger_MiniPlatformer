#if UNITY_6000_0_OR_NEWER
using CrazyGames;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace MiniPlatformer.EditorTools
{
    /// <summary>
    /// One-click CrazyGames builds. Applies the project's WebGL player settings, then runs the CrazySDK release builder
    /// (Brotli, hashed file names, LTO, DXT + ASTC texture variants, 512/1024 MB memory variants for mobile).
    /// Output: Builds/CrazyGamesRelease (upload every file in that folder).
    /// </summary>
    public static class CrazyGamesBuild
    {
        [MenuItem("Tools/Fruit Quest/CrazyGames/1. Switch To WebGL + Apply Settings", false, 30)]
        public static void Configure()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

            var web = NamedBuildTarget.WebGL;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithoutStacktrace; // see Builder.cs note
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.showDiagnostics = false;
            PlayerSettings.WebGL.template = "PROJECT:TileTrek"; // full-window canvas: portrait and landscape
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.initialMemorySize = 64;
            PlayerSettings.WebGL.maximumMemorySize = 2048;
            PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.HighPerformance;
            PlayerSettings.SetManagedStrippingLevel(web, ManagedStrippingLevel.Medium);
            PlayerSettings.SetApiCompatibilityLevel(web, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.runInBackground = false; // pause when the tab is hidden
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            AssetDatabase.SaveAssets();
            Debug.Log("[FruitQuest] WebGL settings applied");
        }

        [MenuItem("Tools/Fruit Quest/CrazyGames/2. Release Build", false, 31)]
        public static void Release()
        {
            Configure();
            new Builder().DoReleaseBuild(new AdditionalBuildOptions { supportsMobile = true });
        }

        /// <summary>
        /// Single-pass release build with the same optimisations as the CrazySDK builder (Brotli, hashed names, data
        /// caching, stripping, LTO, wasm2023, no splash) but without its extra ASTC / memory variant passes: Unity 6.3's
        /// incremental build deletes the main output when those run, and this game's pixel textures are uncompressed
        /// anyway. Output: Builds/CrazyGamesRelease.
        /// </summary>
        [MenuItem("Tools/Fruit Quest/CrazyGames/3. Release Build (single pass)", false, 32)]
        public static void ReleaseSinglePass()
        {
            Configure();
            const string path = "Builds/CrazyGamesRelease";
            if (System.IO.Directory.Exists(path)) System.IO.Directory.Delete(path, true);
            var web = NamedBuildTarget.WebGL;
            bool splash = PlayerSettings.SplashScreen.show;
            var codeGen = PlayerSettings.GetIl2CppCodeGeneration(web);
            try
            {
                PlayerSettings.SplashScreen.show = false;
                PlayerSettings.SetIl2CppCodeGeneration(web, Il2CppCodeGeneration.OptimizeSize);
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
                PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
                PlayerSettings.WebGL.dataCaching = true;
                PlayerSettings.WebGL.nameFilesAsHashes = true;
                PlayerSettings.WebGL.webAssemblyBigInt = true;
                PlayerSettings.WebGL.webAssemblyTable = true;
                PlayerSettings.WebGL.wasm2023 = true;
                PlayerSettings.stripEngineCode = true;
                UnityEditor.WebGL.UserBuildSettings.codeOptimization = UnityEditor.WebGL.WasmCodeOptimization.DiskSizeLTO;
                EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.DXT;
                var scenes = System.Array.ConvertAll(System.Array.FindAll(EditorBuildSettings.scenes, sc => sc.enabled), sc => sc.path);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = path, target = BuildTarget.WebGL, options = BuildOptions.None });
                Debug.Log($"[FruitQuest] CrazyGames single-pass build: {report.summary.result}, {report.summary.totalSize / 1048576f:0.0} MB, {report.summary.totalTime.TotalMinutes:0.0} min");
            }
            finally
            {
                PlayerSettings.SplashScreen.show = splash; // Android keeps its branded splash
                PlayerSettings.SetIl2CppCodeGeneration(web, codeGen);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// itch.io build: the same game, but (1) the CrazyGames SiteLock whitelists itch's domains (it would otherwise
        /// stop the game on any non-CrazyGames site) and (2) gzip with decompression fallback, because itch.io does not
        /// send Content-Encoding headers for pre-compressed files. Restores the CrazyGames settings afterwards.
        /// Output: Builds/Itch/WebGL, zipped by the caller for butler.
        /// </summary>
        [MenuItem("Tools/Fruit Quest/itch.io Build", false, 40)]
        public static void ItchBuild()
        {
            Configure();
            const string path = "Builds/Itch/WebGL";
            if (System.IO.Directory.Exists(path)) System.IO.Directory.Delete(path, true);
            var settings = Resources.Load<CrazySettings>("CrazyGamesSettings");
            var oldDomains = settings != null && settings.whitelistedDomains != null ? (string[])settings.whitelistedDomains.Clone() : null;
            var web = NamedBuildTarget.WebGL;
            bool splash = PlayerSettings.SplashScreen.show;
            var codeGen = PlayerSettings.GetIl2CppCodeGeneration(web);
            try
            {
                if (settings != null)
                {
                    settings.whitelistedDomains = new[] { "itch.io", "itch.zone", "hwcdn.net" };
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssetIfDirty(settings);
                }
                PlayerSettings.SplashScreen.show = false;
                PlayerSettings.SetIl2CppCodeGeneration(web, Il2CppCodeGeneration.OptimizeSize);
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.decompressionFallback = true;
                PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
                PlayerSettings.WebGL.dataCaching = true;
                PlayerSettings.WebGL.nameFilesAsHashes = false;
                PlayerSettings.stripEngineCode = true;
                UnityEditor.WebGL.UserBuildSettings.codeOptimization = UnityEditor.WebGL.WasmCodeOptimization.DiskSizeLTO;
                EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.DXT;
                var scenes = System.Array.ConvertAll(System.Array.FindAll(EditorBuildSettings.scenes, sc => sc.enabled), sc => sc.path);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = path, target = BuildTarget.WebGL, options = BuildOptions.None });
                Debug.Log($"[FruitQuest] itch.io build: {report.summary.result}, {report.summary.totalSize / 1048576f:0.0} MB");
            }
            finally
            {
                // the build can reload assets: fetch the settings again before restoring the CrazyGames whitelist
                var current = Resources.Load<CrazySettings>("CrazyGamesSettings");
                if (current != null)
                {
                    current.whitelistedDomains = oldDomains != null ? (string[])oldDomains.Clone() : new string[0];
                    EditorUtility.SetDirty(current);
                    AssetDatabase.SaveAssetIfDirty(current);
                }
                PlayerSettings.SplashScreen.show = splash;
                PlayerSettings.SetIl2CppCodeGeneration(web, codeGen);
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
                PlayerSettings.WebGL.decompressionFallback = false;
                PlayerSettings.WebGL.nameFilesAsHashes = true;
                AssetDatabase.SaveAssets();
            }
        }

        [MenuItem("Tools/Fruit Quest/CrazyGames/Development Build (local test)", false, 32)]
        public static void Development()
        {
            Configure();
            new Builder().DoDevelopmentBuild();
        }
    }
}
#endif
