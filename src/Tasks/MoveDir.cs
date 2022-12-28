// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.Build.Framework;
using Microsoft.Build.Shared.FileSystem;

namespace Microsoft.Build.Tasks
{
    internal class MoveDir : TaskExtension
    {
        [Required]
        public ITaskItem[] SourceDirectories { get; set; } = Array.Empty<ITaskItem>();

        [Required]
        public ITaskItem[] DestinationDirectories { get; set; } = Array.Empty<ITaskItem>();

        [Output]
        public ITaskItem[] DirectoriesMoved { get; private set; } = Array.Empty<ITaskItem>();

        public override bool Execute()
        {
            if (SourceDirectories == null || SourceDirectories.Length == 0)
            {
                return !Log.HasLoggedErrors;
            }

            if (DestinationDirectories == null || DestinationDirectories.Length == 0)
            {
                Log.LogError($"DestinationDirectories must be provided");
                return !Log.HasLoggedErrors;
            }

            if (SourceDirectories.Length != DestinationDirectories.Length)
            {
                Log.LogErrorWithCodeFromResources("General.TwoVectorsMustHaveSameLength", SourceDirectories.Length, DestinationDirectories.Length, "SourceDirectories", "DestinationDirectories");
                return !Log.HasLoggedErrors;
            }

            var moved = new List<ITaskItem>(SourceDirectories.Length);
            for (var idx = 0; idx < SourceDirectories.Length; idx++)
            {
                if (FileSystems.Default.DirectoryExists(SourceDirectories[idx].ItemSpec))
                {
                    if (FileSystems.Default.DirectoryExists(DestinationDirectories[idx].ItemSpec))
                    {
                        Directory.Delete(DestinationDirectories[idx].ItemSpec, true);
                    }
                    Directory.Move(SourceDirectories[idx].ItemSpec, DestinationDirectories[idx].ItemSpec);
                    moved.Add(DestinationDirectories[idx]);
                }
                else
                {
                    Log.LogMessage($"Skipping Nonexistent Directory '{SourceDirectories[idx].ItemSpec}'");
                }
            }

            if (moved.Count > 0)
            {
                DirectoriesMoved = moved.ToArray();
            }

            return !Log.HasLoggedErrors;
        }
    }
}
