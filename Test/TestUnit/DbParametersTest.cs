using System;
using System.Data;
using Frame.Databases;
using Xunit;

namespace TestUnit
{
    public class DbParametersTest
    {
        // ===========================================================================
        // 构造
        // ===========================================================================

        [Fact]
        public void Ctor_Empty_CountIsZero()
        {
            var p = new DbParameters();
            Assert.Equal(0, p.Count);
        }

        [Fact]
        public void Ctor_AnonymousObject_ReflectsProperties()
        {
            var p = new DbParameters(new { Name = "York", Age = 18 });

            Assert.Equal(2, p.Count);
            Assert.True(p.Contains("Name"));
            Assert.True(p.Contains("Age"));
        }

        [Fact]
        public void Ctor_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new DbParameters(null!));
        }

        [Fact]
        public void From_StaticFactory_SameAsConstructor()
        {
            var p = DbParameters.From(new { Key = "value" });
            Assert.Equal(1, p.Count);
            Assert.True(p.Contains("Key"));
        }

        // ===========================================================================
        // Add
        // ===========================================================================

        [Fact]
        public void Add_Single_CountIncrements()
        {
            var p = new DbParameters().Add("Name", "York");
            Assert.Equal(1, p.Count);
            Assert.True(p.Contains("Name"));
        }

        [Fact]
        public void Add_Chained_MultipleParams()
        {
            var p = new DbParameters()
                .Add("Name", "York")
                .Add("Age", 18)
                .Add("Active", true);

            Assert.Equal(3, p.Count);
        }

        [Fact]
        public void Add_EmptyName_Throws()
        {
            var p = new DbParameters();
            Assert.Throws<ArgumentException>(() => p.Add("", "value"));
            Assert.Throws<ArgumentException>(() => p.Add("  ", "value"));
        }

        [Fact]
        public void Add_WithDbType_StoresCorrectly()
        {
            var p = new DbParameters().Add("Id", 1, DbType.Int32);
            Assert.Equal(1, p.Count);
        }

        [Fact]
        public void Add_NullValue_Works()
        {
            var p = new DbParameters().Add("Name", null);
            Assert.Equal(1, p.Count);
        }

        // ===========================================================================
        // AddIf
        // ===========================================================================

        [Fact]
        public void AddIf_True_Adds()
        {
            var p = new DbParameters().AddIf(true, "Name", "York");
            Assert.Equal(1, p.Count);
            Assert.True(p.Contains("Name"));
        }

        [Fact]
        public void AddIf_False_Skips()
        {
            var p = new DbParameters().AddIf(false, "Name", "York");
            Assert.Equal(0, p.Count);
            Assert.False(p.Contains("Name"));
        }

        [Fact]
        public void AddIf_Chained_MixedConditions()
        {
            var hasName = true;
            var hasAge = false;

            var p = new DbParameters()
                .AddIf(hasName, "Name", "York")
                .AddIf(hasAge, "Age", 18);

            Assert.Equal(1, p.Count);
            Assert.True(p.Contains("Name"));
            Assert.False(p.Contains("Age"));
        }

        // ===========================================================================
        // AddOutput / AddReturn
        // ===========================================================================

        [Fact]
        public void AddOutput_CreatesOutputParameter()
        {
            var p = new DbParameters().AddOutput("Total", DbType.Int32);
            Assert.Equal(1, p.Count);
        }

        [Fact]
        public void AddOutput_WithSize_Works()
        {
            var p = new DbParameters().AddOutput("Name", DbType.String, 100);
            Assert.Equal(1, p.Count);
        }

        [Fact]
        public void AddReturn_Default_Works()
        {
            var p = new DbParameters().AddReturn();
            Assert.Equal(1, p.Count);
            Assert.True(p.Contains("returnValue"));
        }

        [Fact]
        public void AddReturn_CustomName_Works()
        {
            var p = new DbParameters().AddReturn("retVal", DbType.Int64);
            Assert.True(p.Contains("retVal"));
        }

        // ===========================================================================
        // Get / TryGet（未执行时抛异常）
        // ===========================================================================

        [Fact]
        public void Get_BeforeExecution_Throws()
        {
            var p = new DbParameters().Add("X", 1);
            Assert.Throws<InvalidOperationException>(() => p.Get<int>("X"));
        }

        [Fact]
        public void TryGet_BeforeExecution_ReturnsFalse()
        {
            var p = new DbParameters().Add("X", 1);
            var ok = p.TryGet<int>("X", out var val);
            Assert.False(ok);
            Assert.Equal(default, val);
        }

        // ===========================================================================
        // 集合操作
        // ===========================================================================

        [Fact]
        public void Remove_ExistingParam_Decrements()
        {
            var p = new DbParameters().Add("A", 1).Add("B", 2);
            p.Remove("A");
            Assert.Equal(1, p.Count);
            Assert.False(p.Contains("A"));
            Assert.True(p.Contains("B"));
        }

        [Fact]
        public void Remove_Nonexistent_NoOp()
        {
            var p = new DbParameters().Add("A", 1);
            p.Remove("B");
            Assert.Equal(1, p.Count);
        }

        [Fact]
        public void Remove_CaseInsensitive_Works()
        {
            var p = new DbParameters().Add("Name", "York");
            p.Remove("name");
            Assert.Equal(0, p.Count);
        }

        [Fact]
        public void Clear_AllRemoved()
        {
            var p = new DbParameters().Add("A", 1).Add("B", 2);
            p.Clear();
            Assert.Equal(0, p.Count);
        }

        [Fact]
        public void Merge_CombinesBoth()
        {
            var a = new DbParameters().Add("X", 1);
            var b = new DbParameters().Add("Y", 2);

            a.Merge(b);
            Assert.Equal(2, a.Count);
            Assert.True(a.Contains("X"));
            Assert.True(a.Contains("Y"));
        }

        [Fact]
        public void Merge_Null_Throws()
        {
            var p = new DbParameters();
            Assert.Throws<ArgumentNullException>(() => p.Merge(null!));
        }

        [Fact]
        public void Contains_Existing_True()
        {
            var p = new DbParameters().Add("Status", 1);
            Assert.True(p.Contains("Status"));
            Assert.True(p.Contains("status")); // case insensitive
            Assert.False(p.Contains("Missing"));
        }

        // ===========================================================================
        // 组合场景
        // ===========================================================================

        [Fact]
        public void Chained_AndMixed_BuildsExpectedParams()
        {
            var p = new DbParameters(new { BaseId = 100 })
                .Add("Keyword", "test")
                .AddIf(false, "Skipped", 999)
                .AddIf(true, "Active", true)
                .AddOutput("Total", DbType.Int32)
                .AddReturn();

            Assert.Equal(5, p.Count); // BaseId + Keyword + Active + Total + returnValue
            Assert.True(p.Contains("BaseId"));
            Assert.True(p.Contains("Keyword"));
            Assert.False(p.Contains("Skipped"));
            Assert.True(p.Contains("Active"));
            Assert.True(p.Contains("Total"));
            Assert.True(p.Contains("returnValue"));
        }

        [Fact]
        public void Remove_InvalidatesDapperRef()
        {
            var p = new DbParameters().Add("X", 1);
            p.Remove("X");
            // 移除后调用 Get 应抛异常，因为未执行过
            Assert.Throws<InvalidOperationException>(() => p.Get<int>("X"));
        }
    }
}
