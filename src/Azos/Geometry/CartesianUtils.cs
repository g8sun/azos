/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Azos.Geometry
{
  /// <summary>
  /// Map direction enumeration aka. North, South, East, etc...
  /// </summary>
  public enum MapDirection
  {
    North = 0,
    NorthEast,
    East,
    SouthEast,
    South,
    SouthWest,
    West,
    NorthWest
  }

  public static class CartesianUtils
  {
    /// <summary>
    /// Represents count of degrees in PI
    /// </summary>
    public const double PI_DEGREES = 180D;

    /// <summary>
    /// Pi*2 constant
    /// </summary>
    public const double PI2 = Math.PI * 2d;

    /// <summary>
    /// Pi/2 constant
    /// </summary>
    public const double PI_HALF = Math.PI / 2d;

    /// <summary>
    /// Converts degrees into radians
    /// </summary>
    public static double ToRad(this double degrees)
    {
      return (Math.PI / PI_DEGREES) * degrees;
    }

    /// <summary>
    /// Converts radians into degrees
    /// </summary>
    public static double ToDeg(this double radians)
    {
      return radians / Math.PI * PI_DEGREES;
    }

    /// <summary>
    /// Calculates distance between two points
    /// </summary>
    public static int Distance(Point p1, Point p2) => (int)Distance(p1.X, p1.Y, p2.X, p2.Y);

    /// <summary>
    /// Calculates distance between two points in Euclidean space
    /// </summary>
    public static double Distance(double x1, double y1, double x2, double y2)
      => Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));

    /// <summary>
    /// Calculates distance between two points
    /// </summary>
    public static double Distance(PointF p1, PointF p2) => Distance(p1.X, p1.Y, p2.X, p2.Y);

    // <summary>
    /// Calculates azimuth for vector in rads
    /// </summary>
    public static double AzimuthRad(double centerX, double centerY, double pointX, double pointY)
    {
      double theta = Math.Atan2(pointY - centerY, pointX - centerX);

      if (theta < 0)  // provide 0 - 2Pi "experience"
        theta = 2 * Math.PI + theta;

      return theta;
    }

    /// <summary>
    /// Calculates azimuth for vector in degrees
    /// </summary>
    public static double AzimuthDeg(int centerX, int centerY, int pointX, int pointY)
    {
      return (AzimuthRad(centerX, centerY, pointX, pointY) / Math.PI) * 180D;
    }

    /// <summary>
    /// Calculates azimuth for vector in degrees
    /// </summary>
    public static int AzimuthOfRadix(int centerX, int centerY, int pointX, int pointY, int radix)
    {
      if (radix < 2) radix = 2;
      var angle = AzimuthRad(centerX, centerY, pointX, pointY);
      var half = Math.PI / (double)radix;
      angle = WrapAngle(angle, half);
      return (int)(Math.Floor((angle / PI2) * radix));
    }

    /// <summary>
    /// Converts Point to polar coordinate point
    /// </summary>
    public static PolarPoint PointToPolarPoint(Point center, Point point)
    {
      return VectorToPolarPoint(center.X, center.Y, point.X, point.Y);
    }

    /// <summary>
    /// Converts Point to polar coordinate point
    /// </summary>
    public static PolarPoint PointToPolarPoint(PointF center, PointF point)
    {
      return VectorToPolarPoint(center.X, center.Y, point.X, point.Y);
    }

    /// <summary>
    /// Converts vector in 'x1, y1, x2, y2' representation to polar coordinate point
    /// </summary>
    public static PolarPoint VectorToPolarPoint(int centerX, int centerY, int pointX, int pointY)
    {
      double dist = Distance(centerX, centerY, pointX, pointY);

      double theta = AzimuthRad(centerX, centerY, pointX, pointY);

      return new PolarPoint(dist, theta);
    }

    /// <summary>
    /// Converts vector in 'x1, y1, x2, y2' representation to polar coordinate point
    /// </summary>
    public static PolarPoint VectorToPolarPoint(double centerX, double centerY, double pointX, double pointY)
    {
      double dist = Distance(centerX, centerY, pointX, pointY);

      double theta = AzimuthRad(centerX, centerY, pointX, pointY);

      return new PolarPoint(dist, theta);
    }

    /// <summary>
    /// Modifies an angle by delta value ensuring that resulting angle is always between 0 and 2Pi
    /// </summary>
    public static double WrapAngle(double angle, double delta)
    {
      delta = delta % PI2;

      if (delta<0)
        delta =  PI2 + delta;

      double result = angle + delta;

      return result % PI2;
    }



    /// <summary>
    /// Returns a point of intersection between a ray cast from the center of a rectangle
    ///  under certain polar coordinate angle and a rectangle side
    /// </summary>
    public static Point FindRayFromRectangleCenterSideIntersection(Rectangle rect, double theta)
    {
        Point center = new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);

        double rayLength = rect.Width > rect.Height ? rect.Width : rect.Height; //make ray "infinite" in comparison to rect

        if (rayLength < 100) rayLength = 100; //safeguard

        PolarPoint rayEnd = new PolarPoint(rayLength, theta);//create ray "end" point

        double k = (rayEnd.Point.X!=0)? ((double)rayEnd.Point.Y) / ((double)rayEnd.Point.X) : 0; //get line incline  aka. y = kx

        int x, y;

        List<Point> lst = new List<Point>();

        //north
        x = center.X + ((k != 0) ? (int)((rect.Top - center.Y) / k) : 0);
        y = rect.Top;
        if ((x >= rect.Left) &&
            (x <= rect.Right))
           lst.Add(new Point(x, y));

        //south
        x = center.X + ((k != 0) ? (int)((rect.Bottom - center.Y) / k) : 0);
        y = rect.Bottom;
        if ((x >= rect.Left) &&
            (x <= rect.Right))
          lst.Add(new Point(x, y));

        //east
        x = rect.Right;
        y = center.Y + (int)(k * (rect.Right - center.X));
        if ((y >= rect.Top) &&
            (y <= rect.Bottom))
          lst.Add(new Point(x, y));

        //west
        x = rect.Left;
        y = center.Y + (int)(k * (rect.Left - center.X));
        if ((y >= rect.Top) &&
            (y <= rect.Bottom))
          lst.Add(new Point(x, y));

        Point minPoint = new Point(int.MaxValue, int.MaxValue);
        int minDistance = int.MaxValue;

        Point re = rayEnd.Point; //rayEnd is relative to absolute 0,0
        re.Offset(center.X, center.Y); // need to make relative to rectangle center

        foreach (Point p in lst) //find closest point
        {
           int dst = Distance(p, re);

           if (dst < minDistance)
           {
             minPoint = p;
             minDistance = dst;
           }
        }

        return minPoint;
      }


    /// <summary>
    /// Converts map direction to angular coordinate in radians
    /// </summary>
    public static double MapDirectionToAngle(MapDirection direction)
    {
       switch (direction)
       {
         case MapDirection.North:  return 4d/16d * PI2;
         case MapDirection.South:  return 12d/16d * PI2;
         case MapDirection.East:   return 0.0d;
         case MapDirection.West:   return 8d/16d * PI2;

         case MapDirection.NorthEast: return 2d/16d * PI2;
         case MapDirection.NorthWest: return 6d/16d * PI2;
         case MapDirection.SouthEast: return 14d/16d * PI2;
         case MapDirection.SouthWest: return 10d/16d * PI2;

         default: return 0.0d;
       }
    }

    /// <summary>
    /// Converts a radian angular coordinate into map direction
    /// </summary>
    public static MapDirection AngleToMapDirection(double angle)
    {
       angle = WrapAngle(angle, 0);

       if ((angle >= 0.0d) && (angle < PI2 * 1d/16d)) return MapDirection.East;
       else
       if ((angle >= PI2 * 1d/16d) && (angle < PI2 * 3d/16d)) return MapDirection.NorthEast;
       else
       if ((angle >= PI2 * 3d / 16d) && (angle < PI2 * 5d / 16d)) return MapDirection.North;
       else
       if ((angle >= PI2 * 5d / 16d) && (angle < PI2 * 7d / 16d)) return MapDirection.NorthWest;
       else
       if ((angle >= PI2 * 7d / 16d) && (angle < PI2 * 9d / 16d)) return MapDirection.West;
       else
       if ((angle >= PI2 * 9d / 16d) && (angle < PI2 * 11d / 16d)) return MapDirection.SouthWest;
       else
       if ((angle >= PI2 * 11d / 16d) && (angle < PI2 * 13d / 16d)) return MapDirection.South;
       else
       if ((angle >= PI2 * 13d / 16d) && (angle < PI2 * 15d / 16d)) return MapDirection.SouthEast;
       else
         return MapDirection.East;

    }

    /// <summary>
    /// Calculates an area of an inner rectangle that violates outside perimeter
    /// </summary>
    public static int CalculatePerimeterViolationArea(Rectangle perimeter, Rectangle inner)
    {
      int ix = 0;
      int iy = 0;

      if (inner.Left<perimeter.Left) ix += perimeter.Left - inner.Left;
      if (inner.Right>perimeter.Right) ix += inner.Right - perimeter.Right;

      if (inner.Top < perimeter.Top) iy += perimeter.Top - inner.Top;
      if (inner.Bottom > perimeter.Bottom) iy += inner.Bottom - perimeter.Bottom;

      return (ix * inner.Height) + (iy * inner.Width);
    }

  }//class

}
