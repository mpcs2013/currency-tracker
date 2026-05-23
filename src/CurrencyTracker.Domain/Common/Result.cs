using CurrencyTracker.Domain.Exceptions;

namespace CurrencyTracker.Domain.Common;

/// <summary>
/// Discriminated-union-shaped result of a domain operation: either a
/// success carrying a value of <typeparamref name="T"/>, or a failure
/// carrying an <see cref="Common.Error"/>. Used by domain factory
/// methods (<c>Create</c>) to communicate expected failures without
/// throwing.
/// </summary>
/// <typeparam name="T">Type of the success value.</typeparam>
public readonly record struct Result<T>
{
    private readonly T _value;
    private readonly Error? _error;

    private Result(T value, Error? error, bool isSuccess)
    {
        _value = value;
        _error = error;
        IsSuccess = isSuccess;
    }

    /// <summary>Gets a value indicating whether the result is a success.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets a value indicating whether the result is a failure.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the success value. Throws <see cref="DomainException"/> if the
    /// result is a failure; callers should prefer <see cref="Match"/> or
    /// guard with <see cref="IsSuccess"/>.
    /// </summary>
    /// <exception cref="DomainException">If <see cref="IsFailure"/>.</exception>
    public T Value =>
        IsSuccess ? _value : throw new DomainException("Cannot access Value on a Failure result.");

    /// <summary>
    /// Gets the failure error. Throws <see cref="DomainException"/> if the
    /// result is a success.
    /// </summary>
    /// <exception cref="DomainException">If <see cref="IsSuccess"/>.</exception>
    public Error Error =>
        IsFailure ? _error! : throw new DomainException("Cannot access Error on a Success result.");

    /// <summary>Creates a success result carrying the supplied value.</summary>
    /// <param name="value">The success value.</param>
    /// <returns>A success-state <see cref="Result{T}"/>.</returns>
    public static Result<T> Success(T value) => new(value, error: null, isSuccess: true);

    /// <summary>Creates a failure result carrying the supplied error.</summary>
    /// <param name="error">The failure error; must not be <see langword="null"/>.</param>
    /// <returns>A failure-state <see cref="Result{T}"/>.</returns>
    public static Result<T> Failure(Error error) => new(default!, error, isSuccess: false);

    /// <summary>
    /// Projects the result to a value of <typeparamref name="TOut"/> by
    /// invoking <paramref name="onSuccess"/> on the success value or
    /// <paramref name="onFailure"/> on the error. Both projections must
    /// return the same type; the result is the chosen branch's return.
    /// </summary>
    /// <typeparam name="TOut">The type both projections return.</typeparam>
    /// <param name="onSuccess">Projection applied when the result is a success.</param>
    /// <param name="onFailure">Projection applied when the result is a failure.</param>
    /// <returns>The chosen projection's return value.</returns>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess(_value) : onFailure(_error!);

    /// <summary>
    /// Transforms a success value of <typeparamref name="T"/> to a success
    /// value of <typeparamref name="TOut"/> by invoking
    /// <paramref name="mapper"/>. Failure results are returned unchanged.
    /// </summary>
    /// <typeparam name="TOut">Type of the mapped success value.</typeparam>
    /// <param name="mapper">Function applied to the success value.</param>
    /// <returns>A new <see cref="Result{TOut}"/>.</returns>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper) =>
        IsSuccess ? Result<TOut>.Success(mapper(_value)) : Result<TOut>.Failure(_error!);
}
