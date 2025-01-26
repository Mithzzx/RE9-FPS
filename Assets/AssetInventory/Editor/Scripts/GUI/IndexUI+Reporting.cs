using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace AssetInventory
{
    public partial class IndexUI
    {
        private Vector2 _reportScrollPos;

        private List<AssetInfo> _assetUsage;
        private List<string> _usedAssets;
        private List<AssetInfo> _identifiedFiles;
        private List<AssetInfo> _selectedReportEntries;
        private AssetInfo _selectedReportEntry;

        private long _reportTreeSubPackageCount;
        private long _reportTreeSelectionSize;
        private readonly Dictionary<string, Tuple<int, Color>> _reportBulkTags = new Dictionary<string, Tuple<int, Color>>();

        [SerializeField] private MultiColumnHeaderState reportMchState;
        private Rect ReportTreeRect => new Rect(20, 0, position.width - 40, position.height - 60);
        private TreeViewWithTreeModel<AssetInfo> ReportTreeView
        {
            get
            {
                if (_reportTreeViewState == null) _reportTreeViewState = new TreeViewState();

                MultiColumnHeaderState headerState = ReportTreeViewControl.CreateDefaultMultiColumnHeaderState(ReportTreeRect.width);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(reportMchState, headerState)) MultiColumnHeaderState.OverwriteSerializedFields(reportMchState, headerState);
                reportMchState = headerState;

                if (_reportTreeView == null)
                {
                    MultiColumnHeader mch = new MultiColumnHeader(headerState);
                    mch.canSort = false;
                    mch.height = MultiColumnHeader.DefaultGUI.minimumHeight;
                    mch.ResizeToFit();

                    _reportTreeView = new ReportTreeViewControl(_reportTreeViewState, mch, ReportTreeModel);
                    _reportTreeView.OnSelectionChanged += OnReportTreeSelectionChanged;
                    _reportTreeView.OnDoubleClickedItem += OnReportTreeDoubleClicked;
                    _reportTreeView.Reload();
                }
                return _reportTreeView;
            }
        }
        private TreeViewWithTreeModel<AssetInfo> _reportTreeView;
        private TreeViewState _reportTreeViewState;

        private TreeModel<AssetInfo> ReportTreeModel
        {
            get
            {
                if (_reportTreeModel == null) _reportTreeModel = new TreeModel<AssetInfo>(new List<AssetInfo> {new AssetInfo().WithTreeData("Root", depth: -1)});
                return _reportTreeModel;
            }
        }
        private TreeModel<AssetInfo> _reportTreeModel;

        private void DrawReportingTab()
        {
            int assetUsageCount = _assetUsage?.Count ?? 0;
            int identifiedFilesCount = _identifiedFiles?.Count ?? 0;
            int identifiedAssetsCount = _usedAssets?.Count ?? 0;
            int width = 130;

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();

            EditorGUILayout.HelpBox("This view tries to identify used packages inside the current project. It will use guids. If package authors have shared files between projects this can result in multiple hits.", MessageType.Info);
            EditorGUILayout.Space();

            GUILabelWithText("Project files", $"{assetUsageCount:N0}", width);
            if (assetUsageCount > 0)
            {
                GUILabelWithText("Identified packages", $"{identifiedAssetsCount:N0}", width);
                GUILabelWithText("Identified files", $"{identifiedFilesCount:N0}" + " (" + Mathf.RoundToInt((float)identifiedFilesCount / assetUsageCount * 100f) + "%)", width);
            }
            else
            {
                GUILabelWithText("Identified packages", "None", width);
                GUILabelWithText("Identified files", "None", width);
            }

            if (_usedAssets != null && _usedAssets.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Identified Packages", EditorStyles.largeLabel);

                GUILayout.BeginVertical();
                int left = 0;
                int yStart = 160;
                ReportTreeView.OnGUI(new Rect(left, yStart, position.width - UIStyles.INSPECTOR_WIDTH - left - 5, position.height - yStart));
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();
            GUILayout.BeginVertical("Actions", "window", GUILayout.Width(UIStyles.INSPECTOR_WIDTH));
            EditorGUILayout.Space();

            if (_usageCalculationInProgress)
            {
                EditorGUI.BeginDisabledGroup(AssetProgress.CancellationRequested);
                if (GUILayout.Button("Stop Identification")) AssetProgress.CancellationRequested = true;
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.LabelField("Identification Progress", EditorStyles.boldLabel);
                UIStyles.DrawProgressBar(AssetProgress.MainProgress / (float)AssetProgress.MainCount, $"{AssetProgress.MainProgress}/{AssetProgress.MainCount}");
                EditorGUILayout.LabelField(AssetProgress.CurrentMain);
                EditorGUILayout.Space();
            }
            else
            {
                if (GUILayout.Button("Identify Used Packages", GUILayout.Height(50))) CalculateAssetUsage();
            }
            if (GUILayout.Button("Export Data..."))
            {
                ExportUI exportUI = ExportUI.ShowWindow();
                exportUI.Init(_assets, 0);
            }
            EditorGUILayout.Space();
            GUILayout.EndVertical();
            EditorGUILayout.Space();

            _reportScrollPos = GUILayout.BeginScrollView(_reportScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandWidth(true));
            if (_selectedReportEntry != null)
            {
                DrawPackageDetails(_selectedReportEntry, true);
                EditorGUILayout.Space();
            }
            if (_selectedReportEntry == null && _selectedReportEntries != null && _selectedReportEntries.Count > 0)
            {
                DrawBulkPackageActions(_selectedReportEntries, _reportTreeSubPackageCount, _reportBulkTags, _reportTreeSelectionSize, -1, -1, false);
                EditorGUILayout.Space();
            }

            GUILayout.BeginVertical("Project View Selection", "window", GUILayout.Width(UIStyles.INSPECTOR_WIDTH));
            EditorGUILayout.Space();

            if (_pvSelection != null && _pvSelection.Length > 0)
            {
                if (_pvSelection.Length > 1)
                {
                    EditorGUILayout.HelpBox("Multiple files are selected. This is not supported.", MessageType.Warning);
                }
            }
            if (string.IsNullOrEmpty(_pvSelectedPath))
            {
                EditorGUILayout.HelpBox("Select any file in the Unity Project View to identify what package it belongs to.", MessageType.Info);
            }
            else
            {
                GUILabelWithText("Folder", Path.GetDirectoryName(_pvSelectedPath));
                GUILabelWithText("Selection", Path.GetFileName(_pvSelectedPath));

                if (_pvSelectionChanged || _pvSelectedAssets == null)
                {
                    _pvSelectedAssets = AssetUtils.Guid2File(Selection.assetGUIDs[0]);
                    AssetUtils.LoadTextures(_pvSelectedAssets, new CancellationTokenSource().Token);
                }
                if (_pvSelectedAssets.Count == 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("Could not identify package. Guid not found in local database.", MessageType.Info);
                }
                if (_pvSelectedAssets.Count > 1)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("The file was matched with multiple packages. This can happen if identical files were contained in multiple packages.", MessageType.Info);
                }
                foreach (AssetInfo info in _pvSelectedAssets)
                {
                    EditorGUILayout.Space();
                    DrawPackageDetails(info, false, true, false);
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private async void CalculateAssetUsage()
        {
            AssetProgress.CancellationRequested = false;
            _usageCalculationInProgress = true;

            _assetUsage = await new AssetUsage().Calculate();
            _usedAssets = _assetUsage.Select(info => info.GetDisplayName()).Distinct().Where(a => !string.IsNullOrEmpty(a)).ToList();
            _identifiedFiles = _assetUsage.Where(info => info.CurrentState != Asset.State.Unknown).ToList();

            // add installed packages
            Dictionary<string, PackageInfo> packageCollection = AssetStore.GetProjectPackages();
            if (packageCollection != null)
            {
                int unmatchedCount = 0;
                foreach (PackageInfo packageInfo in packageCollection.Values)
                {
                    if (packageInfo.source == PackageSource.BuiltIn) continue;

                    AssetInfo matchedAsset = _assets.FirstOrDefault(info => info.SafeName == packageInfo.name);
                    if (matchedAsset == null)
                    {
                        Debug.Log($"Registry package '{packageInfo.name}' is not yet indexed, information will be incomplete.");
                        matchedAsset = new AssetInfo();
                        matchedAsset.AssetSource = Asset.Source.RegistryPackage;
                        matchedAsset.SafeName = packageInfo.name;
                        matchedAsset.DisplayName = packageInfo.displayName;
                        matchedAsset.Version = packageInfo.version;
                        matchedAsset.Id = int.MaxValue - unmatchedCount;
                        matchedAsset.AssetId = int.MaxValue - unmatchedCount;
                        unmatchedCount++;
                    }
                    _assetUsage.Add(matchedAsset);

                    string packageName = packageInfo.displayName + " - " + packageInfo.version;
                    if (!_usedAssets.Contains(packageName)) _usedAssets.Add(packageName);
                }
            }
            _usedAssets.Sort();
            _requireReportTreeRebuild = true;
            _usageCalculationInProgress = false;
        }

        private void CreateReportTree()
        {
            _requireReportTreeRebuild = false;
            List<AssetInfo> data = new List<AssetInfo>();
            AssetInfo root = new AssetInfo().WithTreeData("Root", depth: -1);
            data.Add(root);

            if (_assetUsage != null)
            {
                // apply filters
                IEnumerable<AssetInfo> filteredAssets = _assetUsage.GroupBy(a => a.AssetId).Select(a => a.First()).Where(a => !string.IsNullOrEmpty(a.GetDisplayName()));

                IOrderedEnumerable<AssetInfo> orderedAssets = filteredAssets.OrderBy(a => a.GetDisplayName(), StringComparer.OrdinalIgnoreCase);
                orderedAssets.ToList().ForEach(a => data.Add(a.WithTreeData(a.GetDisplayName(), a.AssetId)));
            }

            ReportTreeModel.SetData(data, true);
            ReportTreeView.Reload();
            OnReportTreeSelectionChanged(ReportTreeView.GetSelection());

            _textureLoading3?.Cancel();
            _textureLoading3 = new CancellationTokenSource();
            AssetUtils.LoadTextures(data, _textureLoading3.Token);
        }

        private void OnReportTreeDoubleClicked(int id)
        {
            if (id <= 0) return;

            AssetInfo info = ReportTreeModel.Find(id);
            OpenInSearch(info, true);
        }

        private void OnReportTreeSelectionChanged(IList<int> ids)
        {
            _selectedReportEntry = null;
            _selectedReportEntries = _selectedReportEntries ?? new List<AssetInfo>();
            _selectedReportEntries.Clear();

            if (ids.Count == 1 && ids[0] > 0)
            {
                _selectedReportEntry = ReportTreeModel.Find(ids[0]);
                _selectedReportEntry?.Refresh();
            }

            // load all selected items but count each only once
            foreach (int id in ids)
            {
                GatherTreeChildren(id, _selectedReportEntries, ReportTreeModel);
            }
            _selectedReportEntries = _selectedReportEntries.Distinct().ToList();

            _reportBulkTags.Clear();
            _selectedReportEntries.ForEach(info => info.PackageTags?.ForEach(t =>
            {
                if (!_reportBulkTags.ContainsKey(t.Name)) _reportBulkTags.Add(t.Name, new Tuple<int, Color>(0, t.GetColor()));
                _reportBulkTags[t.Name] = new Tuple<int, Color>(_reportBulkTags[t.Name].Item1 + 1, _reportBulkTags[t.Name].Item2);
            }));

            _reportTreeSubPackageCount = _selectedReportEntries.Count(a => a.ParentId > 0);
            _reportTreeSelectionSize = _selectedReportEntries.Sum(a => a.PackageSize);
        }
    }
}