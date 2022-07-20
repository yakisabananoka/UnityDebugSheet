﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityDebugSheet.Runtime.Core.Scripts.DefaultImpl.CellParts;
using UnityDebugSheet.Runtime.Foundation.PageNavigator;
using UnityEngine;
using UnityEngine.UI;

namespace UnityDebugSheet.Runtime.Core.Scripts.DefaultImpl.Cells
{
    public sealed class MultiPickerCell : Cell<MultiPickerCellModel>
    {
        [SerializeField] private LayoutElement _layoutElement;

        public CellIcon icon;
        public CellTexts cellTexts;
        public Button button;

        private MultiPickingPage _pickingPage;

        protected override void SetModel(MultiPickerCellModel model)
        {
            // Icon
            icon.Setup(model.Icon);
            icon.gameObject.SetActive(model.Icon.Sprite != null);

            //Texts
            cellTexts.Text = model.Text;
            cellTexts.SubText = GetSubText(model);
            cellTexts.TextColor = model.TextColor;
            cellTexts.SubTextColor = model.SubTextColor;

            // Click Event
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnClicked(model));

            // Height
            _layoutElement.preferredHeight = 114;

            // Reload the picking page if it is already created.
            if (_pickingPage != null)
                _pickingPage.Setup(model.Options, model.ActiveOptionIndices);
        }

        private void OnClicked(MultiPickerCellModel model)
        {
            void OnLoadPickingPage(MultiPickingPage page)
            {
                IEnumerator OnWillPushEnter()
                {
                    _pickingPage = page;
                    yield break;
                }

                IEnumerator OnWillPopExit()
                {
                    cellTexts.SubText = GetSubText(model);
                    model.InvokeConfirmed();
                    yield break;
                }

                void OnDidPopExit()
                {
                    _pickingPage = null;
                }

                page.AddLifecycleEvent(onWillPushEnter: OnWillPushEnter, onWillPopExit: OnWillPopExit,
                    onDidPopExit: OnDidPopExit);

                page.Setup(model.Options, model.ActiveOptionIndices);
                page.OptionActiveStateChanged += x =>
                {
                    var (index, isActive) = x;
                    if (isActive)
                        model.ActiveOptionIndices.Add(index);
                    else
                        model.ActiveOptionIndices.Remove(index);
                    model.InvokeOptionActiveStateChanged(index, isActive);
                };
            }

            DebugSheet.Of(transform).PushPage<MultiPickingPage>(true, model.Text, OnLoadPickingPage);
            model.InvokeClicked();
        }

        private static string GetSubText(MultiPickerCellModel model)
        {
            var optionsText = new StringBuilder();
            for (var i = 0; i < model.Options.Count; i++)
            {
                if (!model.ActiveOptionIndices.Contains(i))
                    continue;

                if (optionsText.Length >= 1)
                    optionsText.Append(", ");

                optionsText.Append(model.Options[i]);
            }

            return optionsText.ToString();
        }
    }

    public sealed class MultiPickerCellModel : CellModel
    {
        private List<string> _options = new List<string>();

        public List<int> ActiveOptionIndices { get; private set; } = new List<int>();

        public string Text { get; set; }

        public Color TextColor { get; set; } = Color.black;

        public Color SubTextColor { get; set; } = Color.gray;

        public CellIconModel Icon { get; } = new CellIconModel();

        public IReadOnlyList<string> Options => _options;

        /// <summary>
        ///     Event when this cell is clicked.
        /// </summary>
        public event Action Clicked;

        /// <summary>
        ///     Event that is called before the page to select options is closed.
        /// </summary>
        public event Action Confirmed;

        /// <summary>
        ///     Event when option state is changed.
        /// </summary>
        public event Action<int, bool> OptionStateChanged;

        public void SetOptions(IEnumerable<string> options, IEnumerable<int> activeOptionIndices)
        {
            _options = new List<string>(options);
            ActiveOptionIndices = new List<int>(activeOptionIndices);
        }

        internal void InvokeClicked()
        {
            Clicked?.Invoke();
        }

        internal void InvokeConfirmed()
        {
            Confirmed?.Invoke();
        }

        internal void InvokeOptionActiveStateChanged(int index, bool isOn)
        {
            OptionStateChanged?.Invoke(index, isOn);
        }
    }
}