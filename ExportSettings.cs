#nullable enable

using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.MultiUnityPackageExporter
{
    [CreateAssetMenu(menuName = "Multi Unity Package Export Settings", fileName = "ExportSettings", order = 100)]
    internal class ExportSettings : ScriptableObject
    {
        public const string VariantPlaceholder = "{variant}";
        /// <summary>
        /// The name of the unitypackage
        /// </summary>
        public string packageNamePattern = VariantPlaceholder;

        /// <summary>
        /// The files to be included in all variants of the unitypackage.
        /// </summary>
        public ExportFileSet[] commonFiles = Array.Empty<ExportFileSet>();

        /// <summary>
        /// The variants of the unitypackage.
        /// </summary>
        public ExportVariant[] variants = Array.Empty<ExportVariant>();
    }

    /// <summary>
    /// Represents one variant of unitypackage
    /// </summary>
    [Serializable]
    internal class ExportVariant
    {
        /// <summary>
        /// The name of the variant.
        /// </summary>
        public string name = "";

        /// <summary>
        /// The files to be included in this variant.
        /// </summary>
        public ExportFileSet[] files = Array.Empty<ExportFileSet>();
    }

    /// <summary>
    /// Represents a set of files to be included in the unitypackage.
    /// </summary>
    [Serializable]
    internal class ExportFileSet
    {
        public ExportFileSelection selection;
        public Object? file;
        public string matcher = "";
    }

    internal enum ExportFileSelection
    {
        FileOrDirectory,
        FilesInDirectoryWithPrefix,
        FilesInDirectoryWithSuffix,
    }
}
