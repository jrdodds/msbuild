// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Build.Framework;

namespace Microsoft.Build.Tasks
{
    public sealed class Sort : TaskExtension
    {
        private IEnumerable<OrderByInstruction> _ordering = Array.Empty<OrderByInstruction>();

        /// <summary>
        /// The items to sort.
        /// </summary>
        [Required]
        public ITaskItem[] Items { get; set; } = Array.Empty<ITaskItem>();

        /// <summary>
        /// <para>Optional Order By instructions. Expected syntax:</para>
        /// <para>MetadataName[ [c][asc|desc]][;MetadataName[ [c][asc|desc]][...]</para>
        /// <para>Where 'c' is a case-sensitive compare; 'asc' is ascending order; and 'desc' is descending order.</para>
        /// <para>Default is to order by 'Identity', case-insensitive compare, ascending order.</para>
        /// </summary>
        public ITaskItem[] OrderBy { get; set; } = Array.Empty<ITaskItem>();

        /// <summary>
        /// The sorted items.
        /// </summary>
        [Output]
        public ITaskItem[] SortedItems { get; private set; } = Array.Empty<ITaskItem>();

        public override bool Execute()
        {
            if (Items == null || Items.Length <= 0)
            {
                return !Log.HasLoggedErrors;
            }

            _ordering = ParseOrderBy(OrderBy);

            var orderingKeys = _ordering.Select(item => item.Key).ToArray();

            // Check for duplicate sort keys.
            if (orderingKeys.Length > 1)
            {
                var hashset = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
                var duplicates = orderingKeys.Where(key => !hashset.Add(key)).ToArray();
                if (duplicates.Any())
                {
                    Log.LogError($"Cannot sort - repeated key {string.Join(",", duplicates)}.");
                    return !Log.HasLoggedErrors;
                }
            }

            // Check that the metadata exists.
            if (Items.Any(item => item.MetadataNames.Cast<string>().Intersect(orderingKeys, StringComparer.InvariantCultureIgnoreCase).Count() != orderingKeys.Length))
            {
                Log.LogError($"Cannot sort - missing metadata {orderingKeys.Length} '{orderingKeys[0]}'.");
                return !Log.HasLoggedErrors;
            }

            SortedItems = new ITaskItem[Items.Length];
            Items.CopyTo(SortedItems, 0);
            Array.Sort(SortedItems, OrderByComparison);

            return !Log.HasLoggedErrors;
        }

        private IEnumerable<OrderByInstruction> ParseOrderBy(ITaskItem[] orderByParam)
        {
            if (orderByParam == null || orderByParam.Length <= 0)
            {
                // Default to sorting by Identity, Ascending, case-insensitive.
                return new[] { new OrderByInstruction(Shared.FileUtilities.ItemSpecModifiers.Identity) };
            }

            var orderBy = new List<OrderByInstruction>();
            foreach (var item in orderByParam)
            {
                var tokens = item.ItemSpec.TrimStart().Split(null, 2);
                switch (tokens.Length)
                {
                    case <= 0:
                        continue;
                    case 1:
                        orderBy.Add(new OrderByInstruction(tokens[0]));
                        break;
                    case > 1:
                        {
                            bool hasOptionsError = false;
                            var options = tokens[1].ToLowerInvariant();
                            var isCaseInsensitive = options.FirstOrDefault() != 'c';
                            if (!isCaseInsensitive && options.Length >= 1)
                            {
                                options = options.Substring(1);
                            }

                            bool? isAscending = null;
                            if (string.IsNullOrEmpty(options) || options == "asc")
                            {
                                isAscending = true;
                            }
                            else if (options == "desc")
                            {
                                isAscending = false;
                            }
                            else
                            {
                                Log.LogError($"unknown option {options}");
                                hasOptionsError = true;
                            }

                            if (!hasOptionsError && isAscending.HasValue)
                            {
                                orderBy.Add(new OrderByInstruction(tokens[0], isAscending.Value, isCaseInsensitive));
                            }

                            break;
                        }
                }
            }

            return orderBy.ToArray();
        }

        private int OrderByComparison(ITaskItem x, ITaskItem y)
        {
            int comparisonResult = 0;
            foreach (var instruction in _ordering)
            {
                comparisonResult = instruction.Comparison(x, y);
                if (comparisonResult != 0)
                {
                    return comparisonResult;
                }
            }
            return comparisonResult;
        }

        private sealed class OrderByInstruction
        {
            private Comparison<ITaskItem>? _comparison;

            public OrderByInstruction(string key, bool isAscending = true, bool isCaseInsensitive = true)
            {
                Key = key ?? throw new ArgumentNullException(nameof(key));
                IsAscending = isAscending;
                IsCaseInsensitive = isCaseInsensitive;
            }

            public string Key { get; }

            public bool IsAscending { get; }

            public bool IsCaseInsensitive { get; }

            public Comparison<ITaskItem> Comparison => _comparison ??= BuildComparisonFunction(Key, IsAscending, IsCaseInsensitive);

            private static Comparison<ITaskItem> BuildComparisonFunction(string keyName, bool isAscending = true, bool isCaseInsensitive = true)
            {
                int orderModifer = isAscending ? 1 : -1;
                StringComparison comparisonType = isCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

                return (item1, item2) => string.Compare(item1.GetMetadata(keyName), item2.GetMetadata(keyName), comparisonType) * orderModifer;
            }
        }
    }
}
