using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ServiceDefaults;

public static class EntityFrameworkOutboxAdoption
{
    public static MessagingHostConfigurator AddEntityFrameworkInboxOutbox<TDbContext>(
        this MessagingHostConfigurator messaging,
        Action<IEntityFrameworkOutboxConfigurator>? configure = null)
        where TDbContext : DbContext
    {
        messaging.Registration(registration =>
            registration.AddEntityFrameworkOutbox<TDbContext>(outbox =>
            {
                outbox.DuplicateDetectionWindow = TimeSpan.FromHours(1);
                outbox.QueryDelay = TimeSpan.FromSeconds(1);
                outbox.QueryTimeout = TimeSpan.FromSeconds(30);
                configure?.Invoke(outbox);
            }));
        return messaging;
    }

    public static void UseEntityFrameworkInboxOutbox<TDbContext>(
        this IReceiveEndpointConfigurator endpoint,
        IRegistrationContext context,
        Action<IOutboxOptionsConfigurator>? configure = null)
        where TDbContext : DbContext =>
        endpoint.UseEntityFrameworkOutbox<TDbContext>(context, configure);
}
