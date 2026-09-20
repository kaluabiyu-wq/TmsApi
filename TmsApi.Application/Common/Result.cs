

namespace TmsApi.Application.Common;

public readonly struct Result<TValue, TError>
{
    
    private readonly TValue? _value;
    private readonly TError? _error;

    public bool IsSuccess{get;}

    private Result(TValue value)
    {
        _value = value;
        _error = default;
        IsSuccess = true;
    }

    private Result(TError error)
    {
        _value = default;
        _error = error;
        IsSuccess = false;
    }

    public static Result<TValue,TError> Success(TValue value) =>
    new (value);
     public static Result<TValue,TError> Failure(TError error) =>
    new (error);

   public TValue Value => IsSuccess
   ? _value!
   : throw new InvalidOperationException("Result is failure: call Match instead of Value.");
    public TError Error => !IsSuccess
   ? _error!
   : throw new InvalidOperationException("Result is success: call Match instead of Error.");
   
  public Tout Match<Tout>(Func<TValue, Tout> onSuccess, Func<TError,Tout> onFailure) =>
     IsSuccess ? onSuccess(_value!) : onFailure(_error!);

}