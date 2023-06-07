/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Text;
using System.Threading;

namespace Azos.Platform
{
//todo: Add PAL-based platform-specific entropy feed

  /// <summary>
  /// Represents a random generator which is based on System.Random() yet has an ability to feed external samples into it.
  ///  Use RandomGenerator.Instance to use the default thread-safe instance or use App.Random shortcut
  /// </summary>
  /// <remarks>
  /// Introduces external entropy into the generation sequence by adding a sample into the ring buffer.
  /// Call FeedExternalEntropySample(int sample) method from places that have true entropy values, i.e.
  ///  a network-related code may have good entropy sources in server applications.
  ///  External entropy sources may rely on user-dependent actions, i.e.:
  ///   number of bytes/requests received per second, dollar(or cent remainders) amount of purchases made (on a server),
  ///   zip codes of customers, IP addresses of site visitors, average noise level sampled on an open WAVE device(microphone),
  ///    mouse position (i.e. in GUI app) etc...
  ///  This class MAY be crypto-safe if it is fed a good entropy data at high rate, however that depends on the use pattern.
  ///  The framework implementation feeds some entropy from Glue and cache components infrequently (once every few seconds),
  ///   which is definitely not strong for cryptography
  /// </remarks>
  public sealed class RandomGenerator
  {
      private static readonly int BUFF_SIZE = 1024 + IntUtils.Align8((int)DateTime.Now.Ticks & 0x0fff);

      private static RandomGenerator s_Instance;

      /// <summary>
      /// Returns the default process-wide instance of the generator. This instance is thread-safe
      /// </summary>
      public static RandomGenerator Instance
      {
        get
        {
          if (s_Instance==null)//no need to lock, 2nd copy is ok
            s_Instance = new RandomGenerator();
          return s_Instance;
        }
      }

      /// <summary>
      /// Create new instance of ExternalRandomGenerator. Create new instances only if you need to use different sample ring buffers.
      /// In majority of cases use ExternalRandomGenerator.Instance to use default instance instead of creating a new instance.
      /// Default instance is thread-safe for process-wide use
      /// </summary>
      public RandomGenerator()
      {
        m_Buffer = new int[BUFF_SIZE];
        lock(s_GlobalRandom)
        {
          for(int i=0; i<BUFF_SIZE; i++)
            m_Buffer[i] = Guid.NewGuid().GetHashCode();

          for(int i=0; i<BUFF_SIZE; i++)
            m_Buffer[i] ^= s_GlobalRandom.Next(Int32.MinValue, Int32.MaxValue);
        }
      }


      private static Random s_GlobalRandom = new Random();

      [ThreadStatic] private static Random ts_Random;

      private int[] m_Buffer;

      private int m_ReadPosition;
      private int m_WritePosition;

      /// <summary>
      /// Generates next random integer in the Int32.MinValue..Int32.MaxValue range
      /// </summary>
      public int NextRandomInteger
      {
        get
        {
          if (ts_Random==null)
            lock(s_GlobalRandom)
              ts_Random = new Random(s_GlobalRandom.Next());

          var position = Interlocked.Increment(ref m_ReadPosition);
          if(position>=BUFF_SIZE)
          {
            position = 0;
            m_ReadPosition = 0;//no need to lock. Its ok if another thread does not see this instantly
          }
          return m_Buffer[position] ^ ts_Random.Next(Int32.MinValue, Int32.MaxValue);
        }
      }

      /// <summary>
      /// Generates next random integer in the Uint32.MinValue..Uint32.MaxValue diapason
      /// </summary>
      public uint NextRandomUnsignedInteger
      {
        get { return (uint)NextRandomInteger; }
      }

      /// <summary>
      /// Generates next random ulong in the Uint64.MinValue..Uint64.MaxValue range
      /// </summary>
      public ulong NextRandomUnsignedLong
      {
        get { return (((ulong)NextRandomUnsignedInteger) << 32) + (ulong)NextRandomUnsignedInteger; }
      }

      /// <summary>
      /// Generates random byte[16] buffer
      /// </summary>
      public byte[] NextRandom16Bytes
      {
        get
        {
          var arr = new byte[16];

          arr.WriteBEInt32(0,  this.NextRandomInteger);
          arr.WriteBEInt32(4,  this.NextRandomInteger);
          arr.WriteBEInt32(8,  this.NextRandomInteger);
          arr.WriteBEInt32(12, this.NextRandomInteger);

          return arr;
        }
      }

      /// <summary>
      /// Returns 0..1 random double positive coefficient
      /// </summary>
      public double NextRandomDouble
      {
        get { return ((uint)NextRandomInteger) / ((double)uint.MaxValue); }
      }

      /// <summary>
      /// Generates random double number in min..max range
      /// </summary>
      public double NextScaledRandomDouble(double bound1, double bound2 = 0)
      {
        var min = bound1<bound2 ? bound1 : bound2;
        var max = bound1>bound2 ? bound1 : bound2;

        var val = NextRandomInteger;

        var ratio = (UInt32)val / (double)UInt32.MaxValue;

        return min + ((max - min) * ratio);
      }

      /// <summary>
      /// Introduces external entropy into the generation sequence by adding a sample into the ring buffer.
      /// WARNING: Business app developers should not call this method as it skews the generator entropy and must only
      /// be called from places where TRUE unpredictable entropy exists e.g.:
      ///  a network-related code may have good entropy sources in server applications.
      ///  External entropy sources may rely on user-dependent actions, i.e.:
      ///  number of bytes/requests received per second, dollar(or cent remainders) amount of purchases made (assuming large product catalog and price variation),
      ///  IP addresses of site visitors (assuming distributed system with many clients connecting), average noise level sampled on an open WAVE device(microphone),
      ///  mouse position (i.e. in GUI app) etc...
      /// Special care should be taken to prevent entropy steering attempt by users who try to flood the system with repetitive actions.
      /// Event debouncing, inconsistent a-periodical sampling, and entropy multiplexing from various streams should be used
      /// </summary>
      public void FeedExternalEntropySample(int sample)
      {
        var position = Interlocked.Increment(ref m_WritePosition);
        if(position>=BUFF_SIZE)
        {
          position = 0;
          m_WritePosition = 0;//no need to lock. Its ok if another thread does not see this instantly
        }
        m_Buffer[position] = sample;
      }

      /// <summary>
      /// Generates random number in min..max range
      /// </summary>
      public int NextScaledRandomInteger(int bound1, int bound2 = 0)
      {
        var min = bound1<bound2 ? bound1 : bound2;
        var max = bound1>bound2 ? bound1 : bound2;

        var val = NextRandomInteger;

        var ratio = (UInt32)val / (double)UInt32.MaxValue;

        return min + (int)((max - min) * ratio);
      }


      private static readonly char[] CHAR_DICT = new char[]
      {
        'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z', //26
        'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z', //26  52
        '0','1','2','3','4','5','6','7','8','9','-','_', //12 64
        'A','Z','T','W','7','3','9', 'q', '-', 'r', 'x', //12 76
        'j','z','R'                                      //3  79 (prime)
      };

      private static readonly int CHAR_DICT_LEN = CHAR_DICT.Length;


      /// <summary>
      /// Generates a random string of chars which are safe for the use on the web -
      ///  a string that only contains "a-z"/"A-Z" and "0-9" and "-"/"_" chars, i.e.: "bo7O0EFasZe-wEty9w0__JiOKk81".
      /// The length of the string can not be less than 4 and more than 1024 chars
      /// </summary>
      public string NextRandomWebSafeString(int minLength = 16, int maxLength = 32)
      {
        const int MIN_LEN = 4;
        const int MAX_LEN = 1024;

        if (minLength<MIN_LEN) minLength = MIN_LEN;
        if (maxLength>MAX_LEN) maxLength = MAX_LEN;

        var count = minLength;
        if (maxLength>minLength) count += this.NextScaledRandomInteger(0, maxLength - minLength);

        var result = new StringBuilder(count);

        for(var i=0; i<count; i++)
          result.Append( CHAR_DICT[ (this.NextRandomInteger & CoreConsts.ABS_HASH_MASK) % CHAR_DICT_LEN ]);

        return result.ToString();
      }

      /// <summary>
      /// Generates a random secure buffer of chars which are safe for the use on the web -
      ///  a string that only contains "a-z"/"A-Z" and "0-9" and "-"/"_" chars, i.e.: "bo7O0EFasZe-wEty9w0__JiOKk81".
      /// The length of the string can not be less than 4 and more than 1024 chars
      /// </summary>
      public Security.SecureBuffer NextRandomWebSafeSecureBuffer(int minLength = 16, int maxLength = 32)
      {
        const int MIN_LEN = 4;
        const int MAX_LEN = 1024;

        if (minLength<MIN_LEN) minLength = MIN_LEN;
        if (maxLength>MAX_LEN) maxLength = MAX_LEN;

        var count = minLength;
        if (maxLength>minLength) count += this.NextScaledRandomInteger(0, maxLength - minLength);

        var result = new Security.SecureBuffer(count);

        for (var i=0; i<count; i++)
        {
          var b = (byte)CHAR_DICT[(this.NextRandomInteger & CoreConsts.ABS_HASH_MASK) % CHAR_DICT_LEN];
          result.Push(b);
        }
        result.Seal();
        return result;
      }

      /// <summary>
      /// Generates a random buffer of bytes
      /// </summary>
      public byte[] NextRandomBytes(int length) { return NextRandomBytes(length, length); }

      /// <summary>
      /// Generates a random buffer of bytes
      /// </summary>
      public byte[] NextRandomBytes(int minLength, int maxLength)
      {
        if (minLength < 0) minLength = 0;
        var count = minLength;
        if (maxLength > minLength) count = this.NextScaledRandomInteger(minLength, maxLength);

        var bytes = new byte[count];

        var i = 0;
        for (; i < (count / 4) * 4; i += 4)
          bytes.WriteBEInt32(i, this.NextRandomInteger);

        if (i != count)
        {
          var last = new byte[4];
          last.WriteBEInt32(this.NextRandomInteger);
          for (; i < count; i++)
            bytes[i] = last[i % 4];
        }

        return bytes;
      }

      /// <summary>
      /// Generates a random secure buffer of bytes
      /// </summary>
      public Security.SecureBuffer NextRandomSecureBuffer(int length) { return NextRandomSecureBuffer(length, length); }

      /// <summary>
      /// Generates a random secure buffer of bytes
      /// </summary>
      public Security.SecureBuffer NextRandomSecureBuffer(int minLength, int maxLength)
      {
        if (minLength < 0) minLength = 0;
        var count = minLength;
        if (maxLength > minLength) count = this.NextScaledRandomInteger(minLength, maxLength);

        var buffer = new Security.SecureBuffer(count);

        var bytes = new byte[4];
        for (var i = 0; i < count; i += 4)
        {
          bytes.WriteBEInt32(this.NextRandomInteger);
          for (var j = 0; j < 4 && i + j < count; j++)
          {
            buffer.Push(bytes[j]);
            bytes[j] = 0;
          }
        }
        buffer.Seal();
        return buffer;
      }
  }
}
