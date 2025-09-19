using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using Coalition.Demo;

namespace Coalition.Editor
{
    /// <summary>
    /// Automated build pipeline for COALITION demo
    /// Provides command-line build capabilities and CI/CD integration
    /// </summary>
    public class BuildPipeline
    {
        private const string BUILD_OUTPUT_PATH = "Builds";
        private const string VERSION_FILE = "version.txt";

        [MenuItem("Coalition/Build/Build All Platforms")]
        public static void BuildAllPlatforms()
        {
            var buildManager = UnityEngine.Object.FindObjectOfType<ProductionBuildManager>();
            if (buildManager == null)
            {
                Debug.LogError("ProductionBuildManager not found in scene");
                return;
            }

            var task = buildManager.BuildForAllPlatforms();
            // Note: In a real implementation, you'd need to handle async properly in the editor
        }

        [MenuItem("Coalition/Build/Build Windows")]
        public static void BuildWindows()
        {
            BuildForPlatform(BuildTarget.StandaloneWindows64);
        }

        [MenuItem("Coalition/Build/Build macOS")]
        public static void BuildMacOS()
        {
            BuildForPlatform(BuildTarget.StandaloneOSX);
        }

        [MenuItem("Coalition/Build/Build Linux")]
        public static void BuildLinux()
        {
            BuildForPlatform(BuildTarget.StandaloneLinux64);
        }

        /// <summary>
        /// Command-line build method for CI/CD integration
        /// Usage: Unity -batchmode -quit -projectPath . -executeMethod Coalition.Editor.BuildPipeline.CommandLineBuild -buildTarget Windows64
        /// </summary>
        public static void CommandLineBuild()
        {
            var args = System.Environment.GetCommandLineArgs();
            var buildTarget = ParseBuildTarget(args);
            var outputPath = ParseOutputPath(args);
            var version = ParseVersion(args);

            Debug.Log($"Starting command-line build for {buildTarget}");
            Debug.Log($"Output path: {outputPath}");
            Debug.Log($"Version: {version}");

            // Set version
            if (!string.IsNullOrEmpty(version))
            {
                PlayerSettings.bundleVersion = version;
            }

            // Perform build
            var result = BuildForPlatform(buildTarget, outputPath);

            if (result.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build succeeded: {result.summary.outputPath}");
                GenerateBuildReport(result, buildTarget);
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"Build failed: {result.summary.result}");
                EditorApplication.Exit(1);
            }
        }

        private static BuildReport BuildForPlatform(BuildTarget target, string customOutputPath = null)
        {
            Debug.Log($"Building for platform: {target}");

            // Configure build settings
            ConfigureBuildSettings(target);

            // Prepare scenes
            var scenes = GetScenesForBuild();

            // Create build options
            var buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                target = target,
                targetGroup = GetBuildTargetGroup(target),
                options = GetBuildOptions(),
                locationPathName = GetBuildPath(target, customOutputPath)
            };

            // Pre-build optimizations
            PerformPreBuildOptimizations();

            // Execute build
            var buildReport = UnityEditor.BuildPipeline.BuildPlayer(buildOptions);

            // Post-build processing
            PerformPostBuildProcessing(buildReport, target);

            return buildReport;
        }

        private static void ConfigureBuildSettings(BuildTarget target)
        {
            var targetGroup = GetBuildTargetGroup(target);

            // Configure IL2CPP for all platforms
            PlayerSettings.SetScriptingBackend(targetGroup, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(targetGroup, ApiCompatibilityLevel.NET_Standard_2_1);

            // Optimization settings
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(targetGroup, ManagedStrippingLevel.High);

            // Platform-specific settings
            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                    ConfigureWindowsSettings();
                    break;
                case BuildTarget.StandaloneOSX:
                    ConfigureMacSettings();
                    break;
                case BuildTarget.StandaloneLinux64:
                    ConfigureLinuxSettings();
                    break;
            }

            // Graphics settings
            ConfigureGraphicsSettings();

            // Audio settings
            ConfigureAudioSettings();
        }

        private static void ConfigureWindowsSettings()
        {
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, GetWindowsIcons());
            PlayerSettings.companyName = "Coalition Demo";
            PlayerSettings.productName = "COALITION - Dutch Politics Demo";
            PlayerSettings.applicationIdentifier = "com.coalition.demo";

            // Windows-specific settings
            EditorUserBuildSettings.compression = Compression.Lz4HC;
        }

        private static void ConfigureMacSettings()
        {
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, GetMacIcons());
            PlayerSettings.macOS.applicationCategoryType = MacOSApplicationCategoryType.EducationGames;
            PlayerSettings.macOS.cameraUsageDescription = "Not used";
            PlayerSettings.macOS.microphoneUsageDescription = "Not used";
        }

        private static void ConfigureLinuxSettings()
        {
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, GetLinuxIcons());
        }

        private static void ConfigureGraphicsSettings()
        {
            // Optimize graphics settings for demo
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.defaultIsFullScreen = false;
            PlayerSettings.runInBackground = true;
            PlayerSettings.captureSingleScreen = false;
            PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.Disabled;

            // Quality settings
            QualitySettings.vSyncCount = 1;
            QualitySettings.antiAliasing = 4;
        }

        private static void ConfigureAudioSettings()
        {
            // Configure audio for demo
            AudioSettings.Reset();
        }

        private static string[] GetScenesForBuild()
        {
            var scenes = new List<string>();

            // Add all scenes from build settings
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    scenes.Add(scene.path);
                }
            }

            // Ensure demo scene is included
            if (!scenes.Contains("Assets/Scenes/Demo/DemoMain.unity"))
            {
                scenes.Insert(0, "Assets/Scenes/Demo/DemoMain.unity");
            }

            return scenes.ToArray();
        }

        private static BuildOptions GetBuildOptions()
        {
            var options = BuildOptions.None;

            // Enable development build for testing
            if (EditorUserBuildSettings.development)
            {
                options |= BuildOptions.Development;
            }

            // Enable deep profiling if needed
            if (EditorUserBuildSettings.connectProfiler)
            {
                options |= BuildOptions.ConnectWithProfiler;
                options |= BuildOptions.EnableDeepProfilingSupport;
            }

            return options;
        }

        private static string GetBuildPath(BuildTarget target, string customPath = null)
        {
            if (!string.IsNullOrEmpty(customPath))
            {
                return customPath;
            }

            var platformName = GetPlatformName(target);
            var version = PlayerSettings.bundleVersion;
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");

            var buildDir = Path.Combine(BUILD_OUTPUT_PATH, platformName, $"v{version}-{timestamp}");
            Directory.CreateDirectory(buildDir);

            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                    return Path.Combine(buildDir, "Coalition.exe");
                case BuildTarget.StandaloneOSX:
                    return Path.Combine(buildDir, "Coalition.app");
                case BuildTarget.StandaloneLinux64:
                    return Path.Combine(buildDir, "Coalition");
                default:
                    return Path.Combine(buildDir, "Coalition");
            }
        }

        private static void PerformPreBuildOptimizations()
        {
            Debug.Log("Performing pre-build optimizations...");

            // Optimize textures
            OptimizeTextures();

            // Optimize audio clips
            OptimizeAudioClips();

            // Clean up temp files
            CleanTempFiles();

            // Refresh asset database
            AssetDatabase.Refresh();
        }

        private static void OptimizeTextures()
        {
            var textureGuids = AssetDatabase.FindAssets("t:Texture2D");
            var processed = 0;

            foreach (var guid in textureGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer != null && !path.Contains("StreamingAssets"))
                {
                    var settings = importer.GetDefaultPlatformTextureSettings();
                    var modified = false;

                    // Apply compression based on texture type
                    if (path.Contains("UI") || path.Contains("Icons"))
                    {
                        if (settings.format != TextureImporterFormat.DXT5)
                        {
                            settings.format = TextureImporterFormat.DXT5;
                            settings.compressionQuality = 75;
                            modified = true;
                        }
                    }
                    else
                    {
                        if (settings.format != TextureImporterFormat.DXT1)
                        {
                            settings.format = TextureImporterFormat.DXT1;
                            settings.compressionQuality = 50;
                            modified = true;
                        }
                    }

                    if (modified)
                    {
                        importer.SetPlatformTextureSettings(settings);
                        AssetDatabase.ImportAsset(path);
                        processed++;
                    }
                }
            }

            Debug.Log($"Optimized {processed} textures");
        }

        private static void OptimizeAudioClips()
        {
            var audioGuids = AssetDatabase.FindAssets("t:AudioClip");
            var processed = 0;

            foreach (var guid in audioGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;

                if (importer != null)
                {
                    var settings = importer.defaultSampleSettings;
                    var modified = false;

                    if (settings.compressionFormat != AudioCompressionFormat.Vorbis)
                    {
                        settings.compressionFormat = AudioCompressionFormat.Vorbis;
                        settings.quality = 0.7f;
                        settings.loadType = AudioClipLoadType.CompressedInMemory;
                        modified = true;
                    }

                    if (modified)
                    {
                        importer.defaultSampleSettings = settings;
                        AssetDatabase.ImportAsset(path);
                        processed++;
                    }
                }
            }

            Debug.Log($"Optimized {processed} audio clips");
        }

        private static void CleanTempFiles()
        {
            // Clean temporary files and caches
            var tempPaths = new[]
            {
                "Temp",
                "Library/ShaderCache",
                "Library/StateCache"
            };

            foreach (var tempPath in tempPaths)
            {
                if (Directory.Exists(tempPath))
                {
                    try
                    {
                        Directory.Delete(tempPath, true);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Could not clean {tempPath}: {ex.Message}");
                    }
                }
            }
        }

        private static void PerformPostBuildProcessing(BuildReport buildReport, BuildTarget target)
        {
            if (buildReport.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("Build succeeded, performing post-build processing...");

                // Create documentation
                CreateDocumentation(buildReport, target);

                // Create installer (if configured)
                CreateInstaller(buildReport, target);

                // Generate checksums
                GenerateChecksums(buildReport);

                // Copy additional files
                CopyAdditionalFiles(buildReport, target);
            }
        }

        private static void CreateDocumentation(BuildReport buildReport, BuildTarget target)
        {
            var buildDir = Path.GetDirectoryName(buildReport.summary.outputPath);
            var readmePath = Path.Combine(buildDir, "README.txt");

            var readmeContent = GenerateReadmeContent(buildReport, target);
            File.WriteAllText(readmePath, readmeContent);

            var systemReqPath = Path.Combine(buildDir, "SYSTEM_REQUIREMENTS.txt");
            var systemReqContent = GenerateSystemRequirements(target);
            File.WriteAllText(systemReqPath, systemReqContent);
        }

        private static string GenerateReadmeContent(BuildReport buildReport, BuildTarget target)
        {
            var content = $@"COALITION - Dutch Politics Demo
================================

Version: {PlayerSettings.bundleVersion}
Build Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
Platform: {GetPlatformName(target)}
Build Size: {buildReport.summary.totalSize / (1024 * 1024):F1} MB

INSTALLATION
------------
1. Extract all files to a folder of your choice
2. Run Coalition executable
3. Follow the on-screen tutorial

SYSTEM REQUIREMENTS
-------------------
See SYSTEM_REQUIREMENTS.txt for detailed requirements.

FEATURES
--------
- Complete Dutch political party data (2023 election results)
- Accurate D'Hondt electoral system implementation
- Interactive coalition formation simulation
- Multi-window desktop interface
- User testing framework for research validation

CONTROLS
--------
- Mouse: Navigate interface, click buttons and windows
- Keyboard: Use for text input and shortcuts
- ESC: Exit application

TROUBLESHOOTING
---------------
If you experience issues:
1. Ensure your system meets minimum requirements
2. Update graphics drivers
3. Run as administrator (Windows) if needed
4. Check firewall settings if network features are blocked

For research questions or technical support:
https://github.com/coalition-demo/support

This software is provided for research and educational purposes.
";
            return content;
        }

        private static string GenerateSystemRequirements(BuildTarget target)
        {
            var platformName = GetPlatformName(target);
            var content = $@"COALITION Demo - System Requirements
====================================

Platform: {platformName}

MINIMUM REQUIREMENTS
--------------------
Operating System: {GetMinimumOS(target)}
Processor: {GetMinimumProcessor(target)}
Memory: 4 GB RAM
Graphics: {GetMinimumGraphics(target)}
DirectX: Version 11 (Windows only)
Storage: 1 GB available space
Network: Internet connection for initial setup

RECOMMENDED REQUIREMENTS
------------------------
Operating System: {GetRecommendedOS(target)}
Processor: {GetRecommendedProcessor(target)}
Memory: 8 GB RAM
Graphics: {GetRecommendedGraphics(target)}
DirectX: Version 12 (Windows only)
Storage: 2 GB available space (SSD recommended)
Network: Broadband Internet connection

PERFORMANCE TARGETS
-------------------
- Target Frame Rate: 60 FPS
- Maximum Loading Time: 30 seconds
- Memory Usage: < 1 GB RAM
- Calculation Time: < 5 seconds for full election simulation

TESTED CONFIGURATIONS
---------------------
The demo has been tested on the following configurations:
- Windows 10/11 (x64)
- macOS 12+ (Intel and Apple Silicon)
- Ubuntu 20.04+ LTS (x64)

KNOWN LIMITATIONS
-----------------
- Single-user mode only
- No network multiplayer
- Designed for desktop use (not optimized for tablets)
";
            return content;
        }

        private static void CreateInstaller(BuildReport buildReport, BuildTarget target)
        {
            // Placeholder for installer creation
            // In a real implementation, this would integrate with tools like:
            // - NSIS for Windows
            // - pkgbuild for macOS
            // - dpkg for Linux
        }

        private static void GenerateChecksums(BuildReport buildReport)
        {
            var buildPath = buildReport.summary.outputPath;
            var buildDir = Path.GetDirectoryName(buildPath);
            var checksumPath = Path.Combine(buildDir, "checksums.txt");

            var checksums = new List<string>();

            // Generate checksums for all build files
            var files = Directory.GetFiles(buildDir, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (Path.GetFileName(file) != "checksums.txt")
                {
                    var hash = CalculateFileHash(file);
                    var relativePath = Path.GetRelativePath(buildDir, file);
                    checksums.Add($"{hash}  {relativePath}");
                }
            }

            File.WriteAllLines(checksumPath, checksums);
        }

        private static string CalculateFileHash(string filePath)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private static void CopyAdditionalFiles(BuildReport buildReport, BuildTarget target)
        {
            var buildDir = Path.GetDirectoryName(buildReport.summary.outputPath);

            // Copy license file
            var licenseSrc = "LICENSE.txt";
            if (File.Exists(licenseSrc))
            {
                File.Copy(licenseSrc, Path.Combine(buildDir, "LICENSE.txt"), true);
            }

            // Copy documentation
            var docsSrc = "docs";
            if (Directory.Exists(docsSrc))
            {
                var docsTarget = Path.Combine(buildDir, "Documentation");
                CopyDirectory(docsSrc, docsTarget);
            }
        }

        private static void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                var targetFile = Path.Combine(targetDir, fileName);
                File.Copy(file, targetFile, true);
            }

            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(subDir);
                var targetSubDir = Path.Combine(targetDir, dirName);
                CopyDirectory(subDir, targetSubDir);
            }
        }

        private static void GenerateBuildReport(BuildReport buildReport, BuildTarget target)
        {
            var reportPath = $"build_report_{GetPlatformName(target)}_{DateTime.Now:yyyyMMdd_HHmmss}.json";

            var report = new
            {
                Platform = target.ToString(),
                Result = buildReport.summary.result.ToString(),
                OutputPath = buildReport.summary.outputPath,
                BuildTime = buildReport.summary.buildEndedAt - buildReport.summary.buildStartedAt,
                TotalSize = buildReport.summary.totalSize,
                TotalErrors = buildReport.summary.totalErrors,
                TotalWarnings = buildReport.summary.totalWarnings,
                BuildStartTime = buildReport.summary.buildStartedAt,
                BuildEndTime = buildReport.summary.buildEndedAt,
                UnityVersion = Application.unityVersion,
                ProductVersion = PlayerSettings.bundleVersion
            };

            var json = JsonUtility.ToJson(report, true);
            File.WriteAllText(reportPath, json);

            Debug.Log($"Build report saved to: {reportPath}");
        }

        // Utility methods
        private static BuildTarget ParseBuildTarget(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-buildTarget")
                {
                    switch (args[i + 1].ToLower())
                    {
                        case "windows64":
                        case "windows":
                            return BuildTarget.StandaloneWindows64;
                        case "osx":
                        case "macos":
                            return BuildTarget.StandaloneOSX;
                        case "linux64":
                        case "linux":
                            return BuildTarget.StandaloneLinux64;
                    }
                }
            }
            return BuildTarget.StandaloneWindows64; // Default
        }

        private static string ParseOutputPath(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-outputPath")
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        private static string ParseVersion(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-version")
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        private static BuildTargetGroup GetBuildTargetGroup(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneOSX:
                case BuildTarget.StandaloneLinux64:
                    return BuildTargetGroup.Standalone;
                default:
                    return BuildTargetGroup.Unknown;
            }
        }

        private static string GetPlatformName(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                    return "Windows";
                case BuildTarget.StandaloneOSX:
                    return "macOS";
                case BuildTarget.StandaloneLinux64:
                    return "Linux";
                default:
                    return target.ToString();
            }
        }

        private static Texture2D[] GetWindowsIcons()
        {
            // Load Windows-specific icons
            return new Texture2D[0]; // Placeholder
        }

        private static Texture2D[] GetMacIcons()
        {
            // Load macOS-specific icons
            return new Texture2D[0]; // Placeholder
        }

        private static Texture2D[] GetLinuxIcons()
        {
            // Load Linux-specific icons
            return new Texture2D[0]; // Placeholder
        }

        private static string GetMinimumOS(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                    return "Windows 10 (64-bit)";
                case BuildTarget.StandaloneOSX:
                    return "macOS 10.15 Catalina";
                case BuildTarget.StandaloneLinux64:
                    return "Ubuntu 18.04 LTS (64-bit)";
                default:
                    return "Unknown";
            }
        }

        private static string GetRecommendedOS(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                    return "Windows 11 (64-bit)";
                case BuildTarget.StandaloneOSX:
                    return "macOS 12 Monterey or later";
                case BuildTarget.StandaloneLinux64:
                    return "Ubuntu 22.04 LTS (64-bit)";
                default:
                    return "Unknown";
            }
        }

        private static string GetMinimumProcessor(BuildTarget target)
        {
            return "Intel Core i3-4340 / AMD FX-6300";
        }

        private static string GetRecommendedProcessor(BuildTarget target)
        {
            return "Intel Core i5-8400 / AMD Ryzen 5 2600";
        }

        private static string GetMinimumGraphics(BuildTarget target)
        {
            return "NVIDIA GTX 660 / AMD Radeon HD 7870 / Intel HD Graphics 4600";
        }

        private static string GetRecommendedGraphics(BuildTarget target)
        {
            return "NVIDIA GTX 1060 / AMD RX 580 / Intel Iris Xe Graphics";
        }
    }
}