// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common.TypeHintData
{
    /// <summary>
    /// Additional typehint data for file options
    /// </summary>
    public class FilePathTypeHintData
    {
        /// <summary>
        /// Corresponds to a Filter property for a System.Windows.Forms.FileDialog
        /// to determine the choices that would appear in the dialog box if a wrapping tool prompted the user for a file path via a UI.
        public string Filter { get; set; } = "All files (*.*)|*.*";

        /// <summary>
        /// Corresponds to the DefaultExt property for a System.Windows.Forms.FileDialog
        /// to specify the default extension used if the user specifies a file name
        /// without an extension.
        /// </summary>
        public string DefaultExtension { get; set; } = "";

        /// <summary>
        /// Corresponds to the Title property for a System.Windows.Forms.FileDialog
        /// to specify the title of the file dialog box.
        /// </summary>
        public string Title { get; set; } = "Open";

        /// <summary>
        /// Corresponds to the CheckFileExists property for a System.Windows.Forms.FileDialog
        /// to indicate whether the dialog box should display a warning if the user specifies a file that does not exist.
        /// </summary>
        public bool CheckFileExists { get; set; } = true;

        /// <summary>
        /// Corresponds to the the AllowEmpty parameter for ConsoleUtilities.AskUserForValue
        /// This lets a recipe option that uses the FilePathCommand typehint
        /// control whether an empty value is allowed during CLI mode
        /// </summary>
        public bool AllowEmpty { get; set; } = true;
    }
}
