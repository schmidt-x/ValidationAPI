using System;
using System.Data;
using Dapper;

namespace ValidationAPI.Data.TypeHandlers;

public class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
{
	public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
		=> throw new NotImplementedException("Not called (I hope)");

	public override DateTimeOffset Parse(object value) => (DateTime)value;
}