using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MentoringApp.Api.DTOs.Common
{
    public record ApiResult
    {
        public bool IsSuccess { get; init; }
        public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();

        protected ApiResult() { }

        public static ApiResult Success() => new() { IsSuccess = true };

        public static ApiResult Fail(params string[] errors) =>
            new() { IsSuccess = false, Errors = new ReadOnlyCollection<string>((errors ?? Array.Empty<string>()).ToList()) };
    }

    public sealed record ApiResult<T> : ApiResult
    {
        public T? Data { get; init; }

        protected ApiResult() { }

        public static ApiResult<T> Success(T data) =>
            new() { IsSuccess = true, Data = data };

        public static new ApiResult<T> Fail(params string[] errors) =>
            new() { IsSuccess = false, Errors = new ReadOnlyCollection<string>((errors ?? Array.Empty<string>()).ToList()) };
    }
}