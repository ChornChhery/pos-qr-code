using FluentAssertions;
using OrderService.Domain;
using OrderService.Services;

namespace OrderService.Tests;

public class OrderStatusRulesTests
{
    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Cooking)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Cooking, OrderStatus.Ready)]
    [InlineData(OrderStatus.Cooking, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Ready, OrderStatus.Completed)]
    public void CanTransition_AllowsTheNormalFlow(OrderStatus from, OrderStatus to)
    {
        OrderStatusRules.CanTransition(from, to).Should().BeTrue();
    }

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Ready)]
    [InlineData(OrderStatus.Pending, OrderStatus.Completed)]
    [InlineData(OrderStatus.Cooking, OrderStatus.Pending)]
    [InlineData(OrderStatus.Cooking, OrderStatus.Completed)]
    [InlineData(OrderStatus.Ready, OrderStatus.Cooking)]
    [InlineData(OrderStatus.Ready, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Pending, OrderStatus.Pending)]
    public void CanTransition_RejectsSkippingBackwardsAndSameStatus(OrderStatus from, OrderStatus to)
    {
        OrderStatusRules.CanTransition(from, to).Should().BeFalse();
    }

    [Theory]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    public void CanTransition_TerminalStatusesCannotChange(OrderStatus terminal)
    {
        foreach (var target in Enum.GetValues<OrderStatus>())
        {
            OrderStatusRules.CanTransition(terminal, target).Should().BeFalse();
        }
    }

    [Fact]
    public void IsTerminal_OnlyForCompletedAndCancelled()
    {
        OrderStatusRules.IsTerminal(OrderStatus.Completed).Should().BeTrue();
        OrderStatusRules.IsTerminal(OrderStatus.Cancelled).Should().BeTrue();
        OrderStatusRules.IsTerminal(OrderStatus.Ready).Should().BeFalse();
    }

    [Fact]
    public void ActiveStatuses_AreTheOnesThatKeepATableBusy()
    {
        OrderStatusRules.ActiveStatuses.Should().BeEquivalentTo(
            new[] { OrderStatus.Pending, OrderStatus.Cooking, OrderStatus.Ready });
    }
}
