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
using System.Reflection;
using Meta.XR.Editor.StatusMenu;
using Meta.XR.Editor.Tags;
using Meta.XR.Editor.UserInterface;
using UnityEditor;
using UnityEngine;
using static Meta.XR.Editor.UserInterface.Styles.Constants;
using static Meta.XR.Editor.UserInterface.Utils;
using static Meta.XR.Editor.UserInterface.Styles;

namespace Meta.XR.BuildingBlocks.Editor
{
    [CustomEditor(typeof(BuildingBlock))]
    public class BuildingBlockEditor : UnityEditor.Editor
    {
        private const string NextStepLabel = "Next Steps";
        private const string NextStepHandle = "next_steps";

        private const string CustomizeYourBlockTitle = "Customize your block";

        private const string CustomizeYourBlockDescription =
            "All elements inside the Building Block are modifiable, like any other GameObjects and their components.";

        private const string AdvancedOptionsLabel = "Advanced Options";
        private const string AdvancedOptionsHandle = "advanced_options";

        private const string BreakOutBBConnectionsTitle = "Break out the Building Block connections.";

        private const string BreakOutBBConnectionsDescription =
            "In some cases, you may need to break out of the dependency checks of the Building Block. \nThis GameObject will not be considered as a Building Block anymore.";

        private const string BreakOutBBConnectionsButtonLabel = "Break Block Connection";

        private BuildingBlock _block;
        private BlockData _blockData;

        private bool _foldoutInstruction = true;

        public override void OnInspectorGUI()
        {
            _block = target as BuildingBlock;
            _blockData = _block.GetBlockData();

            if (_blockData == null)
            {
                return;
            }

            ShowThumbnail();
            DrawBlockHeader();
            ShowAdditionals();

            EditorGUILayout.Space();
            ShowBlockDataList("Dependencies", "No dependency blocks are required.", _blockData.GetAllDependencies().ToList());

            EditorGUILayout.Space();
            ShowBlockDataList("Used by", "No other blocks depend on this one.", _blockData.GetUsingBlockDatasInScene());

            EditorGUILayout.Space();
            ShowInstructions();

            EditorGUILayout.Space();
            DrawSectionWithIcon(Styles.Contents.UtilitiesIcon, () =>
            {
                EditorGUILayout.LabelField(CustomizeYourBlockTitle, GUIStyles.DialogTextStyle);
                EditorGUILayout.LabelField(CustomizeYourBlockDescription, Styles.GUIStyles.InfoStyle);
            });

            if (ShowFoldout(AdvancedOptionsHandle, AdvancedOptionsLabel,
                    EditorStyles.boldLabel.normal.textColor))
            {
                EditorGUILayout.Space();
                DrawSectionWithIcon(Styles.Contents.BreakBuildingBlockConnectionIcon, () =>
                {
                    EditorGUILayout.LabelField(BreakOutBBConnectionsTitle, GUIStyles.DialogTextStyle);
                    EditorGUILayout.LabelField(BreakOutBBConnectionsDescription, Styles.GUIStyles.InfoStyle);
                    EditorGUILayout.Space();

                    new ActionLinkDescription()
                    {
                        Content = new GUIContent(BreakOutBBConnectionsButtonLabel),
                        Style = Styles.GUIStyles.ThinButtonLarge,
                        Action = _block.BreakBlockConnection,
                        ActionData = _blockData,
                        Origin = OVRTelemetryConstants.BB.Origins.BlockInspector,
                        OriginData = _blockData

                    }.Draw();
                });
            }

        }

        protected virtual void ShowAdditionals()
        {
            // A placeholder for adding more details. E.g., Info box from GuidedSetup.
            // Override this function to implement your additional details.
        }

        private void DrawBlockHeader()
        {
            var horizontal = EditorGUILayout.BeginHorizontal(Styles.GUIStyles.BlockEditorDetails);
            horizontal.x -= DoubleMargin + MiniPadding;
            horizontal.y -= Padding;
            horizontal.width += DoubleMargin + Padding + Padding;
            EditorGUI.DrawRect(horizontal, Colors.CharcoalGraySemiTransparent);

            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            // Label
            UIHelpers.DrawBlockName(_blockData, OVRTelemetryConstants.BB.Origins.BlockInspector, _blockData,
                containerStyle: Styles.GUIStyles.LargeLinkButtonContainer,
                labelStyle: Styles.GUIStyles.LargeLabelStyleWhite,
                iconStyle: Styles.GUIStyles.LargeLinkIconStyle);

            // Tags
            Meta.XR.Editor.Tags.CommonUIHelpers.DrawList(_blockData.id + "_editor", _blockData.Tags, Tag.TagListType.Description);

            // Description
            EditorGUILayout.LabelField(_blockData.Description, Styles.GUIStyles.InfoStyle);

            EditorGUILayout.EndVertical();

            UIHelpers.DrawDocumentation(_blockData, OVRTelemetryConstants.BB.Origins.BlockInspector);

            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
        }

        private bool ShowFoldout(object handle, string label, Color color, bool openByDefault = false)
        {
            EditorGUILayout.Space();
            var foldout = false;
            using (new ColorScope(ColorScope.Scope.Content, color))
            {
                foldout = Foldout(handle, label, 0.0f,
                    GUIStyles.FoldoutHeader, openByDefault);
            }

            return foldout;
        }

        private void DrawSectionWithIcon(TextureContent icon, System.Action drawUIElements)
        {
            EditorGUILayout.BeginHorizontal(GUIStyles.DialogBox);
            EditorGUILayout.LabelField(icon, GUIStyles.DialogIconStyle,
                GUILayout.Width(GUIStyles.DialogIconStyle.fixedWidth));
            EditorGUILayout.BeginVertical();

            drawUIElements?.Invoke();

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void ShowVersionInfo()
        {
            EditorGUILayout.LabelField("Version", EditorStyles.boldLabel);

            var blockVersion = _block ? _block.Version : 0;
            var currentVersionStr = $"Current version: {blockVersion}.";
            if (_blockData.IsUpdateAvailableForBlock(_block))
            {
                EditorGUILayout.LabelField($"{currentVersionStr} Newest version: {_blockData.Version}.",
                    Styles.GUIStyles.InfoStyle);

                if (!GUILayout.Button($"Update to latest version ({_blockData.Version})"))
                {
                    return;
                }

                if (EditorUtility.DisplayDialog("Confirmation",
                        "Any changes done to this block will be lost. Do you want to proceed?", "Yes", "No"))
                {
#pragma warning disable CS4014
                    _blockData.UpdateBlockToLatestVersion(_block);
#pragma warning restore CS4014
                }
            }
            else
            {
                EditorGUILayout.LabelField($"{currentVersionStr} Block is up to date", Styles.GUIStyles.InfoStyle);
            }
        }



        private void ShowInstructions()
        {
            if (string.IsNullOrEmpty(_blockData.UsageInstructions)) return;

            EditorGUILayout.Space();
            _foldoutInstruction =
                EditorGUILayout.Foldout(_foldoutInstruction, "Block instructions", Styles.GUIStyles.FoldoutBoldLabel);
            if (_foldoutInstruction)
            {
                EditorGUILayout.LabelField(_blockData.UsageInstructions, EditorStyles.helpBox);
            }
        }

        private void ShowThumbnail()
        {
            var currentWidth = EditorGUIUtility.currentViewWidth;
            var expectedHeight = currentWidth / Styles.Constants.ThumbnailRatio;
            expectedHeight *= 0.5f;

            // Thumbnail
            var rect = GUILayoutUtility.GetRect(currentWidth, expectedHeight);
            rect.x -= 20;
            rect.width += 40;
            rect.y -= 4;
            GUI.DrawTexture(rect, _blockData.Thumbnail, ScaleMode.ScaleAndCrop);

            // Separator
            rect = GUILayoutUtility.GetRect(currentWidth, 1);
            rect.x -= 20;
            rect.width += 40;
            rect.y -= 4;
            GUI.DrawTexture(rect, Styles.Colors.AccentColor.ToTexture(),
                ScaleMode.ScaleAndCrop);
        }

        private void ShowBlockDataList(string listName, string noneNotice, IReadOnlyCollection<BlockData> list)
        {
            EditorGUILayout.LabelField(listName, EditorStyles.boldLabel);

            if (list.Count == 0)
            {
                EditorGUILayout.LabelField(noneNotice, Styles.GUIStyles.InfoStyle);
                return;
            }

            foreach (var dependency in list)
            {
                UIHelpers.DrawBlockRow(dependency, null, OVRTelemetryConstants.BB.Origins.BlockInspector, _blockData);
            }
        }
    }
}
