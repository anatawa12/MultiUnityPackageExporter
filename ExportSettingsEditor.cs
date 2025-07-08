#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Anatawa12.MultiUnityPackageExporter
{
    [CustomEditor(typeof(ExportSettings))]
    internal class ExportSettingsEditor : Editor
    {
        private static class Styles
        {
            public static readonly GUIContent PackageNamePattern = new GUIContent("Package Name Pattern");
            public static readonly GUIContent RemoveButton = new GUIContent("-", "Remove this file set");
            public static readonly GUIContent Prefix = new GUIContent("Prefix", "Include only files that start with this prefix");
            public static readonly GUIContent Suffix = new GUIContent("Suffix", "Include only files that end with this suffix");

            public static readonly GUIStyle ErrorLabel = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.red },
                wordWrap = true
            };
            public static readonly GUIStyle TextFieldTextOnly = new GUIStyle(EditorStyles.textField)
            {
                name = "textFieldTextOnly",
                normal = { background = null, scaledBackgrounds = null },
                hover = { background = null, scaledBackgrounds = null },
                active = { background = null, scaledBackgrounds = null },
                onNormal = { background = null, scaledBackgrounds = null },
                onHover = { background = null, scaledBackgrounds = null },
                onActive = { background = null, scaledBackgrounds = null },
                focused = { background = null, scaledBackgrounds = null },
                onFocused = { background = null, scaledBackgrounds = null },
            };
            public static readonly GUIStyle DropAreaStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic,
                normal = { textColor = Color.gray },
                hover = { textColor = Color.gray },
                padding = new RectOffset(10, 10, 10, 10),
                stretchWidth = true,
                stretchHeight = true,
            };
        }

        private SerializedProperty _packageNamePatternProperty = null!;
        private SerializedProperty _commonFilesProperty = null!;
        private SerializedProperty _variantsProperty = null!;

        private void OnEnable()
        {
            _packageNamePatternProperty = serializedObject.FindProperty(nameof(ExportSettings.packageNamePattern));
            _commonFilesProperty = serializedObject.FindProperty(nameof(ExportSettings.commonFiles));
            _variantsProperty = serializedObject.FindProperty(nameof(ExportSettings.variants));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_packageNamePatternProperty, new GUIContent(Styles.PackageNamePattern));
            EditorGUILayout.HelpBox("Use {variant} to include the variant name in the package name.", MessageType.Info);

            EditorGUILayout.Space();

            if (GUILayout.Button("Export UnityPackages", GUILayout.Height(25)))
                ExportUnityPackages();

            EditorGUILayout.Space(9);
            GUILayout.Label("Common Files", EditorStyles.boldLabel);
            DrawFiles(_commonFilesProperty);

            EditorGUILayout.Space();
            GUILayout.Label("Variants", EditorStyles.boldLabel);
            DrawVariants(_variantsProperty);

            serializedObject.ApplyModifiedProperties();
        }

        private const string EditorPrefsLastExportLocationKey = "com.anatawa12.multi-unity-package-exporter.last-export-location";

        private void ExportUnityPackages()
        {
            var settings = (ExportSettings)target;

            if (settings.variants.Any(x => string.IsNullOrWhiteSpace(x.name)))
            {
                EditorUtility.DisplayDialog("Error", "Some variant name is blank.", "OK");
                return;
            }
            
            if (settings.variants.Select(x => x.name).Distinct().Count() != settings.variants.Length)
            {
                EditorUtility.DisplayDialog("Error", "Some variant names are duplicated.", "OK");
                return;
            }

            var lastLocation = EditorPrefs.GetString(EditorPrefsLastExportLocationKey, "");

            var location = EditorUtility.SaveFolderPanel("Export UnityPackages", lastLocation, "");
            if (string.IsNullOrEmpty(location)) return;

            if (Directory.GetFileSystemEntries(location).Length != 0)
            {
                switch (EditorUtility.DisplayDialogComplex("Warning",
                            "The selected folder is not empty. Do you want to clear it before exporting?",
                            "Clear", "Do Not Clear", "Cancel Export"))
                {
                    case 0: // Yes
                        try
                        {
                            foreach (var file in Directory.GetFiles(location))
                            {
                                File.Delete(file);
                            }
                            foreach (var dir in Directory.GetDirectories(location))
                            {
                                Directory.Delete(dir, true);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Failed to clear the directory: {e.Message}");
                            return;
                        }
                        break;
                    case 1: // No
                        break;
                    case 2: // Cancel Export
                        return;
                }
            }

            EditorPrefs.SetString(EditorPrefsLastExportLocationKey, location);

            Exporter.ExportPackages(settings, location);
        }

        private void DrawVariants(SerializedProperty variantsProperty)
        {
            for (int i = 0; i < variantsProperty.arraySize; i++)
            {
                var variantProperty = variantsProperty.GetArrayElementAtIndex(i);
                var variantNameProperty = variantProperty.FindPropertyRelative(nameof(ExportVariant.name));
                var filesProperty = variantProperty.FindPropertyRelative(nameof(ExportVariant.files));

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(variantNameProperty, GUIContent.none);
                if (GUILayout.Button(Styles.RemoveButton, EditorStyles.miniButton, GUILayout.MaxWidth(20)))
                {
                    variantsProperty.DeleteArrayElementAtIndex(i);
                    i--; // Adjust index since we removed an element
                    continue;
                }
                EditorGUILayout.EndHorizontal();

                if (variantNameProperty.stringValue.Length == 0)
                {
                    // Empty variant name is not allowed, so show error
                    GUILayout.Label("Variant name cannot be empty.", Styles.ErrorLabel);
                }
                GUILayout.Label("UnityPackage will be named " +
                                Exporter.PackageName(_packageNamePatternProperty.stringValue, variantNameProperty.stringValue),
                                EditorStyles.wordWrappedLabel);

                GUILayout.Label("Files", EditorStyles.boldLabel);
                DrawFiles(filesProperty, variantNameProperty.stringValue);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }

            // Create button to add new variant
            if (GUILayout.Button("Add Variant", EditorStyles.miniButton))
            {
                // New variant will have same contents as last variant, with empty name
                variantsProperty.arraySize++;
                var newVariant = variantsProperty.GetArrayElementAtIndex(variantsProperty.arraySize - 1);
                newVariant.FindPropertyRelative(nameof(ExportVariant.name)).stringValue = "";
            }
        }

        private void DrawFiles(SerializedProperty filesProperty, string variantName = "")
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            for (int i = 0; i < filesProperty.arraySize; i++)
            {
                if (DrawSingleFileSet(filesProperty.GetArrayElementAtIndex(i), variantName))
                {
                    filesProperty.DeleteArrayElementAtIndex(i);
                    i--; // Adjust index since we removed an element
                }
            }

            // Create area fore dropping files
            if (DropArea("Drop files here to add Files / Folders", GUILayout.Height(30)) is {} objects)
            {
                foreach (var o in objects)
                {
                    if (o == null) continue;
                    filesProperty.arraySize++;
                    SerializedProperty newFileProperty = filesProperty.GetArrayElementAtIndex(filesProperty.arraySize - 1);
                    newFileProperty.FindPropertyRelative(nameof(ExportFileSet.file)).objectReferenceValue = o;
                    newFileProperty.FindPropertyRelative(nameof(ExportFileSet.selection)).enumValueIndex = (int)ExportFileSelection.FileOrDirectory;
                    newFileProperty.FindPropertyRelative(nameof(ExportFileSet.matcher)).stringValue = "";
                }
            }

            EditorGUILayout.EndVertical();
        }

        // returns true if the file set is requested to be removed
        private bool DrawSingleFileSet(SerializedProperty fileSetProperty, string variantName = "")
        {
            var selectionProperty = fileSetProperty.FindPropertyRelative(nameof(ExportFileSet.selection));
            var fileProperty = fileSetProperty.FindPropertyRelative(nameof(ExportFileSet.file));
            var matcherProperty = fileSetProperty.FindPropertyRelative(nameof(ExportFileSet.matcher));

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.BeginVertical();

            // At top, we show the file and their path
            EditorGUILayout.PropertyField(fileProperty, GUIContent.none);
            if (fileProperty.objectReferenceValue != null)
            {
                var file = AssetDatabase.GetAssetPath(fileProperty.objectReferenceValue);
                if (string.IsNullOrEmpty(file))
                    GUILayout.Label($"Not a file or directory: {fileProperty.objectReferenceValue.name}", Styles.ErrorLabel);
                else
                    GUILayout.Label($"Path: /{file}");
            }

            if (Exporter.IsDirectory(fileProperty.objectReferenceValue))
            {
                // If the selection is a directory, we show the selection type
                selectionProperty.enumValueIndex = EditorGUILayout.Popup("Include", selectionProperty.enumValueIndex,
                    new[] { "All Files", "Files matches prefix", "Files matches suffix" });
                var selection = (ExportFileSelection)selectionProperty.enumValueIndex;
                switch (selection)
                {
                    case ExportFileSelection.FileOrDirectory:
                        EditorGUILayout.LabelField($"Includes {FilesForFileSet(fileProperty.objectReferenceValue, selection, "", "").Length} files");
                        break;
                    case ExportFileSelection.FilesInDirectoryWithPrefix:
                        DrawMatcherSelectionField(
                            Styles.Prefix,
                            "No prefix is specified so no files will be included."
                        );
                        break;
                    case ExportFileSelection.FilesInDirectoryWithSuffix:
                        DrawMatcherSelectionField(
                            Styles.Suffix,
                            "No suffix is specified so no files will be included."
                        );
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                void DrawMatcherSelectionField(
                    GUIContent matcherLabel,
                    string messageOnEmptyMatcher
                )
                {
                    {
                        var position = GUILayoutUtility.GetRect(matcherLabel, EditorStyles.textField);
                        matcherLabel = EditorGUI.BeginProperty(position, matcherLabel, matcherProperty);
                        position = EditorGUI.PrefixLabel(position, matcherLabel);
                        EditorGUI.BeginChangeCheck();
                        var str1 = EditorGUI.TextField(position, matcherProperty.stringValue);
                        if (EditorGUI.EndChangeCheck()) matcherProperty.stringValue = str1;
                        EditorGUI.EndProperty();
                        if (str1 == "" && variantName != "")
                        {
                            // if matcher is empty, we use variant name as matcher so draw it
                            // style.Draw(position, EditorGUIUtility.TempContent(str2), id, false, position.Contains(UnityEngine.Event.current.mousePosition));
                            if (Event.current.type == EventType.Repaint)
                            {
                                var color = GUI.contentColor;
                                GUI.contentColor = Color.gray;
                                Styles.TextFieldTextOnly.Draw(position, new GUIContent(variantName), -1);
                                GUI.contentColor = color;
                            }
                        }
                    }
                    var files = FilesForFileSet(fileProperty.objectReferenceValue, selection, matcherProperty.stringValue, variantName);
                    if (variantName == "" && matcherProperty.stringValue == "")
                    {
                        EditorGUILayout.LabelField(messageOnEmptyMatcher, Styles.ErrorLabel);
                    }
                    else if (files.Length == 0)
                    {
                        EditorGUILayout.LabelField("No files match the prefix.");
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"Includes {files.Length}:");
                        foreach (var file in files) EditorGUILayout.LabelField($"- /{file}");
                    }
                }
            }

            EditorGUILayout.EndVertical();
            if (GUILayout.Button(Styles.RemoveButton, EditorStyles.miniButton, GUILayout.MaxWidth(20)))
            {
                return true;
            }
            EditorGUILayout.EndHorizontal();
            return false;
        }

        // caching
        private Dictionary<(Object file, ExportFileSelection selection, string natcher), string[]> _filesForFileSetCache = new();
        public string[] FilesForFileSet(Object? file, ExportFileSelection selection, string matcher, string variantName)
        {
            if (file == null) return Array.Empty<string>();
            
            if (string.IsNullOrEmpty(matcher)) matcher = variantName;

            var key = (file, selection, matcher);
            if (_filesForFileSetCache.TryGetValue(key, out var cachedFiles)) return cachedFiles;
            return _filesForFileSetCache[key] = Exporter.FilesForFileSet(file, selection, matcher);
        }

        // utilities
        private static Object[]? DropArea(string content, params GUILayoutOption[] options) => DropArea(new GUIContent(content), options);
        private static Object[]? DropArea(GUIContent content, params GUILayoutOption[] options) => DropArea(content, null, options);
        private static Object[]? DropArea(
            string content,
            GUIStyle? style,
            params GUILayoutOption[] options
        ) => DropArea(new GUIContent(content), style, options);
        private static Object[]? DropArea(
            GUIContent content,
            GUIStyle? style,
            params GUILayoutOption[] options
        )
        {
            style ??= Styles.DropAreaStyle;
            var position = GUILayoutUtility.GetRect(content, style, options);
            var controlId = GUIUtility.GetControlID("DropArea".GetHashCode(), FocusType.Passive);
            var evt = Event.current;
            switch (evt.type)
            {
                case EventType.Repaint:
                    style.Draw(position, content, controlId, false, position.Contains(Event.current.mousePosition));
                    break;
                case EventType.DragUpdated:
                case EventType.DragPerform:
                {
                    if (position.Contains(evt.mousePosition))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            return DragAndDrop.objectReferences;
                        }
                    }
                    break;
                }
            }

            return null;
        }
    }
}