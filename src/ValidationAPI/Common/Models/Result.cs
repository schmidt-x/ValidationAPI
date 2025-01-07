using System;
using System.Diagnostics.CodeAnalysis;

namespace ValidationAPI.Common.Models;

public enum ResultState
{
	Err,
	Ok
}

public readonly struct Result<T>
{
	public ResultState State { get; }
	
	public TResult Match<TResult>(
		Func<T, TResult> success,
		Func<Exception, TResult> failure) 
		=> State == ResultState.Ok ? success(_value) : failure(_error);
	
	public Result(T value)
	{
		_value = value;
		_error = default!;
		State = ResultState.Ok;
	}
	
	public Result(Exception ex)
	{
		_error = ex;
		_value = default!;
		State = ResultState.Err;
	}
	
	private readonly T _value;
	
	private readonly Exception _error;
	
	public T Value => State == ResultState.Ok
		? _value 
		: throw new Exception("Attempt to access Value when State == Err");
	
	public Exception Error => State == ResultState.Err 
		? _error 
		: throw new Exception("Attempt to access Error when State == Ok");
	
	public bool IsError([MaybeNullWhen(false)] out Exception error)
	{
		if (State == ResultState.Err)
		{
			error = _error;
			return true;
		}
		
		error = default!;
		return false;
	}
	
	public bool IsOk([MaybeNullWhen(false)] out T value)
	{
		if (State == ResultState.Ok)
		{
			value = _value;
			return true;
		}
		
		value = default!;
		return false;
	}
	
	public static implicit operator Result<T>(T value) => new(value);
	public static implicit operator Result<T>(Exception ex)	=> new(ex);
}