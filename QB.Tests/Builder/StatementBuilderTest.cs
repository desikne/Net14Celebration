using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QB.Builder.Builder;
using QB.Core.Contracts;
using QB.Core.Enums;

namespace QB.Tests.Builder
{
    [TestClass]
    public class StatementBuilderTest
    {
        private readonly StatementBuilder builder;

        private readonly ColumnFilter nameFilter = new ColumnFilter
        {
            KeyName = "Name",
            Operator = StatementOperation.Like,
            Filter = "@0",
            Params = { new SqlParameter($"@0", "MyName") }
        };

        private readonly ColumnFilter ageFilter = new ColumnFilter
        {
            KeyName = "Age",
            Operator = StatementOperation.MoreAndEquals,
            Filter = "@0",
            Params = { new SqlParameter($"@0", 18) }
        };

        private readonly ColumnFilter emptyFilter = new ColumnFilter
        {
        };

        public StatementBuilderTest()
        {
            this.builder = new StatementBuilder();
        }

        [TestMethod]
        public void BuildAndStatement_ValidFilterContracts_ReturnColumnFilterCombinedByAND()
        {
            var filters = new List<ColumnFilter> { this.nameFilter, this.ageFilter };
            var result = this.builder.BuildAndStatement(filters);

            var expectedFilter = "(Name LIKE '%' + @0 + '%' AND Age >= @1)";

            Assert.IsNotNull(result);
            Assert.AreEqual(result.Params.Count, 2);
            Assert.AreEqual(result.Filter, expectedFilter);
        }

        [TestMethod]
        public void BuildAndStatement_EmptyFilterContracts_TrowInvalidOperationException()
        {
            var filters = new List<ColumnFilter> { };

            Assert.ThrowsException<InvalidOperationException>(() => this.builder.BuildAndStatement(filters));
        }

        [TestMethod]
        public void BuildAndStatement_OneFilterContracts_ReturnsSameColumnFilter()
        {
            var filters = new List<ColumnFilter> { this.nameFilter };
            var result = this.builder.BuildAndStatement(filters);

            var expectedFilter = "(Name LIKE '%' + @0 + '%')";

            Assert.IsNotNull(result);
        }
    }
}