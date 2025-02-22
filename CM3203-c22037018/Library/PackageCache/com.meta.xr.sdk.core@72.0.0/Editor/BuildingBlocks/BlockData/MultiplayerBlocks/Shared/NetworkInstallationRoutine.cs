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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Meta.XR.BuildingBlocks.Editor;
using UnityEngine;

namespace Meta.XR.MultiplayerBlocks.Shared.Editor
{
    public class NetworkInstallationRoutine : InstallationRoutine
    {
        internal enum NetworkImplementation
        {
            UnityNetcodeForGameObjects,
            PhotonFusion
        }

        internal enum MatchmakingType
        {
            AutoMatchmaking,
            CustomMatchmaking,
            None
        }

        [SerializeField]
        [Variant(Behavior = VariantAttribute.VariantBehavior.Definition,
            Description = "The underlying network Implementation that will be used for all network Blocks.",
            Default = NetworkImplementation.UnityNetcodeForGameObjects)]
        internal NetworkImplementation implementation;

        [SerializeField]
        [Variant(Behavior = VariantAttribute.VariantBehavior.Parameter,
            Description = "Indicates whether Matchmaking should also be included.",
            Condition = nameof(CanInstallMatchmaking),
            Default = MatchmakingType.AutoMatchmaking)]
        internal MatchmakingType installMatchmaking;

        protected bool CanInstallMatchmaking()
            => TargetBlockDataId != BlockDataIds.IAutoMatchmaking
               && TargetBlockDataId != BlockDataIds.INetworkManager
               && TargetBlockDataId != BlockDataIds.ICustomMatchmaking
               && !IsMatchmakingPresentInScene;

        private static bool IsMatchmakingPresentInScene =>
            Utils.GetBlock(BlockDataIds.IAutoMatchmaking)
            || Utils.GetBlock(BlockDataIds.ICustomMatchmaking);

        private bool ShouldInstallMatchmaking => installMatchmaking != MatchmakingType.None && CanInstallMatchmaking();
        private string MatchmakingBlockId => installMatchmaking switch
        {
            MatchmakingType.AutoMatchmaking => BlockDataIds.IAutoMatchmaking,
            MatchmakingType.CustomMatchmaking => BlockDataIds.ICustomMatchmaking,
            _ => null
        };

        internal override IEnumerable<string> ComputePackageDependencies(VariantsSelection variantSelection)
        {
            if (ShouldInstallMatchmaking)
            {
                var matchmakingBlock = Utils.GetBlockData(MatchmakingBlockId);
                var additionalDependencies = InterfaceBlockData.ComputePackageDependencies(matchmakingBlock as InterfaceBlockData, variantSelection);
                return base.ComputePackageDependencies(variantSelection).Concat(additionalDependencies);
            }

            return base.ComputePackageDependencies(variantSelection);
        }

        internal override IEnumerable<BlockData> ComputeOptionalDependencies(VariantsSelection variantSelection)
        {
            return ShouldInstallMatchmaking ? new BlockData[] { Utils.GetBlockData(MatchmakingBlockId) } : Enumerable.Empty<BlockData>();
        }

        public override async Task<List<GameObject>> InstallAsync(BlockData blockData, GameObject selectedGameObject)
        {
            // Installing Matchmaking for all networking blocks use cases if not present
            // As an optional dependency, developers can easily remove and use their own matchmaking.
            if (ShouldInstallMatchmaking)
            {
                await InstallMatchmaking(MatchmakingBlockId);
            }

            return await base.InstallAsync(blockData, selectedGameObject);
        }

        private static async Task InstallMatchmaking(string matchmakingBlockId)
        {
            var matchmakingBlockData = Utils.GetBlockData(matchmakingBlockId);
            await matchmakingBlockData.InstallWithDependencies();
        }
    }
}
