// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.Build.Tasks
{
    public sealed class Join : TaskExtension
    {
        private static readonly char[] ListDelimiter = { ';' };
        private static readonly string ListDelimiterAsString = new(ListDelimiter);
        private string[] _excludeMetadata = Array.Empty<string>();
        private bool _hasMetadataExclusions;

        [Required]
        public ITaskItem[] Left { get; set; } = Array.Empty<ITaskItem>();

        [Required]
        public ITaskItem[] Right { get; set; } = Array.Empty<ITaskItem>();

        public string LeftKey { get; set; } = Shared.FileUtilities.ItemSpecModifiers.Identity;

        public string RightKey { get; set; } = Shared.FileUtilities.ItemSpecModifiers.Identity;

        public string[] ExcludeMetadata
        {
            get => _excludeMetadata;
            set
            {
                _excludeMetadata = value;
                _hasMetadataExclusions = _excludeMetadata.Length > 0;
            }
        }

        public bool GroupJoin { get; set; }

        [Output]
        public ITaskItem[] Joined { get; private set; } = Array.Empty<ITaskItem>();

        public override bool Execute()
        {
            if (ValidateMetadataExists("Left", Left, LeftKey) && ValidateMetadataExists("Right", Right, RightKey))
            {
                Joined = GroupJoin ?
                    Left.GroupJoin(Right, outerItem => outerItem.GetMetadata(LeftKey), innerItem => innerItem.GetMetadata(RightKey), MakeResult).ToArray() :
                    Left.Join(Right, outerItem => outerItem.GetMetadata(LeftKey), innerItem => innerItem.GetMetadata(RightKey), MakeResult).ToArray();
            }

            return !Log.HasLoggedErrors;
        }

        private bool ValidateMetadataExists(string itemName, IEnumerable<ITaskItem> items, string metadataName)
        {
            bool result = items.All(item => item.MetadataNames.Cast<string>().Contains(metadataName));
            if (!result)
            {
                // TODO: Change from LogError to LogError*FromResources.
                Log.LogError($"Missing metadata. The {metadataName} metadata must be set on all items in {itemName}.");
            }

            return result;
        }

        private ITaskItem MakeResult(ITaskItem outerItem, ITaskItem innerItem)
        {
            var resultItem = new TaskItem(outerItem);

            // EnumerateMetadata returns only the 'custom' metadata.
            foreach (var custom in innerItem.EnumerateMetadata())
            {
                if (IsExcludeMetadata(custom.Key))
                {
                    continue;
                }

                resultItem.SetMetadata(custom.Key, custom.Value);
            }

            return resultItem;
        }

        private ITaskItem MakeResult(ITaskItem outerItem, IEnumerable<ITaskItem> innerItems)
        {
            bool isInnerEmpty = true;
            var metadataToAdd = new Dictionary<string, List<string>>();
            foreach (var innerItem in innerItems)
            {
                isInnerEmpty = false;
                // EnumerateMetadata returns only the 'custom' metadata.
                foreach (var custom in innerItem.EnumerateMetadata())
                {
                    if (IsExcludeMetadata(custom.Key))
                    {
                        continue;
                    }

                    if (metadataToAdd.TryGetValue(custom.Key, out List<string>? value))
                    {
                        value.Add(custom.Value);
                    }
                    else
                    {
                        metadataToAdd.Add(custom.Key, new List<string> { custom.Value });
                    }
                }
            }

            var resultItem = new TaskItem(outerItem);
            if (metadataToAdd.Count > 0)
            {
                foreach (var name in metadataToAdd.Keys)
                {
                    resultItem.SetMetadata(name, string.Join(ListDelimiterAsString, metadataToAdd[name]));
                }
            }

            // TODO: There seems to be value to an explicit metadata indicating if inner is the empty set, but may need discussion.
            resultItem.SetMetadata(GroupJoinInnerIsEmptyName, isInnerEmpty.ToString());

            return resultItem;
        }

        private bool IsExcludeMetadata(string name)
        {
            return _hasMetadataExclusions && ExcludeMetadata.Contains(name);
        }

        // TODO: Use a different name for this metadata?
        private const string GroupJoinInnerIsEmptyName = "GroupJoinInnerIsEmpty";
    }
}
