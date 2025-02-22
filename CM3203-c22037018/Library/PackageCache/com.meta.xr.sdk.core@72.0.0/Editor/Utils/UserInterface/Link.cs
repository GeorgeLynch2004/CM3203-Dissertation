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
using Meta.XR.Editor.Id;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Meta.XR.Editor.UserInterface
{
    internal abstract class LinkDescription
    {
        public GUIStyle Style;
        public GUIContent Content;
        public string Label => !string.IsNullOrEmpty(Content.text) ? Content.text : Content.image.name;
        public string Origin;
        public IIdentified OriginData;

        public void Draw()
        {
            if (!Valid) return;

            if (DrawInternal())
            {
                Click();
            }
        }

        public void Click()
        {
            if (!Valid) return;

            OnClicked();
            SendTelemetry();
        }

        public virtual bool Valid => Content.image != null || !string.IsNullOrEmpty(Content.text);

        protected abstract void OnClicked();

        protected virtual bool DrawInternal()
        {
            using var color = new Utils.ColorScope(Utils.ColorScope.Scope.All, Color.white);
            var position = GUILayoutUtility.GetRect(Content, Style);
            EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);
            return GUI.Button(position, Content, Style);
        }

        private void SendTelemetry()
        {
            var marker = OVRTelemetry.Start(Telemetry.MarkerId.LinkClick);
            marker = AddAnnotations(marker);
            marker.Send();
        }

        protected virtual OVRTelemetryMarker AddAnnotations(OVRTelemetryMarker marker)
        {
            var newMarker = marker.AddAnnotation(Telemetry.AnnotationType.Label, Label)
                .AddAnnotation(Telemetry.AnnotationType.Type, this.GetType().Name)
                .AddAnnotation(Telemetry.AnnotationType.Origin, Origin);

            if (OriginData != null)
            {
                newMarker = newMarker.AddAnnotation(Telemetry.AnnotationType.OriginData, OriginData.Id);
            }

            return newMarker;
        }
    }

    internal class UrlLinkDescription : LinkDescription
    {
        public string URL;

        public override bool Valid => base.Valid && URL != null;

        protected override void OnClicked()
        {
            Application.OpenURL(URL);
        }

        protected override OVRTelemetryMarker AddAnnotations(OVRTelemetryMarker marker)
        {
            return base.AddAnnotations(marker)
                .AddAnnotation(Telemetry.AnnotationType.Url, URL);
        }
    }

    internal class AssetLinkDescription : LinkDescription
    {
        public Object Asset;

        public override bool Valid => base.Valid && Asset != null;

        protected override void OnClicked()
        {
            EditorGUIUtility.PingObject(Asset);
            Selection.activeObject = Asset;
        }

        protected override OVRTelemetryMarker AddAnnotations(OVRTelemetryMarker marker)
        {
            return base.AddAnnotations(marker)
                .AddAnnotation(Telemetry.AnnotationType.Url, Asset.name);
        }
    }

    internal class ActionLinkDescription : LinkDescription
    {
        public Action Action;
        public IIdentified ActionData;

        public override bool Valid => base.Valid && Action != null;

        protected override void OnClicked()
        {
            Action?.Invoke();
        }

        protected override OVRTelemetryMarker AddAnnotations(OVRTelemetryMarker marker)
        {
            var newMarker = base.AddAnnotations(marker);

            if (ActionData != null)
            {
                newMarker = newMarker.AddAnnotation(Telemetry.AnnotationType.ActionData, ActionData.Id);
            }

            return newMarker;
        }
    }
}
