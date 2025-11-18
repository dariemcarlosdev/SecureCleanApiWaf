namespace SecureCleanApiWaf.Core.Application.Common.Models
{
    /// <summary>
    /// Represents the result of an operation, encapsulating its success status, returned data, and error information.
    /// </summary>
    /// <remarks>Use the static factory methods <see cref="Ok"/> and <see cref="Fail"/> to create instances
    /// representing successful or failed results. When <paramref name="Success"/> is <see langword="false"/>, <paramref
    /// name="Data"/> is set to the default value for <typeparamref name="T"/> and <paramref name="Error"/> contains the
    /// error message.</remarks>
    /// <typeparam name="T">The type of the data returned by the operation.</typeparam>
    /// <param name="Success">Indicates whether the operation was successful. Set to <see langword="true"/> if the operation succeeded;
    /// otherwise, <see langword="false"/>.</param>
    /// <param name="Data">The data returned by the operation if successful; otherwise, the default value for type <typeparamref
    /// name="T"/>.</param>
    /// <param name="Error">The error message describing the reason for failure if the operation was not successful; otherwise, <see
    /// langword="null"/>.</param>
    public record Result<T>(bool Success, T Data, string Error)
    {
        // Factory method that returns an instance of a Result<T>, encapsulate the creation logic, used to create a successful result with data.
        public static Result<T> Ok(T data) => new Result<T>(true, data, null);
        // Factory method for failed result. The Data is set to default value of T. Here, default means null for reference types and zero or equivalent for value types.
        public static Result<T> Fail(string error) => new Result<T>(false, default, error);
    }
}
