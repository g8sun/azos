/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Globalization;
using Azos.Conf;

namespace Azos.Data
{
  /// <summary>
  /// Specifies how to handle errors during object value conversion
  /// </summary>
  public enum ConvertErrorHandling { ReturnDefault = 0, Throw }

  /// <summary>
  /// Provides extension methods for converting object values to different scalar types
  /// </summary>
  public static class ObjectValueConversion
  {
    public const string RADIX_BIN = "0b";
    public const string RADIX_HEX = "0x";

    private static readonly CultureInfo INVARIANT = CultureInfo.InvariantCulture;

    public static string AsString(this object val, string dflt = null, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return dflt;
        return Convert.ToString(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static string AsNonNullOrEmptyString(this object val)
    {
      try
      {
        if (val == null) throw new AzosException("arg = null");

        var result = Convert.ToString(val, INVARIANT);

        if (result.IsNullOrWhiteSpace())
          throw new AzosException("result = null|empty");

        return result;
      }
      catch (Exception error)
      {
        throw new AzosException("AsNonNullOrEmptyString({0})".Args(error.ToMessageWithType()));
      }
    }

    public static ConfigSectionNode AsLaconicConfig(this object val, ConfigSectionNode dflt = null, string wrapRootName = "azos", ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      string content;
      try
      {
        if (val == null) return dflt;
        content = val.ToString();
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }

      try
      {
        return LaconicConfiguration.CreateFromString(content).Root;
      }
      catch
      {
        if (wrapRootName.IsNotNullOrWhiteSpace())
          try
          {
            return LaconicConfiguration.CreateFromString(wrapRootName + "\n{\n" + content + "\n}").Root;
          }
          catch
          {
            if (handling != ConvertErrorHandling.ReturnDefault) throw;
            return dflt;
          }

        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static ConfigSectionNode AsJSONConfig(this object val, ConfigSectionNode dflt = null, string wrapRootName = "azos", ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      string content;
      try
      {
        if (val == null) return dflt;
        content = val.ToString();
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }

      try
      {
        return JSONConfiguration.CreateFromJson(content).Root;
      }
      catch
      {
        if (wrapRootName.IsNotNullOrWhiteSpace())
          try
          {
            return JSONConfiguration.CreateFromJson("{'" + wrapRootName + "':\n" + content + "\n}").Root;
          }
          catch
          {
            if (handling != ConvertErrorHandling.ReturnDefault) throw;
            return dflt;
          }

        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static ConfigSectionNode AsXMLConfig(this object val, ConfigSectionNode dflt = null, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return dflt;
        var content = val.ToString();

        return XMLConfiguration.CreateFromXML(content).Root;
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static char AsChar(this object val, char dflt = (char)0, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return dflt;
        if (val is string)
        {
          var sval = (string)val;
          return (sval.Length > 0) ? sval[0] : (char)0;
        }
        return Convert.ToChar(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static char? AsNullableChar(this object val, char? dflt = null, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return null;
        if (val is string)
        {
          var sval = (string)val;
          return (sval.Length > 0) ? sval[0] : (char)0;
        }
        return Convert.ToChar(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static byte AsByte(this object val, byte dflt = 0, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return dflt;
        if (val is string)
        {
          var sval = ((string)val).Trim();
          if (sval.StartsWith(RADIX_BIN, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToByte(sval.Substring(2), 2);
          if (sval.StartsWith(RADIX_HEX, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToByte(sval.Substring(2), 16);
        }
        return Convert.ToByte(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static byte? AsNullableByte(this object val, byte? dflt = null, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return null;
        if (val is string)
        {
          var sval = ((string)val).Trim();
          if (sval.StartsWith(RADIX_BIN, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToByte(sval.Substring(2), 2);
          if (sval.StartsWith(RADIX_HEX, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToByte(sval.Substring(2), 16);
        }
        return Convert.ToByte(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static sbyte AsSByte(this object val, sbyte dflt = 0, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return dflt;
        return Convert.ToSByte(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static sbyte? AsNullableSByte(this object val, sbyte? dflt = null, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return null;
        return Convert.ToSByte(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static short AsShort(this object val, short dflt = 0, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return dflt;
        if (val is string)
        {
          var sval = ((string)val).Trim();
          if (sval.StartsWith(RADIX_BIN, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToInt16(sval.Substring(2), 2);
          if (sval.StartsWith(RADIX_HEX, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToInt16(sval.Substring(2), 16);
        }
        return Convert.ToInt16(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static short? AsNullableShort(this object val, short? dflt = null, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return null;
        if (val is string)
        {
          var sval = ((string)val).Trim();
          if (sval.StartsWith(RADIX_BIN, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToInt16(sval.Substring(2), 2);
          if (sval.StartsWith(RADIX_HEX, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToInt16(sval.Substring(2), 16);
        }
        return Convert.ToInt16(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static ushort AsUShort(this object val, ushort dflt = 0, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return dflt;
        if (val is string)
        {
          var sval = ((string)val).Trim();
          if (sval.StartsWith(RADIX_BIN, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToUInt16(sval.Substring(2), 2);
          if (sval.StartsWith(RADIX_HEX, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToUInt16(sval.Substring(2), 16);
        }
        return Convert.ToUInt16(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static ushort? AsNullableUShort(this object val, ushort? dflt = null, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return null;
        if (val is string)
        {
          var sval = ((string)val).Trim();
          if (sval.StartsWith(RADIX_BIN, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToUInt16(sval.Substring(2), 2);
          if (sval.StartsWith(RADIX_HEX, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToUInt16(sval.Substring(2), 16);
        }
        return Convert.ToUInt16(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static int AsInt(this object val, int dflt = 0, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return dflt;
        if (val is string)
        {
          var sval = ((string)val).Trim();
          if (sval.StartsWith(RADIX_BIN, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToInt32(sval.Substring(2), 2);
          if (sval.StartsWith(RADIX_HEX, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToInt32(sval.Substring(2), 16);
        }
        if (val is uint) return (int)(uint)val;
        return Convert.ToInt32(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static int? AsNullableInt(this object val, int? dflt = null, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return null;
        if (val is string)
        {
          var sval = ((string)val).Trim();
          if (sval.StartsWith(RADIX_BIN, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToInt32(sval.Substring(2), 2);
          if (sval.StartsWith(RADIX_HEX, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToInt32(sval.Substring(2), 16);
        }
        if (val is uint) return (int)(uint)val;
        return Convert.ToInt32(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static uint AsUInt(this object val, uint dflt = 0, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return dflt;
        if (val is string)
        {
          var sval = ((string)val).Trim();
          if (sval.StartsWith(RADIX_BIN, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToUInt32(sval.Substring(2), 2);
          if (sval.StartsWith(RADIX_HEX, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToUInt32(sval.Substring(2), 16);
        }
        if (val is int) return (uint)(int)val;
        return Convert.ToUInt32(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static uint? AsNullableUInt(this object val, uint? dflt = null, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return null;
        if (val is string)
        {
          var sval = ((string)val).Trim();
          if (sval.StartsWith(RADIX_BIN, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToUInt32(sval.Substring(2), 2);
          if (sval.StartsWith(RADIX_HEX, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToUInt32(sval.Substring(2), 16);
        }
        if (val is int) return (uint)(int)val;
        return Convert.ToUInt32(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static long AsLong(this object val, long dflt = 0, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return dflt;
        if (val is string)
        {
          var sval = ((string)val).Trim();
          if (sval.StartsWith(RADIX_BIN, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToInt64(sval.Substring(2), 2);
          if (sval.StartsWith(RADIX_HEX, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToInt64(sval.Substring(2), 16);
        }
        if (val is ulong) return (long)(ulong)val;
        return Convert.ToInt64(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static long? AsNullableLong(this object val, long? dflt = null, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return null;
        if (val is string)
        {
          var sval = ((string)val).Trim();
          if (sval.StartsWith(RADIX_BIN, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToInt64(sval.Substring(2), 2);
          if (sval.StartsWith(RADIX_HEX, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToInt64(sval.Substring(2), 16);
        }
        if (val is ulong) return (long)(ulong)val;
        return Convert.ToInt64(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static ulong AsULong(this object val, ulong dflt = 0, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return dflt;
        if (val is string)
        {
          var sval = ((string)val).Trim();
          if (sval.StartsWith(RADIX_BIN, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToUInt64(sval.Substring(2), 2);
          if (sval.StartsWith(RADIX_HEX, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToUInt64(sval.Substring(2), 16);
        }
        if (val is long) return (ulong)(long)val;
        return Convert.ToUInt64(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static ulong? AsNullableULong(this object val, ulong? dflt = null, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return null;
        if (val is string)
        {
          var sval = ((string)val).Trim();
          if (sval.StartsWith(RADIX_BIN, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToUInt64(sval.Substring(2), 2);
          if (sval.StartsWith(RADIX_HEX, StringComparison.InvariantCultureIgnoreCase)) return Convert.ToUInt64(sval.Substring(2), 16);
        }
        if (val is long) return (ulong)(long)val;
        return Convert.ToUInt64(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static double AsDouble(this object val, double dflt = 0, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return dflt;
        return Convert.ToDouble(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static double? AsNullableDouble(this object val, double? dflt = null, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return null;
        return Convert.ToDouble(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static float AsFloat(this object val, float dflt = 0, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return dflt;
        return (float)Convert.ToDouble(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static float? AsNullableFloat(this object val, float? dflt = null, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return null;
        return (float)Convert.ToDouble(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static decimal AsDecimal(this object val, decimal dflt = 0, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return dflt;
        return Convert.ToDecimal(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static decimal? AsNullableDecimal(this object val, decimal? dflt = null, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return null;
        return Convert.ToDecimal(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    /// <summary>
    /// A "hack" enum used to provide tri-state checkbox functionality in some systems, i.e. HTML checkmarks
    /// do not understand "nulls". This is a surrogate type not used in server-side programming
    /// </summary>
    public enum TriStateBool { Unspecified = 0, False = 1, True = 2 }

    public static bool? AsNullableBool(this TriStateBool val)
    {
      return val == TriStateBool.Unspecified ? null : val == TriStateBool.True ? (bool?)true : (bool?)false;
    }

    public static bool AsBool(this object val, bool dflt = false, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return dflt;

        if (val is bool) return (bool)val;
        else if (val is string)
        {
          var sval = ((string)val).Trim();

          if (string.Equals("true", sval, StringComparison.InvariantCultureIgnoreCase) ||
              string.Equals("yes", sval, StringComparison.InvariantCultureIgnoreCase) ||
              string.Equals("t", sval, StringComparison.InvariantCultureIgnoreCase) ||
              string.Equals("y", sval, StringComparison.InvariantCultureIgnoreCase) ||
              string.Equals("on", sval, StringComparison.InvariantCultureIgnoreCase) ||
              string.Equals("ok", sval, StringComparison.InvariantCultureIgnoreCase) ||
              string.Equals("pass", sval, StringComparison.InvariantCultureIgnoreCase) ||
              string.Equals("1", sval, StringComparison.InvariantCultureIgnoreCase)
             ) return true;

          if (long.TryParse(sval, out long ival)) return ival != 0;

          if (double.TryParse(sval, out double dval)) return dval != 0;

          if (decimal.TryParse(sval, out decimal dcval)) return dcval != 0;
        }
        else if (val is TriStateBool tsval)  return tsval == TriStateBool.True;
        else if (val is char c)              return c == 'T' || c == 't' || c == 'Y' || c == 'y' || c == '1';
        else if (val is sbyte sbval)         return sbval != 0;
        else if (val is int ival)            return ival != 0;
        else if (val is short sval)          return sval != 0;
        else if (val is long lval)           return lval != 0L;
        else if (val is byte bval)           return bval != 0;
        else if (val is uint uival)          return uival != 0u;
        else if (val is ushort usval)        return usval != 0;
        else if (val is ulong ulval)         return ulval != 0ul;
        else if (val is float fval)          return fval != 0f;
        else if (val is double dval)         return dval != 0d;
        else if (val is decimal dcval)       return dcval != 0m;
        else if (val is TimeSpan tspval)     return tspval.Ticks != 0;
        else if (val is DateTime dtval)      return dtval.Ticks != 0;

        return Convert.ToBoolean(val, INVARIANT);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static bool? AsNullableBool(this object val, bool? dflt = null, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return null;

        if (val is TriStateBool) return ((TriStateBool)val).AsNullableBool();

        return val.AsBool(false, ConvertErrorHandling.Throw);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static Guid AsGUID(this object val, Guid dflt, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return dflt;

        if (val is string)
        {
          var sval = (string)val;

          return Guid.Parse(sval);
        }
        else if (val is byte[])
        {
          var arr = (byte[])val;
          return arr.GuidFromNetworkByteOrder();
        }
        else
          return (Guid)val;
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static Guid? AsNullableGUID(this object val, Guid? dflt = null, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return null;
        return val.AsGUID(dflt ?? Guid.Empty, ConvertErrorHandling.Throw);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static DateTime AsDateTime(this object val, System.Globalization.DateTimeStyles styles = System.Globalization.DateTimeStyles.None)
    {
      if (val is string)
      {
        var sval = ((string)val).Trim();

        if (DateTime.TryParse(sval, null, styles, out var dtval)) return dtval;

        long ival;
        if (long.TryParse(sval, out ival)) return ival.FromSecondsSinceUnixEpochStart();

        double dval;
        if (double.TryParse(sval, out dval)) return ((long)dval).FromSecondsSinceUnixEpochStart();

        decimal dcval;
        if (decimal.TryParse(sval, out dcval)) return ((long)dcval).FromSecondsSinceUnixEpochStart();
      }

      if (val is int _int) { return ((long)_int).FromSecondsSinceUnixEpochStart(); }
      if (val is uint _uint) { return ((long)_uint).FromSecondsSinceUnixEpochStart(); }
      if (val is long _long) { return (_long).FromSecondsSinceUnixEpochStart(); }
      if (val is ulong _ulong) { return (_ulong).FromSecondsSinceUnixEpochStart(); }

      if (val is double _double) { return ((long)_double).FromSecondsSinceUnixEpochStart(); }
      if (val is float _float) { return ((long)_float).FromSecondsSinceUnixEpochStart(); }
      if (val is decimal _decimal) { return ((long)_decimal).FromSecondsSinceUnixEpochStart(); }

      return Convert.ToDateTime(val, INVARIANT);
    }

    public static DateTime AsDateTime(this object val,
                                      DateTime dflt,
                                      ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault,
                                      System.Globalization.DateTimeStyles styles = System.Globalization.DateTimeStyles.None)
    {
      try
      {
        if (val == null) return dflt;
        return val.AsDateTime(styles);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static DateTime? AsNullableDateTime(this object val,
                                               DateTime? dflt = null,
                                               ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault,
                                               System.Globalization.DateTimeStyles styles = System.Globalization.DateTimeStyles.None)
    {
      try
      {
        if (val == null) return null;
        return val.AsDateTime(styles);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static GDID AsGDID(this object val)
    {
      if (val == null) return GDID.ZERO;

      if (val is GDID gdval) return gdval;
      if (val is ELink elval) return elval.GDID;

      if (val is string sval)
      {
        if (GDID.TryParse(sval, out GDID gdid)) return gdid;

        try
        {
          var elink = new ELink(sval);
          return elink.GDID;
        }
        catch { }
      }

      if (val is ulong ulval) { return new GDID(0, ulval); }
      if (val is byte[] bval) { return new GDID(bval); }

      return new GDID(0, Convert.ToUInt64(val, INVARIANT));
    }

    public static GDID AsGDID(this object val, GDID dflt, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return dflt;
        return val.AsGDID();
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static GDID? AsNullableGDID(this object val, GDID? dflt = null, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return null;
        return val.AsGDID();
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }


    public static RGDID AsRGDID(this object val)
    {
      if (val == null) return RGDID.ZERO;

      if (val is RGDID rgdval) return rgdval;

      if (val is string sval)
      {
        if (RGDID.TryParse(sval, out RGDID rgdid)) return rgdid;
      }

      if (val is byte[] bval) { return new RGDID(bval); }

      throw new AzosException($"AsRGDID({val.GetType().DisplayNameWithExpandedGenericArgs()})");
    }

    public static RGDID AsRGDID(this object val, RGDID dflt, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return dflt;
        return val.AsRGDID();
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static RGDID? AsNullableRGDID(this object val, RGDID? dflt = null, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return null;
        return val.AsRGDID();
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }


    //20160622 DKh
    public static GDIDSymbol AsGDIDSymbol(this object val)
    {
      if (val == null) return new GDIDSymbol();

      if (val is GDIDSymbol) return (GDIDSymbol)val;
      if (val is GDID) return new GDIDSymbol((GDID)val, val.ToString());

      if (val is ELink) return ((ELink)val).AsGDIDSymbol;

      if (val is string)
      {
        var sval = ((string)val).Trim();

        if (GDID.TryParse(sval, out GDID gdid)) return new GDIDSymbol(gdid, sval);

        try
        {
          var elink = new ELink(sval);
          return elink.AsGDIDSymbol;
        }
        catch { }
      }

      if (val is ulong) { return new GDIDSymbol(new GDID(0, (ulong)val), val.ToString()); }
      if (val is byte[])
      {
        var buf = (byte[])val;
        return new GDIDSymbol(new GDID(buf), buf.ToDumpString(DumpFormat.Hex));
      }
      return new GDIDSymbol(new GDID(0, Convert.ToUInt64(val, INVARIANT)), val.ToString());
    }

    public static GDIDSymbol AsGDIDSymbol(this object val, GDIDSymbol dflt, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return dflt;
        return val.AsGDIDSymbol();
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static GDIDSymbol? AsNullableGDIDSymbol(this object val, GDIDSymbol? dflt = null, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return null;
        return val.AsGDIDSymbol();
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static TimeSpan AsTimeSpan(this object val)
    {
      return val.AsTimeSpan(TimeSpan.FromSeconds(0), ConvertErrorHandling.Throw);
    }

    public static TimeSpan AsTimeSpan(this object val, TimeSpan dflt, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return dflt;

        if (val is string)
        {
          var sval = (string)val;

          long ival;
          if (long.TryParse(sval, out ival)) return new TimeSpan(ival);

          double dval;
          if (double.TryParse(sval, out dval)) return new TimeSpan((long)dval);

          decimal dcval;
          if (decimal.TryParse(sval, out dcval)) return new TimeSpan((long)dcval);

          TimeSpan tsval;
          if (TimeSpan.TryParse(sval, out tsval)) return tsval;
        }

        var ticks = Convert.ToInt64(val, INVARIANT);

        return new TimeSpan(ticks);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static TimeSpan? AsNullableTimeSpan(this object val, TimeSpan? dflt = null, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return null;
        return val.AsTimeSpan(TimeSpan.FromSeconds(0), ConvertErrorHandling.Throw);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static TEnum AsEnum<TEnum>(this object val, TEnum dflt, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    where TEnum : struct
    {
      try
      {
        if (val == null) return dflt;

        if (val is string)
        {
          var sval = (string)val;

          return (TEnum)Enum.Parse(typeof(TEnum), sval, true);
        }

        val = Convert.ToInt32(val, INVARIANT);
        return (TEnum)val;
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static TEnum? AsNullableEnum<TEnum>(this object val, TEnum? dflt = null, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    where TEnum : struct
    {
      try
      {
        if (val == null) return null;
        return val.AsEnum(default(TEnum), ConvertErrorHandling.Throw);
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static Uri AsUri(this object val, Uri dflt = null, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return null;
        if (val is Uri) return (Uri)val;
        if (val is string)
        {
          var uri = (string)val;
          if (uri.IsNullOrWhiteSpace()) return null;
          return new Uri(uri);
        }
        throw new AzosException("{0}.AsUri".Args(val.GetType().Name));
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static Atom AsAtom(this object val)
    {
      if (val == null) return Atom.ZERO;

      if (val is Atom existing) return existing;

      if (val is ulong ul) return new Atom(ul);

      if (val is string str)
      {
        str = str.Trim();

        if (Atom.TryEncodeValueOrId(str, out var atom)) return atom;
      }

      return new Atom(Convert.ToUInt64(val, INVARIANT));
    }

    public static Atom AsAtom(this object val, Atom dflt, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return dflt;
        return val.AsAtom();
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static Atom? AsNullableAtom(this object val, Atom? dflt = null, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return null;
        return val.AsAtom();
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }


    public static EntityId AsEntityId(this object val)
    {
      if (val == null) return new EntityId();

      if (val is EntityId existing) return existing;

      if (val is string str)
      {
        str = str.Trim();
        return EntityId.Parse(str);
      }

      return EntityId.Parse(Convert.ToString(val, INVARIANT));
    }

    public static EntityId AsEntityId(this object val, EntityId dflt, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return dflt;
        return val.AsEntityId();
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

    public static EntityId? AsNullableEntityId(this object val, EntityId? dflt = null, ConvertErrorHandling handling = ConvertErrorHandling.ReturnDefault)
    {
      try
      {
        if (val == null) return null;
        return val.AsEntityId();
      }
      catch
      {
        if (handling != ConvertErrorHandling.ReturnDefault) throw;
        return dflt;
      }
    }

  }
}
