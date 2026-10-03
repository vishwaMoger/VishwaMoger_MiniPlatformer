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

        [MenuItem("Tools/Fruit Quest/CrazyGames/Development Build (local test)", false, 32)]
        public static void Development()
        {
            Configure();
            new Builder().DoDevelopmentBuild();
        }
    }
}
#endif
