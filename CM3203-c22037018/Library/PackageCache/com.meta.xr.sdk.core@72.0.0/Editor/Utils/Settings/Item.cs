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
using UnityEngine;

namespace Meta.XR.Editor.Settings
{
    internal abstract class Item
    {
        public const string KeyPrefix = "Meta.XR.SDK";

        public string Uid { get; }
        public string Label { get; }
        public string Key { get; }

        protected Item(string uid, string label = null)

        {
            Uid = uid;
            Label = label ?? uid;
            Key = KeyPrefix + "." + Uid;
        }
    }

    internal abstract class Item<T> : Item
    {
        public T Default { get; }
        public abstract T Value { get; set; }

        protected Item(string uid, T defaultValue, string label = null) : base(uid, label)
        {
            Default = defaultValue;
        }

        public abstract void AppendToMenu(GenericMenu menu, Action callback = null);

        public void Reset()
        {
            Value = Default;
        }
    }

    internal abstract class Bool : Item<bool>
    {
        protected Bool(string uid, bool defaultValue, string label = null)
            : base(uid, defaultValue, label)
        {
        }

        public override void AppendToMenu(GenericMenu menu, Action callback = null)
        {
            menu.AddItem(new GUIContent(Label), Value, () =>
            {
                Value = !Value;
                callback?.Invoke();
            });
        }
    }

    internal abstract class Float : Item<float>
    {
        protected Float(string uid, float defaultValue, string label = null)
            : base(uid, defaultValue, label)
        {
        }

        public override void AppendToMenu(GenericMenu menu, Action callback = null)
        {
            // Do not Append to a GenericMenu
        }
    }

    internal class UserFloat : Float
    {
        public UserFloat(string uid, float defaultValue, string label = null)
            : base(uid, defaultValue, label)
        {
        }

        public override float Value
        {
            get => EditorPrefs.GetFloat(Key, Default);
            set => EditorPrefs.SetFloat(Key, value);
        }
    }

    internal class UserBool : Bool
    {
        public UserBool(string uid, bool defaultValue, string label = null)
            : base(uid, defaultValue, label)
        {
        }

        public override bool Value
        {
            get => EditorPrefs.GetBool(Key, Default);
            set => EditorPrefs.SetBool(Key, value);
        }
    }
}

