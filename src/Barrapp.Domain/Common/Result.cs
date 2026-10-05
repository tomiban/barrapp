namespace Barrapp.Domain.Common;

/// <summary>
/// Resultado de una operación de dominio: éxito sin valor o fallo con un <see cref="Common.Error"/>.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("Un resultado exitoso no puede contener un error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("Un resultado fallido debe contener un error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.Success(value);

    public static Result<TValue> Failure<TValue>(Error error) => Result<TValue>.Failure(error);
}

/// <summary>
/// Resultado de una operación de dominio que, en caso de éxito, transporta un valor.
/// </summary>
public class Result<TValue> : Result
{
    private readonly TValue _value;

    protected Result(TValue value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value => IsSuccess
        ? _value
        : throw new InvalidOperationException("No se puede acceder al valor de un resultado fallido.");

    public static Result<TValue> Success(TValue value) => new(value, true, Error.None);

    public static new Result<TValue> Failure(Error error) => new(default!, false, error);

    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
