using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Domain.Commands;
using Core.Domain.Entities;
using EventsProcessor.StateMachines;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests;

public class OrderSagaIntegrationTests
{
    [Fact]
    public async Task SubmitOrder_creates_saga_instance_in_Submitted_state()
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddSagaStateMachine<OrderStateMachine, OrderState>().InMemoryRepository();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderId = NewId.NextGuid();
        await harness.Bus.Publish<SubmitOrder>(new { OrderId = orderId, CustomerId = Guid.NewGuid(), Items = new List<Item>() });

        var sagaHarness = provider.GetRequiredService<ISagaStateMachineTestHarness<OrderState, OrderStateMachine>>();
        var instanceId = await sagaHarness.Exists(orderId, x => x.CurrentState == "Submitted");
        Assert.NotNull(instanceId);
    }
}
