// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;
using Microsoft.Build.Utilities;

using Shouldly;

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Build.UnitTests
{
    /// <summary>
    /// Tests for sort task.
    /// </summary>
    public sealed class Sort_Tests
    {
        public Sort_Tests(ITestOutputHelper output) => this.output = output;

        [Fact]
        public void NoItems()
        {
            // From an MSBuild file this situation won't happen because `Items` is `Required`.
            var task = new Sort
            {
                BuildEngine = new MockEngine(true),
            };
            task.Execute().ShouldBeTrue();
            task.SortedItems.ShouldNotBeNull();
            task.SortedItems.Length.ShouldBe(0);
        }

        [Fact]
        public void EmptyItem()
        {
            var task = new Sort
            {
                BuildEngine = new MockEngine(true),
                Items = new ITaskItem[] { new TaskItem(string.Empty) },
            };
            task.Execute().ShouldBeTrue();
            task.SortedItems.ShouldNotBeNull();
            task.SortedItems.Length.ShouldBe(1);
        }

        [Fact]
        public void DefaultSort()
        {
            // Sort by Identity (case-insensitive, ascending).
            int[] starting = { 3, 8, 1, 5, 2, 2, 7, 6, 4 };
            int[] expected = { 1, 2, 2, 3, 4, 5, 6, 7, 8 };

            var task = new Sort
            {
                BuildEngine = new MockEngine(true),
                Items = starting.Select(value => new TaskItem(value.ToString())).Cast<ITaskItem>().ToArray(),
            };
            task.Execute().ShouldBeTrue();
            task.SortedItems.ShouldNotBeNull();
            task.SortedItems.Select(item => int.Parse(item.ItemSpec)).ToArray().SequenceEqual(expected).ShouldBeTrue();
        }

        [Fact]
        public void OrderByAltKeyThenIdentity()
        {
            // Sort by altKey (case-insensitive, ascending) then by Identity (case-insensitive, ascending).
            ITaskItem[] starting =
            {
                new TaskItem("3", new Dictionary<string, string> { { "altKey", "1" }, { "expected", "3" } }),
                new TaskItem("1", new Dictionary<string, string> { { "altKey", "1" }, { "expected", "1" } }),
                new TaskItem("2", new Dictionary<string, string> { { "altKey", "3" }, { "expected", "4" } }),
                new TaskItem("2", new Dictionary<string, string> { { "altKey", "1" }, { "expected", "2" } }),
            };

            var task = new Sort
            {
                BuildEngine = new MockEngine(true),
                Items = starting,
                OrderBy = new ITaskItem[] { new TaskItem("altKey"), new TaskItem("Identity"), }
            };
            task.Execute().ShouldBeTrue();
            task.SortedItems.ShouldNotBeNull();
            task.SortedItems.Length.ShouldBe(starting.Length);
            task.SortedItems.Select(item => int.Parse(item.GetMetadata("expected"))).SequenceEqual(new[] { 1, 2, 3, 4 }).ShouldBeTrue();
        }

        [Fact]
        public void OrderByAltKeyDescThenIdentityDesc()
        {
            // Sort by altKey (case-insensitive, ascending) then by Identity (case-insensitive, ascending).
            ITaskItem[] starting =
            {
                new TaskItem("3", new Dictionary<string, string> { { "altKey", "1" }, { "expected", "2" } }),
                new TaskItem("1", new Dictionary<string, string> { { "altKey", "1" }, { "expected", "4" } }),
                new TaskItem("2", new Dictionary<string, string> { { "altKey", "3" }, { "expected", "1" } }),
                new TaskItem("2", new Dictionary<string, string> { { "altKey", "1" }, { "expected", "3" } }),
            };

            var task = new Sort
            {
                BuildEngine = new MockEngine(true),
                Items = starting,
                OrderBy = new ITaskItem[] { new TaskItem("altKey desc"), new TaskItem("Identity desc"), }
            };
            task.Execute().ShouldBeTrue();
            task.SortedItems.ShouldNotBeNull();
            task.SortedItems.Length.ShouldBe(starting.Length);
            task.SortedItems.Select(item => int.Parse(item.GetMetadata("expected"))).SequenceEqual(new[] { 1, 2, 3, 4 }).ShouldBeTrue();
        }

        [Fact]
        public void OrderByIdentityCaseSensitive()
        {
            // Sort by Identity (case-sensitive, ascending).
            ITaskItem[] starting =
            {
                new TaskItem("aaa", new Dictionary<string, string> { { "expected", "3" } }),
                new TaskItem("BBB", new Dictionary<string, string> { { "expected", "2" } }),
                new TaskItem("AAA", new Dictionary<string, string> { { "expected", "1" } }),
                new TaskItem("bbb", new Dictionary<string, string> { { "expected", "4" } }),
            };

            var task = new Sort
            {
                BuildEngine = new MockEngine(true),
                Items = starting,
                OrderBy = new ITaskItem[] { new TaskItem("Identity c"), }
            };
            task.Execute().ShouldBeTrue();
            task.SortedItems.ShouldNotBeNull();
            task.SortedItems.Length.ShouldBe(starting.Length);
            task.SortedItems.Select(item => int.Parse(item.GetMetadata("expected"))).SequenceEqual(new[] { 1, 2, 3, 4 }).ShouldBeTrue();
        }

        [Fact]
        public void OrderByIdentityCaseSensitiveDesc()
        {
            // Sort by Identity (case-sensitive, descending).
            ITaskItem[] starting =
            {
                new TaskItem("aaa", new Dictionary<string, string> { { "expected", "2" } }),
                new TaskItem("BBB", new Dictionary<string, string> { { "expected", "3" } }),
                new TaskItem("AAA", new Dictionary<string, string> { { "expected", "4" } }),
                new TaskItem("bbb", new Dictionary<string, string> { { "expected", "1" } }),
            };

            var task = new Sort
            {
                BuildEngine = new MockEngine(true),
                Items = starting,
                OrderBy = new ITaskItem[] { new TaskItem("Identity cdesc"), }
            };
            task.Execute().ShouldBeTrue();
            task.SortedItems.ShouldNotBeNull();
            task.SortedItems.Length.ShouldBe(starting.Length);
            task.SortedItems.Select(item => int.Parse(item.GetMetadata("expected"))).SequenceEqual(new[] { 1, 2, 3, 4 }).ShouldBeTrue();
        }

        private readonly ITestOutputHelper output;
    }
}
