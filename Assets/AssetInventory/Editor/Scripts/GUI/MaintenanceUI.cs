using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetInventory
{
    public sealed class MaintenanceUI : EditorWindow
    {
        public static event Action OnMaintenanceDone;

        private readonly List<Validator> _validators = new List<Validator>();
        private Vector2 _checksScrollPos;

        public MaintenanceUI()
        {
            Init();
        }

        public static MaintenanceUI ShowWindow()
        {
            MaintenanceUI window = GetWindow<MaintenanceUI>("Maintenance Wizard");
            window.minSize = new Vector2(500, 300);

            return window;
        }

        private void Init()
        {
            _validators.Clear();
            _validators.Add(new UnindexedSubPackagesValidator());
            _validators.Add(new OrphanedTagAssignmentsValidator());
            _validators.Add(new MissingParentPackagesValidator());
            _validators.Add(new OrphanedAssetFilesValidator());
            _validators.Add(new OrphanedPackagesValidator());
            _validators.Add(new ReassignedMediaIndexValidator());
            _validators.Add(new DuplicateMediaIndexValidator());
            _validators.Add(new MissingAudioLengthValidator());
            _validators.Add(new OrphanedPreviewFoldersValidator());
            _validators.Add(new OrphanedPreviewFilesValidator());
        }

        public void Prepare()
        {
        }

        private void ScanAll()
        {
            _validators.ForEach(v =>
            {
                if (v.CurrentState == Validator.State.Idle || v.CurrentState == Validator.State.Completed) v.Validate();
            });
        }

        public void OnGUI()
        {
            EditorGUI.BeginDisabledGroup(false);

            EditorGUILayout.LabelField("This wizard will scan your database, previews and files for issues and provide means to repair or clean these up.", EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space();
            if (GUILayout.Button("Scan All", GUILayout.ExpandWidth(false), GUILayout.Height(40)))
            {
                ScanAll();
            }

            EditorGUILayout.Space();
            _checksScrollPos = GUILayout.BeginScrollView(_checksScrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandWidth(true));
            foreach (Validator validator in _validators)
            {
                EditorGUILayout.LabelField(validator.Name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(validator.Description, EditorStyles.wordWrappedMiniLabel);

                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(validator.CurrentState != Validator.State.Idle && validator.CurrentState != Validator.State.Completed);
                if (GUILayout.Button("Scan", GUILayout.ExpandWidth(false)))
                {
                    validator.Validate();
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.LabelField("Result:", GUILayout.Width(40));
                Color oldColor = GUI.color;
                GUI.color = Color.yellow;
                switch (validator.CurrentState)
                {
                    case Validator.State.Idle:
                        EditorGUILayout.LabelField("-not scanned yet-");
                        break;

                    case Validator.State.Scanning:
                        EditorGUILayout.LabelField("scanning...");
                        break;

                    case Validator.State.Completed:
                        GUI.color = validator.IssueCount == 0 ? Color.green : Color.red;
                        EditorGUILayout.LabelField($"{validator.IssueCount} issues found" + (validator.IssueCount > 0 && !validator.Fixable ? " (not automatically fixable)" : ""));
                        GUI.color = oldColor;

                        if (validator.IssueCount > 0)
                        {
                            EditorGUI.BeginDisabledGroup(validator.CurrentState == Validator.State.Fixing);
                            if (GUILayout.Button("Show...", GUILayout.ExpandWidth(false)))
                            {
                                switch (validator.Type)
                                {
                                    case Validator.ValidatorType.DB:
                                        EditorUtility.DisplayDialog("Issue List (Top 50)", string.Join("\n", validator.DBIssues.Take(50).Select(i => $"{(string.IsNullOrWhiteSpace(i.Path) ? i.GetDisplayName() : i.Path)} ({i.Id})")), "OK");
                                        break;

                                    case Validator.ValidatorType.FileSystem:
                                        EditorUtility.DisplayDialog("Issue List (Top 50)", string.Join("\n", validator.FileIssues.Take(50)), "OK");
                                        break;

                                }
                            }
                            if (validator.Fixable)
                            {
                                if (GUILayout.Button(validator.FixCaption, GUILayout.ExpandWidth(false)))
                                {
                                    validator.Fix();
                                    OnMaintenanceDone?.Invoke();
                                }
                            }
                            EditorGUI.EndDisabledGroup();
                        }
                        break;

                    case Validator.State.Fixing:
                        GUI.color = Color.red;
                        EditorGUILayout.LabelField("fixing...");
                        break;

                }
                GUI.color = oldColor;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(15);
            }
            GUILayout.EndScrollView();

            EditorGUI.EndDisabledGroup();
        }
    }
}
