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
using System.Reflection;
using Meta.XR.ImmersiveDebugger.Manager;
using UnityEngine;

namespace Meta.XR.ImmersiveDebugger.UserInterface.Generic
{
    public class Dropdown : Controller
    {
        private Flex _flex;
        private TweakEnum _tweak;
        private ButtonWithLabel _baseLabel;
        private bool IsMenuVisible => _flex.Visibility;

        public string Label
        {
            get => _baseLabel.Label;
            set
            {
                _baseLabel.Label = value;
                _tweak.Value = value;
            }
        }

        internal void SetupMenu(TweakEnum tweak)
        {
            _tweak = tweak;
            Label = _tweak.Value;
            SetupDropdownList();
        }

        protected override void Setup(Controller owner)
        {
            base.Setup(owner);
            _baseLabel = Append<ButtonWithLabel>("label");
            _baseLabel.LayoutStyle = Style.Instantiate<LayoutStyle>("DropdownValueItem");
            _baseLabel.TextStyle = Style.Load<TextStyle>("MemberValue");
            _baseLabel.BackgroundStyle = Style.Instantiate<ImageStyle>("DropdownValueBackground");
            _baseLabel.Callback += OnDropdownClick;

            var icon = _baseLabel.Append<Icon>("icon");
            icon.LayoutStyle = Style.Load<LayoutStyle>("DropdownArrowIcon");
            var style = Style.Load<ImageStyle>("DownArrowIcon");
            icon.Texture = style.icon;
            icon.Color = style.color;
        }

        private void OnDropdownClick() => SetDropdownMenuVisibility(!IsMenuVisible);

        internal void OnMenuItemClick(DropdownMenuItem menuItem)
        {
            Label = menuItem.Label;
            SetDropdownMenuVisibility(false);
        }

        private void SetDropdownMenuVisibility(bool visible)
        {
            if (visible) _flex.Show();
            else _flex.Hide();
        }

        private void HideDropdownItems() => _flex.Hide();

        private void SetupDropdownList()
        {
            _flex = Append<Flex>("list");
            _flex.LayoutStyle = Style.Load<LayoutStyle>("DropdownValuesFlex");
            var canvas = _flex.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = Utils.DropDownMenuSortOrder;

            _flex.gameObject.AddComponent<CanvasGroup>();

            var raycaster = _flex.gameObject.AddComponent<OVRRaycaster>();
            raycaster.sortOrder = Utils.DropDownMenuSortOrder;

            Array values = null;
            var fieldType = (_tweak.Member as FieldInfo)?.FieldType;
            var propertyType = (_tweak.Member as PropertyInfo)?.PropertyType;
            if (fieldType != null)
            {
                values = Enum.GetValues(fieldType);
            }
            else if (propertyType != null)
            {
                values = Enum.GetValues(propertyType);
            }

            foreach (var value in values)
            {
                AppendValue(value.ToString());
            }

            HideDropdownItems();
        }

        private void AppendValue(string data)
        {
            var value = _flex.Append<DropdownMenuItem>($"menu_item_{data}");
            value.Label = data;
            value.RegisterDropdownSourceMenu(this);
        }
    }
}
