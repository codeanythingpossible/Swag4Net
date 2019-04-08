using System;

namespace ClientsForSwagger.RestClient.Results
{
    public class Result
    {
        public Result(bool isSuccess, string errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public bool IsSuccess { get; }
        public string ErrorMessage { get; }


        public static Result<T> SuccessOf<T>(T value) => new Result<T>(true, value, string.Empty);
    
        public static Result Success() => new Result(true, string.Empty);
    
        public static Result<T> FailureOf<T>(string message) => new Result<T>(false, default, message);
    
        public static Result Failure(string message) => new Result(false, message);
    }

    public class Result<T> : Result
    {
        private readonly T value;

        public Result(bool isSuccess, T value, string errorMessage) : base(isSuccess, errorMessage)
        {
            this.value = value;
        }

        public T Value
        {
            get
            {
                if (!IsSuccess)
                    throw new InvalidOperationException(ErrorMessage);
                return value;
            }
        }
    }
}