using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AssetInventory
{
    public sealed class RelativeUI : EditorWindow
    {
        private const int BREAK_INTERVAL = 50;

        private string _key;
        private bool _conversionRunning;
        private Action<string> _callback;
        private bool _disableMode;
        private string _location;
        private FolderSpec _spec;
        private int _conversionCount;
        private int _currentConversion;
        private string _conversionText;
        private RelativeLocation _relLocation;
        private HashSet<string> _locations;
        private int _locationIdx;
        private string[] _locationsArr;

        public static RelativeUI ShowWindow()
        {
            RelativeUI window = GetWindow<RelativeUI>("Relative Storage");
            window.minSize = new Vector2(200, 250);
            window.maxSize = new Vector2(1000, 250);

            return window;
        }

        public void Init(FolderSpec spec)
        {
            _spec = spec;
            _disableMode = spec.location.StartsWith(AssetInventory.TAG_START);
            _key = _disableMode ? spec.relativeKey : Path.GetFileName(spec.location);
            _conversionRunning = false;
            _locationIdx = 0;

            if (_disableMode)
            {
                _relLocation = AssetInventory.RelativeLocations.FirstOrDefault(rl => rl.Key == _spec.relativeKey);
                _location = _relLocation?.Location;
                _locations = new HashSet<string>();
                if (!string.IsNullOrWhiteSpace(_location)) _locations.Add(ConvertSlashToUnicodeSlash(_location));
                _relLocation?.otherLocations.ForEach(rl => _locations.Add(ConvertSlashToUnicodeSlash(rl)));
                _locationsArr = _locations.ToArray();
            }
        }

        public void OnGUI()
        {
            // check if window was restored by Unity
            if (_spec == null || string.IsNullOrWhiteSpace(_spec.location))
            {
                Close();
                return;
            }

            EditorGUILayout.LabelField("Storing assets in a relative way allows using the same database from multiple devices that map to it but have different drive mappings or folder structures.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();

            if (_disableMode)
            {
                int width = 120;

                EditorGUILayout.LabelField("Reverting will replace all usages of the key with the original location again, potentially breaking usage from all other systems.", EditorStyles.wordWrappedLabel);
                EditorGUILayout.Space();

                EditorGUI.BeginDisabledGroup(_conversionRunning);

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(UIStyles.Content("Key"), EditorStyles.boldLabel, GUILayout.Width(width));
                EditorGUILayout.LabelField(_spec.relativeKey);
                GUILayout.EndHorizontal();

                if (_relLocation != null)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Location to Restore"), EditorStyles.boldLabel, GUILayout.Width(width));
                    _locationIdx = EditorGUILayout.Popup(_locationIdx, _locationsArr);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(UIStyles.Content("Affected Systems"), EditorStyles.boldLabel, GUILayout.Width(width));
                    EditorGUILayout.LabelField((_relLocation?.otherLocations.Count + (_relLocation?.Id > 0 ? 1 : 0)).ToString());
                    GUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox("No relative location mapping could be found for the key. Reversing the database is not possible.", MessageType.Error);
                }
            }
            else
            {
                int width = 60;

                EditorGUILayout.LabelField("Conversion will remove all absolute paths and instead use the key for the base path. This can then be mapped to different locations on other devices.", EditorStyles.wordWrappedLabel);
                EditorGUILayout.Space();

                EditorGUILayout.HelpBox("The conversion will happen for all assets matching the location. It can be easily reverted.", MessageType.Info);

                EditorGUI.BeginDisabledGroup(_conversionRunning);

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(UIStyles.Content("Location"), EditorStyles.boldLabel, GUILayout.Width(width));
                EditorGUILayout.LabelField(_spec.location);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(UIStyles.Content("Key", "Unique key for this location"), EditorStyles.boldLabel, GUILayout.Width(width));
                _key = EditorGUILayout.TextField(_key);
                GUILayout.EndHorizontal();
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();

            if (_conversionRunning)
            {
                UIStyles.DrawProgressBar((float)_currentConversion / _conversionCount, $"{_conversionText}: {_currentConversion + 1}/{_conversionCount}");
            }
            else
            {
                GUILayout.BeginHorizontal();
                if (_disableMode)
                {
                    EditorGUI.BeginDisabledGroup(_relLocation == null);
                    if (GUILayout.Button("Revert relative persistence")) RevertRelative();
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(_key));
                    if (GUILayout.Button("Start conversion")) MakeRelative();
                    EditorGUI.EndDisabledGroup();
                }
                GUILayout.EndHorizontal();
            }
        }

        private async void MakeRelative()
        {
            if (new[] {"ac", "pc"}.Contains(_key.ToLowerInvariant()))
            {
                EditorUtility.DisplayDialog("Invalid key", "The key cannot be 'ac' or 'pc' as these are reserved for the Asset and Package cache.", "OK");
                return;
            }

            _conversionRunning = true;

            // create configuration
            RelativeLocation rel = new RelativeLocation();
            rel.System = AssetInventory.GetSystemId();
            rel.Key = _key;
            rel.SetLocation(_spec.location);
            DBAdapter.DB.Insert(rel);

            // adapt all folder specs with that location since it is not unique to know exactly which folder resulted in which asset entry
            AssetInventory.Config.folders.Where(f => f.location == rel.Location).ForEach(f =>
            {
                f.storeRelative = true;
                f.relativeKey = _key;
                f.location = $"{AssetInventory.TAG_START}{_key}{AssetInventory.TAG_END}";
            });
            AssetInventory.SaveConfig();
            AssetInventory.LoadRelativeLocations();

            // fetch assets in question
            string dbKey = $"{AssetInventory.TAG_START}{_key}{AssetInventory.TAG_END}";
            List<Asset> assets = DBAdapter.DB.Query<Asset>("SELECT Id, Location from Asset where Location like ?", rel.Location + "%");
            _conversionCount = assets.Count;
            _conversionText = "Packages done";
            for (_currentConversion = 0; _currentConversion < _conversionCount; _currentConversion++)
            {
                string newLocation = assets[_currentConversion].Location.Replace(rel.Location, dbKey);
                DBAdapter.DB.Execute("UPDATE Asset set Location = ? where Id = ?", newLocation, assets[_currentConversion].Id);
            }

            List<AssetFile> files = DBAdapter.DB.Query<AssetFile>("SELECT Id, Path, SourcePath from AssetFile where Path like ?", rel.Location + "%");
            _conversionCount = files.Count;
            _conversionText = "Files done";
            for (_currentConversion = 0; _currentConversion < _conversionCount; _currentConversion++)
            {
                string newPath = files[_currentConversion].Path.Replace(rel.Location, dbKey);
                string newSourcePath = files[_currentConversion].SourcePath.Replace(rel.Location, dbKey);
                DBAdapter.DB.Execute("UPDATE AssetFile set Path = ?, SourcePath = ? where Id = ?", newPath, newSourcePath, files[_currentConversion].Id);
                if (_currentConversion % BREAK_INTERVAL == 0) await Task.Yield(); // let editor breath in case many files are already indexed
            }

            _conversionRunning = false;

            Close();
        }

        private async void RevertRelative()
        {
            _conversionRunning = true;
            _location = ConvertUnicodeSlashToSlash(_locationsArr[_locationIdx]);

            // adapt all folder specs with that location since it is not unique to know exactly which folder resulted in which asset entry
            AssetInventory.Config.folders.Where(f => f.relativeKey == _key).ForEach(f =>
            {
                f.storeRelative = false;
                f.relativeKey = null;
                f.location = _location;
            });
            AssetInventory.SaveConfig();

            int keyUsages = AssetInventory.Config.folders.Count(fs => fs.relativeKey == _key);
            if (keyUsages == 0)
            {
                DBAdapter.DB.Execute("DELETE from RelativeLocation where Key=?", _key);
            }
            AssetInventory.LoadRelativeLocations();

            // fetch assets in question
            string dbKey = $"{AssetInventory.TAG_START}{_key}{AssetInventory.TAG_END}";
            List<Asset> assets = DBAdapter.DB.Query<Asset>("SELECT Id, Location from Asset where Location like ?", dbKey + "%");
            _conversionCount = assets.Count;
            _conversionText = "Packages done";
            for (_currentConversion = 0; _currentConversion < _conversionCount; _currentConversion++)
            {
                string newLocation = assets[_currentConversion].Location.Replace(dbKey, _location);
                DBAdapter.DB.Execute("UPDATE Asset set Location = ? where Id = ?", newLocation, assets[_currentConversion].Id);
            }

            List<AssetFile> files = DBAdapter.DB.Query<AssetFile>("SELECT Id, Path, SourcePath from AssetFile where Path like ?", dbKey + "%");
            _conversionCount = files.Count;
            _conversionText = "Files done";
            for (_currentConversion = 0; _currentConversion < _conversionCount; _currentConversion++)
            {
                string newPath = files[_currentConversion].Path.Replace(dbKey, _location);
                string newSourcePath = files[_currentConversion].SourcePath.Replace(dbKey, _location);
                DBAdapter.DB.Execute("UPDATE AssetFile set Path = ?, SourcePath = ? where Id = ?", newPath, newSourcePath, files[_currentConversion].Id);
                if (_currentConversion % BREAK_INTERVAL == 0) await Task.Yield(); // let editor breath in case many files are already indexed
            }

            _conversionRunning = false;

            Close();
        }

        private string ConvertSlashToUnicodeSlash(string text)
        {
            return text.Replace('/', '\u2215');
        }

        private string ConvertUnicodeSlashToSlash(string text)
        {
            return text.Replace('\u2215', '/');
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}
