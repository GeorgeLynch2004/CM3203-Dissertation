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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Oculus.VR.Editor;
using Meta.XR.Editor;
using Meta.XR.Editor.Tags;
using Oculus.VR.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.BuildingBlocks.Editor
{
    [InitializeOnLoad]
    internal static class BlocksContentManager
    {
        private const double CacheDurationInHours = 6;
        private const string CommonTag = "Common";
        private const string DownloadPath = "https://www.facebook.com/building-blocks-content";
        private static readonly RemoteContentDownloader Downloader;

        private static BlockData[] _contentFilter;
        private static Dictionary<Tag, BlockUrl[]> _documentationsData;

        static BlocksContentManager()
        {
            Downloader = new RemoteContentDownloader(CacheDurationInHours, "bb_content.json", DownloadPath);
#pragma warning disable CS4014
            InitializeAsync();
#pragma warning restore CS4014
        }

        public static async Task InitializeAsync()
        {
            var successfulLoad = await Reload(false);
            if (!successfulLoad)
            {
                await Reload(true);
            }
        }

        public static async Task<bool> Reload(bool forceRedownload)
        {
            if (forceRedownload)
            {
                Downloader.ClearCache();
            }
            var result = await Downloader.RefreshAndLoad();
            if (result.Item1)
            {
                return LoadContentJsonData(result.Item2);
            }
            else
            {
                return false;
            }
        }

        internal static BlockUrl[] GetBlockUrls(Tag tag)
        {
            if (_documentationsData == null)
                return Array.Empty<BlockUrl>();

            return _documentationsData.TryGetValue(tag, out var urls) ? urls : Array.Empty<BlockUrl>();
        }

        // For common / generic docs related to building blocks.
        internal static BlockUrl[] GetCommonDocs() => GetBlockUrls(CommonTag);


        #region Data Parsing

        // ReSharper disable InconsistentNaming
        [Serializable]
        internal struct BlockData
        {
            public string id;
            public string blockName;
            public string description;
            public string[] tags;
        }

        [Serializable]
        internal struct BlockDocumentation
        {
            public string tag;
            public BlockUrl[] urls;
        }

        [Serializable]
        internal struct BlockUrl
        {
            public string title;
            public string url;
        }

        [Serializable]
        internal struct BlockDataResponse
        {
            public BlockData[] content;
            public BlockDocumentation[] docs;
        }
        // ReSharper restore InconsistentNaming

        internal static BlockDataResponse ParseJsonData(string jsonData)
        {
            BlockDataResponse response;
            try
            {
                response = JsonUtility.FromJson<BlockDataResponse>(jsonData);
            }
            catch (Exception)
            {
                response = default;
            }

            response.content ??= Array.Empty<BlockData>();
            response.docs ??= Array.Empty<BlockDocumentation>();

            return response;
        }

        #endregion

        #region Blocks TextureContent

        internal static bool LoadContentJsonData(string jsonData)
        {
            var response = ParseJsonData(jsonData);

            _contentFilter = response.content;
            _documentationsData = new Dictionary<Tag, BlockUrl[]>();
            foreach (var doc in response.docs)
            {
                _documentationsData[doc.tag] = doc.urls;
            }

            return _contentFilter is { Length: > 0 };
        }


        public static IReadOnlyList<BlockBaseData> FilterBlockWindowContent(IReadOnlyList<BlockBaseData> content)
        {
            return FilterBlockWindowContent(content, _contentFilter);
        }

        private static void ClearOverrides(IEnumerable<BlockBaseData> content)
        {
            foreach (var block in content)
            {
                block.BlockName.RemoveOverride();
                block.Description.RemoveOverride();
                block.OverridableTags.RemoveOverride();
            }
        }

        internal static IReadOnlyList<BlockBaseData> FilterBlockWindowContent(IReadOnlyList<BlockBaseData> content,
            BlockData[] contentFilter)
        {
            if (contentFilter == null || contentFilter.Length == 0)
            {
                ClearOverrides(content);
                return content;
            }

            var contentFilterDictionary = contentFilter
                .Select((value, index) => new { value, index })
                .ToDictionary(pair => pair.value.id, pair => new { pair.value, pair.index });

            var filteredContent = content
                .Where(block => contentFilterDictionary.ContainsKey(block.Id))
                .OrderBy(block => contentFilterDictionary[block.Id].index)
                .ToList();

            foreach (var blockBaseData in filteredContent)
            {
                blockBaseData.BlockName.SetOverride(contentFilterDictionary[blockBaseData.Id].value.blockName);
                blockBaseData.Description.SetOverride(contentFilterDictionary[blockBaseData.Id].value.description);
                blockBaseData.OverridableTags.SetOverride(
                    GenerateTagArrayFromTags(contentFilterDictionary[blockBaseData.Id].value.tags));
            }

            return filteredContent;
        }

        private static TagArray GenerateTagArrayFromTags(string[] tags)
        {
            if (tags == null)
            {
                return null;
            }

            var tagArray = new TagArray();
            tagArray.Add(tags.Select(tag => new Tag(tag)));
            return tagArray;
        }

        #endregion
    }
}
