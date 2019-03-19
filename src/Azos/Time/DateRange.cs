﻿using System;
using System.IO;
using System.Collections;
using Azos.Serialization.JSON;

namespace Azos.Time
{
  /// <summary>
  /// Represents a range of dates denoted by start/end date/times
  /// </summary>
  public struct DateRange : IEquatable<DateRange>, IJsonWritable, IFormattable
  {
    /// <summary>
    /// Create a range, at least one component is required. If both are specified both need to be in the same timezone and
    /// end should be greater than the start
    /// </summary>
    public DateRange(DateTime? start, DateTime? end)
    {
      if (!start.HasValue && ! end.HasValue)
        throw new TimeException(StringConsts.ARGUMENT_ERROR + nameof(DateRange)+".ctor(start.null & end.null)");

      if (start.HasValue && end.HasValue && start.Value.Kind != end.Value.Kind)
        throw new TimeException(StringConsts.ARGUMENT_ERROR + nameof(DateRange) + ".ctor(start.Kind != end.Kind)");

      if (end<start)
        throw new TimeException(StringConsts.ARGUMENT_ERROR + nameof(DateRange) + ".ctor(end < start)");

      Start = start;
      End = end;
    }

    /// <summary>
    /// Start date or null, if this is null then end date may not be null
    /// </summary>
    public readonly DateTime? Start;

    /// <summary>
    /// End date or null, if this is null then start date is not null
    /// </summary>
    public readonly DateTime? End;


    /// <summary>
    /// Returns true if neither dates are set
    /// </summary>
    public bool IsUnassigned => !Start.HasValue && !End.HasValue;

    /// <summary>
    /// Specified the kind of the range: Unspecified|Local|Utc
    /// </summary>
    public DateTimeKind Kind => Start.HasValue ? Start.Value.Kind : End.HasValue ? End.Value.Kind : DateTimeKind.Unspecified;


    /// <summary>
    /// Returns true if range is closed - both start and end are defined vs .IsOpen when only start or only end are defined
    /// </summary>
    public bool IsClosed => Start.HasValue && End.HasValue;

    /// <summary>
    /// Returns true if range is open - when either start or end are defined, but not both; vs .IsClosed
    /// </summary>
    public bool IsOpen => !Start.HasValue || !End.HasValue;

    /// <summary>
    /// Returns null for open ranges, or time spans for closed
    /// </summary>
    public TimeSpan? ClosedSpan => IsOpen ? null : End - Start;

    /// <summary>
    /// Returns true only when the range is closed and specified date is of the same kind as the range
    /// and the date is between Start and End inclusive
    /// </summary>
    public bool Contains(DateTime value)
    {
      if (this.Kind != value.Kind) return false;
      if (IsUnassigned) return false;
      return Start.HasValue && End.HasValue && value >= Start.Value && value <= End.Value;
    }

    /// <summary>
    /// Intersects this range with another one. Returns null if they do not intersect
    /// </summary>
    public DateRange? Intersect(DateRange other)
    {
      if (IsUnassigned || other.IsUnassigned) return null;

      if (this.Kind != other.Kind)
        throw new TimeException(StringConsts.ARGUMENT_ERROR + $"{nameof(DateRange)}.{nameof(Intersect)}(this.Kind!=other.Kind)");

      var left = DateTime.MinValue;
      var right = DateTime.MaxValue;

      if (this.Start.HasValue) left = this.Start.Value;
      if (other.Start.HasValue && other.Start.Value > left) left = other.Start.Value;

      if (this.End.HasValue) right = this.End.Value;
      if (other.End.HasValue && other.End.Value < right) right = other.End.Value;

      if (right<left) return null;

      return new DateRange(left>DateTime.MinValue ? left : (DateTime?)null,
                           right < DateTime.MaxValue ? right : (DateTime?)null);
    }

    public bool Equals(DateRange other) => this.Start==other.Start && this.End==other.End;

    public override bool Equals(object obj)
    {
      if (obj is DateRange other) return this.Equals(other);
      return false;
    }

    public override int GetHashCode() => Start.GetHashCode() ^ End.GetHashCode();

    public static bool operator ==(DateRange lhs, DateRange rhs) =>  lhs.Equals(rhs);
    public static bool operator !=(DateRange lhs, DateRange rhs) => !lhs.Equals(rhs);


    public override string ToString()
    {
      return "[{0} - {1}]".Args(Start.HasValue ? Start.Value.ToString() : " ",
                                End.HasValue ? End.Value.ToString() : " ");
    }

    public string ToString(string format, IFormatProvider formatProvider)
    {
      return "[{0} - {1}]".Args(Start.HasValue ? Start.Value.ToString(format, formatProvider) : " ",
                                End.HasValue ? End.Value.ToString(format, formatProvider) : " ");
    }

    void IJsonWritable.WriteAsJson(TextWriter wri, int nestingLevel, JsonWritingOptions options)
    {
      JsonWriter.WriteMap(wri, nestingLevel, options, new DictionaryEntry("start", Start), new DictionaryEntry("end", End));
    }
  }


  /// <summary>
  /// Provides extensions for create DateRange such as normalizing time zones
  /// </summary>
  public static class DateRangeExtensions
  {
    /// <summary>
    /// Create a date range converting each component to UTC and ordering start/end pair as of specified ILocalizedTimeProivder
    /// </summary>
    public static DateRange MakeUtcDateRange(this ILocalizedTimeProvider provider, DateTime? t1, DateTime? t2)
    {
      provider.NonNull(nameof(provider));
      if (t1.HasValue && t1.Value.Kind != DateTimeKind.Utc) t1 = provider.LocalizedTimeToUniversalTime(t1.Value);
      if (t2.HasValue && t2.Value.Kind != DateTimeKind.Utc) t2 = provider.LocalizedTimeToUniversalTime(t2.Value);

      if (t1.HasValue && t2.HasValue && t2.Value < t1.Value)
      {
        var t = t2;
        t2 = t1;
        t1 = t;
      }
      return new DateRange(t1, t2);
    }

    /// <summary>
    /// Create a date range converting each component to local time and ordering start/end pair as of specified ILocalizedTimeProivder
    /// </summary>
    public static DateRange MakeLocalDateRange(this ILocalizedTimeProvider provider, DateTime? t1, DateTime? t2)
    {
      provider.NonNull(nameof(provider));
      if (t1.HasValue && t1.Value.Kind == DateTimeKind.Utc) t1 = provider.UniversalTimeToLocalizedTime(t1.Value);
      if (t2.HasValue && t2.Value.Kind == DateTimeKind.Utc) t2 = provider.UniversalTimeToLocalizedTime(t2.Value);

      if (t1.HasValue && t2.HasValue && t2.Value < t1.Value)
      {
        var t = t2;
        t2 = t1;
        t1 = t;
      }
      return new DateRange(t1, t2);
    }

  }


}
