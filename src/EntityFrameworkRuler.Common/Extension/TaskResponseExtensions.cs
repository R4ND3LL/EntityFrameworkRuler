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

    /// <summary>   Convert a message to a ResponseStatusType. If the message is null or "OK" then it returns ResponseStatusType.Ok </summary>
    public static ResponseStatusType MessageToStatusType(this string message) {
        return message is null || string.Equals(message, "OK", StringComparison.OrdinalIgnoreCase) ? ResponseStatusType.Ok : ResponseStatusType.Failure;
    }

    /// <summary>  Convert a message to a ResponseStatusType. If the message is null or "OK" then it returns ResponseStatusType.Ok </summary> </summary>
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
    public static TaskResponse<T> FromResult(T result, string message = null, ResponseStatusType? statusType = null, byte? statusCode = null) =>
        new(result, message, statusType, statusCode);

    public static new TaskResponse<T> FromError(string message, ResponseStatusType? statusType = null, byte? statusCode = null) =>
        new(default, false, message, statusType, statusCode);

    public static new TaskResponse<T> FromError(Exception error, ResponseStatusType statusType = ResponseStatusType.Failure, byte? statusCode = null) =>
        new(default, error, statusType, statusCode);

    public TaskResponse(T result, string message = null, ResponseStatusType? statusType = null, byte? statusCode = null) {
        Result = result;
        StatusType = statusType ?? message.MessageToStatusType();
        StatusCode = statusCode ?? (StatusType > ResponseStatusType.Ok ? (byte)StatusType : byte.MinValue);
        this.message = message;
    }

    public TaskResponse(T result, bool succeeded, string message = null, ResponseStatusType? statusType = null, byte? statusCode = null) {
        Result = result;
        StatusType = statusType ?? (succeeded ? ResponseStatusType.Ok : ResponseStatusType.Failure);
        StatusCode = statusCode ?? (StatusType > ResponseStatusType.Ok ? (byte)StatusType : byte.MinValue);
        this.message = message;
    }

    public TaskResponse(T result, Exception exception, ResponseStatusType statusType = ResponseStatusType.Failure, byte? statusCode = null) {
        Result = result;
        StatusType = statusType == ResponseStatusType.Ok ? ResponseStatusType.Failure : statusType;
        StatusCode = statusCode ?? (byte)StatusType;
        Exception = exception;
    }

    public TaskResponse(Exception exception, ResponseStatusType statusType = ResponseStatusType.Failure, byte? statusCode = null) {
        Result = default;
        StatusType = statusType == ResponseStatusType.Ok ? ResponseStatusType.Failure : statusType;
        StatusCode = statusCode ?? (byte)StatusType;
        Exception = exception;
    }

    [JsonConstructor]
    public TaskResponse(T result, string message, Guid operationId, ResponseStatusType statusType, byte statusCode) {
        Result = result;
        StatusType = statusType;
        StatusCode = statusCode;
        this.message = message;
        OperationID = operationId;
    }

    #region properties

    [DataMember] public T Result { get; }

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

    [IgnoreDataMember, JsonIgnore] public Exception Exception { get; }


    [IgnoreDataMember, JsonIgnore] public bool Succeeded => StatusType == ResponseStatusType.Ok;

    [IgnoreDataMember, JsonIgnore] public readonly bool Failed => StatusType != ResponseStatusType.Ok;

    #endregion

    public readonly object GetResult() => Result;
    public TaskResponse<T> WithOperationID(Guid opId) => this with { OperationID = opId };
    ITaskResponse ITaskResponse.WithOperationID(Guid opId) => this with { OperationID = opId };

#if false
    public override Microsoft.AspNetCore.Mvc.JsonResult ToJsonResult() =>
        new(new { Success = Succeeded, Message = Message, Result = Result });
#endif

    public static implicit operator TaskResponse<T>((bool success, string message) tuple) =>
        new(default, tuple.success, tuple.message.NullIfEmpty());

    public static implicit operator TaskResponse<T>((T result, string message) tuple) => new TaskResponse<T>(tuple.result, tuple.message);

    public static implicit operator TaskResponse<T>(T result) => new(result);
    public static implicit operator TaskResponse<T>(string message) => FromError(message);
    public static implicit operator TaskResponse<T>(Exception error) => FromError(error);
    public static implicit operator bool(TaskResponse<T> response) => response.Result is bool b ? b : response.Succeeded;

    public static implicit operator TaskResponse<T>(ResponseStatusType statusType) =>
        new(default, statusType == ResponseStatusType.Ok, null, statusType);

    public static implicit operator TaskResponse<T>((ResponseStatusType statusType, byte code) tuple) =>
        new(default(T), (Exception)null, tuple.statusType, tuple.code);

    public static implicit operator TaskResponse<T>((ResponseStatusType statusType, string message) tuple) =>
        new(default, tuple.statusType == ResponseStatusType.Ok, tuple.message, tuple.statusType);

    public static implicit operator TaskResponse<T>((ResponseStatusType statusType, byte code, string message) tuple) =>
        new(default, tuple.statusType == ResponseStatusType.Ok, tuple.message, tuple.statusType, tuple.code);

    public static implicit operator TaskResponse<T>((T result, ResponseStatusType statusType) tuple) =>
        new(tuple.result, tuple.statusType == ResponseStatusType.Ok, null, tuple.statusType);

    public static implicit operator TaskResponse<T>((T result, ResponseStatusType statusType, byte code) tuple) =>
        new(tuple.result, tuple.statusType == ResponseStatusType.Ok, null, tuple.statusType, tuple.code);

    public static implicit operator TaskResponse<T>((T result, ResponseStatusType statusType, string message) tuple) =>
        new(tuple.result, tuple.statusType == ResponseStatusType.Ok, tuple.message, tuple.statusType);

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

    public bool Equals(TaskResponse<T> other) {
        return StatusType == other.StatusType && StatusCode == other.StatusCode && Message == other.Message &&
               (ReferenceEquals(Result, other.Result) || Result?.Equals(other.Result) == true);
    }

    public bool Equals(ITaskResponse other) => other is TaskResponse<T> response && Equals(response);

#if true
    public override int GetHashCode() => HashCode.Combine(StatusType, StatusCode, Message);
#else
    public override int GetHashCode() => Succeeded.GetHashCode() ^ Exception.GetHashCode();
#endif

    #endregion

    public OperationException ToException() => StatusType == ResponseStatusType.Ok ? default : new OperationException(ToString(), StatusType, StatusCode, OperationID, Exception);

    public override string ToString() => Message.HasNonWhiteSpace() ? $"{StatusType}: {Message} ({StatusCode}) {Result}" : $"{StatusType} ({StatusCode}) {Result}";
}

/// <summary>
/// A response object for a function or task that protects from exceptions by providing result status and any exception that occurred.
/// Get this object by calling 'TaskFactory'.StartNewWithErrorHandling(...) or 'Func'.RunWithErrorHandling() extensions
/// </summary>
[DataContract]
public record struct TaskResponse : ITaskResponse, IEquatable<TaskResponse>, IHasOperationID {
    public static TaskResponse Success(string message = null, ResponseStatusType? statusType = null, byte? statusCode = null) =>
        new(true, message, statusType, statusCode);

    public static TaskResponse<T> Success<T>(T result, string message = null, ResponseStatusType? statusType = null, byte? statusCode = null) =>
        FromResult<T>(result, message, statusType, statusCode);

    public static TaskResponse<T> FromResult<T>(T result, string message = null, ResponseStatusType? statusType = null, byte? statusCode = null) =>
        new(result, message, statusType, statusCode);

    public static TaskResponse FromError(string message, ResponseStatusType statusType = ResponseStatusType.Failure, byte? statusCode = null) =>
        new(false, message, statusType, statusCode);

    public static TaskResponse FromError(Exception error, ResponseStatusType statusType = ResponseStatusType.Failure, byte? statusCode = null) =>
        new(error, statusType, statusCode);

    public TaskResponse(string message = null, ResponseStatusType? statusType = null, byte? statusCode = null) {
        StatusType = statusType ?? message.MessageToStatusType();
        StatusCode = statusCode ?? (StatusType > ResponseStatusType.Ok ? (byte)StatusType : byte.MinValue);
        this.message = message;
    }

    public TaskResponse(bool succeeded, string message = null, ResponseStatusType? statusType = null, byte? statusCode = null) {
        StatusType = statusType ?? (succeeded ? ResponseStatusType.Ok : ResponseStatusType.Failure);
        StatusCode = statusCode ?? (StatusType > ResponseStatusType.Ok ? (byte)StatusType : byte.MinValue);
        this.message = message;
    }

    public TaskResponse(Exception exception, ResponseStatusType statusType = ResponseStatusType.Failure, byte? statusCode = null) {
        StatusType = statusType == ResponseStatusType.Ok ? ResponseStatusType.Failure : statusType;
        StatusCode = statusCode ?? (byte)StatusType;
        Exception = exception;
    }

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

    [IgnoreDataMember, JsonIgnore] public Exception Exception { get; }

    [IgnoreDataMember, JsonIgnore] public bool Succeeded => StatusType == ResponseStatusType.Ok;

    [IgnoreDataMember, JsonIgnore] public readonly bool Failed => StatusType != ResponseStatusType.Ok;

    #endregion

    public OperationException ToException() => StatusType == ResponseStatusType.Ok ? default : new OperationException(ToString(), StatusType, StatusCode, OperationID, Exception);

    public override string ToString() => Message.HasNonWhiteSpace() ? $"{StatusType}: {Message} ({StatusCode})" : $"{StatusType} ({StatusCode})";

#if false
    public virtual Microsoft.AspNetCore.Mvc.JsonResult ToJsonResult() =>
        new(new { Success = Succeeded, Message = Message, });
#endif

    public readonly object GetResult() => null;
    public TaskResponse WithOperationID(Guid opId) => this with { OperationID = opId };
    ITaskResponse ITaskResponse.WithOperationID(Guid opId) => this with { OperationID = opId };

    public static implicit operator TaskResponse((bool success, string message) tuple) => tuple.success
        ? (tuple.message.HasCharacters() ? new TaskResponse(true, tuple.message) : new TaskResponse(true))
        : FromError(tuple.message);

    public static implicit operator TaskResponse(bool success) => success ? new TaskResponse() : new TaskResponse(TaskResponseExtensions.UnknownError);
    public static implicit operator TaskResponse(string message) => new(message);
    public static implicit operator TaskResponse(Exception error) => new(error);
    public static implicit operator Exception(TaskResponse response) => response.Exception;
    public static implicit operator bool(TaskResponse response) => response.Succeeded;

    public static implicit operator TaskResponse(ResponseStatusType statusType) =>
        new(statusType == ResponseStatusType.Ok, null, statusType);

    public static implicit operator TaskResponse((ResponseStatusType statusType, byte code) tuple) =>
        new(null, tuple.statusType, tuple.code);

    public static implicit operator TaskResponse((ResponseStatusType statusType, string message) tuple) =>
        new(tuple.statusType == ResponseStatusType.Ok, tuple.message, tuple.statusType);

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

    public bool Equals(TaskResponse other) {
        return StatusType == other.StatusType && StatusCode == other.StatusCode && Message == other.Message;
    }

    public bool Equals(ITaskResponse other) => other is TaskResponse response && Equals(response);

#if true
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

public interface ITaskResponse<out T> : ITaskResponse {
    T Result { get; }
}

/// <summary>  A response object for a function or task that protects from exceptions by providing result status and any exception that occurred. </summary>
public interface ITaskResponse : IHasOperationID, IEquatable<ITaskResponse> {
    /// <summary> Gets the unique status code. </summary>
    byte StatusCode { get; }

    /// <summary> Gets the response status type </summary>
    ResponseStatusType StatusType { get; }

    bool Failed { get; }
    bool Succeeded { get; }
    Exception Exception { get; }
    string Message { get; }
    object GetResult();
    ITaskResponse WithOperationID(Guid opId);
    OperationException ToException();
}

public class OperationException : Exception {
    public OperationException(string message, ResponseStatusType statusType = ResponseStatusType.Failure, byte statusCode = default, Guid operationId = default,
        Exception innerException = null) :
        base(message, innerException) {
        StatusType = statusType;
        StatusCode = statusCode;
    }

    public byte StatusCode { get; }
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

public enum ResponseStatusType : byte {
    Ok = 0,
    Failure,
    Unexpected,
    Validation,
    Conflict,
    NotFound,
    Unauthorized,
    Forbidden,
    NonServiceableRequest
}