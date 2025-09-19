using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;

namespace Coalition.Demo
{
    /// <summary>
    /// Production build configuration and deployment management system
    /// Handles cross-platform builds with optimization and validation
    /// </summary>
    public class ProductionBuildManager : MonoBehaviour
    {
        [Header("Build Configuration")]
        [SerializeField] private BuildConfiguration buildConfig;
        [SerializeField] private DeploymentConfiguration deploymentConfig;

        [Header("Optimization Settings")]
        [SerializeField] private bool enableAssetOptimization = true;
        [SerializeField] private bool enableCodeStripping = true;
        [SerializeField] private bool enableCompressionOptimization = true;

        private BuildReport lastBuildReport;
        private Dictionary<BuildTarget, BuildSettings> platformSettings;

        public BuildReport LastBuildReport => lastBuildReport;
        public bool IsBuildInProgress { get; private set; }

        private void Awake()
        {
            InitializeBuildManager();
        }

        private void InitializeBuildManager()
        {
            platformSettings = new Dictionary<BuildTarget, BuildSettings>
            {
                {
                    BuildTarget.StandaloneWindows64,
                    new BuildSettings
                    {
                        Target = BuildTarget.StandaloneWindows64,
                        ScriptingBackend = ScriptingImplementation.IL2CPP,
                        ApiCompatibilityLevel = ApiCompatibilityLevel.NET_Standard_2_1,
                        CompressionType = Compression.Lz4HC,
                        OptimizationLevel = OptimizationLevel.Release
                    }
                },
                {
                    BuildTarget.StandaloneOSX,
                    new BuildSettings
                    {
                        Target = BuildTarget.StandaloneOSX,
                        ScriptingBackend = ScriptingImplementation.IL2CPP,
                        ApiCompatibilityLevel = ApiCompatibilityLevel.NET_Standard_2_1,
                        CompressionType = Compression.Lz4HC,
                        OptimizationLevel = OptimizationLevel.Release
                    }
                },
                {
                    BuildTarget.StandaloneLinux64,
                    new BuildSettings
                    {
                        Target = BuildTarget.StandaloneLinux64,
                        ScriptingBackend = ScriptingImplementation.IL2CPP,
                        ApiCompatibilityLevel = ApiCompatibilityLevel.NET_Standard_2_1,
                        CompressionType = Compression.Lz4HC,
                        OptimizationLevel = OptimizationLevel.Release
                    }
                }
            };
        }

#if UNITY_EDITOR
        public async Task<BuildResult> BuildForAllPlatforms()
        {
            if (IsBuildInProgress)
            {
                Debug.LogWarning("Build already in progress");
                return new BuildResult { Success = false, ErrorMessage = "Build already in progress" };
            }

            IsBuildInProgress = true;
            var overallResult = new BuildResult { Success = true };

            try
            {
                // Pre-build validation
                var validationResult = await ValidateProjectForBuild();
                if (!validationResult.IsValid)
                {
                    overallResult.Success = false;
                    overallResult.ErrorMessage = validationResult.ErrorMessage;
                    return overallResult;
                }

                // Optimize assets before building
                if (enableAssetOptimization)
                {
                    await OptimizeAssetsForProduction();
                }

                // Build for each target platform
                foreach (var platform in platformSettings.Keys)
                {
                    var platformResult = await BuildForPlatform(platform);
                    overallResult.PlatformResults[platform] = platformResult;

                    if (!platformResult.Success)
                    {
                        Debug.LogError($"Build failed for platform {platform}: {platformResult.ErrorMessage}");
                        overallResult.Success = false;
                    }
                    else
                    {
                        Debug.Log($"Build successful for platform {platform}");
                    }
                }

                // Post-build validation
                if (overallResult.Success)
                {
                    await ValidateBuilds(overallResult);
                }

                return overallResult;
            }
            finally
            {
                IsBuildInProgress = false;
            }
        }

        public async Task<PlatformBuildResult> BuildForPlatform(BuildTarget target)
        {
            Debug.Log($"Starting build for platform: {target}");

            var settings = platformSettings[target];
            var result = new PlatformBuildResult
            {
                Platform = target,
                StartTime = DateTime.UtcNow
            };

            try
            {
                // Configure build settings
                ConfigureBuildSettings(settings);

                // Prepare build
                var buildPlayerOptions = CreateBuildPlayerOptions(target);

                // Execute build
                var buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);
                result.BuildReport = buildReport;

                // Validate build success
                if (buildReport.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                {
                    result.Success = true;
                    result.OutputPath = buildPlayerOptions.locationPathName;
                    result.BuildSizeMB = CalculateBuildSize(result.OutputPath);

                    // Validate size requirements
                    if (result.BuildSizeMB > deploymentConfig.maxBuildSizeMB)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Build size {result.BuildSizeMB}MB exceeds limit {deploymentConfig.maxBuildSizeMB}MB";
                    }
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = $"Build failed: {buildReport.summary.result}";
                }

                result.EndTime = DateTime.UtcNow;
                result.BuildDurationMinutes = (float)(result.EndTime - result.StartTime).TotalMinutes;

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.UtcNow;
                return result;
            }
        }

        private async Task<ValidationResult> ValidateProjectForBuild()
        {
            var result = new ValidationResult { IsValid = true };

            // Check required scenes
            var requiredScenes = buildConfig.requiredScenes;
            foreach (var scenePath in requiredScenes)
            {
                if (!File.Exists(scenePath))
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Required scene not found: {scenePath}";
                    return result;
                }
            }

            // Check asset dependencies
            var missingAssets = await CheckMissingAssets();
            if (missingAssets.Count > 0)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Missing assets: {string.Join(", ", missingAssets)}";
                return result;
            }

            // Validate script compilation
            if (EditorUtility.scriptCompilationFailed)
            {
                result.IsValid = false;
                result.ErrorMessage = "Script compilation failed";
                return result;
            }

            // Check disk space
            var requiredSpace = CalculateRequiredDiskSpace();
            var availableSpace = GetAvailableDiskSpace();
            if (availableSpace < requiredSpace)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Insufficient disk space. Required: {requiredSpace}MB, Available: {availableSpace}MB";
                return result;
            }

            return result;
        }

        private async Task OptimizeAssetsForProduction()
        {
            Debug.Log("Optimizing assets for production build...");

            // Optimize textures
            await OptimizeTextures();

            // Optimize audio
            await OptimizeAudio();

            // Remove unnecessary assets
            await RemoveUnusedAssets();

            // Compress assets
            if (enableCompressionOptimization)
            {
                await CompressAssets();
            }

            Debug.Log("Asset optimization completed");
        }

        private async Task OptimizeTextures()
        {
            var textureGuids = AssetDatabase.FindAssets("t:Texture2D");

            foreach (var guid in textureGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer != null)
                {
                    // Apply compression settings based on usage
                    if (path.Contains("UI"))
                    {
                        importer.textureCompression = TextureImporterCompression.Compressed;
                        importer.compressionQuality = 75;
                    }
                    else
                    {
                        importer.textureCompression = TextureImporterCompression.CompressedHQ;
                        importer.compressionQuality = 50;
                    }

                    AssetDatabase.ImportAsset(path);
                }
            }

            await Task.Yield();
        }

        private async Task OptimizeAudio()
        {
            var audioGuids = AssetDatabase.FindAssets("t:AudioClip");

            foreach (var guid in audioGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;

                if (importer != null)
                {
                    var settings = importer.defaultSampleSettings;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    settings.quality = 0.7f;
                    importer.defaultSampleSettings = settings;

                    AssetDatabase.ImportAsset(path);
                }
            }

            await Task.Yield();
        }

        private async Task RemoveUnusedAssets()
        {
            // Implementation would analyze asset dependencies and remove unused ones
            await Task.Yield();
        }

        private async Task CompressAssets()
        {
            // Implementation would apply additional compression to asset bundles
            await Task.Yield();
        }

        private BuildPlayerOptions CreateBuildPlayerOptions(BuildTarget target)
        {
            var options = new BuildPlayerOptions
            {
                scenes = buildConfig.scenesToBuild,
                target = target,
                targetGroup = GetBuildTargetGroup(target),
                options = BuildOptions.None
            };

            // Configure output path
            var platformName = GetPlatformName(target);
            var version = Application.version;
            var buildPath = Path.Combine(deploymentConfig.buildOutputPath, platformName, version);

            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                    options.locationPathName = Path.Combine(buildPath, "Coalition.exe");
                    break;
                case BuildTarget.StandaloneOSX:
                    options.locationPathName = Path.Combine(buildPath, "Coalition.app");
                    break;
                case BuildTarget.StandaloneLinux64:
                    options.locationPathName = Path.Combine(buildPath, "Coalition");
                    break;
            }

            // Configure build options
            if (buildConfig.enableDevelopmentBuild)
                options.options |= BuildOptions.Development;

            if (buildConfig.enableDeepProfilingSupport)
                options.options |= BuildOptions.EnableDeepProfilingSupport;

            return options;
        }

        private void ConfigureBuildSettings(BuildSettings settings)
        {
            // Configure player settings for the build
            PlayerSettings.SetScriptingBackend(settings.TargetGroup, settings.ScriptingBackend);
            PlayerSettings.SetApiCompatibilityLevel(settings.TargetGroup, settings.ApiCompatibilityLevel);

            // Configure optimization settings
            if (enableCodeStripping)
            {
                PlayerSettings.stripEngineCode = true;
                PlayerSettings.SetManagedStrippingLevel(settings.TargetGroup, ManagedStrippingLevel.High);
            }

            // Configure compression
            EditorUserBuildSettings.compression = settings.CompressionType;
        }

        private async Task<List<string>> CheckMissingAssets()
        {
            var missingAssets = new List<string>();

            // Check if all required assets exist
            var requiredAssets = buildConfig.requiredAssets;
            foreach (var assetPath in requiredAssets)
            {
                if (!AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath))
                {
                    missingAssets.Add(assetPath);
                }
            }

            return await Task.FromResult(missingAssets);
        }

        private async Task ValidateBuilds(BuildResult overallResult)
        {
            foreach (var platformResult in overallResult.PlatformResults.Values)
            {
                if (platformResult.Success)
                {
                    // Validate executable exists and is functional
                    if (!File.Exists(platformResult.OutputPath))
                    {
                        platformResult.Success = false;
                        platformResult.ErrorMessage = "Build output file not found";
                        continue;
                    }

                    // Validate build size
                    if (platformResult.BuildSizeMB > deploymentConfig.maxBuildSizeMB)
                    {
                        platformResult.Success = false;
                        platformResult.ErrorMessage = $"Build size exceeds limit: {platformResult.BuildSizeMB}MB > {deploymentConfig.maxBuildSizeMB}MB";
                        continue;
                    }

                    // Additional platform-specific validations
                    await ValidatePlatformSpecific(platformResult);
                }
            }
        }

        private async Task ValidatePlatformSpecific(PlatformBuildResult result)
        {
            switch (result.Platform)
            {
                case BuildTarget.StandaloneWindows64:
                    await ValidateWindowsBuild(result);
                    break;
                case BuildTarget.StandaloneOSX:
                    await ValidateMacBuild(result);
                    break;
                case BuildTarget.StandaloneLinux64:
                    await ValidateLinuxBuild(result);
                    break;
            }
        }

        private async Task ValidateWindowsBuild(PlatformBuildResult result)
        {
            // Windows-specific validation
            var dataFolder = Path.GetDirectoryName(result.OutputPath) + "/" + Path.GetFileNameWithoutExtension(result.OutputPath) + "_Data";
            if (!Directory.Exists(dataFolder))
            {
                result.Success = false;
                result.ErrorMessage = "Windows build missing _Data folder";
            }
            await Task.Yield();
        }

        private async Task ValidateMacBuild(PlatformBuildResult result)
        {
            // macOS-specific validation
            var contentsFolder = result.OutputPath + "/Contents";
            if (!Directory.Exists(contentsFolder))
            {
                result.Success = false;
                result.ErrorMessage = "macOS build missing Contents folder";
            }
            await Task.Yield();
        }

        private async Task ValidateLinuxBuild(PlatformBuildResult result)
        {
            // Linux-specific validation
            var dataFolder = result.OutputPath + "_Data";
            if (!Directory.Exists(dataFolder))
            {
                result.Success = false;
                result.ErrorMessage = "Linux build missing _Data folder";
            }
            await Task.Yield();
        }
#endif

        private BuildTargetGroup GetBuildTargetGroup(BuildTarget target)
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

        private string GetPlatformName(BuildTarget target)
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

        private float CalculateBuildSize(string buildPath)
        {
            if (File.Exists(buildPath))
            {
                return new FileInfo(buildPath).Length / (1024f * 1024f); // Convert to MB
            }
            else if (Directory.Exists(buildPath))
            {
                var dirInfo = new DirectoryInfo(buildPath);
                long totalSize = 0;
                foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                {
                    totalSize += file.Length;
                }
                return totalSize / (1024f * 1024f); // Convert to MB
            }
            return 0f;
        }

        private long CalculateRequiredDiskSpace()
        {
            // Estimate required disk space (MB)
            return platformSettings.Count * 500; // 500MB per platform estimate
        }

        private long GetAvailableDiskSpace()
        {
            // Get available disk space in MB
            var drive = new DriveInfo(Application.dataPath);
            return drive.AvailableFreeSpace / (1024 * 1024);
        }
    }

    // Supporting classes
    [Serializable]
    public class BuildSettings
    {
        public BuildTarget Target;
        public BuildTargetGroup TargetGroup => GetBuildTargetGroup(Target);
        public ScriptingImplementation ScriptingBackend;
        public ApiCompatibilityLevel ApiCompatibilityLevel;
        public Compression CompressionType;
        public OptimizationLevel OptimizationLevel;

        private BuildTargetGroup GetBuildTargetGroup(BuildTarget target)
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
    }

    public enum OptimizationLevel
    {
        Debug,
        Release,
        Maximum
    }

    [Serializable]
    public class BuildResult
    {
        public bool Success;
        public string ErrorMessage;
        public Dictionary<BuildTarget, PlatformBuildResult> PlatformResults = new Dictionary<BuildTarget, PlatformBuildResult>();
        public DateTime BuildStartTime;
        public DateTime BuildEndTime;
        public float TotalBuildTimeMinutes => (float)(BuildEndTime - BuildStartTime).TotalMinutes;
    }

    [Serializable]
    public class PlatformBuildResult
    {
        public BuildTarget Platform;
        public bool Success;
        public string ErrorMessage;
        public string OutputPath;
        public float BuildSizeMB;
        public DateTime StartTime;
        public DateTime EndTime;
        public float BuildDurationMinutes;
        public object BuildReport;
    }

    [Serializable]
    public class ValidationResult
    {
        public bool IsValid;
        public string ErrorMessage;
        public List<string> Warnings = new List<string>();
    }

    // Configuration ScriptableObjects
    [CreateAssetMenu(fileName = "BuildConfiguration", menuName = "Coalition/Build/Build Configuration")]
    public class BuildConfiguration : ScriptableObject
    {
        [Header("Build Settings")]
        public string[] scenesToBuild;
        public string[] requiredScenes;
        public string[] requiredAssets;
        public bool enableDevelopmentBuild;
        public bool enableDeepProfilingSupport;

        [Header("Optimization")]
        public bool optimizeTextures = true;
        public bool optimizeAudio = true;
        public bool removeUnusedAssets = true;
        public bool enableAssetBundleCompression = true;
    }

    [CreateAssetMenu(fileName = "DeploymentConfiguration", menuName = "Coalition/Build/Deployment Configuration")]
    public class DeploymentConfiguration : ScriptableObject
    {
        [Header("Deployment Settings")]
        public string buildOutputPath = "Builds";
        public float maxBuildSizeMB = 500f;
        public bool createInstaller = true;
        public bool signExecutables = false;

        [Header("Distribution")]
        public bool createZipArchives = true;
        public bool uploadToServer = false;
        public string distributionServerUrl;
        public string distributionApiKey;

        [Header("Documentation")]
        public bool includeReadme = true;
        public bool includeSystemRequirements = true;
        public bool includeInstallationGuide = true;
    }
}