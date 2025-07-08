#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Anatawa12.MultiUnityPackageExporter
{
    internal static class Exporter
    {
        public static void ExportPackages(ExportSettings target, string location)
        {
            var commonFiles = target.commonFiles
                .SelectMany(fileSet => FilesForFileSet(fileSet, ""))
                .SelectMany(FileAndParentDirectories)
                .Distinct()
                .ToArray();

            foreach (var variant in target.variants)
            {
                var packageName = PackageName(target.packageNamePattern, variant.name);
                var files = commonFiles
                    .Concat(variant.files.SelectMany(fileSet => FilesForFileSet(fileSet!, variant.name)))
                    .SelectMany(FileAndParentDirectories);

                AssetDatabase.ExportPackage(
                    commonFiles.Concat(files).Distinct().ToArray(),
                    Path.Join(location, packageName),
                    ExportPackageOptions.Default
                );
            }
        }

        private static IEnumerable<string> FileAndParentDirectories(string path)
        {
            if (string.IsNullOrEmpty(path)) yield break;

            // Normalize path to use '/' as directory separator
            path = path.Replace('\\', '/');

            // Yield the file itself
            yield return path;

            // Yield parent directories
            while (true)
            {
                var parent = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(parent)) break;
                if (!parent.Contains('/')) break; // Stop if we reach the root directory like "Assets"
                yield return parent;
                path = parent;
            }
        }

        public static string PackageName(string pattern, string variantName)
        {
            var baseName = pattern.Contains(ExportSettings.VariantPlaceholder)
                ? pattern.Replace(ExportSettings.VariantPlaceholder, variantName)
                : pattern + '_' + variantName; // fallback for poor export settings
            return baseName + ".unitypackage";
        }

        public static bool IsDirectory(Object? obj)
        {
            if (obj == null) return false;

            // Check if the object is a directory by checking its path
            string path = AssetDatabase.GetAssetPath(obj);
            return !string.IsNullOrEmpty(path) && Directory.Exists(path);
        }

        public static string[] FilesForFileSet(ExportFileSet fileSet, string variantName)
        {
            var matcher = fileSet.matcher;
            if (string.IsNullOrEmpty(matcher)) matcher = variantName;
            return FilesForFileSet(fileSet.file, fileSet.selection, matcher);
        }

        public static string[] FilesForFileSet(Object? file, ExportFileSelection selection, string matcher)
        {
            if (file == null) return Array.Empty<string>();

            string path = AssetDatabase.GetAssetPath(file);
            if (string.IsNullOrEmpty(path)) return Array.Empty<string>();

            if (!IsDirectory(file)) return new[] { path };

            if (selection != ExportFileSelection.FileOrDirectory && string.IsNullOrEmpty(matcher)) return Array.Empty<string>();

            IEnumerable<string> files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            // normalize to use '/' as directory separator
            files = files.Select(filePath => filePath.Replace('\\', '/'));
            // exclude files that do not appear in the unity editor
            files = files.Where(filePath => !filePath.Contains("/.") // dotfiles
                                            && !filePath.Contains("~/") &&
                                            !filePath.EndsWith("~") // files/directories that ends with '~'
            );

            files = selection switch
            {
                ExportFileSelection.FileOrDirectory => files,
                ExportFileSelection.FilesInDirectoryWithPrefix => files.Where(filePath =>
                    Path.GetFileNameWithoutExtension(filePath)
                        .StartsWith(matcher, StringComparison.OrdinalIgnoreCase)),
                ExportFileSelection.FilesInDirectoryWithSuffix => files.Where(filePath =>
                    Path.GetFileNameWithoutExtension(filePath)
                        .EndsWith(matcher, StringComparison.OrdinalIgnoreCase)),
                _ => Array.Empty<string>(),
            };

            return files.ToArray();
        }
    }
}