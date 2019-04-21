using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Swag4Net.RestClient.Results
{
    public static class ResultExtensions
    {
        public static async Task<Result<T>> OnSuccess<T>(this Result<T> result, Func<T, Task> action)
        {
            if (result.IsSuccess)
                await action(result.Value).ConfigureAwait(false);
            return result;
        }
        
        public static async Task<Result<T>> OnSuccess<T>(this Task<Result<T>> result, Func<T, Task> action)
            => await OnSuccess(await result, action).ConfigureAwait(false);
        
        public static async Task<Result<T>> OnError<T>(this Result<T> result, Func<string, Task> action)
        {
            if (!result.IsSuccess)
                await action(result.ErrorMessage).ConfigureAwait(false);
            return result;
        }

        public static async Task<Result<T>> OnError<T>(this Task<Result<T>> result, Func<string, Task> action)
            => await OnError(await result, action).ConfigureAwait(false);

        public static async Task<Result<IEnumerable<T2>>> ThenMany<T1, T2>(
            this Task<Result<IEnumerable<T1>>> previous, 
            Func<T1,Task<Result<IEnumerable<T2>>>> next, 
            CancellationToken cancellationToken = default)
        {
            var result = await previous.ConfigureAwait(false);
            if (!result.IsSuccess)
                return Result.FailureOf<IEnumerable<T2>>(result.ErrorMessage);

            var results = new List<T2>();

            foreach (var v in result.Value)
            {
                var r = await next(v).ConfigureAwait(false);
                if (!r.IsSuccess)
                    return Result.FailureOf<IEnumerable<T2>>(r.ErrorMessage);
                if (cancellationToken.IsCancellationRequested)
                    return Result.FailureOf<IEnumerable<T2>>("Task cancelled.");
        
                results.AddRange(r.Value);
            }

            return Result.SuccessOf((IEnumerable<T2>)results);
        }

        public static async Task<Result<T2>> Then<T1, T2>(this Task<Result<T1>> previous, Func<T1,Task<Result<T2>>> next, 
            CancellationToken cancellationToken = default)
        {
            var result = await previous.ConfigureAwait(false);
            if (!result.IsSuccess)
                return Result.FailureOf<T2>(result.ErrorMessage);

            return await next(result.Value).ConfigureAwait(false);
        }        
    }
}