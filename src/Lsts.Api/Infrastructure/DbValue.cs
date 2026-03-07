using Oracle.ManagedDataAccess.Types;

namespace Lsts.Api.Infrastructure;

internal static class DbValue
{
    public static long ToInt64(object? value)
    {
        if (value is null || value is DBNull) return 0;
        if (value is long l) return l;
        if (value is int i) return i;
        if (value is decimal d) return (long)d;
        if (value is OracleDecimal od) return od.ToInt64();
        return Convert.ToInt64(value);
    }

    public static int ToInt32(object? value)
    {
        if (value is null || value is DBNull) return 0;
        if (value is int i) return i;
        if (value is long l) return (int)l;
        if (value is decimal d) return (int)d;
        if (value is OracleDecimal od) return od.ToInt32();
        return Convert.ToInt32(value);
    }

    public static decimal ToDecimal(object? value)
    {
        if (value is null || value is DBNull) return 0m;
        if (value is decimal d) return d;
        if (value is int i) return i;
        if (value is long l) return l;
        if (value is OracleDecimal od) return od.Value;
        return Convert.ToDecimal(value);
    }
}