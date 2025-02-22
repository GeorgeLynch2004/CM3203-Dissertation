/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using UnityEditor;

namespace Meta.XR.Editor.Rules
{
    [InitializeOnLoad]
    internal static class AndroidCompatibility
    {
        static AndroidCompatibility()
        {
            // [Required] Generate Android Manifest
            OVRProjectSetup.AddTask(
                level: OVRProjectSetup.TaskLevel.Optional,
                group: OVRProjectSetup.TaskGroup.Compatibility,
                platform: BuildTargetGroup.Android,
                isDone: _ => OVRManifestPreprocessor.DoesAndroidManifestExist(),
                message: "An Android Manifest file is required",
                fix: _ => OVRManifestPreprocessor.GenerateManifestForSubmission(),
                fixMessage: "Generates a default Manifest file"
            );

            // [Required] Android minimum level API
            OVRProjectSetup.AddTask(
                level: OVRProjectSetup.TaskLevel.Required,
                group: OVRProjectSetup.TaskGroup.Compatibility,
                platform: BuildTargetGroup.Android,
                isDone: _ => PlayerSettings.Android.minSdkVersion >= MinimumAPILevel,
                message: $"Minimum Android API Level must be at least {MinimumAPILevelName}",
                fix: _ => PlayerSettings.Android.minSdkVersion = MinimumAPILevel,
                fixMessage: $"PlayerSettings.Android.minSdkVersion = {MinimumAPILevel}"
            );

            // [Recommended] Android target level API
            OVRProjectSetup.AddTask(
                level: OVRProjectSetup.TaskLevel.Recommended,
                group: OVRProjectSetup.TaskGroup.Compatibility,
                platform: BuildTargetGroup.Android,
                isDone: _ => PlayerSettings.Android.targetSdkVersion == TargetAPILevel,
                message: $"Target API should be set to {TargetAPILevelName} as to ensure latest version",
                fix: _ => PlayerSettings.Android.targetSdkVersion = TargetAPILevel,
                fixMessage: $"PlayerSettings.Android.targetSdkVersion = {TargetAPILevelName}"
            );

            // [Required] Install Location
            OVRProjectSetup.AddTask(
                level: OVRProjectSetup.TaskLevel.Recommended,
                group: OVRProjectSetup.TaskGroup.Compatibility,
                platform: BuildTargetGroup.Android,
                isDone: _ =>
                    PlayerSettings.Android.preferredInstallLocation == AndroidPreferredInstallLocation.Auto,
                message: "Install Location should be set to \"Automatic\"",
                fix: _ =>
                    PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto,
                fixMessage: "PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto"
            );

            // [Required] : IL2CPP when ARM64, [Recommended] : IL2CPP
            OVRProjectSetup.AddTask(
                conditionalLevel: _ =>
                    IsTargetingARM64 ? OVRProjectSetup.TaskLevel.Required : OVRProjectSetup.TaskLevel.Recommended,
                group: OVRProjectSetup.TaskGroup.Compatibility,
                platform: BuildTargetGroup.Android,
                isDone: buildTargetGroup =>
                    PlayerSettings.GetScriptingBackend(buildTargetGroup) == ScriptingImplementation.IL2CPP,
                conditionalMessage: _ =>
                    IsTargetingARM64
                        ? "Building the ARM64 architecture requires using IL2CPP as the scripting backend"
                        : "Using IL2CPP as the scripting backend is recommended",
                fix: buildTargetGroup =>
                    PlayerSettings.SetScriptingBackend(buildTargetGroup, ScriptingImplementation.IL2CPP),
                fixMessage: "PlayerSettings.SetScriptingBackend(buildTargetGroup, ScriptingImplementation.IL2CPP)"
            );

            // [Required] Use ARM64 target architecture
            OVRProjectSetup.AddTask(
                level: OVRProjectSetup.TaskLevel.Required,
                group: OVRProjectSetup.TaskGroup.Compatibility,
                platform: BuildTargetGroup.Android,
                isDone: _ => IsTargetingARM64,
                message: "Use ARM64 as target architecture",
                fix: SetARM64Target,
                fixMessage: "PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64"
            );

            // [Required] Check that Android TV Compatibility is disabled
            OVRProjectSetup.AddTask(
                level: OVRProjectSetup.TaskLevel.Required,
                group: OVRProjectSetup.TaskGroup.Compatibility,
                platform: BuildTargetGroup.Android,
                isDone: _ => !PlayerSettings.Android.androidTVCompatibility,
                message: "Apps with Android TV Compatibility enabled are not accepted by the Oculus Store",
                fix: _ => PlayerSettings.Android.androidTVCompatibility = false,
                fixMessage: "PlayerSettings.Android.androidTVCompatibility = false"
            );

#if UNITY_2023_2_OR_NEWER
            // [Required] Force using GameActivity on Unity 2023.2+ (reference: T169740072)
            OVRProjectSetup.AddTask(
                level: OVRProjectSetup.TaskLevel.Required,
                group: OVRProjectSetup.TaskGroup.Compatibility,
                platform: BuildTargetGroup.Android,
                isDone: buildTargetGroup =>
                    PlayerSettings.Android.applicationEntry == AndroidApplicationEntry.GameActivity,
                message: "Always specify single \"GameActivity\" application entry on Unity 2023.2+",
                fix: buildTargetGroup =>
                    PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.GameActivity,
                fixMessage: "PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.GameActivity"
            );
#endif
        }

        private static AndroidSdkVersions MinimumAPILevel
            => Enum.TryParse("AndroidApiLevel32", out AndroidSdkVersions androidSdkVersion) ?
                androidSdkVersion : AndroidSdkVersions.AndroidApiLevel29;

        private static AndroidSdkVersions TargetAPILevel => AndroidSdkVersions.AndroidApiLevelAuto;

        private static string MinimumAPILevelName => ComputeTargetAPILevelNumericalName(MinimumAPILevel);

        private static string TargetAPILevelName => ComputeTargetAPILevelNumericalName(TargetAPILevel);

        private static string ComputeTargetAPILevelNumericalName(AndroidSdkVersions version)
            => version == AndroidSdkVersions.AndroidApiLevelAuto ? "Auto" : $"{(int)version}";

        public static bool IsTargetingARM64 =>
            (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != 0;

        public static readonly Action<BuildTargetGroup> SetARM64Target = (_) =>
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
    }
}
