using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JD.EditorAudioUtils;
using SQLite;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AssetInventory
{
    public partial class IndexUI
    {
        // customizable interaction modes, search mode will only show search tab contents and no actions except "Select"
        public bool searchMode;

        // special mode that will return accompanying textures to the selected one, trying to identify normal, metallic etc. 
        public bool textureMode;

        // will hide detail pane
        public bool hideDetailsPane;

        // will not select items in the project window upon selection
        public bool disablePings;

        // will cause clicking on a grid tile to return the selection to the caller and close the window
        public bool instantSelection;

        // locks the search to a specific type, e.g. "Prefabs" 
        public string fixedSearchType;

        // event handler during search mode
        protected Action<string> searchModeCallback;
        protected Action<Dictionary<string, string>> searchModeTextureCallback;

        private List<AssetInfo> _files;

        private readonly GridControl _sgrid = new GridControl();
        private int _resultCount;
        private string _searchPhrase;
        private string _searchWidth;
        private string _searchHeight;
        private string _searchLength;
        private string _searchSize;
        private bool _checkMaxWidth;
        private bool _checkMaxHeight;
        private bool _checkMaxLength;
        private bool _checkMaxSize;
        private int _selectedPublisher;
        private int _selectedCategory;
        private int _selectedExpertSearchField;
        private int _selectedAsset;
        private int _selectedPackageTypes = 1;
        private int _selectedPackageTag;
        private int _selectedFileTag;
        private int _selectedMaintenance;
        private int _selectedColorOption;
        private Color _selectedColor;
        private bool _showSettings;

        private Vector2 _searchScrollPos;
        private Vector2 _inspectorScrollPos;

        private int _curPage = 1;
        private int _pageCount;

        private CancellationTokenSource _textureLoading;
        private CancellationTokenSource _textureLoading2;
        private CancellationTokenSource _textureLoading3;

        private AssetInfo _selectedEntry;

        private SearchField SearchField => _searchField = _searchField ?? new SearchField();
        private SearchField _searchField;

        private float _nextSearchTime;
        private Rect _pageButtonRect;
        private DateTime _lastTileSizeChange;
        private string _searchError;
        private bool _searchDone;
        private bool _lockSelection;
        private string _curOperation;
        private int _fixedSearchTypeIdx;
        private bool _mouseOverSearchResultRect;
        private bool _dragging;
        private bool _keepSearchResultPage = true;
        private readonly Dictionary<string, Tuple<int, Color>> _assetFileBulkTags = new Dictionary<string, Tuple<int, Color>>();

        protected void SetInitialSearch(string searchPhrase)
        {
            _searchPhrase = searchPhrase;
        }

        private void DrawSearchTab()
        {
            if (_packageFileCount == 0)
            {
                bool canStillSearch = AssetInventory.IndexingInProgress || _packageCount == 0 || AssetInventory.Config.indexAssetPackageContents;
                if (canStillSearch)
                {
                    EditorGUILayout.HelpBox("The search index needs to be initialized. Start it right from here or go to the Settings tab to configure the details.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("The search is only available if package contents was indexed.", MessageType.Info);
                }

                EditorGUILayout.Space(30);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Box(Logo, EditorStyles.centeredGreyMiniLabel, GUILayout.MaxWidth(300), GUILayout.MaxHeight(300));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                if (canStillSearch)
                {
                    EditorGUILayout.Space(30);
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.BeginVertical();
                    EditorGUI.BeginDisabledGroup(AssetInventory.IndexingInProgress);
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(AssetInventory.IndexingInProgress ? "Indexing..." : "Start Indexing", GUILayout.Height(50), GUILayout.MaxWidth(400))) PerformFullUpdate();
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Settings...", GUILayout.ExpandWidth(false))) SetupWizardUI.ShowWindow();
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    EditorGUI.EndDisabledGroup();
                    if (AssetInventory.IndexingInProgress)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Index results will appear here automatically once available. To see the detailed progress go to the Settings tab.", EditorStyles.centeredGreyMiniLabel);
                    }
                    GUILayout.EndVertical();
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.HelpBox("Since the search index is shared across Unity projects it is highly recommended for performance to perform initial indexing from an empty project on a new Unity version and if possible on an SSD drive.", MessageType.Warning);
                }
            }
            else if (_lockSelection)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Making asset available in project...", UIStyles.centerLabel);
                EditorGUILayout.LabelField("This can take a while depending on the size of the source package.", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.Space(30);
                EditorGUILayout.LabelField(_curOperation, EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
            }
            else
            {
                bool dirty = false;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(EditorGUIUtility.IconContent("Preset.Context", "|Show/Hide Search Filters")))
                {
                    AssetInventory.Config.showSearchFilterBar = !AssetInventory.Config.showSearchFilterBar;
                    AssetInventory.SaveConfig();
                    if (AssetInventory.Config.filterOnlyIfBarVisible) dirty = true;
                }
                EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
                EditorGUIUtility.labelWidth = 60;
                EditorGUI.BeginChangeCheck();
                _searchPhrase = SearchField.OnGUI(_searchPhrase, GUILayout.ExpandWidth(true));
                if (EditorGUI.EndChangeCheck())
                {
                    // delay search to allow fast typing
                    _nextSearchTime = Time.realtimeSinceStartup + AssetInventory.Config.searchDelay;
                }
                else if (_nextSearchTime > 0 && Time.realtimeSinceStartup > _nextSearchTime)
                {
                    _nextSearchTime = 0;
                    if (AssetInventory.Config.searchAutomatically && !_searchPhrase.StartsWith("=")) dirty = true;
                }
                if (_allowLogic && Event.current.keyCode == KeyCode.Return) dirty = true;
                if (!AssetInventory.Config.searchAutomatically)
                {
                    if (GUILayout.Button("Go", GUILayout.Width(30))) PerformSearch();
                }

                if (_searchPhrase != null && _searchPhrase.StartsWith("="))
                {
                    EditorGUI.BeginChangeCheck();
                    GUILayout.Space(2);
                    _selectedExpertSearchField = EditorGUILayout.Popup(_selectedExpertSearchField, _expertSearchFields, GUILayout.Width(90));
                    if (EditorGUI.EndChangeCheck())
                    {
                        string field = _expertSearchFields[_selectedExpertSearchField];
                        if (!string.IsNullOrEmpty(field) && !field.StartsWith("-"))
                        {
                            _searchPhrase += field.Replace('/', '.');
                        }
                        _selectedExpertSearchField = 0;
                    }
                }

                if (_fixedSearchTypeIdx < 0)
                {
                    EditorGUI.BeginChangeCheck();
                    GUILayout.Space(2);
                    AssetInventory.Config.searchType = EditorGUILayout.Popup(AssetInventory.Config.searchType, _types, GUILayout.ExpandWidth(false), GUILayout.MinWidth(85));
                    if (EditorGUI.EndChangeCheck())
                    {
                        AssetInventory.SaveConfig();
                        dirty = true;
                    }
                    GUILayout.Space(2);
                }
                if (ShowAdvanced() && !hideDetailsPane && !searchMode && GUILayout.Button(EditorGUIUtility.IconContent("d_animationvisibilitytoggleon", "|Show/Hide Details Inspector")))
                {
                    AssetInventory.Config.showSearchDetailsBar = !AssetInventory.Config.showSearchDetailsBar;
                    AssetInventory.SaveConfig();
                }
                GUILayout.EndHorizontal();
                if (!string.IsNullOrEmpty(_searchError))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(90);
                    EditorGUILayout.LabelField($"Error: {_searchError}", UIStyles.ColoredText(Color.red));
                    GUILayout.EndHorizontal();
                }
                EditorGUILayout.Space();

                GUILayout.BeginHorizontal();
                if (AssetInventory.Config.showSearchFilterBar)
                {
                    GUILayout.BeginVertical("Filter Bar", "window", GUILayout.Width(UIStyles.INSPECTOR_WIDTH));
                    EditorGUILayout.Space();

                    EditorGUI.BeginChangeCheck();
                    AssetInventory.Config.showDetailFilters = EditorGUILayout.Foldout(AssetInventory.Config.showDetailFilters, "Additional Filters");
                    if (EditorGUI.EndChangeCheck()) AssetInventory.SaveConfig();

                    if (AssetInventory.Config.showDetailFilters)
                    {
                        EditorGUI.BeginChangeCheck();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Package Tag", EditorStyles.boldLabel, GUILayout.Width(85));
                        _selectedPackageTag = EditorGUILayout.Popup(_selectedPackageTag, _tagNames, GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("File Tag", EditorStyles.boldLabel, GUILayout.Width(85));
                        _selectedFileTag = EditorGUILayout.Popup(_selectedFileTag, _tagNames, GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Package", EditorStyles.boldLabel, GUILayout.Width(85));
                        _selectedAsset = EditorGUILayout.Popup(_selectedAsset, _assetNames, GUILayout.ExpandWidth(true), GUILayout.MinWidth(200));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Publisher", EditorStyles.boldLabel, GUILayout.Width(85));
                        _selectedPublisher = EditorGUILayout.Popup(_selectedPublisher, _publisherNames, GUILayout.ExpandWidth(true), GUILayout.MinWidth(150));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Category", EditorStyles.boldLabel, GUILayout.Width(85));
                        _selectedCategory = EditorGUILayout.Popup(_selectedCategory, _categoryNames, GUILayout.ExpandWidth(true), GUILayout.MinWidth(150));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Width", EditorStyles.boldLabel, GUILayout.Width(85));
                        if (GUILayout.Button(_checkMaxWidth ? "<=" : ">=", GUILayout.Width(25))) _checkMaxWidth = !_checkMaxWidth;
                        _searchWidth = EditorGUILayout.TextField(_searchWidth, GUILayout.Width(58));
                        EditorGUILayout.LabelField("px", EditorStyles.miniLabel);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Height", EditorStyles.boldLabel, GUILayout.Width(85));
                        if (GUILayout.Button(_checkMaxHeight ? "<=" : ">=", GUILayout.Width(25))) _checkMaxHeight = !_checkMaxHeight;
                        _searchHeight = EditorGUILayout.TextField(_searchHeight, GUILayout.Width(58));
                        EditorGUILayout.LabelField("px", EditorStyles.miniLabel);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Length", EditorStyles.boldLabel, GUILayout.Width(85));
                        if (GUILayout.Button(_checkMaxLength ? "<=" : ">=", GUILayout.Width(25))) _checkMaxLength = !_checkMaxLength;
                        _searchLength = EditorGUILayout.TextField(_searchLength, GUILayout.Width(58));
                        EditorGUILayout.LabelField("sec", EditorStyles.miniLabel);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("File Size", "File size in kilobytes"), EditorStyles.boldLabel, GUILayout.Width(85));
                        if (GUILayout.Button(_checkMaxSize ? "<=" : ">=", GUILayout.Width(25))) _checkMaxSize = !_checkMaxSize;
                        _searchSize = EditorGUILayout.TextField(_searchSize, GUILayout.Width(58));
                        EditorGUILayout.LabelField("kb", EditorStyles.miniLabel);
                        GUILayout.EndHorizontal();

                        if (AssetInventory.Config.extractColors)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Color", EditorStyles.boldLabel, GUILayout.Width(85));
                            _selectedColorOption = EditorGUILayout.Popup(_selectedColorOption, _colorOptions, GUILayout.Width(87));
                            if (_selectedColorOption > 0) _selectedColor = EditorGUILayout.ColorField(_selectedColor);
                            GUILayout.EndHorizontal();
                        }

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Packages", EditorStyles.boldLabel, GUILayout.Width(85));
                        _selectedPackageTypes = EditorGUILayout.Popup(_selectedPackageTypes, _packageListingOptions, GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();

                        if (EditorGUI.EndChangeCheck()) dirty = true;

                        EditorGUILayout.Space();
                        if (GUILayout.Button("Reset Filters"))
                        {
                            ResetSearch(true, false);
                            _requireSearchUpdate = true;
                        }
                    }

                    EditorGUILayout.Space();
                    EditorGUI.BeginChangeCheck();
                    AssetInventory.Config.showSavedSearches = EditorGUILayout.Foldout(AssetInventory.Config.showSavedSearches, "Saved Searches");
                    if (EditorGUI.EndChangeCheck()) AssetInventory.SaveConfig();

                    if (AssetInventory.Config.showSavedSearches)
                    {
                        if (AssetInventory.Config.searches.Count == 0)
                        {
                            EditorGUILayout.HelpBox("Save different search settings to quickly pull up the results later again.", MessageType.Info);
                        }
                        if (GUILayout.Button("Save current search..."))
                        {
                            NameUI nameUI = new NameUI();
                            nameUI.Init(string.IsNullOrEmpty(_searchPhrase) ? "My Search" : _searchPhrase, SaveSearch);
                            PopupWindow.Show(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 0, 0), nameUI);
                        }

                        EditorGUILayout.Space();
                        Color oldCol = GUI.backgroundColor;
                        for (int i = 0; i < AssetInventory.Config.searches.Count; i++)
                        {
                            SavedSearch search = AssetInventory.Config.searches[i];
                            GUILayout.BeginHorizontal();

                            if (ColorUtility.TryParseHtmlString($"#{search.color}", out Color color)) GUI.backgroundColor = color;
                            if (GUILayout.Button(UIStyles.Content(search.name, search.searchPhrase), GUILayout.MaxWidth(250))) LoadSearch(search);
                            GUI.backgroundColor = oldCol;

                            if (GUILayout.Button(EditorGUIUtility.IconContent("TrueTypeFontImporter Icon", "|Set only search text"), GUILayout.Width(30), GUILayout.Height(EditorGUIUtility.singleLineHeight + 2)))
                            {
                                _searchPhrase = search.searchPhrase;
                                dirty = true;
                            }
                            if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash", "|Delete saved search"), GUILayout.Width(30), GUILayout.Height(EditorGUIUtility.singleLineHeight + 2)))
                            {
                                AssetInventory.Config.searches.RemoveAt(i);
                                AssetInventory.SaveConfig();
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.FlexibleSpace();
                    if (AssetInventory.DEBUG_MODE && GUILayout.Button("Reload Lookups")) ReloadLookups();

                    GUILayout.EndVertical();
                }

                // result
                if (_sgrid == null || (_sgrid.contents != null && _sgrid.contents.Length > 0 && _files == null)) PerformSearch(); // happens during recompilation
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();

                // assets
                GUILayout.BeginVertical();
                bool isAudio = AssetInventory.IsFileType(_selectedEntry?.Path, "Audio");
                if (_sgrid.contents != null && _sgrid.contents.Length > 0)
                {
                    EditorGUI.BeginChangeCheck();
                    _searchScrollPos = GUILayout.BeginScrollView(_searchScrollPos, false, false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        // TODO: implement paged endless scrolling, needs some pixel calculations though
                        // if (_textureLoading != null) EditorCoroutineUtility.StopCoroutine(_textureLoading);
                        // _textureLoading = EditorCoroutineUtility.StartCoroutine(LoadTextures(false), this);
                    }

                    // draw contents
                    EditorGUI.BeginChangeCheck();

                    int inspectorCount = (AssetInventory.Config.showSearchFilterBar ? 2 : 1) - ((hideDetailsPane || !AssetInventory.Config.showSearchDetailsBar) ? 1 : 0);
                    _sgrid.Draw(position.width, inspectorCount, AssetInventory.Config.searchTileSize, UIStyles.searchTile, UIStyles.selectedSearchTile);

                    if (Event.current.type == EventType.Repaint)
                    {
                        _mouseOverSearchResultRect = GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition);
                    }
                    if (EditorGUI.EndChangeCheck() || (_allowLogic && _searchDone))
                    {
                        // interactions
                        _sgrid.HandleMouseClicks();

                        if (AssetInventory.Config.autoHideSettings) _showSettings = false;
                        _sgrid.LimitSelection(_files.Count);
                        _selectedEntry = _files[_sgrid.selectionTile];

                        EditorAudioUtility.StopAllPreviewClips();
                        isAudio = AssetInventory.IsFileType(_selectedEntry?.Path, "Audio");
                        if (_selectedEntry != null)
                        {
                            _selectedEntry.Refresh();
                            AssetInventory.GetObserver().SetPrioritized(new List<AssetInfo> {_selectedEntry});
                            _selectedEntry.PackageDownloader.RefreshState();

                            _selectedEntry.CheckIfInProject();
                            _selectedEntry.IsMaterialized = AssetInventory.IsMaterialized(_selectedEntry.ToAsset(), _selectedEntry);
#pragma warning disable CS4014
                            AssetUtils.LoadPackageTexture(_selectedEntry);
#pragma warning restore CS4014

                            // if entry is already materialized calculate dependencies immediately
                            if (!_previewInProgress && _selectedEntry.DependencyState == AssetInfo.DependencyStateOptions.Unknown && _selectedEntry.IsMaterialized)
                            {
#pragma warning disable CS4014
                                // must run in same thread
                                CalculateDependencies(_selectedEntry);
#pragma warning restore CS4014
                            }

                            if (!_searchDone && AssetInventory.Config.pingSelected && _selectedEntry.InProject) PingAsset(_selectedEntry);
                        }
                        _searchDone = false;

                        // Used event is thrown if user manually selected the entry
                        if (Event.current.type == EventType.Used)
                        {
                            if (instantSelection)
                            {
                                ExecuteSingleAction();
                            }
                            else if (AssetInventory.Config.autoPlayAudio && isAudio) PlayAudio(_selectedEntry);
                        }
                    }
                    GUILayout.EndScrollView();

                    // navigation
                    EditorGUILayout.Space();
                    GUILayout.BeginHorizontal();

                    if (AssetInventory.Config.showTileSizeSlider)
                    {
                        EditorGUI.BeginChangeCheck();
                        AssetInventory.Config.searchTileSize = EditorGUILayout.IntSlider(AssetInventory.Config.searchTileSize, 50, 300, GUILayout.Width(150));
                        if (EditorGUI.EndChangeCheck())
                        {
                            _lastTileSizeChange = DateTime.Now;
                            AssetInventory.SaveConfig();
                        }
                    }

                    GUILayout.FlexibleSpace();
                    if (_pageCount > 1)
                    {
                        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.PageUp) SetPage(1);
                        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.PageDown) SetPage(_pageCount);

                        EditorGUI.BeginDisabledGroup(_curPage <= 1);
                        if ((!_showSettings && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.LeftArrow) ||
                            GUILayout.Button("<", GUILayout.ExpandWidth(false))) SetPage(_curPage - 1);
                        EditorGUI.EndDisabledGroup();

                        if (EditorGUILayout.DropdownButton(UIStyles.Content($"Page {_curPage:N0}/{_pageCount:N0}", $"{_resultCount:N0} results in total"), FocusType.Keyboard, UIStyles.centerPopup, GUILayout.MinWidth(100)))
                        {
                            DropDownUI pageUI = new DropDownUI();
                            pageUI.Init(1, _pageCount, _curPage, "Page ", null, SetPage);
                            PopupWindow.Show(_pageButtonRect, pageUI);
                        }
                        if (Event.current.type == EventType.Repaint) _pageButtonRect = GUILayoutUtility.GetLastRect();

                        EditorGUI.BeginDisabledGroup(_curPage >= _pageCount);
                        if ((!_showSettings && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.RightArrow) ||
                            GUILayout.Button(">", GUILayout.ExpandWidth(false))) SetPage(_curPage + 1);
                        EditorGUI.EndDisabledGroup();
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"{_resultCount:N0} results", UIStyles.centerLabel, GUILayout.ExpandWidth(true));
                    }
                    GUILayout.FlexibleSpace();
                    if (!hideDetailsPane && AssetInventory.Config.showSearchDetailsBar)
                    {
                        if (GUILayout.Button(EditorGUIUtility.IconContent("Settings", "|Show/Hide Settings Tab"))) _showSettings = !_showSettings;
                    }
                    GUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                }
                else
                {
                    if (!_lockSelection) _selectedEntry = null;
                    GUILayout.Label("No matching results", UIStyles.whiteCenter, GUILayout.MinHeight(AssetInventory.Config.searchTileSize));

                    bool isIndexing = AssetInventory.IndexingInProgress;
                    bool hasHiddenExtensions = AssetInventory.Config.searchType == 0 && !string.IsNullOrWhiteSpace(AssetInventory.Config.excludedExtensions);
                    bool hasHiddenPreviews = AssetInventory.Config.previewVisibility > 0;
                    if (isIndexing || hasHiddenExtensions || hasHiddenPreviews)
                    {
                        GUILayout.Label("Search result is potentially limited", EditorStyles.centeredGreyMiniLabel);
                        if (isIndexing) GUILayout.Label("Index is currently being updated", EditorStyles.centeredGreyMiniLabel);
                        if (hasHiddenExtensions)
                        {
                            EditorGUILayout.Space();
                            GUILayout.Label($"Hidden extensions: {AssetInventory.Config.excludedExtensions}", EditorStyles.centeredGreyMiniLabel);
                            GUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Ignore Once", GUILayout.Width(100))) PerformSearch(false, true);
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                            EditorGUILayout.Space();
                        }
                        if (hasHiddenPreviews) GUILayout.Label("Results depend on preview availability", EditorStyles.centeredGreyMiniLabel);
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(EditorGUIUtility.IconContent("Settings", "|Show/Hide Settings Tab"))) _showSettings = !_showSettings;
                    GUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                }
                GUILayout.EndVertical();

                // inspector
                if (!hideDetailsPane && AssetInventory.Config.showSearchDetailsBar)
                {
                    EditorGUILayout.Space();

                    int labelWidth = 95;
                    GUILayout.BeginVertical();
                    if (_sgrid.selectionCount <= 1)
                    {
                        GUILayout.BeginVertical("Details Inspector", "window", GUILayout.Width(UIStyles.INSPECTOR_WIDTH));
                        EditorGUILayout.Space();
                        _inspectorScrollPos = GUILayout.BeginScrollView(_inspectorScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
                        if (_selectedEntry == null || string.IsNullOrEmpty(_selectedEntry.SafeName))
                        {
                            // will happen after script reload
                            EditorGUILayout.HelpBox("Select an asset for details", MessageType.Info);
                        }
                        else
                        {
                            EditorGUILayout.LabelField("File", EditorStyles.largeLabel);
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(UIStyles.Content("Name", $"Internal Id: {_selectedEntry.Id}"), EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                            EditorGUILayout.LabelField(UIStyles.Content(Path.GetFileName(_selectedEntry.GetPath(true)), _selectedEntry.GetPath(true)), EditorStyles.wordWrappedLabel);
                            GUILayout.EndHorizontal();
                            if (_selectedEntry.AssetSource == Asset.Source.Directory) GUILabelWithText("Location", $"{Path.GetDirectoryName(_selectedEntry.GetPath(true))}", 95, null, true);
                            GUILabelWithText("Size", EditorUtility.FormatBytes(_selectedEntry.Size));
                            if (_selectedEntry.Width > 0) GUILabelWithText("Dimensions", $"{_selectedEntry.Width}x{_selectedEntry.Height} px");
                            if (_selectedEntry.Length > 0) GUILabelWithText("Length", $"{_selectedEntry.Length:N2} seconds");
                            if (ShowAdvanced() || _selectedEntry.InProject) GUILabelWithText("In Project", _selectedEntry.InProject ? "Yes" : "No");
                            if (_selectedEntry.Downloaded)
                            {
                                bool needsDependencyScan = false;
                                if (AssetInventory.NeedsDependencyScan(_selectedEntry.Type))
                                {
                                    switch (_selectedEntry.DependencyState)
                                    {
                                        case AssetInfo.DependencyStateOptions.Unknown:
                                            needsDependencyScan = true;
                                            GUILayout.BeginHorizontal();
                                            EditorGUILayout.LabelField("Dependencies", EditorStyles.boldLabel, GUILayout.Width(labelWidth));
                                            EditorGUI.BeginDisabledGroup(_previewInProgress);
                                            if (GUILayout.Button("Calculate", GUILayout.ExpandWidth(false)))
                                            {
#pragma warning disable CS4014
                                                // must run in same thread
                                                CalculateDependencies(_selectedEntry);
#pragma warning restore CS4014
                                            }
                                            EditorGUI.EndDisabledGroup();
                                            GUILayout.EndHorizontal();
                                            break;

                                        case AssetInfo.DependencyStateOptions.Calculating:
                                            GUILabelWithText("Dependencies", "Calculating...");
                                            break;

                                        case AssetInfo.DependencyStateOptions.NotPossible:
                                            GUILabelWithText("Dependencies", "Cannot determine (binary)");
                                            break;

                                        case AssetInfo.DependencyStateOptions.Failed:
                                            GUILabelWithText("Dependencies", "Failed to determine");
                                            break;

                                        case AssetInfo.DependencyStateOptions.Done:
                                            GUILayout.BeginHorizontal();
                                            if (ShowAdvanced())
                                            {
                                                string scriptDeps = _selectedEntry.ScriptDependencies?.Count > 0 ? $" + {_selectedEntry.ScriptDependencies?.Count} scripts" : string.Empty;
                                                GUILabelWithText("Dependencies", $"{_selectedEntry.MediaDependencies?.Count}{scriptDeps} ({EditorUtility.FormatBytes(_selectedEntry.DependencySize)})");
                                            }
                                            else
                                            {
                                                GUILabelWithText("Dependencies", $"{_selectedEntry.Dependencies?.Count}");
                                            }
                                            if (_selectedEntry.Dependencies.Count > 0 && GUILayout.Button("Show..."))
                                            {
                                                DependenciesUI depUI = DependenciesUI.ShowWindow();
                                                depUI.Init(_selectedEntry);
                                            }

                                            GUILayout.EndHorizontal();
                                            break;
                                    }
                                }

                                if (!searchMode)
                                {
                                    if (!_selectedEntry.InProject && string.IsNullOrEmpty(_importFolder))
                                    {
                                        EditorGUILayout.Space();
                                        EditorGUILayout.LabelField("Select a folder in Project View for import options", EditorStyles.centeredGreyMiniLabel);
                                        EditorGUI.BeginDisabledGroup(true);
                                        GUILayout.Button("Import File");
                                        EditorGUI.EndDisabledGroup();
                                    }
                                    else
                                    {
                                        if (ShowAdvanced())
                                        {
                                            EditorGUI.BeginDisabledGroup(_previewInProgress);
                                            if ((!_selectedEntry.InProject || ShowAdvanced()) && !string.IsNullOrEmpty(_importFolder))
                                            {
                                                string command = _selectedEntry.InProject ? "Reimport" : "Import";
                                                GUILabelWithText($"{command} To", _importFolder, 95, null, true);
                                                EditorGUILayout.Space();
                                                if (needsDependencyScan)
                                                {
                                                    EditorGUILayout.LabelField("Dependency scan needed to determine import options.", EditorStyles.centeredGreyMiniLabel);
                                                    EditorGUI.BeginDisabledGroup(true);
                                                    GUILayout.Button("Import File");
                                                    EditorGUI.EndDisabledGroup();
                                                }
                                                else
                                                {
                                                    if (GUILayout.Button($"{command} File" + (_selectedEntry.DependencySize > 0 ? " Only" : ""))) CopyTo(_selectedEntry, _importFolder);
                                                    if (_selectedEntry.DependencySize > 0 && AssetInventory.NeedsDependencyScan(_selectedEntry.Type))
                                                    {
                                                        if (GUILayout.Button($"{command} With Dependencies")) CopyTo(_selectedEntry, _importFolder, true);
                                                        if (_selectedEntry.ScriptDependencies.Count > 0)
                                                        {
                                                            if (GUILayout.Button($"{command} With Dependencies + Scripts")) CopyTo(_selectedEntry, _importFolder, true, true);
                                                        }

                                                        EditorGUILayout.Space();
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            EditorGUILayout.Space();
                                            if (!_selectedEntry.InProject)
                                            {
                                                EditorGUI.BeginDisabledGroup(_previewInProgress);
                                                if (GUILayout.Button("Import")) CopyTo(_selectedEntry, _importFolder, true);
                                                EditorGUI.EndDisabledGroup();
                                            }
                                        }
                                    }
                                }

                                if (isAudio)
                                {
                                    bool isPreviewClipPlaying = EditorAudioUtility.IsPreviewClipPlaying();

                                    GUILayout.BeginHorizontal();
                                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_PlayButton", "|Play"), GUILayout.Width(40))) PlayAudio(_selectedEntry);
                                    EditorGUI.BeginDisabledGroup(!isPreviewClipPlaying);
                                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_PreMatQuad", "|Stop"), GUILayout.Width(40))) EditorAudioUtility.StopAllPreviewClips();
                                    EditorGUI.EndDisabledGroup();
                                    EditorGUILayout.Space();
                                    EditorGUI.BeginChangeCheck();
                                    AssetInventory.Config.autoPlayAudio = GUILayout.Toggle(AssetInventory.Config.autoPlayAudio, "Auto-Play");
                                    AssetInventory.Config.loopAudio = GUILayout.Toggle(AssetInventory.Config.loopAudio, "Loop");
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        AssetInventory.SaveConfig();
                                        if (AssetInventory.Config.autoPlayAudio) PlayAudio(_selectedEntry);
                                    }
                                    GUILayout.EndHorizontal();

                                    // scrubbing (Unity 2020.1+)
                                    if (isPreviewClipPlaying)
                                    {
                                        AudioClip currentClip = EditorAudioUtility.LastPlayedPreviewClip;
                                        EditorGUI.BeginChangeCheck();
                                        float newVal = EditorGUILayout.Slider(EditorAudioUtility.GetPreviewClipPosition(), 0, currentClip.length);
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            EditorAudioUtility.StopAllPreviewClips();
                                            EditorAudioUtility.PlayPreviewClip(currentClip, Mathf.RoundToInt(currentClip.samples * newVal / currentClip.length), false);
                                        }
                                    }
                                    EditorGUILayout.Space();
                                }

                                if (_selectedEntry.InProject && !AssetInventory.Config.pingSelected)
                                {
                                    if (GUILayout.Button("Ping")) PingAsset(_selectedEntry);
                                }

                                if (!searchMode)
                                {
                                    if (GUILayout.Button(UIStyles.Content("Open", "Open the file with the assigned system application"))) Open(_selectedEntry);
                                    if (GUILayout.Button(Application.platform == RuntimePlatform.OSXEditor ? "Show in Finder" : "Show in Explorer")) OpenExplorer(_selectedEntry);
                                    EditorGUI.BeginDisabledGroup(_previewInProgress);
                                    if ((ShowAdvanced() || _selectedEntry.PreviewState == AssetFile.PreviewOptions.Error || _selectedEntry.PreviewState == AssetFile.PreviewOptions.None || _selectedEntry.PreviewState == AssetFile.PreviewOptions.Redo) && GUILayout.Button("Recreate Preview")) RecreatePreview(_selectedEntry);
                                    EditorGUI.EndDisabledGroup();
                                    if (ShowAdvanced()) EditorGUILayout.Space();
                                    if (ShowAdvanced() && GUILayout.Button(UIStyles.Content("Delete from Index", "Will delete the indexed file from the database. The package will need to be reindexed in order for it to appear again."))) DeleteFromIndex(_selectedEntry);
                                }
                                if (!_selectedEntry.IsMaterialized && !_previewInProgress)
                                {
                                    EditorGUILayout.LabelField($"{EditorUtility.FormatBytes(_selectedEntry.PackageSize)} will be extracted before actions are performed", EditorStyles.centeredGreyMiniLabel);
                                }
                            }
                            else if (_selectedEntry.IsLocationUnmappedRelative())
                            {
                                EditorGUILayout.HelpBox("The location of this package is stored relative and no mapping has been done yet for this system in the settings: " + _selectedEntry.Location, MessageType.Info);
                            }

                            if (_previewInProgress)
                            {
                                EditorGUI.EndDisabledGroup();
                                EditorGUILayout.LabelField("Extracting...", UIStyles.centeredWhiteMiniLabel);
                                EditorGUI.BeginDisabledGroup(_previewInProgress);
                            }

                            if (!string.IsNullOrWhiteSpace(_selectedEntry.AICaption))
                            {
                                EditorGUILayout.LabelField(_selectedEntry.AICaption, EditorStyles.wordWrappedLabel);
                            }

                            if (!searchMode)
                            {
                                // tags
                                DrawAddFileTag(new List<AssetInfo> {_selectedEntry});

                                if (_selectedEntry.AssetTags != null && _selectedEntry.AssetTags.Count > 0)
                                {
                                    float x = 0f;
                                    foreach (TagInfo tagInfo in _selectedEntry.AssetTags)
                                    {
                                        x = CalcTagSize(x, tagInfo.Name);
                                        UIStyles.DrawTag(tagInfo, () =>
                                        {
                                            AssetInventory.RemoveTagAssignment(_selectedEntry, tagInfo);
                                            _requireAssetTreeRebuild = true;
                                            _requireSearchUpdate = true;
                                        });
                                    }
                                }
                                GUILayout.EndHorizontal();
                            }
                            EditorGUILayout.Space();
                            UIStyles.DrawUILine(Color.gray * 0.6f);
                            EditorGUILayout.Space();

                            DrawPackageDetails(_selectedEntry, false, !searchMode, false);
                        }

                        GUILayout.EndScrollView();
                        GUILayout.EndVertical();
                    }
                    else
                    {
                        GUILayout.BeginVertical("Bulk Actions", "window", GUILayout.Width(UIStyles.INSPECTOR_WIDTH));
                        EditorGUILayout.Space();
                        _inspectorScrollPos = GUILayout.BeginScrollView(_inspectorScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
                        GUILabelWithText("Selected", $"{_sgrid.selectionCount:N0}");
                        if (ShowAdvanced()) GUILabelWithText("Packages", $"{_sgrid.selectionPackageCount:N0}");
                        GUILabelWithText("Size", EditorUtility.FormatBytes(_sgrid.selectionSize));

                        int inProject = _sgrid.selectionItems.Count(item => item.InProject);
                        GUILabelWithText("In Project", $"{inProject:N0}/{_sgrid.selectionCount:N0}");

                        EditorGUI.BeginDisabledGroup(_previewInProgress);
                        if (!searchMode && !string.IsNullOrEmpty(_importFolder))
                        {
                            if (inProject < _sgrid.selectionCount)
                            {
                                string command = "Import";
                                if (inProject > 0) command += $" {_sgrid.selectionCount - inProject} Remaining";

                                GUILabelWithText("Import To", _importFolder);
                                EditorGUILayout.Space();
                                if (GUILayout.Button($"{command} Files")) ImportBulkFiles(_sgrid.selectionItems);
                            }
                        }

                        if (!searchMode)
                        {
                            if (GUILayout.Button(UIStyles.Content("Open", "Open the files with the assigned system application"))) _sgrid.selectionItems.ForEach(Open);
                            if (GUILayout.Button(Application.platform == RuntimePlatform.OSXEditor ? "Show in Finder" : "Show in Explorer")) _sgrid.selectionItems.ForEach(OpenExplorer);
                            EditorGUI.BeginDisabledGroup(_previewInProgress);
                            if (ShowAdvanced() && GUILayout.Button("Recreate Previews")) RecreatePreviews(_sgrid.selectionItems);
                            EditorGUI.EndDisabledGroup();
                            if (ShowAdvanced()) EditorGUILayout.Space();
                            if (ShowAdvanced() && GUILayout.Button(UIStyles.Content("Delete from Index", "Will delete the indexed files from the database. The package will need to be reindexed in order for it to appear again.")))
                            {
                                _sgrid.selectionItems.ForEach(DeleteFromIndex);
                            }
                        }
                        EditorGUI.EndDisabledGroup();
                        if (_previewInProgress) EditorGUILayout.LabelField("Operation in progress...", UIStyles.centeredWhiteMiniLabel);

                        // tags
                        DrawAddFileTag(_sgrid.selectionItems);

                        float x = 0f;
                        List<string> toRemove = new List<string>();
                        foreach (KeyValuePair<string, Tuple<int, Color>> bulkTag in _assetFileBulkTags)
                        {
                            string tagName = $"{bulkTag.Key} ({bulkTag.Value.Item1})";
                            x = CalcTagSize(x, tagName);
                            UIStyles.DrawTag(tagName, bulkTag.Value.Item2, () =>
                            {
                                AssetInventory.RemoveAssetTagAssignment(_sgrid.selectionItems, bulkTag.Key);
                                toRemove.Add(bulkTag.Key);
                            }, UIStyles.TagStyle.Remove);
                        }
                        toRemove.ForEach(key => _assetFileBulkTags.Remove(key));
                        GUILayout.EndHorizontal();

                        GUILayout.EndScrollView();
                        GUILayout.EndVertical();
                    }
                    if (_showSettings)
                    {
                        EditorGUILayout.Space();
                        GUILayout.BeginVertical("View Settings", "window", GUILayout.Width(UIStyles.INSPECTOR_WIDTH));
                        EditorGUILayout.Space();

                        EditorGUI.BeginChangeCheck();

                        int width = 135;

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("Search In", "Field to use for finding assets when doing plain searches and no expert search."), EditorStyles.boldLabel, GUILayout.Width(width));
                        AssetInventory.Config.searchField = EditorGUILayout.Popup(AssetInventory.Config.searchField, _searchFields);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("Sort by", "Specify the sort order. Unsorted will result in the fastest experience."), EditorStyles.boldLabel, GUILayout.Width(width));
                        AssetInventory.Config.sortField = EditorGUILayout.Popup(AssetInventory.Config.sortField, _sortFields);
                        if (GUILayout.Button(AssetInventory.Config.sortDescending ? UIStyles.Content("˅", "Descending") : UIStyles.Content("˄", "Ascending"), GUILayout.Width(17)))
                        {
                            AssetInventory.Config.sortDescending = !AssetInventory.Config.sortDescending;
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("Results", $"Maximum number of results to show. A (configurable) hard limit of {AssetInventory.Config.maxResultsLimit} will be enforced to keep Unity responsive."), EditorStyles.boldLabel, GUILayout.Width(width));
                        AssetInventory.Config.maxResults = EditorGUILayout.Popup(AssetInventory.Config.maxResults, _resultSizes);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("Hide Extensions", "File extensions to hide from search results when searching for all file types, e.g. asset;json;txt. These will still be indexed."), EditorStyles.boldLabel, GUILayout.Width(width));
                        AssetInventory.Config.excludeExtensions = EditorGUILayout.Toggle(AssetInventory.Config.excludeExtensions, GUILayout.Width(16));
                        AssetInventory.Config.excludedExtensions = EditorGUILayout.DelayedTextField(AssetInventory.Config.excludedExtensions);
                        GUILayout.EndHorizontal();

                        if (EditorGUI.EndChangeCheck())
                        {
                            dirty = true;
                            _curPage = 1;
                            AssetInventory.SaveConfig();
                        }

                        EditorGUILayout.Space();
                        EditorGUI.BeginChangeCheck();
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("Tile Size", "Dimensions of search result previews. Preview images will still be 128x128 max."), EditorStyles.boldLabel, GUILayout.Width(width));
                        AssetInventory.Config.searchTileSize = EditorGUILayout.IntSlider(AssetInventory.Config.searchTileSize, 50, 300);
                        GUILayout.EndHorizontal();
                        if (EditorGUI.EndChangeCheck())
                        {
                            _lastTileSizeChange = DateTime.Now;
                            AssetInventory.SaveConfig();
                        }

                        EditorGUI.BeginChangeCheck();
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("Tile Text", "Text to be shown on the tile"), EditorStyles.boldLabel, GUILayout.Width(width));
                        AssetInventory.Config.tileText = EditorGUILayout.Popup(AssetInventory.Config.tileText, _tileTitle);
                        GUILayout.EndHorizontal();
                        if (EditorGUI.EndChangeCheck())
                        {
                            dirty = true;
                            AssetInventory.SaveConfig();
                        }

                        EditorGUILayout.Space();
                        EditorGUI.BeginChangeCheck();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("Search While Typing", "Will search immediately while typing and update results constantly."), EditorStyles.boldLabel, GUILayout.Width(width));
                        AssetInventory.Config.searchAutomatically = EditorGUILayout.Toggle(AssetInventory.Config.searchAutomatically);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("Auto-Play Audio", "Will automatically extract unity packages to play the sound file if they were not extracted yet. This is the most convenient option but will require sufficient hard disk space."), EditorStyles.boldLabel, GUILayout.Width(width));
                        AssetInventory.Config.autoPlayAudio = EditorGUILayout.Toggle(AssetInventory.Config.autoPlayAudio);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("Ping Selected", "Highlight selected items in the Unity project tree if they are found in the current project."), EditorStyles.boldLabel, GUILayout.Width(width));
                        AssetInventory.Config.pingSelected = EditorGUILayout.Toggle(AssetInventory.Config.pingSelected);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("Double-Click Import", "Highlight selected items in the Unity project tree if they are found in the current project."), EditorStyles.boldLabel, GUILayout.Width(width));
                        AssetInventory.Config.doubleClickImport = EditorGUILayout.Toggle(AssetInventory.Config.doubleClickImport);
                        GUILayout.EndHorizontal();

                        if (EditorGUI.EndChangeCheck()) AssetInventory.SaveConfig();

                        EditorGUI.BeginChangeCheck();
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("Group List", "Add a second level hierarchy to dropdowns if they become too long to scroll."), EditorStyles.boldLabel, GUILayout.Width(width));
                        AssetInventory.Config.groupLists = EditorGUILayout.Toggle(AssetInventory.Config.groupLists);
                        GUILayout.EndHorizontal();
                        if (EditorGUI.EndChangeCheck())
                        {
                            AssetInventory.SaveConfig();
                            ReloadLookups();
                        }

                        EditorGUI.BeginChangeCheck();
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(UIStyles.Content("Previews", "Optionally restricts search results to those with either preview images available or not."), EditorStyles.boldLabel, GUILayout.Width(width));
                        AssetInventory.Config.previewVisibility = EditorGUILayout.Popup(AssetInventory.Config.previewVisibility, _previewOptions);
                        GUILayout.EndHorizontal();
                        if (EditorGUI.EndChangeCheck())
                        {
                            dirty = true;
                            AssetInventory.SaveConfig();
                        }

                        EditorGUILayout.Space();
                        GUILayout.EndVertical();
                    }
                    if (searchMode)
                    {
                        if (GUILayout.Button("Select", GUILayout.Height(40))) ExecuteSingleAction();
                    }
                    else
                    {
                        if (!ShowAdvanced() && AssetInventory.Config.showHints) EditorGUILayout.LabelField("Hold down CTRL for additional options.", EditorStyles.centeredGreyMiniLabel);
                    }

                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

                _sgrid.HandleKeyboardCommands();

                if (dirty)
                {
                    _requireSearchUpdate = true;
                    _keepSearchResultPage = false;
                }
                EditorGUIUtility.labelWidth = 0;
            }
        }

        private async void ImportBulkFiles(List<AssetInfo> items)
        {
            _previewInProgress = true;
            foreach (AssetInfo info in items)
            {
                // must be done consecutively to avoid IO conflicts
                await AssetInventory.CopyTo(info, _importFolder, true);
            }
            _previewInProgress = false;
        }

        private void DrawAddFileTag(List<AssetInfo> assets)
        {
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(UIStyles.Content("Add Tag..."), GUILayout.Width(70)))
            {
                TagSelectionUI tagUI = new TagSelectionUI();
                tagUI.Init(TagAssignment.Target.Asset, CalculateSearchBulkSelection);
                tagUI.SetAssets(assets);
                PopupWindow.Show(_tag2ButtonRect, tagUI);
            }
            if (Event.current.type == EventType.Repaint) _tag2ButtonRect = GUILayoutUtility.GetLastRect();
            GUILayout.Space(15);
        }

        private async void ExecuteSingleAction()
        {
            if (_selectedEntry == null) return;

            List<AssetInfo> files = new List<AssetInfo>();
            Dictionary<string, AssetInfo> identifiedTextures = null;
            if (textureMode)
            {
                identifiedTextures = IdentifyTextures(_selectedEntry);
                files.AddRange(identifiedTextures.Values); // TODO: one file will be duplicate, not an issue but will save time to eliminate it
            }
            else
            {
                files.Add(_selectedEntry);
            }

            foreach (AssetInfo info in files)
            {
                info.CheckIfInProject();
                if (!info.InProject)
                {
                    _previewInProgress = true;
                    _lockSelection = true;

                    // download on-demand
                    if (!info.Downloaded)
                    {
                        if (info.IsAbandoned)
                        {
                            Debug.LogError($"Cannot download {info.GetDisplayName()} as it is an abandoned package.");
                            _lockSelection = false;
                            return;
                        }

                        AssetInventory.GetObserver().Attach(info);
                        if (info.PackageDownloader.IsDownloadSupported())
                        {
                            _curOperation = $"Downloading {info.GetDisplayName()}...";
                            info.PackageDownloader.Download();
                            do
                            {
                                await Task.Delay(200);

                                info.PackageDownloader.RefreshState();
                                float progress = info.PackageDownloader.GetState().progress * 100f;
                                _curOperation = $"Downloading {info.GetDisplayName()}: {progress:N0}%...";
                            } while (info.IsDownloading());
                            await Task.Delay(3000); // ensure all file operations have finished, can otherwise lead to issues
                            info.Refresh();
                        }
                    }

                    _curOperation = $"Extracting & Importing '{info.FileName}'...";
                    await AssetInventory.CopyTo(info, _importFolder, true);
                    _previewInProgress = false;

                    if (!info.InProject)
                    {
                        Debug.LogError("The file could not be materialized into the project.");
                        _lockSelection = false;
                        return;
                    }
                }
            }

            Close();
            EditorAudioUtility.StopAllPreviewClips();

            if (textureMode)
            {
                Dictionary<string, string> result = new Dictionary<string, string>();
                foreach (KeyValuePair<string, AssetInfo> file in identifiedTextures)
                {
                    result.Add(file.Key, file.Value.ProjectPath);
                }
                searchModeTextureCallback?.Invoke(result);
            }
            else
            {
                searchModeCallback?.Invoke(_selectedEntry.ProjectPath);
            }
            _lockSelection = false;
        }

        private Dictionary<string, AssetInfo> IdentifyTextures(AssetInfo info)
        {
            TextureNameSuggester tns = new TextureNameSuggester();
            Dictionary<string, string> files = tns.SuggestFileNames(info.Path, path =>
            {
                string sep = info.Path.Contains("/") ? "/" : "\\";
                string toCheck = info.Path.Substring(0, info.Path.LastIndexOf(sep) + 1) + Path.GetFileName(path);
                AssetInfo ai = AssetInventory.GetAssetByPath(toCheck, info.ToAsset());
                return ai?.Path; // capitalization could be different from actual validation request, so use result
            });

            Dictionary<string, AssetInfo> result = new Dictionary<string, AssetInfo>();
            foreach (KeyValuePair<string, string> file in files)
            {
                AssetInfo ai = AssetInventory.GetAssetByPath(file.Value, info.ToAsset());
                if (ai != null) result.Add(file.Key, ai);
            }
            return result;
        }

        private void DeleteFromIndex(AssetInfo info)
        {
            AssetInventory.ForgetAssetFile(info);
            _requireSearchUpdate = true;
        }

        private async void RecreatePreview(AssetInfo info)
        {
            _previewInProgress = true;
            AssetProgress.CancellationRequested = false;
            if (await new PreviewImporter().RecreatePreview(info)) _requireSearchUpdate = true;
            _previewInProgress = false;
        }

        private async void RecreatePreviews(List<AssetInfo> infos)
        {
            _previewInProgress = true;
            AssetProgress.CancellationRequested = false;
            if (await new PreviewImporter().RecreatePreviews(infos) > 0) _requireSearchUpdate = true;
            _previewInProgress = false;
        }

        private async void RecreatePreviews(Asset asset, bool missingOnly, bool retryErroneous, string[] types = null)
        {
            AssetInventory.IndexingInProgress = true;
            AssetProgress.CancellationRequested = false;

            PreviewImporter.ScheduleRecreatePreviews(asset, missingOnly, retryErroneous, types);
            int created = await new PreviewImporter().RecreatePreviews(asset, _assets);
            Debug.Log($"Preview recreation done. {created} created.");

            _requireSearchUpdate = true;
            AssetInventory.IndexingInProgress = false;
        }

        private void LoadSearch(SavedSearch search)
        {
            _searchPhrase = search.searchPhrase;
            _selectedPackageTypes = search.packageTypes;
            _selectedColorOption = search.colorOption;
            _selectedColor = ImageUtils.FromHex(search.searchColor);
            _searchWidth = search.width;
            _searchHeight = search.height;
            _searchLength = search.length;
            _searchSize = search.size;
            _checkMaxWidth = search.checkMaxWidth;
            _checkMaxHeight = search.checkMaxHeight;
            _checkMaxLength = search.checkMaxLength;
            _checkMaxSize = search.checkMaxSize;

            AssetInventory.Config.searchType = Mathf.Max(0, Array.FindIndex(_types, s => s == search.type || s.EndsWith($"/{search.type}")));
            _selectedPublisher = Mathf.Max(0, Array.FindIndex(_publisherNames, s => s == search.publisher || s.EndsWith($"/{search.publisher}")));
            _selectedAsset = Mathf.Max(0, Array.FindIndex(_assetNames, s => s == search.package || s.EndsWith($"/{search.package}")));
            _selectedCategory = Mathf.Max(0, Array.FindIndex(_categoryNames, s => s == search.category || s.EndsWith($"/{search.category}")));
            _selectedPackageTag = Mathf.Max(0, Array.FindIndex(_tagNames, s => s == search.packageTag || s.EndsWith($"/{search.packageTag}")));
            _selectedFileTag = Mathf.Max(0, Array.FindIndex(_tagNames, s => s == search.fileTag || s.EndsWith($"/{search.fileTag}")));

            _requireSearchUpdate = true;
        }

        private void SaveSearch(string value)
        {
            SavedSearch spec = new SavedSearch();
            spec.name = value;
            spec.searchPhrase = _searchPhrase;
            spec.packageTypes = _selectedPackageTypes;
            spec.colorOption = _selectedColorOption;
            spec.searchColor = "#" + ColorUtility.ToHtmlStringRGB(_selectedColor);
            spec.width = _searchWidth;
            spec.height = _searchHeight;
            spec.length = _searchLength;
            spec.size = _searchSize;
            spec.checkMaxWidth = _checkMaxWidth;
            spec.checkMaxHeight = _checkMaxHeight;
            spec.checkMaxLength = _checkMaxLength;
            spec.checkMaxSize = _checkMaxSize;
            spec.color = ColorUtility.ToHtmlStringRGB(Random.ColorHSV());

            if (AssetInventory.Config.searchType > 0 && _types.Length > AssetInventory.Config.searchType)
            {
                spec.type = _types[AssetInventory.Config.searchType].Split('/').LastOrDefault();
            }

            if (_selectedPublisher > 0 && _publisherNames.Length > _selectedPublisher)
            {
                spec.publisher = _publisherNames[_selectedPublisher].Split('/').LastOrDefault();
            }

            if (_selectedAsset > 0 && _assetNames.Length > _selectedAsset)
            {
                spec.package = _assetNames[_selectedAsset].Split('/').LastOrDefault();
            }

            if (_selectedCategory > 0 && _categoryNames.Length > _selectedCategory)
            {
                spec.category = _categoryNames[_selectedCategory].Split('/').LastOrDefault();
            }

            if (_selectedPackageTag > 0 && _tagNames.Length > _selectedPackageTag)
            {
                spec.packageTag = _tagNames[_selectedPackageTag].Split('/').LastOrDefault();
            }

            if (_selectedFileTag > 0 && _tagNames.Length > _selectedFileTag)
            {
                spec.fileTag = _tagNames[_selectedFileTag].Split('/').LastOrDefault();
            }

            AssetInventory.Config.searches.Add(spec);
            AssetInventory.SaveConfig();
        }

        private async void PlayAudio(AssetInfo info)
        {
            // play instantly if no extraction is required
            if (_previewInProgress)
            {
                if (AssetInventory.IsMaterialized(info.ToAsset(), info)) await AssetInventory.PlayAudio(info);
                return;
            }

            _previewInProgress = true;

            await AssetInventory.PlayAudio(info);

            _previewInProgress = false;
        }

        private async void PingAsset(AssetInfo info)
        {
            if (disablePings) return;

            // requires pauses in-between to allow editor to catch up
            EditorApplication.ExecuteMenuItem("Window/General/Project");
            await Task.Yield();

            Selection.activeObject = null;
            await Task.Yield();

            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(info.ProjectPath);
            if (Selection.activeObject == null) info.ProjectPath = null; // probably got deleted again
        }

        private async Task CalculateDependencies(AssetInfo info)
        {
            _previewInProgress = true;
            await AssetInventory.CalculateDependencies(info);
            _previewInProgress = false;
        }

        private async void Open(AssetInfo info)
        {
            if (!info.Downloaded) return;

            _previewInProgress = true;
            string targetPath;
            if (info.InProject)
            {
                targetPath = info.ProjectPath;
            }
            else
            {
                targetPath = await AssetInventory.EnsureMaterializedAsset(info);
                if (info.Id == 0) _requireSearchUpdate = true; // was deleted
            }

            if (targetPath != null) EditorUtility.OpenWithDefaultApp(targetPath);
            _previewInProgress = false;
        }

        private async void OpenExplorer(AssetInfo info)
        {
            if (!info.Downloaded) return;

            _previewInProgress = true;
            string targetPath;
            if (info.InProject)
            {
                targetPath = info.ProjectPath;
            }
            else
            {
                targetPath = await AssetInventory.EnsureMaterializedAsset(info);
                if (info.Id == 0) _requireSearchUpdate = true; // was deleted
            }

            if (targetPath != null) EditorUtility.RevealInFinder(targetPath);
            _previewInProgress = false;
        }

        private async void CopyTo(AssetInfo info, string targetFolder, bool withDependencies = false, bool withScripts = false, bool autoPing = true, bool fromDragDrop = false)
        {
            _previewInProgress = true;

            string mainFile = await AssetInventory.CopyTo(info, targetFolder, withDependencies, withScripts, fromDragDrop);
            if (autoPing && mainFile != null)
            {
                PingAsset(new AssetInfo().WithProjectPath(mainFile));
                if (AssetInventory.Config.statsImports == 5) ShowInterstitial();
            }

            _previewInProgress = false;
        }

        private void SetPage(int newPage)
        {
            SetPage(newPage, false);
        }

        private void SetPage(int newPage, bool ignoreExcludedExtensions)
        {
            newPage = Mathf.Clamp(newPage, 1, _pageCount);
            if (newPage != _curPage)
            {
                _curPage = newPage;
                _sgrid.DeselectAll();
                _searchScrollPos = Vector2.zero;
                if (_curPage > 0) PerformSearch(true, ignoreExcludedExtensions);
            }
        }

        private void PerformSearch(bool keepPage = false, bool ignoreExcludedExtensions = false)
        {
            if (AssetInventory.DEBUG_MODE) Debug.LogWarning("Perform Search");

            _requireSearchUpdate = false;
            _keepSearchResultPage = true;
            int lastCount = _resultCount; // a bit of a heuristic but works great and is very performant
            string selectedSize = _resultSizes[AssetInventory.Config.maxResults];
            int.TryParse(selectedSize, out int maxResults);
            if (maxResults <= 0 || maxResults > AssetInventory.Config.maxResultsLimit) maxResults = AssetInventory.Config.maxResultsLimit;
            List<string> wheres = new List<string>();
            List<object> args = new List<object>();
            string escape = "";
            string packageTagJoin = "";
            string fileTagJoin = "";
            string lastWhere = null;

            wheres.Add("(Asset.Exclude=0 or Asset.Exclude is null)");

            // only add detail filters if section is open to not have confusing search results
            if (!AssetInventory.Config.filterOnlyIfBarVisible || AssetInventory.Config.showSearchFilterBar)
            {
                // numerical conditions first
                if (!string.IsNullOrWhiteSpace(_searchWidth))
                {
                    if (int.TryParse(_searchWidth, out int width) && width > 0)
                    {
                        string widthComp = _checkMaxWidth ? "<=" : ">=";
                        wheres.Add($"AssetFile.Width > 0 and AssetFile.Width {widthComp} ?");
                        args.Add(width);
                    }
                }

                if (!string.IsNullOrWhiteSpace(_searchHeight))
                {
                    if (int.TryParse(_searchHeight, out int height) && height > 0)
                    {
                        string heightComp = _checkMaxHeight ? "<=" : ">=";
                        wheres.Add($"AssetFile.Height > 0 and AssetFile.Height {heightComp} ?");
                        args.Add(height);
                    }
                }

                if (!string.IsNullOrWhiteSpace(_searchLength))
                {
                    if (float.TryParse(_searchLength, out float length) && length > 0)
                    {
                        string lengthComp = _checkMaxLength ? "<=" : ">=";
                        wheres.Add($"AssetFile.Length > 0 and AssetFile.Length {lengthComp} ?");
                        args.Add(length);
                    }
                }

                if (!string.IsNullOrWhiteSpace(_searchSize))
                {
                    if (int.TryParse(_searchSize, out int size) && size > 0)
                    {
                        string sizeComp = _checkMaxSize ? "<=" : ">=";
                        wheres.Add($"AssetFile.Size > 0 and AssetFile.Size {sizeComp} ?");
                        args.Add(size * 1024);
                    }
                }

                if (_selectedPackageTag == 1)
                {
                    wheres.Add("not exists (select tap.Id from TagAssignment as tap where Asset.Id = tap.TargetId and tap.TagTarget = 0)");
                }
                else if (_selectedPackageTag > 1 && _tagNames.Length > _selectedPackageTag)
                {
                    string[] arr = _tagNames[_selectedPackageTag].Split('/');
                    string tag = arr[arr.Length - 1];
                    wheres.Add("tap.TagId = ?");
                    args.Add(_tags.First(t => t.Name == tag).Id);

                    packageTagJoin = "inner join TagAssignment as tap on (Asset.Id = tap.TargetId and tap.TagTarget = 0)";
                }

                if (_selectedFileTag == 1)
                {
                    wheres.Add("not exists (select taf.Id from TagAssignment as taf where AssetFile.Id = taf.TargetId and taf.TagTarget = 1)");
                }
                else if (_selectedFileTag > 1 && _tagNames.Length > _selectedFileTag)
                {
                    string[] arr = _tagNames[_selectedFileTag].Split('/');
                    string tag = arr[arr.Length - 1];
                    wheres.Add("taf.TagId = ?");
                    args.Add(_tags.First(t => t.Name == tag).Id);

                    fileTagJoin = "inner join TagAssignment as taf on (AssetFile.Id = taf.TargetId and taf.TagTarget = 1)";
                }

                switch (_selectedPackageTypes)
                {
                    case 1:
                        wheres.Add("Asset.AssetSource != ?");
                        args.Add(Asset.Source.RegistryPackage);
                        break;

                    case 2:
                        wheres.Add("Asset.AssetSource = ?");
                        args.Add(Asset.Source.AssetStorePackage);
                        break;

                    case 3:
                        wheres.Add("Asset.AssetSource = ?");
                        args.Add(Asset.Source.RegistryPackage);
                        break;

                    case 4:
                        wheres.Add("Asset.AssetSource = ?");
                        args.Add(Asset.Source.CustomPackage);
                        break;

                    case 5:
                        wheres.Add("Asset.AssetSource = ?");
                        args.Add(Asset.Source.Directory);
                        break;

                    case 6:
                        wheres.Add("Asset.AssetSource = ?");
                        args.Add(Asset.Source.Archive);
                        break;
                }

                if (_selectedPublisher > 0 && _publisherNames.Length > _selectedPublisher)
                {
                    string[] arr = _publisherNames[_selectedPublisher].Split('/');
                    string publisher = arr[arr.Length - 1];
                    wheres.Add("Asset.SafePublisher = ?");
                    args.Add($"{publisher}");
                }

                if (_selectedAsset > 0 && _assetNames.Length > _selectedAsset)
                {
                    string[] arr = _assetNames[_selectedAsset].Split('/');
                    string asset = arr[arr.Length - 1];
                    if (asset.LastIndexOf('[') > 0)
                    {
                        string assetId = asset.Substring(asset.LastIndexOf('[') + 1);
                        assetId = assetId.Substring(0, assetId.Length - 1);
                        wheres.Add("Asset.Id = ?"); // TODO: going via In would be more efficient but not available at this point
                        args.Add(int.Parse(assetId));
                    }
                    else
                    {
                        wheres.Add("Asset.SafeName = ?"); // TODO: going via In would be more efficient but not available at this point
                        args.Add($"{asset}");
                    }
                }

                if (_selectedCategory > 0 && _categoryNames.Length > _selectedCategory)
                {
                    string[] arr = _categoryNames[_selectedCategory].Split('/');
                    string category = arr[arr.Length - 1];
                    wheres.Add("Asset.SafeCategory = ?");
                    args.Add($"{category}");
                }

                if (_selectedColorOption > 0)
                {
                    wheres.Add("AssetFile.Hue >= ?");
                    wheres.Add("AssetFile.Hue <= ?");
                    args.Add(_selectedColor.ToHue() - AssetInventory.Config.hueRange / 2f);
                    args.Add(_selectedColor.ToHue() + AssetInventory.Config.hueRange / 2f);
                }
            }

            if (!string.IsNullOrWhiteSpace(_searchPhrase))
            {
                string phrase = _searchPhrase;
                string searchField = "AssetFile.Path";

                switch (AssetInventory.Config.searchField)
                {
                    case 1:
                        searchField = "AssetFile.FileName";
                        break;

                    case 2:
                        searchField = "AssetFile.AICaption";
                        break;
                }

                // check for sqlite escaping requirements
                if (phrase.Contains("_"))
                {
                    phrase = phrase.Replace("_", "\\_");
                    escape = "ESCAPE '\\'";
                }

                if (_searchPhrase.StartsWith("=")) // expert mode
                {
                    if (_searchPhrase.Length > 1) lastWhere = _searchPhrase.Substring(1) + $" {escape}";
                }
                else if (_searchPhrase.StartsWith("~")) // exact mode
                {
                    string term = _searchPhrase.Substring(1);
                    wheres.Add($"{searchField} like ? {escape}");
                    args.Add($"%{term}%");
                }
                else
                {
                    string[] fuzzyWords = _searchPhrase.Split(' ');
                    foreach (string fuzzyWord in fuzzyWords.Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)))
                    {
                        if (fuzzyWord.StartsWith("+"))
                        {
                            wheres.Add($"{searchField} like ? {escape}");
                            args.Add($"%{fuzzyWord.Substring(1)}%");
                        }
                        else if (fuzzyWord.StartsWith("-"))
                        {
                            wheres.Add($"{searchField} not like ? {escape}");
                            args.Add($"%{fuzzyWord.Substring(1)}%");
                        }
                        else
                        {
                            wheres.Add($"{searchField} like ? {escape}");
                            args.Add($"%{fuzzyWord}%");
                        }
                    }
                }
            }

            int searchType = _fixedSearchTypeIdx >= 0 ? _fixedSearchTypeIdx : AssetInventory.Config.searchType;
            if (searchType > 0 && _types.Length > searchType)
            {
                string rawType = _types[searchType];
                string[] type = rawType.Split('/');
                if (type.Length > 1)
                {
                    wheres.Add("AssetFile.Type = ?");
                    args.Add(type.Last());
                }
                else if (AssetInventory.TypeGroups.TryGetValue(rawType, out string[] group))
                {
                    // sqlite does not support binding lists, parameters must be spelled out
                    List<string> paramCount = new List<string>();
                    foreach (string t in group)
                    {
                        paramCount.Add("?");
                        args.Add(t);
                    }

                    wheres.Add("AssetFile.Type in (" + string.Join(",", paramCount) + ")");
                }
            }

            if (!ignoreExcludedExtensions && AssetInventory.Config.excludeExtensions && AssetInventory.Config.searchType == 0 && !string.IsNullOrWhiteSpace(AssetInventory.Config.excludedExtensions))
            {
                string[] extensions = AssetInventory.Config.excludedExtensions.Split(';');
                List<string> paramCount = new List<string>();
                foreach (string ext in extensions)
                {
                    paramCount.Add("?");
                    args.Add(ext.Trim());
                }

                wheres.Add("AssetFile.Type not in (" + string.Join(",", paramCount) + ")");
            }

            switch (AssetInventory.Config.previewVisibility)
            {
                case 2:
                    wheres.Add("(AssetFile.PreviewState = 1 or AssetFile.PreviewState = 3)");
                    break;

                case 3:
                    wheres.Add("(AssetFile.PreviewState != 1 and AssetFile.PreviewState != 3)");
                    break;
            }

            // ordering, can only be done on DB side since post-processing results would only work on the paged results which is incorrect
            string orderBy = "order by ";
            switch (AssetInventory.Config.sortField)
            {
                case 0:
                    orderBy += "AssetFile.Path";
                    break;

                case 1:
                    orderBy += "AssetFile.FileName";
                    break;

                case 2:
                    orderBy += "AssetFile.Size";
                    break;

                case 3:
                    orderBy += "AssetFile.Type";
                    break;

                case 4:
                    orderBy += "AssetFile.Length";
                    break;

                case 5:
                    orderBy += "AssetFile.Width";
                    break;

                case 6:
                    orderBy += "AssetFile.Height";
                    break;

                case 7:
                    orderBy += "AssetFile.Hue";
                    wheres.Add("AssetFile.Hue >=0");
                    break;

                case 8:
                    orderBy += "Asset.DisplayCategory";
                    break;

                case 9:
                    orderBy += "Asset.LastRelease";
                    break;

                case 10:
                    orderBy += "Asset.AssetRating";
                    break;

                case 11:
                    orderBy += "Asset.RatingCount";
                    break;

                default:
                    orderBy = "";
                    break;
            }

            if (!string.IsNullOrEmpty(orderBy))
            {
                orderBy += " COLLATE NOCASE";
                if (AssetInventory.Config.sortDescending) orderBy += " desc";
                orderBy += ", AssetFile.Path"; // always sort by path in case of equality of first level sorting
            }
            if (!string.IsNullOrEmpty(lastWhere)) wheres.Add(lastWhere);

            string where = wheres.Count > 0 ? "where " + string.Join(" and ", wheres) : "";
            string baseQuery = $"from AssetFile inner join Asset on Asset.Id = AssetFile.AssetId {packageTagJoin} {fileTagJoin} {where}";
            string countQuery = $"select count(*) {baseQuery}";
            string dataQuery = $"select *, AssetFile.Id as Id {baseQuery} {orderBy}";
            if (maxResults > 0) dataQuery += $" limit {maxResults} offset {(_curPage - 1) * maxResults}";
            try
            {
                _searchError = null;
                _resultCount = DBAdapter.DB.ExecuteScalar<int>($"{countQuery}", args.ToArray());
                _files = DBAdapter.DB.Query<AssetInfo>($"{dataQuery}", args.ToArray());
            }
            catch (SQLiteException e)
            {
                _searchError = e.Message;
            }

            // pagination
            _sgrid.contents = _files.Select(file =>
            {
                string text = "";
                int tileTextToUse = AssetInventory.Config.tileText;
                if (tileTextToUse == 0) // intelligent
                {
                    if (AssetInventory.Config.searchTileSize < 70)
                    {
                        tileTextToUse = 6;
                    }
                    else if (AssetInventory.Config.searchTileSize < 90)
                    {
                        tileTextToUse = 4;
                    }
                    else if (AssetInventory.Config.searchTileSize < 150)
                    {
                        tileTextToUse = 3;
                    }
                    else
                    {
                        tileTextToUse = 2;
                    }
                }
                switch (tileTextToUse)
                {
                    case 2:
                        text = file.ShortPath;
                        break;

                    case 3:
                        text = file.FileName;
                        break;

                    case 4:
                        text = Path.GetFileNameWithoutExtension(file.FileName);
                        break;
                }
                text = text == null ? "" : text.Replace('/', Path.DirectorySeparatorChar);

                return new GUIContent(text);
            }).ToArray();
            _sgrid.enlargeTiles = AssetInventory.Config.enlargeTiles;
            _sgrid.centerTiles = AssetInventory.Config.centerTiles;
            _sgrid.Init(_assets, _files, CalculateSearchBulkSelection);

            AssetInventory.ResolveParents(_files, _assets);

            _pageCount = AssetUtils.GetPageCount(_resultCount, maxResults);
            if (!keepPage && lastCount != _resultCount)
            {
                SetPage(1, ignoreExcludedExtensions);
            }
            else
            {
                SetPage(_curPage, ignoreExcludedExtensions);
            }

            // preview images
            _textureLoading?.Cancel();
            _textureLoading = new CancellationTokenSource();
            LoadTextures(false, _textureLoading.Token); // TODO: should be true once pages endless scrolling is in

            _searchDone = true;
        }

        private async void LoadTextures(bool firstPageOnly, CancellationToken ct)
        {
            string previewFolder = AssetInventory.GetPreviewFolder();
            int idx = -1;
            IEnumerable<AssetInfo> files = _files.Take(firstPageOnly ? 20 * 8 : _files.Count);
            foreach (AssetInfo info in files)
            {
                if (ct.IsCancellationRequested) return;
                idx++;

                string previewFile = info.GetPreviewFile(previewFolder);
                if (info.HasPreview()) AssetImporter.ValidatePreviewFile(info, previewFolder);
                if (!info.HasPreview())
                {
                    if (!AssetInventory.Config.showIconsForMissingPreviews) continue;

                    // check if well-known extension
                    if (_staticPreviews.TryGetValue(info.Type, out string preview))
                    {
                        _sgrid.contents[idx].image = EditorGUIUtility.IconContent(preview).image;
                    }
                    else
                    {
                        _sgrid.contents[idx].image = EditorGUIUtility.IconContent("d_DefaultAsset Icon").image;
                    }
                    continue;
                }

                Texture2D texture = await AssetUtils.LoadLocalTexture(previewFile, false, (AssetInventory.Config.upscalePreviews && !AssetInventory.Config.upscaleLossless) ? AssetInventory.Config.upscaleSize : 0);
                if (texture != null && _sgrid.contents.Length > idx) _sgrid.contents[idx].image = texture;
            }
        }

        private void CalculateSearchBulkSelection()
        {
            _assetFileBulkTags.Clear();
            _sgrid.selectionItems.ForEach(info => info.AssetTags?.ForEach(t =>
            {
                if (!_assetFileBulkTags.ContainsKey(t.Name)) _assetFileBulkTags.Add(t.Name, new Tuple<int, Color>(0, t.GetColor()));
                _assetFileBulkTags[t.Name] = new Tuple<int, Color>(_assetFileBulkTags[t.Name].Item1 + 1, _assetFileBulkTags[t.Name].Item2);
            }));
        }

        private void OpenInSearch(AssetInfo info, bool force = false)
        {
            if (info.Id <= 0) return;
            if (!force && info.FileCount <= 0) return;
            AssetInfo oldEntry = _selectedEntry;

            if (info.Exclude)
            {
                if (!EditorUtility.DisplayDialog("Package is Excluded", "This package is currently excluded from the search. Should it be included again?", "Include Again", "Cancel"))
                {
                    return;
                }
                AssetInventory.SetAssetExclusion(info, false);
                ReloadLookups();
            }
            ResetSearch(false, true);
            if (force) _selectedEntry = oldEntry;

            AssetInventory.Config.tab = 0;

            // search for exact match first
            _selectedAsset = Array.IndexOf(_assetNames, _assetNames.FirstOrDefault(a => a == info.SafeName + $" [{info.AssetId}]"));
            if (_selectedAsset < 0) _selectedAsset = Array.IndexOf(_assetNames, _assetNames.FirstOrDefault(a => a == info.SafeName.Substring(0, 1) + "/" + info.SafeName + $" [{info.AssetId}]"));
            if (_selectedAsset < 0) _selectedAsset = Array.IndexOf(_assetNames, _assetNames.FirstOrDefault(a => a.EndsWith(info.SafeName + $" [{info.AssetId}]")));

            if (info.AssetSource == Asset.Source.RegistryPackage && _selectedPackageTypes == 1) _selectedPackageTypes = 0;
            _requireSearchUpdate = true;
            AssetInventory.Config.showSearchFilterBar = true;
        }

        private void ResetSearch(bool filterBarOnly, bool keepAssetType)
        {
            if (!filterBarOnly)
            {
                _searchPhrase = "";
                if (!keepAssetType) AssetInventory.Config.searchType = 0;
            }

            _selectedEntry = null;
            _selectedAsset = 0;
            _selectedPackageTypes = 1;
            _selectedColorOption = 0;
            _selectedColor = Color.clear;
            _selectedPackageTag = 0;
            _selectedFileTag = 0;
            _selectedPublisher = 0;
            _selectedCategory = 0;
            _searchHeight = "";
            _checkMaxHeight = false;
            _searchWidth = "";
            _checkMaxWidth = false;
            _searchLength = "";
            _checkMaxLength = false;
            _searchSize = "";
            _checkMaxSize = false;
        }

        private async Task PerformCopyTo(AssetInfo info, string path, bool fromDragDrop = false)
        {
            if (info.InProject) return;
            if (string.IsNullOrEmpty(path)) return;

            while (info.DependencyState == AssetInfo.DependencyStateOptions.Calculating) await Task.Yield();
            if (info.DependencyState == AssetInfo.DependencyStateOptions.Unknown) await CalculateDependencies(info);
            if (info.DependencySize > 0 && AssetInventory.NeedsDependencyScan(info.Type))
            {
                CopyTo(info, path, true, false, false, fromDragDrop);
            }
            else
            {
                CopyTo(info, path, false, false, true, fromDragDrop);
            }
        }

        private static bool DragDropAvailable()
        {
#if UNITY_2021_2_OR_NEWER
            return true;
#else
            return false;
#endif
        }

        private void InitDragAndDrop()
        {
#if UNITY_2021_2_OR_NEWER
            DragAndDrop.ProjectBrowserDropHandler dropHandler = OnProjectWindowDrop;
            if (!DragAndDrop.HasHandler("ProjectBrowser".GetHashCode(), dropHandler))
            {
                DragAndDrop.AddDropHandler(dropHandler);
            }
#endif
        }

        private void DeinitDragAndDrop()
        {
#if UNITY_2021_2_OR_NEWER
            DragAndDrop.ProjectBrowserDropHandler dropHandler = OnProjectWindowDrop;
            if (DragAndDrop.HasHandler("ProjectBrowser".GetHashCode(), dropHandler))
            {
                DragAndDrop.RemoveDropHandler(dropHandler);
            }
#endif
        }

        private DragAndDropVisualMode OnProjectWindowDrop(int dragInstanceId, string dropUponPath, bool perform)
        {
            if (perform && _dragging)
            {
                _dragging = false;
                DeinitDragAndDrop();

                List<AssetInfo> infos = (List<AssetInfo>)DragAndDrop.GetGenericData("AssetInfo");
                if (infos != null && infos.Count > 0) // can happen in some edge asynchronous scenarios
                {
                    if (File.Exists(dropUponPath)) dropUponPath = Path.GetDirectoryName(dropUponPath);
                    PerformCopyToBulk(infos, dropUponPath);
                }
                DragAndDrop.AcceptDrag();
            }
            return DragAndDropVisualMode.Copy;
        }

        private async void PerformCopyToBulk(List<AssetInfo> infos, string targetPath)
        {
            if (infos.Count == 0) return;

            foreach (AssetInfo info in infos)
            {
                await PerformCopyTo(info, targetPath, true);
            }
            PingAsset(infos[0]);
        }

        private void HandleDragDrop()
        {
            switch (Event.current.type)
            {
                case EventType.MouseDrag:
                    if (!_mouseOverSearchResultRect) return;
                    if (!_dragging && _selectedEntry != null)
                    {
                        _dragging = true;

                        InitDragAndDrop();
                        DragAndDrop.PrepareStartDrag();

                        if (_sgrid.selectionCount > 0)
                        {
                            DragAndDrop.SetGenericData("AssetInfo", _sgrid.selectionItems);
                            DragAndDrop.objectReferences = _sgrid.selectionItems
                                .Where(item => !string.IsNullOrWhiteSpace(item.ProjectPath))
                                .Select(item => AssetDatabase.LoadMainAssetAtPath(item.ProjectPath))
                                .ToArray();
                        }
                        else
                        {
                            DragAndDrop.SetGenericData("AssetInfo", new List<AssetInfo> {_selectedEntry});
                            if (!string.IsNullOrWhiteSpace(_selectedEntry.ProjectPath))
                            {
                                DragAndDrop.objectReferences = new[] {AssetDatabase.LoadMainAssetAtPath(_selectedEntry.ProjectPath)};
                            }
                        }
                        DragAndDrop.StartDrag("Dragging " + _selectedEntry);
                        Event.current.Use();
                    }
                    break;

                /* FIXME: not finishing the drag will cause no DeInit right now, needs subsequent mouse up event
                case EventType.DragExited:
                    // drag exit will also fire when out of drag-start-control bounds
                    if (!_mouseOverSearchResultRect) return;
                    _dragging = false;
                    DeinitDragAndDrop();
                    break;
                */

                case EventType.MouseUp:
                    _dragging = false;
                    DeinitDragAndDrop();

                    // clean up, also in case MouseDrag never occurred
                    DragAndDrop.PrepareStartDrag();
                    break;
            }
        }
    }
}