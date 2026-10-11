using Dapper;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using PainelGestao.OrganizaliFinancas.Application.Planning;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Services;
namespace PainelGestao.OrganizaliFinancas.Tests.Helpers
{
    /// <summary>M8: the cross-process job lock and the daily horizon routine over the households created by the previous assertions.</summary>
    internal static class OperationsAssertions
    {
        public static async Task VerifyAsync(ServiceProvider provider, string connection)
        {
            await using (var first = provider.CreateAsyncScope())
            await using (var second = provider.CreateAsyncScope())
            {
                var held = await first.ServiceProvider.GetRequiredService<IJobLock>().TryAcquireAsync("organizali_test_lock", CancellationToken.None);
                held.Should().NotBeNull();
                (await second.ServiceProvider.GetRequiredService<IJobLock>().TryAcquireAsync("organizali_test_lock", CancellationToken.None))
                    .Should().BeNull("another connection already holds the lock");
                await held!.DisposeAsync();
                var again = await second.ServiceProvider.GetRequiredService<IJobLock>().TryAcquireAsync("organizali_test_lock", CancellationToken.None);
                again.Should().NotBeNull("the lock is released on dispose");
                await again!.DisposeAsync();
            }
            // A released lock must be free for other processes too (it must not linger in a pooled session).
            await using (var outside = new MySqlConnection(connection + ";Pooling=false"))
                (await outside.QuerySingleAsync<long>("SELECT IS_FREE_LOCK('organizali_test_lock')")).Should().Be(1);

            var initial = await RollAsync(provider);
            initial.Skipped.Should().BeFalse();
            initial.Households.Should().BeGreaterThanOrEqualTo(2, "every active household is processed");
            (await RollAsync(provider)).Should().Be(new HorizonRollDto(false, initial.Households, 0, 0, 0), "a second run changes nothing");

            await using var holder = provider.CreateAsyncScope();
            await using var lockHandle = await holder.ServiceProvider.GetRequiredService<IJobLock>().TryAcquireAsync(RollHorizonHandler.LockName, CancellationToken.None);
            (await RollAsync(provider)).Skipped.Should().BeTrue("another replica is already running the routine");
        }

        private static async Task<HorizonRollDto> RollAsync(ServiceProvider provider)
        {
            await using var scope = provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new RollHorizonCommand("test:horizon"));
        }
    }
}
