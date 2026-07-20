using System.ComponentModel.DataAnnotations;

namespace Application.Saga;

public sealed class OrderSagaOptions
{
    public const string SectionName = "OrderSaga";

    [Range(typeof(TimeSpan), "00:00:01", "30.00:00:00")]
    public TimeSpan PaymentTimeout { get; init; } = TimeSpan.FromMinutes(5);
}
