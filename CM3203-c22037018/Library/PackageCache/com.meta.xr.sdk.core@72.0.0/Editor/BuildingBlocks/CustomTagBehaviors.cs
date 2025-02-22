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

using Meta.XR.Editor.Tags;
using Meta.XR.Editor.UserInterface;
using UnityEditor;
using static Meta.XR.Editor.UserInterface.Styles.Colors;

namespace Meta.XR.BuildingBlocks.Editor
{
    [InitializeOnLoad]
    internal static class CustomTagBehaviors
    {
        // Collection tags
        private static Tag ImmersiveExperienceTag { get; }
        private static Tag MixedRealityTag { get; }
        private static Tag ColocationTag { get; }
        private static Tag CopresenceTag { get; }
        private static Tag TabletopTag { get; }
        private static Tag CompanionAppTag { get; }
        private static Tag InteractionsTag { get; }

        // Feature tags
        private static Tag AudioTag { get; }
        private static Tag CoreTag { get; }
        private static Tag InteractionTag { get; }
        private static Tag PlatformTag { get; }
        private static Tag VoiceTag { get; }
        private static Tag AvatarsTag { get; }
        private static Tag PassthroughTag { get; }
        private static Tag SceneTag { get; }
        private static Tag SpatialAnchorTag { get; }
        private static Tag HapticsTag { get; }
        private static Tag MultiplayerTag { get; }

        // Usage tags
        private static Tag UtilityTag { get; }
        private static Tag PrototypeTag => Utils.PrototypingTag;
        private static Tag ExperimentalTag => Utils.ExperimentalTag;

        static CustomTagBehaviors()
        {
            // Collection tags
            ImmersiveExperienceTag = new Tag("Immersive Experience");
            SetDefaultCollectionTagBehavior(ImmersiveExperienceTag);

            MixedRealityTag = new Tag("Mixed Reality");
            SetDefaultCollectionTagBehavior(MixedRealityTag);

            ColocationTag = new Tag("Colocation");
            SetDefaultCollectionTagBehavior(ColocationTag);

            CopresenceTag = new Tag("Copresence");
            SetDefaultCollectionTagBehavior(CopresenceTag);

            TabletopTag = new Tag("Tabletop");
            SetDefaultCollectionTagBehavior(TabletopTag);

            CompanionAppTag = new Tag("Companion App");
            SetDefaultCollectionTagBehavior(CompanionAppTag);

            InteractionsTag = new Tag("Interactions");
            SetDefaultCollectionTagBehavior(InteractionsTag);

            // Feature tags
            AudioTag = MakeFeatureTag("Audio", "sdk_icons/mono/audio_icon.png");
            CoreTag = MakeFeatureTag("Core", "sdk_icons/mono/core_icon.png");
            InteractionTag = MakeFeatureTag("Interaction", "sdk_icons/mono/interaction_icon.png");
            PlatformTag = MakeFeatureTag("Platform", "sdk_icons/mono/platform_icon.png");
            VoiceTag = MakeFeatureTag("Voice", "sdk_icons/mono/voice_icon.png");
            HapticsTag = MakeFeatureTag("Haptics", "sdk_icons/mono/haptics_icon.png");

            AvatarsTag = MakeFeatureTag("Avatars", "sdk_icons/mono/avatar_icon.png");
            PassthroughTag = MakeFeatureTag("Passthrough", "sdk_icons/mono/passthrough_icon.png");
            SceneTag = MakeFeatureTag("Scene", "sdk_icons/mono/scene_icon.png");
            SpatialAnchorTag = MakeFeatureTag("Spatial Anchor", "sdk_icons/mono/anchor_icon.png");
            MultiplayerTag = MakeFeatureTag("Multiplayer", "sdk_icons/mono/multiplayer_icon.png");

            // Usage tags
            UtilityTag = new Tag("Utility")
            {
                Behavior =
                {
                    Color = XR.Editor.UserInterface.Utils.HexToColor("#4ed998"),
                    Icon = TextureContent.CreateContent("ovr_icon_utilities.png", Utils.BuildingBlocksIcons),
                    Order = 100,
                    CanFilterBy = false,
                    ToggleableVisibility = true
                }
            };
        }

        private static Tag MakeFeatureTag(string tag, string iconPath)
        {
            return new Tag(tag)
            {
                Behavior =
                {
                    Order = -1,
                    Show = true,
                    CanFilterBy = true,
                    Icon = iconPath != null ? TextureContent.CreateContent(iconPath, TextureContent.Categories.Generic) : null
                }
            };
        }

        private static void SetDefaultCollectionTagBehavior(Tag tag)
        {
            _ = new CollectionTagBehavior(tag)
            {
                Order = -10,
                Color = CollectionTagsColor,
                Show = false,
                CanFilterBy = true,
                ShowOverlay = false,
            };
        }
    }
}
