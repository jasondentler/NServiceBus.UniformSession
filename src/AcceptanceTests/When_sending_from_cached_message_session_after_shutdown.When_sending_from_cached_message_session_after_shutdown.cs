﻿namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using UniformSession;

    public class When_sending_from_cached_message_session_after_shutdown : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_throw_when_sending()
        {
            var ctx = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithStartupTask>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.NotNull(ctx.StartupUniformSession);
            var exception = Assert.ThrowsAsync<InvalidOperationException>(() => ctx.StartupUniformSession.SendLocal(new MyMessage()));
            StringAssert.Contains("This session has been disposed and can no longer send messages.", exception.Message);
        }

        class Context : ScenarioContext
        {
            public IUniformSession StartupUniformSession;
        }

        class EndpointWithStartupTask : EndpointConfigurationBuilder
        {
            public EndpointWithStartupTask()
            {
                EndpointSetup<DefaultServer>(e => e.EnableFeature<FeatureWithStartupTask>());
            }

            class FeatureWithStartupTask : Feature
            {
                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.Container.ConfigureComponent<FeatureStartupTaskUsingDependencyInjection>(DependencyLifecycle.InstancePerCall);
                    context.RegisterStartupTask(b => b.Build<FeatureStartupTaskUsingDependencyInjection>());
                }

                class FeatureStartupTaskUsingDependencyInjection : FeatureStartupTask
                {
                    public FeatureStartupTaskUsingDependencyInjection(IUniformSession uniformSession, Context testContext)
                    {
                        this.uniformSession = uniformSession;
                        this.testContext = testContext;
                    }

                    protected override Task OnStart(IMessageSession session)
                    {
                        testContext.StartupUniformSession = uniformSession;
                        return Task.CompletedTask;
                    }

                    protected override Task OnStop(IMessageSession session)
                    {
                        return Task.CompletedTask;
                    }

                    IUniformSession uniformSession;
                    Context testContext;
                }
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}