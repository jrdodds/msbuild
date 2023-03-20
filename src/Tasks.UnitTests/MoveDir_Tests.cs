// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Tasks;

using Shouldly;

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Build.UnitTests
{
    public sealed class MoveDir_Tests
    {
        public MoveDir_Tests(ITestOutputHelper output) => this.output = output;

        [Fact]
        public void NoInput()
        {
            var task = new MoveDir
            {
                BuildEngine = new MockEngine(true),
            };
            task.Execute().ShouldBeTrue();
            task.DirectoriesMoved.ShouldNotBeNull();
            task.DirectoriesMoved.Length.ShouldBe(0);
        }

        private readonly ITestOutputHelper output;
    }
}
