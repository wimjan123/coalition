# Unity 6 Package Installation and Project Setup Workflow
## COALITION Project - Detailed Implementation Guide

**Document Version**: 1.0
**Unity Target Version**: Unity 6.0.0f1
**Project**: COALITION
**Estimated Total Time**: 8-10 hours
**Prerequisites**: Unity Hub 3.8+ installed, Unity 6.0.0f1 downloaded

---

## Overview

This workflow provides 25 detailed micro-steps for setting up Unity 6 package dependencies and project architecture for the COALITION project. Each step includes specific commands, validation procedures, and rollback mechanisms.

### Core Package Dependencies
- **Newtonsoft.Json**: 3.2.1+ (JSON serialization)
- **UI Toolkit**: Built-in Unity 6 package (Modern UI framework)
- **Addressables**: 1.22.2+ (Asset management system)

### Architecture Goals
- Modular assembly definition structure
- Scalable package dependency management
- Robust error handling and validation
- Production-ready configuration

---

# PHASE 1: PRE-INSTALLATION SETUP AND VALIDATION

## Step 1.1: Unity Hub and Editor Validation (20 min, Low)

**Objective:** Verify Unity 6.0.0f1 installation and prepare development environment

**Prerequisites:** Unity Hub 3.8+ installed

**Actions:**
1. Launch Unity Hub and verify version: `Unity Hub > About`
2. Confirm Unity 6.0.0f1 is installed: `Unity Hub > Installs`
3. If missing, download Unity 6.0.0f1 with modules:
   - Windows Build Support (IL2CPP)
   - WebGL Build Support
   - Documentation
4. Create desktop shortcut for direct Unity 6 editor access
5. Set Unity Hub default editor version to 6.0.0f1

**Validation:**
- Unity Hub shows version 3.8+
- Unity 6.0.0f1 appears in Installs list with "Ready" status
- Required modules show green checkmarks
- Unity Editor launches successfully from Hub

**Rollback:**
- Reinstall Unity Hub from official Unity website
- Clear Unity Hub cache: `%AppData%\UnityHub` (Windows) or `~/Library/Application Support/UnityHub` (Mac)

**Files Modified:**
- Unity Hub preferences
- System PATH variables (if applicable)

---

## Step 1.2: Project Backup and Version Control Setup (25 min, Medium)

**Objective:** Create comprehensive backup system and initialize version control

**Prerequisites:** Step 1.1 completed, COALITION project directory exists

**Actions:**
1. Navigate to COALITION project directory: `/home/wvisser/coalition`
2. Create backup directory: `mkdir -p ./backups/pre-package-install`
3. Copy current project state:
   ```bash
   cp -r ./Assets ./backups/pre-package-install/Assets_backup
   cp -r ./ProjectSettings ./backups/pre-package-install/ProjectSettings_backup
   cp -r ./Packages ./backups/pre-package-install/Packages_backup
   ```
4. Initialize git repository (if not exists):
   ```bash
   git init
   git add .gitignore Assets/ ProjectSettings/ Packages/
   git commit -m "Initial project state before package installation"
   ```
5. Create package installation branch:
   ```bash
   git checkout -b feature/unity-package-setup
   ```

**Validation:**
- Backup directory exists with complete project copy
- Git repository initialized with initial commit
- Currently on feature/unity-package-setup branch
- .gitignore includes standard Unity patterns

**Rollback:**
- Restore from backup: `cp -r ./backups/pre-package-install/* ./`
- Reset git: `git reset --hard HEAD~1`

**Files Modified:**
- `./backups/pre-package-install/` (created)
- `.git/` (initialized)
- Current working branch

---

## Step 1.3: Package Manager Configuration (15 min, Low)

**Objective:** Configure Unity Package Manager for optimal performance and reliability

**Prerequisites:** Steps 1.1-1.2 completed, Unity Editor access

**Actions:**
1. Launch Unity Editor for COALITION project
2. Open Package Manager: `Window > Package Manager`
3. Configure Package Manager settings:
   - `Edit > Project Settings > Package Manager`
   - Set Registry: `https://packages.unity.com`
   - Enable "Show preview packages" if needed
4. Clear Package Manager cache:
   - Close Unity Editor
   - Delete cache: `~/Library/Unity/cache` (Mac) or `%LOCALAPPDATA%\Unity\cache` (Windows)
5. Restart Unity Editor and verify Package Manager loads

**Validation:**
- Package Manager opens without errors
- Registry shows "packages.unity.com"
- Built-in packages list loads successfully
- No network connection warnings

**Rollback:**
- Reset Package Manager settings to defaults
- Clear cache and restart Unity Editor

**Files Modified:**
- `ProjectSettings/PackageManagerSettings.asset`
- Local Package Manager cache

---

## Step 1.4: Network and Proxy Configuration Verification (20 min, Low)

**Objective:** Ensure reliable network connectivity for package downloads

**Prerequisites:** Step 1.3 completed

**Actions:**
1. Test Unity Package Manager network connectivity:
   - Package Manager > Advanced > Reset Packages to defaults
   - Monitor for successful package list refresh
2. Configure proxy settings (if required):
   - `Edit > Preferences > Network`
   - Set HTTP/HTTPS proxy if corporate network
3. Test package registry access:
   ```bash
   curl -I https://packages.unity.com
   ```
4. Verify DNS resolution:
   ```bash
   nslookup packages.unity.com
   ```
5. Configure firewall exceptions for Unity Editor

**Validation:**
- Package Manager refreshes package list without timeout
- HTTP 200 response from packages.unity.com
- DNS resolves Unity package registry
- No firewall blocking Unity Editor

**Rollback:**
- Reset network preferences to defaults
- Remove proxy configuration
- Restore original firewall rules

**Files Modified:**
- Unity Editor preferences
- System network configuration (if proxy configured)

---

## Step 1.5: Initial Project State Documentation (15 min, Low)

**Objective:** Document baseline project configuration before package installation

**Prerequisites:** Steps 1.1-1.4 completed

**Actions:**
1. Generate current package manifest snapshot:
   ```bash
   cp ./Packages/manifest.json ./docs/manifest-baseline.json
   ```
2. Document current assembly definitions:
   ```bash
   find ./Assets -name "*.asmdef" > ./docs/initial-assemblies.txt
   ```
3. Capture Package Manager state:
   - Package Manager > Advanced > Show dependencies
   - Screenshot dependency tree
4. Record Unity Editor version and modules:
   ```bash
   echo "Unity Editor: $(unity -version)" > ./docs/environment-baseline.txt
   ```
5. Create installation log file:
   ```bash
   touch ./docs/package-installation-log.txt
   echo "Package Installation Started: $(date)" >> ./docs/package-installation-log.txt
   ```

**Validation:**
- `manifest-baseline.json` contains current package list
- `initial-assemblies.txt` lists existing .asmdef files
- Environment baseline documented
- Installation log initialized

**Rollback:**
- Remove documentation files if needed
- No system impact from documentation

**Files Modified:**
- `./docs/manifest-baseline.json` (created)
- `./docs/initial-assemblies.txt` (created)
- `./docs/environment-baseline.txt` (created)
- `./docs/package-installation-log.txt` (created)

---

# PHASE 2: CORE PACKAGE DEPENDENCIES

## Step 2.1: Newtonsoft.Json Package Installation (25 min, Medium)

**Objective:** Install and configure Newtonsoft.Json 3.2.1+ for Unity 6 compatibility

**Prerequisites:** Phase 1 completed, Unity Editor running

**Actions:**
1. Open Package Manager: `Window > Package Manager`
2. Change package source to "Unity Registry"
3. Search for "Newtonsoft.Json" package
4. Select version 3.2.1 or latest compatible with Unity 6
5. Click "Install" and monitor installation progress
6. Wait for compilation to complete
7. Verify installation in Package Manager "In Project" view
8. Update installation log:
   ```bash
   echo "Newtonsoft.Json installed: $(date)" >> ./docs/package-installation-log.txt
   ```

**Validation:**
- Package appears in "In Project" list
- No compilation errors in Console
- `manifest.json` contains `"com.unity.nuget.newtonsoft-json": "3.2.1"`
- Assembly reload completes successfully

**Rollback:**
- Package Manager > Select Newtonsoft.Json > Remove
- Restore `manifest.json` from backup
- Delete package cache if needed

**Files Modified:**
- `./Packages/manifest.json`
- `./Library/PackageCache/` (package cache)
- `./docs/package-installation-log.txt`

---

## Step 2.2: Newtonsoft.Json Integration Testing (20 min, Medium)

**Objective:** Validate Newtonsoft.Json functionality and performance in Unity 6

**Prerequisites:** Step 2.1 completed

**Actions:**
1. Create test script directory: `./Assets/Tests/PackageValidation/`
2. Create NewtonsoftJsonTest.cs:
   ```csharp
   using UnityEngine;
   using Newtonsoft.Json;
   using System.Collections.Generic;

   public class NewtonsoftJsonTest : MonoBehaviour
   {
       [System.Serializable]
       public class TestData
       {
           public string name;
           public int value;
           public List<string> items;
       }

       void Start()
       {
           TestSerialization();
       }

       void TestSerialization()
       {
           var testData = new TestData
           {
               name = "COALITION Test",
               value = 42,
               items = new List<string> {"unity", "packages", "json"}
           };

           string json = JsonConvert.SerializeObject(testData, Formatting.Indented);
           Debug.Log($"Serialized: {json}");

           var deserialized = JsonConvert.DeserializeObject<TestData>(json);
           Debug.Log($"Deserialized: {deserialized.name}, {deserialized.value}");
       }
   }
   ```
3. Attach script to test GameObject in scene
4. Run test and verify Console output
5. Monitor performance impact with Profiler

**Validation:**
- Test script compiles without errors
- Console shows correct serialization/deserialization output
- No memory leaks detected in Profiler
- Performance within acceptable limits

**Rollback:**
- Delete test script and GameObject
- No permanent system changes

**Files Modified:**
- `./Assets/Tests/PackageValidation/NewtonsoftJsonTest.cs` (created)
- Test scene (temporary GameObject)

---

## Step 2.3: UI Toolkit Package Verification (20 min, Low)

**Objective:** Verify UI Toolkit built-in package is properly configured for Unity 6

**Prerequisites:** Step 2.2 completed

**Actions:**
1. Package Manager > Change to "Built-in packages"
2. Locate "UI Toolkit" package
3. Verify version compatibility with Unity 6.0.0f1
4. Check package dependencies and conflicts
5. Enable UI Toolkit if not already enabled
6. Verify UI Builder accessibility: `Window > UI Toolkit > UI Builder`
7. Create simple test UI document:
   - UI Builder > Create new UXML
   - Save as `./Assets/UI/TestDocument.uxml`

**Validation:**
- UI Toolkit appears in Built-in packages list
- Version shows Unity 6 compatibility
- UI Builder opens without errors
- Test UXML document creates successfully

**Rollback:**
- Disable UI Toolkit if issues occur
- Delete test UI assets
- No major system impact expected

**Files Modified:**
- UI Toolkit built-in package (enabled)
- `./Assets/UI/TestDocument.uxml` (created)

---

## Step 2.4: UI Toolkit Runtime Integration Test (30 min, Medium)

**Objective:** Validate UI Toolkit runtime functionality and binding systems

**Prerequisites:** Step 2.3 completed

**Actions:**
1. Create UI test directory: `./Assets/UI/Tests/`
2. Create UIToolkitTest.cs runtime script:
   ```csharp
   using UnityEngine;
   using UnityEngine.UIElements;

   public class UIToolkitTest : MonoBehaviour
   {
       public UIDocument uiDocument;

       void Start()
       {
           if (uiDocument == null) return;

           var root = uiDocument.rootVisualElement;

           var testButton = new Button(() => Debug.Log("UI Toolkit button clicked"))
           {
               text = "COALITION Test Button"
           };

           root.Add(testButton);

           Debug.Log("UI Toolkit runtime test completed");
       }
   }
   ```
3. Create corresponding UXML document:
   ```xml
   <ui:UXML xmlns:ui="UnityEngine.UIElements">
       <ui:VisualElement style="flex-grow: 1; background-color: rgba(60, 60, 60, 1);">
           <ui:Label text="COALITION UI Toolkit Test" style="font-size: 20px; color: white;"/>
       </ui:VisualElement>
   </ui:UXML>
   ```
4. Create test scene with UI Document component
5. Run runtime test and verify functionality

**Validation:**
- UI renders correctly at runtime
- Button interaction works properly
- Console shows successful test completion
- No rendering errors or warnings

**Rollback:**
- Delete UI test assets and scene
- Remove UI Document components
- No permanent system changes

**Files Modified:**
- `./Assets/UI/Tests/UIToolkitTest.cs` (created)
- `./Assets/UI/Tests/TestUI.uxml` (created)
- Test scene with UI Document

---

## Step 2.5: Addressables Package Installation (25 min, Medium)

**Objective:** Install and configure Addressables 1.22.2+ for asset management

**Prerequisites:** Steps 2.1-2.4 completed

**Actions:**
1. Package Manager > Unity Registry
2. Search for "Addressables" package
3. Select Addressables version 1.22.2 or latest Unity 6 compatible
4. Click "Install" and monitor progress
5. Wait for compilation and addressable system initialization
6. Verify installation in Package Manager "In Project"
7. Initialize Addressables system: `Window > Asset Management > Addressables > Groups`
8. Create default Addressables settings when prompted

**Validation:**
- Addressables appears in "In Project" packages
- Addressables Groups window opens successfully
- Default settings created in `Assets/AddressableAssetsData/`
- No compilation errors or warnings

**Rollback:**
- Package Manager > Remove Addressables
- Delete `Assets/AddressableAssetsData/` folder
- Restore manifest.json from backup

**Files Modified:**
- `./Packages/manifest.json` (updated)
- `./Assets/AddressableAssetsData/` (created)
- `./Library/PackageCache/` (package cache)

---

## Step 2.6: Addressables Configuration and Testing (30 min, High)

**Objective:** Configure Addressables system and validate asset loading functionality

**Prerequisites:** Step 2.5 completed

**Actions:**
1. Open Addressables Groups: `Window > Asset Management > Addressables > Groups`
2. Configure Addressables settings:
   - `Asset Management > Addressables > Settings`
   - Set Build Remote Catalog: false (for development)
   - Configure Local Build Path: `ServerData/[BuildTarget]`
3. Create test asset group:
   - Right-click in Groups window > Create Group > Templates > Packed Assets
   - Name: "COALITION_Test_Assets"
4. Create test addressable asset:
   - Create prefab: `./Assets/Prefabs/TestAddressablePrefab.prefab`
   - Mark as Addressable with address "test-prefab"
5. Create Addressables test script:
   ```csharp
   using UnityEngine;
   using UnityEngine.AddressableAssets;
   using UnityEngine.ResourceManagement.AsyncOperations;

   public class AddressablesTest : MonoBehaviour
   {
       async void Start()
       {
           var handle = Addressables.LoadAssetAsync<GameObject>("test-prefab");
           await handle.Task;

           if (handle.Status == AsyncOperationStatus.Succeeded)
           {
               Debug.Log("Addressables test: Asset loaded successfully");
               Instantiate(handle.Result);
           }
           else
           {
               Debug.LogError("Addressables test: Failed to load asset");
           }
       }
   }
   ```

**Validation:**
- Addressables Groups window shows configured groups
- Test asset marked as addressable successfully
- Test script loads asset without errors
- Asset instantiates correctly at runtime

**Rollback:**
- Delete Addressables settings and groups
- Remove addressable marks from assets
- Delete test scripts and prefabs

**Files Modified:**
- `./Assets/AddressableAssetsData/AddressableAssetSettings.asset`
- `./Assets/Prefabs/TestAddressablePrefab.prefab` (created)
- `./Assets/Tests/AddressablesTest.cs` (created)

---

## Step 2.7: Package Dependency Conflict Resolution (20 min, High)

**Objective:** Identify and resolve conflicts between installed packages

**Prerequisites:** Steps 2.1-2.6 completed

**Actions:**
1. Open Package Manager and check for dependency conflicts
2. Review Console for any package-related warnings or errors
3. Verify package compatibility matrix:
   - Newtonsoft.Json 3.2.1+ ✓ Unity 6
   - UI Toolkit (built-in) ✓ Unity 6
   - Addressables 1.22.2+ ✓ Unity 6
4. Check for circular dependencies:
   ```bash
   grep -r "dependencies" ./Packages/manifest.json
   ```
5. Resolve any identified conflicts:
   - Update package versions if needed
   - Remove conflicting packages
   - Add explicit dependency overrides
6. Force package resolution refresh:
   - `Assets > Reimport All`
   - Clear Library cache if needed

**Validation:**
- No red error messages in Console
- Package Manager shows all packages as "Ready"
- Build compiles successfully without package errors
- All test scripts from previous steps still function

**Rollback:**
- Restore manifest.json from backup
- Remove problematic packages
- Clear package cache and restart Unity

**Files Modified:**
- `./Packages/manifest.json` (potential updates)
- Package resolution cache

---

# PHASE 3: ASSEMBLY DEFINITION ARCHITECTURE

## Step 3.1: Core Assembly Definition Structure Planning (25 min, Medium)

**Objective:** Design modular assembly architecture for COALITION project scalability

**Prerequisites:** Phase 2 completed, all packages installed successfully

**Actions:**
1. Create assembly definition documentation:
   ```bash
   touch ./docs/assembly-architecture-plan.md
   ```
2. Plan assembly structure for COALITION:
   ```
   COALITION.Core.asmdef (Core functionality)
   ├── COALITION.Data.asmdef (Data models and serialization)
   ├── COALITION.UI.asmdef (UI components and systems)
   ├── COALITION.Gameplay.asmdef (Game logic and mechanics)
   └── COALITION.Tests.asmdef (Testing framework)
   ```
3. Define assembly dependencies:
   - Core: No dependencies (foundation)
   - Data: Depends on Core, Newtonsoft.Json
   - UI: Depends on Core, UI Toolkit
   - Gameplay: Depends on Core, Data, Addressables
   - Tests: Depends on all assemblies
4. Document assembly separation rationale
5. Plan compilation order and optimization

**Validation:**
- Assembly architecture document created
- Clear dependency hierarchy established
- No circular dependencies in design
- Logical separation of concerns achieved

**Rollback:**
- Delete planning documentation
- No system changes at this stage

**Files Modified:**
- `./docs/assembly-architecture-plan.md` (created)

---

## Step 3.2: Core Assembly Definition Creation (20 min, Medium)

**Objective:** Create foundational COALITION.Core assembly definition

**Prerequisites:** Step 3.1 completed

**Actions:**
1. Create core assembly directory: `mkdir -p ./Assets/Scripts/Core`
2. Create COALITION.Core.asmdef:
   ```json
   {
       "name": "COALITION.Core",
       "rootNamespace": "COALITION.Core",
       "references": [],
       "includePlatforms": [],
       "excludePlatforms": [],
       "allowUnsafeCode": false,
       "overrideReferences": false,
       "precompiledReferences": [],
       "autoReferenced": true,
       "defineConstraints": [],
       "versionDefines": [],
       "noEngineReferences": false
   }
   ```
3. Create core interface structure:
   ```csharp
   // ./Assets/Scripts/Core/ICoalitionSystem.cs
   namespace COALITION.Core
   {
       public interface ICoalitionSystem
       {
           void Initialize();
           void Shutdown();
           bool IsInitialized { get; }
       }
   }
   ```
4. Verify assembly compiles successfully
5. Update installation log

**Validation:**
- COALITION.Core.asmdef appears in Project window
- Assembly compiles without errors
- Core interfaces accessible from other scripts
- Assembly shows in Unity's Assembly Definition Inspector

**Rollback:**
- Delete COALITION.Core.asmdef and core scripts
- No other assemblies depend on this yet

**Files Modified:**
- `./Assets/Scripts/Core/COALITION.Core.asmdef` (created)
- `./Assets/Scripts/Core/ICoalitionSystem.cs` (created)

---

## Step 3.3: Data Assembly Definition with Newtonsoft Integration (30 min, High)

**Objective:** Create data layer assembly with JSON serialization capabilities

**Prerequisites:** Step 3.2 completed

**Actions:**
1. Create data assembly directory: `mkdir -p ./Assets/Scripts/Data`
2. Create COALITION.Data.asmdef:
   ```json
   {
       "name": "COALITION.Data",
       "rootNamespace": "COALITION.Data",
       "references": [
           "COALITION.Core",
           "Unity.Nuget.Newtonsoft-Json"
       ],
       "includePlatforms": [],
       "excludePlatforms": [],
       "allowUnsafeCode": false,
       "overrideReferences": false,
       "precompiledReferences": [],
       "autoReferenced": true,
       "defineConstraints": [],
       "versionDefines": [],
       "noEngineReferences": false
   }
   ```
3. Create data model base class:
   ```csharp
   // ./Assets/Scripts/Data/DataModelBase.cs
   using COALITION.Core;
   using Newtonsoft.Json;
   using UnityEngine;

   namespace COALITION.Data
   {
       [System.Serializable]
       public abstract class DataModelBase
       {
           [JsonProperty("id")]
           public string Id { get; set; } = System.Guid.NewGuid().ToString();

           [JsonProperty("timestamp")]
           public long Timestamp { get; set; } = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

           public virtual string ToJson()
           {
               return JsonConvert.SerializeObject(this, Formatting.Indented);
           }

           public static T FromJson<T>(string json) where T : DataModelBase
           {
               return JsonConvert.DeserializeObject<T>(json);
           }
       }
   }
   ```
4. Test assembly compilation and JSON functionality

**Validation:**
- Data assembly compiles successfully
- Can access COALITION.Core interfaces
- Newtonsoft.Json integration works
- JSON serialization test passes

**Rollback:**
- Delete COALITION.Data.asmdef and data scripts
- Remove assembly reference from any dependent assemblies

**Files Modified:**
- `./Assets/Scripts/Data/COALITION.Data.asmdef` (created)
- `./Assets/Scripts/Data/DataModelBase.cs` (created)

---

## Step 3.4: UI Assembly Definition with UI Toolkit Integration (30 min, High)

**Objective:** Create UI layer assembly with UI Toolkit framework integration

**Prerequisites:** Step 3.3 completed

**Actions:**
1. Create UI assembly directory: `mkdir -p ./Assets/Scripts/UI`
2. Create COALITION.UI.asmdef:
   ```json
   {
       "name": "COALITION.UI",
       "rootNamespace": "COALITION.UI",
       "references": [
           "COALITION.Core",
           "UnityEngine.UIElementsModule",
           "UnityEditor.UIElementsModule"
       ],
       "includePlatforms": [],
       "excludePlatforms": [],
       "allowUnsafeCode": false,
       "overrideReferences": false,
       "precompiledReferences": [],
       "autoReferenced": true,
       "defineConstraints": [],
       "versionDefines": [],
       "noEngineReferences": false
   }
   ```
3. Create UI system base class:
   ```csharp
   // ./Assets/Scripts/UI/UISystemBase.cs
   using COALITION.Core;
   using UnityEngine;
   using UnityEngine.UIElements;

   namespace COALITION.UI
   {
       public abstract class UISystemBase : MonoBehaviour, ICoalitionSystem
       {
           [SerializeField] protected UIDocument uiDocument;
           protected VisualElement rootElement;

           public bool IsInitialized { get; protected set; }

           public virtual void Initialize()
           {
               if (uiDocument == null)
               {
                   Debug.LogError($"UIDocument not assigned for {GetType().Name}");
                   return;
               }

               rootElement = uiDocument.rootVisualElement;
               SetupUI();
               IsInitialized = true;
           }

           protected abstract void SetupUI();

           public virtual void Shutdown()
           {
               IsInitialized = false;
           }
       }
   }
   ```
4. Test UI assembly compilation and UI Toolkit access

**Validation:**
- UI assembly compiles without errors
- Can access Core interfaces and UI Toolkit
- UI system base class instantiates correctly
- UIElements integration functional

**Rollback:**
- Delete COALITION.UI.asmdef and UI scripts
- Remove UI Toolkit references if issues persist

**Files Modified:**
- `./Assets/Scripts/UI/COALITION.UI.asmdef` (created)
- `./Assets/Scripts/UI/UISystemBase.cs` (created)

---

## Step 3.5: Gameplay Assembly Definition with Addressables Integration (30 min, High)

**Objective:** Create gameplay layer assembly with Addressables asset management

**Prerequisites:** Step 3.4 completed

**Actions:**
1. Create gameplay assembly directory: `mkdir -p ./Assets/Scripts/Gameplay`
2. Create COALITION.Gameplay.asmdef:
   ```json
   {
       "name": "COALITION.Gameplay",
       "rootNamespace": "COALITION.Gameplay",
       "references": [
           "COALITION.Core",
           "COALITION.Data",
           "Unity.Addressables"
       ],
       "includePlatforms": [],
       "excludePlatforms": [],
       "allowUnsafeCode": false,
       "overrideReferences": false,
       "precompiledReferences": [],
       "autoReferenced": true,
       "defineConstraints": [],
       "versionDefines": [],
       "noEngineReferences": false
   }
   ```
3. Create asset management system:
   ```csharp
   // ./Assets/Scripts/Gameplay/AssetManager.cs
   using COALITION.Core;
   using COALITION.Data;
   using UnityEngine;
   using UnityEngine.AddressableAssets;
   using UnityEngine.ResourceManagement.AsyncOperations;
   using System.Threading.Tasks;

   namespace COALITION.Gameplay
   {
       public class AssetManager : MonoBehaviour, ICoalitionSystem
       {
           public bool IsInitialized { get; private set; }

           public void Initialize()
           {
               Debug.Log("AssetManager initialized with Addressables integration");
               IsInitialized = true;
           }

           public async Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
           {
               var handle = Addressables.LoadAssetAsync<T>(address);
               await handle.Task;

               if (handle.Status == AsyncOperationStatus.Succeeded)
               {
                   return handle.Result;
               }

               Debug.LogError($"Failed to load asset: {address}");
               return null;
           }

           public void Shutdown()
           {
               IsInitialized = false;
           }
       }
   }
   ```
4. Verify gameplay assembly compiles and Addressables integration works

**Validation:**
- Gameplay assembly compiles successfully
- Can access Core, Data, and Addressables APIs
- Asset loading functionality works correctly
- No circular dependency warnings

**Rollback:**
- Delete COALITION.Gameplay.asmdef and gameplay scripts
- Remove assembly references from other assemblies

**Files Modified:**
- `./Assets/Scripts/Gameplay/COALITION.Gameplay.asmdef` (created)
- `./Assets/Scripts/Gameplay/AssetManager.cs` (created)

---

## Step 3.6: Testing Assembly Definition Creation (25 min, Medium)

**Objective:** Create comprehensive testing assembly with access to all modules

**Prerequisites:** Step 3.5 completed

**Actions:**
1. Create testing assembly directory: `mkdir -p ./Assets/Scripts/Tests`
2. Create COALITION.Tests.asmdef:
   ```json
   {
       "name": "COALITION.Tests",
       "rootNamespace": "COALITION.Tests",
       "references": [
           "COALITION.Core",
           "COALITION.Data",
           "COALITION.UI",
           "COALITION.Gameplay",
           "UnityEngine.TestRunner",
           "UnityEditor.TestRunner"
       ],
       "includePlatforms": [],
       "excludePlatforms": [],
       "allowUnsafeCode": false,
       "overrideReferences": false,
       "precompiledReferences": [],
       "autoReferenced": false,
       "defineConstraints": ["UNITY_INCLUDE_TESTS"],
       "versionDefines": [],
       "noEngineReferences": false
   }
   ```
3. Create integration test example:
   ```csharp
   // ./Assets/Scripts/Tests/IntegrationTests.cs
   using NUnit.Framework;
   using COALITION.Core;
   using COALITION.Data;
   using COALITION.UI;
   using COALITION.Gameplay;
   using UnityEngine;

   namespace COALITION.Tests
   {
       public class IntegrationTests
       {
           [Test]
           public void AllAssembliesAccessible()
           {
               // Test that all assemblies are properly referenced
               Assert.IsNotNull(typeof(ICoalitionSystem));
               Assert.IsNotNull(typeof(DataModelBase));
               Assert.IsNotNull(typeof(UISystemBase));
               Assert.IsNotNull(typeof(AssetManager));
           }
       }
   }
   ```
4. Run test to verify assembly access

**Validation:**
- Testing assembly compiles successfully
- Can access all COALITION assemblies
- Test Runner recognizes test assembly
- Integration test passes

**Rollback:**
- Delete COALITION.Tests.asmdef and test scripts
- Remove test runner dependencies if issues occur

**Files Modified:**
- `./Assets/Scripts/Tests/COALITION.Tests.asmdef` (created)
- `./Assets/Scripts/Tests/IntegrationTests.cs` (created)

---

# PHASE 4: INTEGRATION TESTING AND VALIDATION

## Step 4.1: Cross-Assembly Compilation Validation (20 min, Medium)

**Objective:** Verify all assemblies compile correctly and dependencies resolve

**Prerequisites:** Phase 3 completed, all assemblies created

**Actions:**
1. Force full project recompilation:
   - `Assets > Reimport All`
   - Clear Library folder and restart Unity
2. Check Assembly Definition Inspector for each assembly:
   - Select each .asmdef file
   - Verify "Compiled Assembly" shows no errors
   - Check dependency graph in Inspector
3. Monitor Console for compilation errors or warnings
4. Verify assembly load order in Unity Editor:
   - `Window > Analysis > Profiler`
   - Check Assembly Loading in CPU Usage
5. Run build test to verify all assemblies work in builds:
   - `File > Build Settings`
   - Build to temporary directory
   - Check for assembly-related build errors

**Validation:**
- All assemblies compile without errors
- No circular dependency warnings
- Build completes successfully
- Assembly load times within acceptable limits

**Rollback:**
- Fix compilation errors by adjusting assembly references
- Remove problematic assemblies if needed
- Restore from backup if major issues occur

**Files Modified:**
- No new files, validation of existing assemblies
- Temporary build output (deleted after validation)

---

## Step 4.2: Package Integration Stress Testing (30 min, High)

**Objective:** Test package functionality under load and various scenarios

**Prerequisites:** Step 4.1 completed

**Actions:**
1. Create comprehensive stress test script:
   ```csharp
   // ./Assets/Scripts/Tests/PackageStressTest.cs
   using UnityEngine;
   using System.Collections;
   using System.Threading.Tasks;
   using COALITION.Core;
   using COALITION.Data;
   using COALITION.Gameplay;
   using Newtonsoft.Json;
   using UnityEngine.AddressableAssets;

   public class PackageStressTest : MonoBehaviour, ICoalitionSystem
   {
       public bool IsInitialized { get; private set; }

       public async void Initialize()
       {
           await RunStressTests();
           IsInitialized = true;
       }

       private async Task RunStressTests()
       {
           // JSON serialization stress test
           for (int i = 0; i < 100; i++)
           {
               var testData = new { id = i, data = $"Test data {i}" };
               var json = JsonConvert.SerializeObject(testData);
               var deserialized = JsonConvert.DeserializeObject(json);
           }

           // Addressables loading stress test
           var tasks = new Task[10];
           for (int i = 0; i < tasks.Length; i++)
           {
               tasks[i] = LoadAssetStressTest();
           }
           await Task.WhenAll(tasks);

           Debug.Log("Package stress testing completed successfully");
       }

       private async Task LoadAssetStressTest()
       {
           // Test concurrent asset loading
           var handle = Addressables.LoadAssetAsync<GameObject>("test-prefab");
           await handle.Task;
           Addressables.Release(handle);
       }

       public void Shutdown() => IsInitialized = false;
   }
   ```
2. Create stress test scene and run for extended period
3. Monitor memory usage, CPU performance, and error logs
4. Test package functionality during scene transitions

**Validation:**
- Stress tests complete without crashes
- Memory usage remains stable
- No package-related performance degradation
- All packages function correctly under load

**Rollback:**
- Delete stress test scripts
- Investigate and fix any performance issues identified
- Adjust package configurations if needed

**Files Modified:**
- `./Assets/Scripts/Tests/PackageStressTest.cs` (created)
- Temporary stress test scene

---

## Step 4.3: Performance Baseline Establishment (25 min, Medium)

**Objective:** Create performance benchmarks for package-enhanced project

**Prerequisites:** Step 4.2 completed

**Actions:**
1. Create performance measurement script:
   ```csharp
   // ./Assets/Scripts/Tests/PerformanceBenchmark.cs
   using UnityEngine;
   using UnityEngine.Profiling;
   using System.Diagnostics;
   using COALITION.Core;

   public class PerformanceBenchmark : MonoBehaviour, ICoalitionSystem
   {
       public bool IsInitialized { get; private set; }

       public void Initialize()
       {
           RunBenchmarks();
           IsInitialized = true;
       }

       private void RunBenchmarks()
       {
           // Memory usage benchmark
           long memoryBefore = Profiler.GetTotalAllocatedMemory(false);

           // CPU performance benchmark
           Stopwatch stopwatch = Stopwatch.StartNew();

           // Simulate typical package operations
           PerformTypicalOperations();

           stopwatch.Stop();
           long memoryAfter = Profiler.GetTotalAllocatedMemory(false);

           UnityEngine.Debug.Log($"Performance Baseline:");
           UnityEngine.Debug.Log($"- Execution Time: {stopwatch.ElapsedMilliseconds}ms");
           UnityEngine.Debug.Log($"- Memory Delta: {(memoryAfter - memoryBefore) / 1024}KB");
           UnityEngine.Debug.Log($"- Total Memory: {memoryAfter / 1024 / 1024}MB");
       }

       private void PerformTypicalOperations()
       {
           // Simulate typical package usage patterns
           for (int i = 0; i < 1000; i++)
           {
               var testObj = new { data = i };
               var json = Newtonsoft.Json.JsonConvert.SerializeObject(testObj);
               Newtonsoft.Json.JsonConvert.DeserializeObject(json);
           }
       }

       public void Shutdown() => IsInitialized = false;
   }
   ```
2. Run benchmark tests and record results
3. Save baseline metrics to documentation:
   ```bash
   echo "Performance Baseline - $(date)" >> ./docs/performance-baseline.txt
   echo "Package Configuration: Newtonsoft.Json 3.2.1, UI Toolkit (built-in), Addressables 1.22.2" >> ./docs/performance-baseline.txt
   ```
4. Setup automated performance regression detection

**Validation:**
- Baseline performance metrics recorded
- No unexpected performance degradation detected
- Memory usage within acceptable limits
- Benchmark results documented for future comparison

**Rollback:**
- Delete benchmark scripts if not needed
- Remove performance monitoring overhead

**Files Modified:**
- `./Assets/Scripts/Tests/PerformanceBenchmark.cs` (created)
- `./docs/performance-baseline.txt` (created)

---

## Step 4.4: Error Handling and Recovery Testing (30 min, High)

**Objective:** Test package error scenarios and implement robust recovery mechanisms

**Prerequisites:** Step 4.3 completed

**Actions:**
1. Create error simulation test:
   ```csharp
   // ./Assets/Scripts/Tests/ErrorRecoveryTest.cs
   using UnityEngine;
   using System;
   using COALITION.Core;
   using Newtonsoft.Json;
   using UnityEngine.AddressableAssets;

   public class ErrorRecoveryTest : MonoBehaviour, ICoalitionSystem
   {
       public bool IsInitialized { get; private set; }

       public void Initialize()
       {
           TestErrorScenarios();
           IsInitialized = true;
       }

       private void TestErrorScenarios()
       {
           // Test JSON deserialization errors
           TestJsonErrors();

           // Test Addressables loading errors
           TestAddressableErrors();

           // Test UI Toolkit runtime errors
           TestUIErrors();

           UnityEngine.Debug.Log("Error recovery testing completed");
       }

       private void TestJsonErrors()
       {
           try
           {
               // Test malformed JSON
               JsonConvert.DeserializeObject("{invalid json}");
           }
           catch (JsonException ex)
           {
               UnityEngine.Debug.Log($"JSON error handled: {ex.Message}");
           }
       }

       private async void TestAddressableErrors()
       {
           try
           {
               // Test invalid address
               var handle = Addressables.LoadAssetAsync<GameObject>("nonexistent-asset");
               await handle.Task;
           }
           catch (Exception ex)
           {
               UnityEngine.Debug.Log($"Addressables error handled: {ex.Message}");
           }
       }

       private void TestUIErrors()
       {
           try
           {
               // Test null UI operations
               UnityEngine.UIElements.VisualElement nullElement = null;
               nullElement.Add(new UnityEngine.UIElements.Button());
           }
           catch (Exception ex)
           {
               UnityEngine.Debug.Log($"UI error handled: {ex.Message}");
           }
       }

       public void Shutdown() => IsInitialized = false;
   }
   ```
2. Create error recovery strategies for each package
3. Implement logging and monitoring for package errors
4. Test recovery mechanisms under various failure conditions

**Validation:**
- All error scenarios handled gracefully
- Recovery mechanisms function correctly
- No unhandled exceptions crash the application
- Error logging provides useful diagnostic information

**Rollback:**
- Remove error testing if it causes instability
- Adjust error handling strategies based on results

**Files Modified:**
- `./Assets/Scripts/Tests/ErrorRecoveryTest.cs` (created)
- Error handling additions to existing systems

---

## Step 4.5: Final Integration Validation and Documentation (25 min, Medium)

**Objective:** Comprehensive final validation and complete installation documentation

**Prerequisites:** Steps 4.1-4.4 completed

**Actions:**
1. Run complete integration test suite:
   - All package functionality tests
   - Assembly compilation verification
   - Performance benchmarks
   - Error recovery tests
2. Generate final package configuration report:
   ```bash
   echo "COALITION Unity 6 Package Installation - Final Report" > ./docs/installation-final-report.md
   echo "Installation Date: $(date)" >> ./docs/installation-final-report.md
   echo "" >> ./docs/installation-final-report.md
   echo "Installed Packages:" >> ./docs/installation-final-report.md
   grep "com.unity" ./Packages/manifest.json >> ./docs/installation-final-report.md
   echo "" >> ./docs/installation-final-report.md
   echo "Assembly Definitions Created:" >> ./docs/installation-final-report.md
   find ./Assets -name "*.asmdef" >> ./docs/installation-final-report.md
   ```
3. Create package maintenance procedures:
   - Update procedures for each package
   - Conflict resolution guidelines
   - Performance monitoring recommendations
4. Commit all changes to version control:
   ```bash
   git add .
   git commit -m "Complete Unity 6 package installation and assembly setup

   - Installed Newtonsoft.Json 3.2.1+ for JSON serialization
   - Configured UI Toolkit for modern UI development
   - Set up Addressables 1.22.2+ for asset management
   - Created modular assembly definition architecture
   - Implemented comprehensive testing and validation
   - Established performance baselines and error handling"
   ```

**Validation:**
- All tests pass successfully
- Complete documentation generated
- Version control properly updated
- Package installation fully documented and validated

**Rollback:**
- Full project restore from initial backup
- Git reset to pre-installation state if needed

**Files Modified:**
- `./docs/installation-final-report.md` (created)
- Version control history (committed changes)
- All project files (final validated state)

---

# POST-INSTALLATION CHECKLIST

## Verification Points
- [ ] All packages appear in Package Manager "In Project" list
- [ ] No compilation errors in Console
- [ ] All assembly definitions compile successfully
- [ ] Integration tests pass
- [ ] Performance benchmarks within acceptable limits
- [ ] Error handling mechanisms functional
- [ ] Documentation complete and version controlled

## Maintenance Schedule
- **Weekly**: Check for package updates in Package Manager
- **Monthly**: Run performance benchmarks and compare to baseline
- **Quarterly**: Review assembly architecture for optimization opportunities
- **Before Major Updates**: Full backup and validation testing

## Support Resources
- Unity 6 Package Manager documentation
- Newtonsoft.Json Unity integration guide
- UI Toolkit official documentation
- Addressables system documentation
- COALITION project assembly architecture guide

---

**Installation Complete**: Unity 6 package dependencies and modular assembly architecture successfully implemented for COALITION project.