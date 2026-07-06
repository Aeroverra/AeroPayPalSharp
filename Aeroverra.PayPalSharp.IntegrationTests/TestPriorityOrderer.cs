using Xunit.Abstractions;
using Xunit.Sdk;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>Orders tests in a class by an explicit <see cref="TestPriorityAttribute"/> (lowest first).</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TestPriorityAttribute : Attribute
{
    public int Priority { get; }
    public TestPriorityAttribute(int priority) => Priority = priority;
}

public sealed class TestPriorityOrderer : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : ITestCase
    {
        return testCases.OrderBy(testCase =>
        {
            var attribute = testCase.TestMethod.Method
                .GetCustomAttributes(typeof(TestPriorityAttribute).AssemblyQualifiedName!)
                .FirstOrDefault();
            return attribute?.GetNamedArgument<int>(nameof(TestPriorityAttribute.Priority)) ?? 0;
        });
    }
}
