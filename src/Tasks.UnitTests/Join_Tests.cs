// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;
using Microsoft.Build.Utilities;

using Shouldly;

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Build.UnitTests
{
    public sealed class Join_Tests
    {
        public Join_Tests(ITestOutputHelper output) => this.output = output;

        [Fact]
        public void NoLeftRight()
        {
            var task = new Join { BuildEngine = new MockEngine(true) };
            task.Execute().ShouldBeTrue();
            task.Joined.ShouldNotBeNull();
            task.Joined.Length.ShouldBe(0);
        }

        [Fact]
        public void NoLeftRightGroupJoin()
        {
            var task = new Join { BuildEngine = new MockEngine(true), GroupJoin = true };
            task.Execute().ShouldBeTrue();
            task.Joined.ShouldNotBeNull();
            task.Joined.Length.ShouldBe(0);
        }

        [Fact]
        public void NoLeftRightWithLeftKey()
        {
            var task = new Join { BuildEngine = new MockEngine(true), LeftKey = "noneSuch" };
            task.Execute().ShouldBeTrue();
            task.Joined.ShouldNotBeNull();
            task.Joined.Length.ShouldBe(0);
        }

        [Fact]
        public void NoLeftRightWithRightKey()
        {
            var task = new Join { BuildEngine = new MockEngine(true), RightKey = "noneSuch" };
            task.Execute().ShouldBeTrue();
            task.Joined.ShouldNotBeNull();
            task.Joined.Length.ShouldBe(0);
        }

        [Fact]
        public void JoinCustomerToOrder()
        {
            var task = new Join
            {
                BuildEngine = new MockEngine(true),
                Left = CustomerItems(),
                Right = OrderItems(),
                RightKey = "CustomerId",
                ExcludeMetadata = new[] { "CustomerId" },
            };
            task.Execute().ShouldBeTrue();
            task.Joined.ShouldNotBeNull();
            task.Joined.Length.ShouldBe(4);

            // Map to a collection of string and then test that the set difference between the actual and expected is empty.
            var actual = task.Joined.Select(item => $"{item.ItemSpec}|{item.GetMetadata("OrderName")}");
            string[] expected = { "C2|Order1", "C2|Order5", "C3|Order3", "C3|Order4", };
            actual.Except(expected).Any().ShouldBeFalse();

            // Check for excluded metadata.
            task.Joined.All(item => !item.MetadataNames.Cast<string>().Contains("CustomerId")).ShouldBeTrue("'CustomerId' metadata should be excluded.");

            // Check for combined metadata.
            task.Joined.All(item => item.MetadataNames.Cast<string>().Contains("CustomerPhone")).ShouldBeTrue("Missing 'CustomerPhone' metadata.");
            task.Joined.All(item => item.MetadataNames.Cast<string>().Contains("OrderDate")).ShouldBeTrue("Missing 'OrderDate' metadata.");
        }

        [Fact]
        public void GroupJoinCustomerToOrder()
        {
            var task = new Join
            {
                BuildEngine = new MockEngine(true),
                Left = CustomerItems(),
                Right = OrderItems(),
                RightKey = "CustomerId",
                ExcludeMetadata = new[] { "CustomerId" },
                GroupJoin = true,
            };
            task.Execute().ShouldBeTrue();
            task.Joined.ShouldNotBeNull();
            task.Joined.Length.ShouldBe(3);

            // Map to a collection of string and then test that the set difference between the actual and expected is empty.
            var actual = task.Joined.Select(item => $"{item.ItemSpec}|{item.GetMetadata("OrderName")}");
            string[] expected = { "C1|", "C2|Order1;Order5", "C3|Order3;Order4", };
            actual.Except(expected).Any().ShouldBeFalse();

            // Check for excluded metadata.
            task.Joined.All(item => !item.MetadataNames.Cast<string>().Contains("CustomerId")).ShouldBeTrue("'CustomerId' metadata should be excluded.");

            // Check for combined metadata.
            task.Joined.All(item => item.MetadataNames.Cast<string>().Contains("CustomerPhone")).ShouldBeTrue("Missing 'CustomerPhone' metadata.");
            // This is a GroupJoin. There will be metadata from the 'Right' or 'Inner' item only when there is a match.
            task.Joined.All(item => string.IsNullOrEmpty(item.GetMetadata("OrderName")) || (!string.IsNullOrEmpty(item.GetMetadata("OrderName")) && item.MetadataNames.Cast<string>().Contains("OrderDate"))).ShouldBeTrue("Missing 'OrderDate' metadata.");
        }

        private static ITaskItem[] CustomerItems()
        {
            /*
              <ItemGroup>
                <Customer Include="C1" CustomerName="Customer1" CustomerPhone="555-555-5550" />
                <Customer Include="C2" CustomerName="Customer2" CustomerPhone="555-555-5551" />
                <Customer Include="C3" CustomerName="Customer3" CustomerPhone="555-555-5552" />
              </ItemGroup>
            */
            return new ITaskItem[]
             {
                new TaskItem("C1", new Dictionary<string, string> { { "CustomerName", "Customer1" }, { "CustomerPhone", "555-555-5550" } } ),
                new TaskItem("C2", new Dictionary<string, string> { { "CustomerName", "Customer2" }, { "CustomerPhone", "555-555-5551" } } ),
                new TaskItem("C3", new Dictionary<string, string> { { "CustomerName", "Customer3" }, { "CustomerPhone", "555-555-5552" } } ),
            };
        }

        private static ITaskItem[] OrderItems()
        {
            /*
              <ItemGroup>
                <Order Include="O1" OrderName="Order1" CustomerId="C2" OrderDate="Yesterday" />
                <Order Include="O2" OrderName="Order2" CustomerId="C4" OrderDate="Today" />
                <Order Include="O3" OrderName="Order3" CustomerId="C3" OrderDate="Tomorrow" />
                <Order Include="O4" OrderName="Order4" CustomerId="C3" OrderDate="Future" />
                <Order Include="O5" OrderName="Order5" CustomerId="C2" OrderDate="Past" />
              </ItemGroup>
            */
            return new ITaskItem[]
            {
                new TaskItem("O1", new Dictionary<string, string> { { "OrderName", "Order1" }, { "CustomerId", "C2"}, { "OrderDate", "Yesterday"} } ),
                new TaskItem("O2", new Dictionary<string, string> { { "OrderName", "Order2" }, { "CustomerId", "C4"}, { "OrderDate", "Today"} } ),
                new TaskItem("O3", new Dictionary<string, string> { { "OrderName", "Order3" }, { "CustomerId", "C3"}, { "OrderDate", "Tomorrow"} } ),
                new TaskItem("O4", new Dictionary<string, string> { { "OrderName", "Order4" }, { "CustomerId", "C3"}, { "OrderDate", "Future"} } ),
                new TaskItem("O5", new Dictionary<string, string> { { "OrderName", "Order5" }, { "CustomerId", "C2"}, { "OrderDate", "Past" } } ),
            };
        }

        private static ITaskItem[] ProductItems()
        {
            /*
              <ItemGroup>
                <Product Include="P1" ProductName="Product1" />
                <Product Include="P2" ProductName="Product2" />
                <Product Include="P3" ProductName="Product3" />
              </ItemGroup>
            */
            return new ITaskItem[]
            {
                new TaskItem("P1", new Dictionary<string, string> { { "ProductName", "Product1" } } ),
                new TaskItem("P2", new Dictionary<string, string> { { "ProductName", "Product2" } } ),
                new TaskItem("P3", new Dictionary<string, string> { { "ProductName", "Product3" } } ),
            };
        }

        private static ITaskItem[] OrderProductItems()
        {
            /*
              <ItemGroup>
                <OrderProduct Include="OP1" OrderId="O1" ProductId="P2" />
                <OrderProduct Include="OP2" OrderId="O1" ProductId="P3" />
                <OrderProduct Include="OP3" OrderId="O1" ProductId="P1" />
                <OrderProduct Include="OP4" OrderId="O2" ProductId="P1" />
                <OrderProduct Include="OP5" OrderId="O3" ProductId="P2" />
                <OrderProduct Include="OP6" OrderId="O4" ProductId="P3" />
                <OrderProduct Include="OP7" OrderId="O5" ProductId="P3" />
              </ItemGroup>
            */
            return new ITaskItem[]
            {
                new TaskItem("OP1", new Dictionary<string, string> { { "OrderId", "O1" }, { "ProductId", "P2"} } ),
                new TaskItem("OP2", new Dictionary<string, string> { { "OrderId", "O1" }, { "ProductId", "P3"} } ),
                new TaskItem("OP3", new Dictionary<string, string> { { "OrderId", "O1" }, { "ProductId", "P1"} } ),
                new TaskItem("OP4", new Dictionary<string, string> { { "OrderId", "O2" }, { "ProductId", "P1"} } ),
                new TaskItem("OP5", new Dictionary<string, string> { { "OrderId", "O3" }, { "ProductId", "P2"} } ),
                new TaskItem("OP6", new Dictionary<string, string> { { "OrderId", "O4" }, { "ProductId", "P3"} } ),
                new TaskItem("OP7", new Dictionary<string, string> { { "OrderId", "O5" }, { "ProductId", "P3"} } ),
            };
        }

        private readonly ITestOutputHelper output;
    }
}
