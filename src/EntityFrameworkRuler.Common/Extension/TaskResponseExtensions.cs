using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace EntityFrameworkRuler.Extension;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public static class TaskResponseExtensions {
    /// <summary> Execute new task and handle error safely, returning ITaskResponse object. </summary>
    public static Task<TaskResponse<T>> StartNewWithErrorHandling<T>(this TaskFactory factory, Func<T> task) => factory.StartNew(() => RunWithErrorHandling(task));

    /// <summary> Execute task and handle error safely, returning ITaskResponse object. </summary>
    private static TaskResponse<T> RunWithErrorHandling<T>(Func<T> task) {
        try {
            var x = task();
            Debug.Assert(x is not Task);
            return new TaskResponse<T>(x);
        } catch (Exception ex) {
            return new TaskResponse<T>(ex);
        }
    }

    /// <summary> Execute task and handle error safely, returning ITaskResponse object. </summary>
    public static async Task<TaskResponse<T>> RunWithErrorHandling<T>(this Func<Task<T>> task, bool continueOnCapturedContext = true) {
        try {
            return new TaskResponse<T>(await task().ConfigureAwait(continueOnCapturedContext));
        } catch (Exception ex) {
            return new TaskResponse<T>(ex);
        }
    }

    /// <summary> Execute task and handle error safely, returning ITaskResponse object. </summary>
    public static async Task<TaskResponse<T>> RunWithErrorHandling<T>(this Task<T> task, bool continueOnCapturedContext = true) {
        try {
#if NET6_0_OR_GREATER
            if (task.IsCompletedSuccessfully)
#else
            if (task.IsCompleted && !task.IsFaulted)
#endif
                return new TaskResponse<T>(task.Result);
            return new TaskResponse<T>(await task.ConfigureAwait(continueOnCapturedContext));
        } catch (Exception ex) {
            return new TaskResponse<T>(ex);
        }
    }

    /// <summary> Execute task and handle error safely, returning ITaskResponse object. </summary>
    public static async Task<TaskResponse<T>> RunWithErrorHandling<T>(this Func<ValueTask<T>> task, bool continueOnCapturedContext = true) {
        try {
            var valueTask = task();
            if (valueTask.IsCompletedSuccessfully)
                return new TaskResponse<T>(valueTask.Result);
            var result = await valueTask.ConfigureAwait(continueOnCapturedContext);
            return new TaskResponse<T>(result);
        } catch (Exception ex) {
            return new TaskResponse<T>(ex);
        }
    }

    /// <summary> Execute task and handle error safely, returning ITaskResponse object. </summary>
    public static async Task<TaskResponse<T>> RunWithErrorHandling<T>(this ValueTask<T> valueTask, bool continueOnCapturedContext = true) {
        try {
            if (valueTask.IsCompletedSuccessfully)
                return new TaskResponse<T>(valueTask.Result);
            var result = await valueTask.ConfigureAwait(continueOnCapturedContext);
            return new TaskResponse<T>(result);
        } catch (Exception ex) {
            return new TaskResponse<T>(ex);
        }
    }

    /// <summary> convert result to ITaskResponse object. </summary>
    public static TaskResponse<T> ToTaskResponse<T>(this T result) => new(result);

    /// <summary> convert result to ITaskResponse object. </summary>
    public static TaskResponse<T> ToSuccessResponse<T>(this T result) => new(result);

    /// <summary> convert result to ITaskResponse object. </summary>
    public static TaskResponse<T> ToFailedResponse<T>(this Exception result) => new(result);

    /// <summary> Throw exception if the response is failed status </summary>
    public static void ThrowIfFailed(this ITaskResponse response, bool throwInnerMostExceptionOnly = false) {
        if (!response.Failed)
            return;
        if (response.Exception is not null) {
            if (throwInnerMostExceptionOnly)
                response.Exception.GetInnerMostException().Throw();
            response.Exception.Throw();
        }

        throw new Exception(response.Message ?? UnknownError.Message);
    }

    /// <summary>
    /// Converts a status message into a <see cref="ResponseStatusType"/>.
    /// Returns <see cref="ResponseStatusType.Ok"/> when the message is null or equivalent to "OK".
    /// </summary>
    public static ResponseStatusType MessageToStatusType(this string message) {
        return message is null || string.Equals(message, "OK", StringComparison.OrdinalIgnoreCase) ? ResponseStatusType.Ok : ResponseStatusType.Failure;
    }

    /// <summary>
    /// Assigns an operation identifier to a response and returns the updated response.
    /// </summary>
    /// <param name="tr">The response to update.</param>
    /// <param name="opId">The identifier that should be applied.</param>
    /// <typeparam name="T">The response type being updated.</typeparam>
    /// <returns>The response instance with its <see cref="IHasOperationID.OperationID"/> applied.</returns>
    public static ITaskResponse WithOperationID<T>(T tr, Guid opId) where T : ITaskResponse => tr.WithOperationID(opId);

    /// <summary> Throw this exception </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public static string Throw(this Exception e) {
        if (e is null)
            throw new Exception("Unknown error");
        throw e;
    }

    /// <summary> Get innermost Exception only </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public static Exception GetInnerMostException(this Exception e) {
        while (e?.InnerException != null)
            e = e.InnerException;
        return e;
    }

    internal static readonly Exception UnknownError = new("Unknown error");
}

/// <summary>
/// A response object for a function or task that protects from exceptions by providing result status and any exception that occurred.
/// Get this object by calling 'TaskFactory'.StartNewWithErrorHandling(...) or 'Func'.RunWithErrorHandling() extensions
/// </summary>
[DataContract]
public record struct TaskResponse<T> : ITaskResponse<T>, IEquatable<TaskResponse<T>>, IHasOperationID {
    /// <summary>
    /// Creates a successful response that encapsulates the supplied result.
    /// </summary>
    /// <param name="result">The value produced by the operation.</param>
    /// <param name="message">An optional human-readable status message.</param>
    /// <param name="statusType">An optional explicit status classification.</param>
    /// <param name="statusCode">An optional explicit numeric status code.</param>
    /// <returns>A <see cref="TaskResponse{T}"/> flagged as successful.</returns>
    public static TaskResponse<T> FromResult(T result, string message = null, ResponseStatusType? statusType = null, byte? statusCode = null) =>
        new(result, message, statusType, statusCode);

    /// <summary>
    /// Creates a failed response using the provided status message.
    /// </summary>
    /// <param name="message">Details about the failure.</param>
    /// <param name="statusType">An optional explicit status classification.</param>
    /// <param name="statusCode">An optional explicit numeric status code.</param>
    /// <returns>A <see cref="TaskResponse{T}"/> flagged as failed.</returns>
    public static TaskResponse<T> FromError(string message, ResponseStatusType? statusType = null, byte? statusCode = null) =>
        new(default, false, message, statusType, statusCode);

    /// <summary>
    /// Creates a failed response that captures the supplied exception.
    /// </summary>
    /// <param name="error">The exception thrown by the operation.</param>
    /// <param name="statusType">An optional explicit status classification.</param>
    /// <param name="statusCode">An optional explicit numeric status code.</param>
    /// <returns>A <see cref="TaskResponse{T}"/> flagged as failed.</returns>
    public static TaskResponse<T> FromError(Exception error, ResponseStatusType statusType = ResponseStatusType.Failure, byte? statusCode = null) =>
        new(default, error, statusType, statusCode);

    /// <summary>
    /// Initializes a new success response that wraps the provided result.
    /// </summary>
    /// <param name="result">The value produced by the operation.</param>
    /// <param name="message">An optional human-readable status message.</param>
    /// <param name="statusType">An optional explicit status classification.</param>
    /// <param name="statusCode">An optional explicit numeric status code.</param>
    public TaskResponse(T result, string message = null, ResponseStatusType? statusType = null, byte? statusCode = null) {
        Result = result;
        StatusType = statusType ?? message.MessageToStatusType();
        StatusCode = statusCode ?? (StatusType > ResponseStatusType.Ok ? (byte)StatusType : byte.MinValue);
        this.message = message;
    }

    /// <summary>
    /// Initializes a new response using the supplied result and explicit success flag.
    /// </summary>
    /// <param name="result">The value produced by the operation.</param>
    /// <param name="succeeded">Indicates whether the response should be considered successful.</param>
    /// <param name="message">An optional human-readable status message.</param>
    /// <param name="statusType">An optional explicit status classification.</param>
    /// <param name="statusCode">An optional explicit numeric status code.</param>
    public TaskResponse(T result, bool succeeded, string message = null, ResponseStatusType? statusType = null, byte? statusCode = null) {
        Result = result;
        StatusType = statusType ?? (succeeded ? ResponseStatusType.Ok : ResponseStatusType.Failure);
        StatusCode = statusCode ?? (StatusType > ResponseStatusType.Ok ? (byte)StatusType : byte.MinValue);
        this.message = message;
    }

    /// <summary>
    /// Initializes a failed response that includes a result payload and the captured exception.
    /// </summary>
    /// <param name="result">The value produced prior to the failure, if any.</param>
    /// <param name="exception">The exception thrown by the operation.</param>
    /// <param name="statusType">An optional explicit status classification.</param>
    /// <param name="statusCode">An optional explicit numeric status code.</param>
    public TaskResponse(T result, Exception exception, ResponseStatusType statusType = ResponseStatusType.Failure, byte? statusCode = null) {
        Result = result;
        StatusType = statusType == ResponseStatusType.Ok ? ResponseStatusType.Failure : statusType;
        StatusCode = statusCode ?? (byte)StatusType;
        Exception = exception;
    }

    /// <summary>
    /// Initializes a failed response that only captures the exception details.
    /// </summary>
    /// <param name="exception">The exception thrown by the operation.</param>
    /// <param name="statusType">An optional explicit status classification.</param>
    /// <param name="statusCode">An optional explicit numeric status code.</param>
    public TaskResponse(Exception exception, ResponseStatusType statusType = ResponseStatusType.Failure, byte? statusCode = null) {
        Result = default;
        StatusType = statusType == ResponseStatusType.Ok ? ResponseStatusType.Failure : statusType;
        StatusCode = statusCode ?? (byte)StatusType;
        Exception = exception;
    }

    /// <summary>
    /// Initializes a new response with explicit serialization values.
    /// </summary>
    /// <param name="result">The value produced by the operation.</param>
    /// <param name="message">An optional human-readable status message.</param>
    /// <param name="operationId">The identifier of the operation that produced the response.</param>
    /// <param name="statusType">The status classification that was persisted.</param>
    /// <param name="statusCode">The numeric status code that was persisted.</param>
    [JsonConstructor]
    public TaskResponse(T result, string message, Guid operationId, ResponseStatusType statusType, byte statusCode) {
        Result = result;
        StatusType = statusType;
        StatusCode = statusCode;
        this.message = message;
        OperationID = operationId;
    }

    #region properties

    /// <summary>
    /// Gets the value produced by the operation.
    /// </summary>
    [DataMember]
    public T Result { get; }

    /// <summary>
    /// Gets or sets the unique identifier for the task operation.
    /// </summary>
    [DataMember]
    public Guid OperationID { get; set; }

    /// <summary>
    /// Gets the status classification returned by the operation.
    /// </summary>
    [DataMember]
    public ResponseStatusType StatusType { get; }

    /// <summary>
    /// Gets the numeric status code associated with the response.
    /// </summary>
    [DataMember]
    public byte StatusCode { get; }

    private readonly string message;

    /// <summary>
    /// Gets the status message supplied by the operation or captured exception.
    /// </summary>
    [DataMember]
    public string Message => message ?? Exception?.Message ?? (Succeeded ? null : TaskResponseExtensions.UnknownError.Message);

    /// <summary>
    /// Gets the exception that caused the operation to fail, if one was captured.
    /// </summary>
    [IgnoreDataMember, JsonIgnore]
    public Exception Exception { get; }

    /// <summary>
    /// Gets a value indicating whether the response represents a successful outcome.
    /// </summary>
    [IgnoreDataMember, JsonIgnore]
    public bool Succeeded => StatusType == ResponseStatusType.Ok;

    /// <summary>
    /// Gets a value indicating whether the response represents a failed outcome.
    /// </summary>
    [IgnoreDataMember, JsonIgnore]
    public readonly bool Failed => StatusType != ResponseStatusType.Ok;

    #endregion

    /// <summary>
    /// Returns the result value as an <see cref="object"/>.
    /// </summary>
    public readonly object GetResult() => Result;

    /// <summary>
    /// Creates a copy of the response that includes the supplied operation identifier.
    /// </summary>
    /// <param name="opId">The identifier to assign.</param>
    /// <returns>A copy of the response with <see cref="OperationID"/> set.</returns>
    public TaskResponse<T> WithOperationID(Guid opId) => this with { OperationID = opId };
    ITaskResponse ITaskResponse.WithOperationID(Guid opId) => this with { OperationID = opId };

#if false
    public override Microsoft.AspNetCore.Mvc.JsonResult ToJsonResult() =>
        new(new { Success = Succeeded, Message = Message, Result = Result });
#endif

    /// <summary>
    /// Converts a success flag and message tuple into a response.
    /// </summary>
    /// <param name="tuple">The tuple describing success and accompanying message.</param>
    /// <returns>A response representing the tuple.</returns>
    public static implicit operator TaskResponse<T>((bool success, string message) tuple) =>
        new(default, tuple.success, tuple.message.NullIfEmpty());

    /// <summary>
    /// Converts a result and message tuple into a response.
    /// </summary>
    /// <param name="tuple">The tuple containing the result and optional message.</param>
    /// <returns>A response representing the tuple.</returns>
    public static implicit operator TaskResponse<T>((T result, string message) tuple) => new TaskResponse<T>(tuple.result, tuple.message);

    /// <summary>
    /// Wraps a result value in a successful response.
    /// </summary>
    /// <param name="result">The result to wrap.</param>
    /// <returns>A successful response containing the result.</returns>
    public static implicit operator TaskResponse<T>(T result) => new(result);

    /// <summary>
    /// Converts an error message into a failed response.
    /// </summary>
    /// <param name="message">Details about the failure.</param>
    /// <returns>A failed response containing the message.</returns>
    public static implicit operator TaskResponse<T>(string message) => FromError(message);

    /// <summary>
    /// Converts an exception into a failed response.
    /// </summary>
    /// <param name="error">The exception thrown by the operation.</param>
    /// <returns>A failed response capturing the exception.</returns>
    public static implicit operator TaskResponse<T>(Exception error) => FromError(error);

    /// <summary>
    /// Extracts the success flag from a response when it wraps a Boolean result.
    /// </summary>
    /// <param name="response">The response to inspect.</param>
    /// <returns><c>true</c> when the response indicates success.</returns>
    public static implicit operator bool(TaskResponse<T> response) => response.Result is bool b ? b : response.Succeeded;

    /// <summary>
    /// Creates a response from a status type.
    /// </summary>
    /// <param name="statusType">The status classification to convert.</param>
    /// <returns>A response representing the supplied status.</returns>
    public static implicit operator TaskResponse<T>(ResponseStatusType statusType) =>
        new(default, statusType == ResponseStatusType.Ok, null, statusType);

    /// <summary>
    /// Creates a response from a status type and status code tuple.
    /// </summary>
    /// <param name="tuple">The tuple containing the status information.</param>
    /// <returns>A response representing the supplied status.</returns>
    public static implicit operator TaskResponse<T>((ResponseStatusType statusType, byte code) tuple) =>
        new(default(T), (Exception)null, tuple.statusType, tuple.code);

    /// <summary>
    /// Creates a response from a status type and message tuple.
    /// </summary>
    /// <param name="tuple">The tuple containing the status information.</param>
    /// <returns>A response representing the supplied status.</returns>
    public static implicit operator TaskResponse<T>((ResponseStatusType statusType, string message) tuple) =>
        new(default, tuple.statusType == ResponseStatusType.Ok, tuple.message, tuple.statusType);

    /// <summary>
    /// Creates a response from a status type, code, and message tuple.
    /// </summary>
    /// <param name="tuple">The tuple containing detailed status information.</param>
    /// <returns>A response representing the supplied status.</returns>
    public static implicit operator TaskResponse<T>((ResponseStatusType statusType, byte code, string message) tuple) =>
        new(default, tuple.statusType == ResponseStatusType.Ok, tuple.message, tuple.statusType, tuple.code);

    /// <summary>
    /// Creates a response from a result value and status type tuple.
    /// </summary>
    /// <param name="tuple">The tuple containing the result and status information.</param>
    /// <returns>A response representing the supplied tuple.</returns>
    public static implicit operator TaskResponse<T>((T result, ResponseStatusType statusType) tuple) =>
        new(tuple.result, tuple.statusType == ResponseStatusType.Ok, null, tuple.statusType);

    /// <summary>
    /// Creates a response from a result value, status type, and code tuple.
    /// </summary>
    /// <param name="tuple">The tuple containing the result and status information.</param>
    /// <returns>A response representing the supplied tuple.</returns>
    public static implicit operator TaskResponse<T>((T result, ResponseStatusType statusType, byte code) tuple) =>
        new(tuple.result, tuple.statusType == ResponseStatusType.Ok, null, tuple.statusType, tuple.code);

    /// <summary>
    /// Creates a response from a result value, status type, and message tuple.
    /// </summary>
    /// <param name="tuple">The tuple containing the result and status information.</param>
    /// <returns>A response representing the supplied tuple.</returns>
    public static implicit operator TaskResponse<T>((T result, ResponseStatusType statusType, string message) tuple) =>
        new(tuple.result, tuple.statusType == ResponseStatusType.Ok, tuple.message, tuple.statusType);

    /// <summary>
    /// Creates a response from a result value, status type, code, and message tuple.
    /// </summary>
    /// <param name="tuple">The tuple containing the result and status information.</param>
    /// <returns>A response representing the supplied tuple.</returns>
    public static implicit operator TaskResponse<T>((T result, ResponseStatusType statusType, byte code, string message) tuple) =>
        new(tuple.result, tuple.statusType == ResponseStatusType.Ok, tuple.message, tuple.statusType, tuple.code);


    /// <summary> This method allow you to deconstruct the type into a tuple </summary>
    public void Deconstruct(out T result, out bool success) {
        result = Result;
        success = Succeeded;
    }

    /// <summary> This method allow you to deconstruct the type into a tuple </summary>
    public void Deconstruct(out T result, out bool success, out string message) {
        result = Result;
        success = Succeeded;
        message = Message;
    }

    /// <summary> This method allow you to deconstruct the type into a tuple </summary>
    public void Deconstruct(out T result, out bool success, out string message, out ResponseStatusType statusType) {
        success = Succeeded;
        result = Result;
        message = Message;
        statusType = StatusType;
    }

    /// <summary> This method allow you to deconstruct the type into a tuple </summary>
    public void Deconstruct(out T result, out bool success, out string message, out ResponseStatusType statusType, out byte statusCode) {
        success = Succeeded;
        result = Result;
        message = Message;
        statusType = StatusType;
        statusCode = StatusCode;
    }

    #region IEquatable

    /// <summary>
    /// Determines whether this response is equal to another response of the same type.
    /// </summary>
    /// <param name="other">The response to compare with the current instance.</param>
    /// <returns><c>true</c> when the responses represent the same status and result.</returns>
    public bool Equals(TaskResponse<T> other) {
        return StatusType == other.StatusType && StatusCode == other.StatusCode && Message == other.Message &&
               (ReferenceEquals(Result, other.Result) || Result?.Equals(other.Result) == true);
    }

    /// <summary>
    /// Determines whether this response is equal to another <see cref="ITaskResponse"/>.
    /// </summary>
    /// <param name="other">The response to compare with the current instance.</param>
    /// <returns><c>true</c> when the responses represent the same status and result.</returns>
    public bool Equals(ITaskResponse other) => other is TaskResponse<T> response && Equals(response);

#if true
    /// <summary>
    /// Returns a hash code that uniquely identifies this response.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(StatusType, StatusCode, Message);
#else
    public override int GetHashCode() => Succeeded.GetHashCode() ^ Exception.GetHashCode();
#endif

    #endregion

    /// <summary>
    /// Creates an <see cref="OperationException"/> that represents this response when it failed.
    /// </summary>
    /// <returns>An exception describing the failure, or <c>null</c> when the response succeeded.</returns>
    public OperationException ToException() => StatusType == ResponseStatusType.Ok ? default : new OperationException(ToString(), StatusType, StatusCode, OperationID, Exception);

    /// <summary>
    /// Returns a human-readable representation of the response.
    /// </summary>
    /// <returns>A string that describes the status, message, and result.</returns>
    public override string ToString() => Message.HasNonWhiteSpace() ? $"{StatusType}: {Message} ({StatusCode}) {Result}" : $"{StatusType} ({StatusCode}) {Result}";
}

/// <summary>
/// A response object for a function or task that protects from exceptions by providing result status and any exception that occurred.
/// Get this object by calling 'TaskFactory'.StartNewWithErrorHandling(...) or 'Func'.RunWithErrorHandling() extensions
/// </summary>
[DataContract]
public record struct TaskResponse : ITaskResponse, IEquatable<TaskResponse>, IHasOperationID {
    /// <summary>
    /// Creates a successful response with an optional message and status metadata.
    /// </summary>
    /// <param name="message">An optional human-readable status message.</param>
    /// <param name="statusType">An optional explicit status classification.</param>
    /// <param name="statusCode">An optional explicit numeric status code.</param>
    /// <returns>A <see cref="TaskResponse"/> flagged as successful.</returns>
    public static TaskResponse Success(string message = null, ResponseStatusType? statusType = null, byte? statusCode = null) =>
        new(true, message, statusType, statusCode);

    /// <summary>
    /// Creates a successful response that wraps the supplied result.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The value produced by the operation.</param>
    /// <param name="message">An optional human-readable status message.</param>
    /// <param name="statusType">An optional explicit status classification.</param>
    /// <param name="statusCode">An optional explicit numeric status code.</param>
    /// <returns>A <see cref="TaskResponse{T}"/> flagged as successful.</returns>
    public static TaskResponse<T> Success<T>(T result, string message = null, ResponseStatusType? statusType = null, byte? statusCode = null) =>
        FromResult<T>(result, message, statusType, statusCode);

    /// <summary>
    /// Creates a successful response that wraps the supplied result.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The value produced by the operation.</param>
    /// <param name="message">An optional human-readable status message.</param>
    /// <param name="statusType">An optional explicit status classification.</param>
    /// <param name="statusCode">An optional explicit numeric status code.</param>
    /// <returns>A <see cref="TaskResponse{T}"/> flagged as successful.</returns>
    public static TaskResponse<T> FromResult<T>(T result, string message = null, ResponseStatusType? statusType = null, byte? statusCode = null) =>
        new(result, message, statusType, statusCode);

    /// <summary>
    /// Creates a failed response using the provided status message.
    /// </summary>
    /// <param name="message">Details about the failure.</param>
    /// <param name="statusType">An optional explicit status classification.</param>
    /// <param name="statusCode">An optional explicit numeric status code.</param>
    /// <returns>A <see cref="TaskResponse"/> flagged as failed.</returns>
    public static TaskResponse FromError(string message, ResponseStatusType statusType = ResponseStatusType.Failure, byte? statusCode = null) =>
        new(false, message, statusType, statusCode);

    /// <summary>
    /// Creates a failed response that captures the supplied exception.
    /// </summary>
    /// <param name="error">The exception thrown by the operation.</param>
    /// <param name="statusType">An optional explicit status classification.</param>
    /// <param name="statusCode">An optional explicit numeric status code.</param>
    /// <returns>A <see cref="TaskResponse"/> flagged as failed.</returns>
    public static TaskResponse FromError(Exception error, ResponseStatusType statusType = ResponseStatusType.Failure, byte? statusCode = null) =>
        new(error, statusType, statusCode);

    /// <summary>
    /// Initializes a success response with an optional message and status metadata.
    /// </summary>
    /// <param name="message">An optional human-readable status message.</param>
    /// <param name="statusType">An optional explicit status classification.</param>
    /// <param name="statusCode">An optional explicit numeric status code.</param>
    public TaskResponse(string message = null, ResponseStatusType? statusType = null, byte? statusCode = null) {
        StatusType = statusType ?? message.MessageToStatusType();
        StatusCode = statusCode ?? (StatusType > ResponseStatusType.Ok ? (byte)StatusType : byte.MinValue);
        this.message = message;
    }

    /// <summary>
    /// Initializes a response using an explicit success flag.
    /// </summary>
    /// <param name="succeeded">Indicates whether the response should be considered successful.</param>
    /// <param name="message">An optional human-readable status message.</param>
    /// <param name="statusType">An optional explicit status classification.</param>
    /// <param name="statusCode">An optional explicit numeric status code.</param>
    public TaskResponse(bool succeeded, string message = null, ResponseStatusType? statusType = null, byte? statusCode = null) {
        StatusType = statusType ?? (succeeded ? ResponseStatusType.Ok : ResponseStatusType.Failure);
        StatusCode = statusCode ?? (StatusType > ResponseStatusType.Ok ? (byte)StatusType : byte.MinValue);
        this.message = message;
    }

    /// <summary>
    /// Initializes a failed response that captures the supplied exception.
    /// </summary>
    /// <param name="exception">The exception thrown by the operation.</param>
    /// <param name="statusType">An optional explicit status classification.</param>
    /// <param name="statusCode">An optional explicit numeric status code.</param>
    public TaskResponse(Exception exception, ResponseStatusType statusType = ResponseStatusType.Failure, byte? statusCode = null) {
        StatusType = statusType == ResponseStatusType.Ok ? ResponseStatusType.Failure : statusType;
        StatusCode = statusCode ?? (byte)StatusType;
        Exception = exception;
    }

    /// <summary>
    /// Initializes a response with explicit serialization values.
    /// </summary>
    /// <param name="message">An optional human-readable status message.</param>
    /// <param name="operationId">The identifier of the operation that produced the response.</param>
    /// <param name="statusType">The status classification that was persisted.</param>
    /// <param name="statusCode">The numeric status code that was persisted.</param>
    [JsonConstructor]
    public TaskResponse(string message, Guid operationId, ResponseStatusType statusType, byte statusCode) {
        StatusType = statusType;
        StatusCode = statusCode;
        this.message = message;
        OperationID = operationId;
    }

    #region properties

    /// <summary> A unique identifier for this task operation. </summary>
    [DataMember]
    public Guid OperationID { get; set; }

    /// <summary> Gets the response status type </summary>
    [DataMember]
    public ResponseStatusType StatusType { get; }

    /// <summary> Gets the unique status code. </summary>
    [DataMember]
    public byte StatusCode { get; }

    private readonly string message;

    /// <summary> Gets the status message. </summary>
    [DataMember]
    public string Message => message ?? Exception?.Message ?? (Succeeded ? null : TaskResponseExtensions.UnknownError.Message);

    /// <summary>
    /// Gets the exception that caused the operation to fail, if one was captured.
    /// </summary>
    [IgnoreDataMember, JsonIgnore]
    public Exception Exception { get; }

    /// <summary>
    /// Gets a value indicating whether the response represents a successful outcome.
    /// </summary>
    [IgnoreDataMember, JsonIgnore]
    public bool Succeeded => StatusType == ResponseStatusType.Ok;

    /// <summary>
    /// Gets a value indicating whether the response represents a failed outcome.
    /// </summary>
    [IgnoreDataMember, JsonIgnore]
    public readonly bool Failed => StatusType != ResponseStatusType.Ok;

    #endregion

    /// <summary>
    /// Creates an <see cref="OperationException"/> that represents this response when it failed.
    /// </summary>
    /// <returns>An exception describing the failure, or <c>null</c> when the response succeeded.</returns>
    public OperationException ToException() => StatusType == ResponseStatusType.Ok ? default : new OperationException(ToString(), StatusType, StatusCode, OperationID, Exception);

    /// <summary>
    /// Returns a human-readable representation of the response.
    /// </summary>
    /// <returns>A string that describes the status and message.</returns>
    public override string ToString() => Message.HasNonWhiteSpace() ? $"{StatusType}: {Message} ({StatusCode})" : $"{StatusType} ({StatusCode})";

#if false
    public virtual Microsoft.AspNetCore.Mvc.JsonResult ToJsonResult() =>
        new(new { Success = Succeeded, Message = Message, });
#endif

    /// <summary>
    /// Returns the result value as an <see cref="object"/>. Non-generic responses do not carry results, so this returns <c>null</c>.
    /// </summary>
    public readonly object GetResult() => null;

    /// <summary>
    /// Creates a copy of the response that includes the supplied operation identifier.
    /// </summary>
    /// <param name="opId">The identifier to assign.</param>
    /// <returns>A copy of the response with <see cref="OperationID"/> set.</returns>
    public TaskResponse WithOperationID(Guid opId) => this with { OperationID = opId };
    ITaskResponse ITaskResponse.WithOperationID(Guid opId) => this with { OperationID = opId };

    /// <summary>
    /// Converts a success flag and message tuple into a response.
    /// </summary>
    /// <param name="tuple">The tuple describing success and accompanying message.</param>
    /// <returns>A response representing the tuple.</returns>
    public static implicit operator TaskResponse((bool success, string message) tuple) => tuple.success
        ? (tuple.message.HasCharacters() ? new TaskResponse(true, tuple.message) : new TaskResponse(true))
        : FromError(tuple.message);

    /// <summary>
    /// Converts a boolean value into a response.
    /// </summary>
    /// <param name="success">Indicates whether the response should be successful.</param>
    /// <returns>A response representing the supplied value.</returns>
    public static implicit operator TaskResponse(bool success) => success ? new TaskResponse() : new TaskResponse(TaskResponseExtensions.UnknownError);

    /// <summary>
    /// Converts an error message into a response.
    /// </summary>
    /// <param name="message">Details about the operation.</param>
    /// <returns>A response representing the supplied message.</returns>
    public static implicit operator TaskResponse(string message) => new(message);

    /// <summary>
    /// Converts an exception into a failed response.
    /// </summary>
    /// <param name="error">The exception thrown by the operation.</param>
    /// <returns>A response capturing the exception.</returns>
    public static implicit operator TaskResponse(Exception error) => new(error);

    /// <summary>
    /// Converts a response into its captured exception.
    /// </summary>
    /// <param name="response">The response to inspect.</param>
    /// <returns>The captured exception, if any.</returns>
    public static implicit operator Exception(TaskResponse response) => response.Exception;

    /// <summary>
    /// Converts a response into a boolean success indicator.
    /// </summary>
    /// <param name="response">The response to inspect.</param>
    /// <returns><c>true</c> when the response indicates success.</returns>
    public static implicit operator bool(TaskResponse response) => response.Succeeded;

    /// <summary>
    /// Creates a response from a status type.
    /// </summary>
    /// <param name="statusType">The status classification to convert.</param>
    /// <returns>A response representing the supplied status.</returns>
    public static implicit operator TaskResponse(ResponseStatusType statusType) =>
        new(statusType == ResponseStatusType.Ok, null, statusType);

    /// <summary>
    /// Creates a response from a status type and status code tuple.
    /// </summary>
    /// <param name="tuple">The tuple containing the status information.</param>
    /// <returns>A response representing the supplied status.</returns>
    public static implicit operator TaskResponse((ResponseStatusType statusType, byte code) tuple) =>
        new(null, tuple.statusType, tuple.code);

    /// <summary>
    /// Creates a response from a status type and message tuple.
    /// </summary>
    /// <param name="tuple">The tuple containing the status information.</param>
    /// <returns>A response representing the supplied status.</returns>
    public static implicit operator TaskResponse((ResponseStatusType statusType, string message) tuple) =>
        new(tuple.statusType == ResponseStatusType.Ok, tuple.message, tuple.statusType);

    /// <summary>
    /// Creates a response from a status type, code, and message tuple.
    /// </summary>
    /// <param name="tuple">The tuple containing detailed status information.</param>
    /// <returns>A response representing the supplied status.</returns>
    public static implicit operator TaskResponse((ResponseStatusType statusType, byte code, string message) tuple) =>
        new(tuple.statusType == ResponseStatusType.Ok, tuple.message, tuple.statusType, tuple.code);


#if false
    public static implicit operator Microsoft.AspNetCore.Mvc.JsonResult(TaskResponse response) => response?.ToJsonResult() ?? new Microsoft.AspNetCore.Mvc.JsonResult("");
#endif
    /// <summary> This method allow you to deconstruct the type into a tuple </summary>
    public void Deconstruct(out bool success, out string message) {
        success = Succeeded;
        message = Message;
    }

    /// <summary> This method allow you to deconstruct the type into a tuple </summary>
    public void Deconstruct(out bool success, out string message, out ResponseStatusType statusType) {
        success = Succeeded;
        message = Message;
        statusType = StatusType;
    }

    /// <summary> This method allow you to deconstruct the type into a tuple </summary>
    public void Deconstruct(out bool success, out string message, out ResponseStatusType statusType, out byte code) {
        success = Succeeded;
        message = Message;
        statusType = StatusType;
        code = StatusCode;
    }

    #region IEquatable

    /// <summary>
    /// Determines whether this response is equal to another response.
    /// </summary>
    /// <param name="other">The response to compare with the current instance.</param>
    /// <returns><c>true</c> when the responses represent the same status and message.</returns>
    public bool Equals(TaskResponse other) {
        return StatusType == other.StatusType && StatusCode == other.StatusCode && Message == other.Message;
    }

    /// <summary>
    /// Determines whether this response is equal to another <see cref="ITaskResponse"/>.
    /// </summary>
    /// <param name="other">The response to compare with the current instance.</param>
    /// <returns><c>true</c> when the responses represent the same status and message.</returns>
    public bool Equals(ITaskResponse other) => other is TaskResponse response && Equals(response);

#if true
    /// <summary>
    /// Returns a hash code that uniquely identifies this response.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(StatusType, StatusCode, Message);
#else
    public override int GetHashCode() => Succeeded.GetHashCode() ^ Exception.GetHashCode();
#endif

    #endregion
}

/// <summary>
///  A response object for a function or task that contains an operationID guid
/// </summary>
public interface IHasOperationID {
    /// <summary> A unique identifier for a task operation. </summary>
    Guid OperationID { get; }
}

/// <summary>
/// Represents a task response that exposes a strongly typed result value.
/// </summary>
public interface ITaskResponse<out T> : ITaskResponse {
    /// <summary>
    /// Gets the value produced by the operation.
    /// </summary>
    T Result { get; }
}

/// <summary>  A response object for a function or task that protects from exceptions by providing result status and any exception that occurred. </summary>
public interface ITaskResponse : IHasOperationID, IEquatable<ITaskResponse> {
    /// <summary> Gets the unique status code. </summary>
    byte StatusCode { get; }

    /// <summary> Gets the response status type </summary>
    ResponseStatusType StatusType { get; }

    /// <summary> Gets a value indicating whether the response represents a failed outcome. </summary>
    bool Failed { get; }

    /// <summary> Gets a value indicating whether the response represents a successful outcome. </summary>
    bool Succeeded { get; }

    /// <summary> Gets the exception that caused the operation to fail, if one was captured. </summary>
    Exception Exception { get; }

    /// <summary> Gets the status message supplied by the operation or captured exception. </summary>
    string Message { get; }

    /// <summary> Returns the operation result as an <see cref="object"/>. </summary>
    object GetResult();

    /// <summary> Creates a copy of the response that includes the supplied operation identifier. </summary>
    ITaskResponse WithOperationID(Guid opId);

    /// <summary> Creates an <see cref="OperationException"/> that represents the response when it failed. </summary>
    OperationException ToException();
}

/// <summary>
/// Represents an exception that captures the status information from a task response failure.
/// </summary>
public class OperationException : Exception {
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationException"/> class using response metadata.
    /// </summary>
    /// <param name="message">Details about the failure.</param>
    /// <param name="statusType">The status classification of the failure.</param>
    /// <param name="statusCode">The numeric status code associated with the failure.</param>
    /// <param name="operationId">The identifier of the operation that failed.</param>
    /// <param name="innerException">The exception that originally caused the failure.</param>
    public OperationException(string message, ResponseStatusType statusType = ResponseStatusType.Failure, byte statusCode = default, Guid operationId = default,
        Exception innerException = null) :
        base(message, innerException) {
        StatusType = statusType;
        StatusCode = statusCode;
    }

    /// <summary>
    /// Gets the numeric status code associated with the failure.
    /// </summary>
    public byte StatusCode { get; }

    /// <summary>
    /// Gets the status classification associated with the failure.
    /// </summary>
    public ResponseStatusType StatusType { get; }
}

// public interface IResponseStatus {
//     /// <summary> Gets the unique status code. </summary>
//     byte Code { get; }
//
//     /// <summary> Gets the status message. </summary>
//     string Description { get; }
//
//     /// <summary> Gets the response status type </summary>
//     ResponseStatusType StatusType { get; }
//
//     bool Succeeded { get; }
//
//     Exception ToException();
// }

/// <summary>
/// Defines the set of status outcomes that a task response can report.
/// </summary>
public enum ResponseStatusType : byte {
    /// <summary> The operation completed successfully. </summary>
    Ok = 0,

    /// <summary> The operation failed with an unspecified error. </summary>
    Failure,

    /// <summary> The operation failed due to an unexpected error condition. </summary>
    Unexpected,

    /// <summary> The operation failed validation checks. </summary>
    Validation,

    /// <summary> The operation could not complete because of a conflicting state. </summary>
    Conflict,

    /// <summary> The target resource was not found. </summary>
    NotFound,

    /// <summary> The caller was not authenticated. </summary>
    Unauthorized,

    /// <summary> The caller was authenticated but lacks permission. </summary>
    Forbidden,

    /// <summary> The request cannot be serviced by the current system. </summary>
    NonServiceableRequest
}
