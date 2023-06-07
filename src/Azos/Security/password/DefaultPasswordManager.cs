/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;
using System.Linq;

using System.Text;
using System.Collections.Generic;

using Azos.Apps;
using Azos.Conf;
using Azos.Collections;

namespace Azos.Security
{
  /// <summary>
  /// Provides default implementation for password management functionality based on injectable algorithms and default password strength calculation
  /// </summary>
  public class DefaultPasswordManager : DaemonWithInstrumentation<ISecurityManagerImplementation>, IPasswordManagerImplementation
  {
    #region CONSTS
      public const string CONFIG_ALGORITHM_SECTION = "algo";

      private const int CREDIT_CHAR_PRESENT = 10;

      private const int CREDIT_CASE_MIX = 30;
      private const int CREDIT_DIGIT_MIX = 35;
      private const int CREDIT_SYMBOL_MIX = 50;

      private const int CREDIT_TYPE_TRANSITION = 12;

      private const int DEBIT_CHAR_REPEAT = 9;
      private const int DEBIT_ADJACENT_CHAR = 7;

      private const int DEBIT_COMMON_WORD = 30;

      public static readonly string[] DEFAULT_COMMON_WORDS =
      {
        "noah","jesus","liam","jacob","mason","william","ethan","michael","alex","john","jack","jayden","daniel","bill","rick","frank","fred","mike","mark","jason","jeff","eugene",
        "dave","david","robert","roger","jerry","justin","elvis","adam","abraham","george","winston","jordan","peter","paul","joseph","jacob","nick","bob","rich","chris","greg",
        "tim", "charlie", "thomas", "sam", "pat", "drew", "don", "phil",

        "sophia", "emma","olivia","ava", "mia", "zoe", "lisa", "emily","abigail","madison","elizabeth","tanya","tonya","suzi","anna","sarah","maggie","helena","marilyn","mary","cheryl","sheryl",
        "jen", "lily", "ella", "aria", "chloe", "kay", "lee", "madelyn", "julia", "jasmine",

        "god", "lord", "budda","buddha", "muhammad", "evil", "hell", //CHRISt , dEVIL

        "sinatra","monroe","lennon","bach","mozart","chopin","beethoven","beatles",
        "music", "game", "drum", "piano", "bass", "guitar", "violin", "trump", "ace", "diamond", "spade", "card", "play", "chess", "compute",

        "kennedy","fuck","suck","dick","cunt","sex","pussy","monkey","master", "anus", "asshole", "bitch","demon","daemon","angel","link","work","love","connect","dragon","soccer",
        "kill","pepper","princess", "mother", "father", "brother", "sister", "cousin", "uncle", "good", "bad", "ugly", "mustang", "tango", "ball", "shadow", "test", "access",

        "qwerty", "asdf", "zxcv", "letme", "secret",

        "silver", "old",

        "boy", "girl", "man", "lady",

        "summer", "spring", "winter", "fall", "autumn",

        "sun","moon","mercury","jupiter","mars","saturn","venus","earth",

        "cool", "dude", "trust", "time", "pass", "word", "port", "friend", "tigger", "dog", "cat", "fish", "bird", "super",

        "york","washington","cleveland","chicago","boston","tampa","dallas","angeles","phoenix","seattle","odessa","moscow","london","paris","milan","berlin","dresden",
        "rome","madrid","cairo","dehli","india","chin","german","russ","beijing","france","fren", "spain","italy","england","usa","amer","mexi",

        //Common CARS
        "honda","toyota","nissan","suzuki","mazda","mitsubishi","buick","cadillac","ford","pontiac","jeep","lexus","acura","infinit","chrysler",
        "audi","porsche","opel","subaru","volkswagen","mercedes","citroen","renault","peugeot",

        "apple","droid","sony","sharp","ibm","phone","tab",

        "4you", "4me", "4them", "4him", "4her", "4it", "4us", "2you", "2me", "2them", "2him", "2her", "2it", "2us"
      };

      public const int TOP_SCORE_MINIMUM = 180;
      public const int TOP_SCORE_BELOW_NORMAL = 208;
      public const int TOP_SCORE_NORMAL = 237;
      public const int TOP_SCORE_ABOVE_NORMAL = 293;
      public const int TOP_SCORE_MAXIMUM = 350;
    #endregion

    #region .ctor

    //used for unit testing
    internal DefaultPasswordManager(IApplication app) : base(app)
    {
      DefaultStrengthLevel = PasswordStrengthLevel.Normal;
    }

    public DefaultPasswordManager(ISecurityManagerImplementation director) : base(director)
    {
      DefaultStrengthLevel = PasswordStrengthLevel.Normal;
    }

    protected override void Destructor()
    {
      base.Destructor();
      cleanupAlgorithms();
    }

    #endregion

    #region Fields
    private Registry<PasswordHashingAlgorithm> m_Algorithms = new Registry<PasswordHashingAlgorithm>();
    private bool m_InstrumentationEnabled;
    #endregion

    #region Properties

    public override string ComponentLogTopic => CoreConsts.SECURITY_TOPIC;

    [Config(Default = false)]
    [Instrumentation.ExternalParameter(CoreConsts.EXT_PARAM_GROUP_INSTRUMENTATION, CoreConsts.EXT_PARAM_GROUP_PAY)]
    public override bool InstrumentationEnabled
    {
      get { return m_InstrumentationEnabled; }
      set { m_InstrumentationEnabled = value; }
    }

    [Config(Default = PasswordStrengthLevel.Normal)]
    public PasswordStrengthLevel DefaultStrengthLevel { get; set; }

    public IRegistry<PasswordHashingAlgorithm> Algorithms => m_Algorithms;

    #endregion

    #region Public

    public HashedPassword ComputeHash(PasswordFamily family, SecureBuffer password, PasswordStrengthLevel level = PasswordStrengthLevel.Default)
    {
      if (password == null)
        throw new SecurityException(StringConsts.ARGUMENT_ERROR + "DefaultPasswordManager.ComputeHash(password==null)");
      if (!password.IsSealed)
        throw new SecurityException(StringConsts.ARGUMENT_ERROR + "DefaultPasswordManager.ComputeHash(!password.IsSealed)");

      CheckDaemonActive();

      return DoComputeHash(family, password, level == PasswordStrengthLevel.Default ? DefaultStrengthLevel : level);
    }

    public bool Verify(SecureBuffer password, HashedPassword hash, out bool needRehash)
    {
      if (password == null || hash == null)
        throw new SecurityException(StringConsts.ARGUMENT_ERROR + "DefaultPasswordManager.Verify((password|hash)==null)");
      if (!password.IsSealed)
        throw new SecurityException(StringConsts.ARGUMENT_ERROR + "DefaultPasswordManager.Verify(!password.IsSealed)");

      needRehash = false;

      if (!Running) return false;
      return DoVerify(password, hash, out needRehash);
    }

    public bool AreEquivalent(HashedPassword a, HashedPassword b)
    {
      if (a == null || b == null) return false;
      if (a.AlgoName != b.AlgoName) return false;

      if (!Running) return false;
      return DoAreEquivalent(a, b);
    }

    public int CalculateStrenghtScore(PasswordFamily family, SecureBuffer password)
    {
      if (password == null)
        throw new SecurityException(StringConsts.ARGUMENT_ERROR + "DefaultPasswordManager.CalculateStrenghtScore(password==null)");
      if (!password.IsSealed)
        throw new SecurityException(StringConsts.ARGUMENT_ERROR + "DefaultPasswordManager.CalculateStrenghtScore(!password.IsSealed)");

      CheckDaemonActive();
      return DoCalculateStrenghtScore(family, password);
    }

    public int CalculateStrenghtPercent(PasswordFamily family, SecureBuffer password, int maxScore = 0)
    {
      if (maxScore <= 0) maxScore = TOP_SCORE_NORMAL;
      var score = CalculateStrenghtScore(family, password);
      var result = (int)(100d * (score / (double)maxScore));
      return result > 100 ? 100 : result;
    }

    public IEnumerable<PasswordRepresentation> GeneratePassword(PasswordFamily family, PasswordRepresentationType type, PasswordStrengthLevel level = PasswordStrengthLevel.Default)
    {
      CheckDaemonActive();
      return DoGeneratePassword(family, type, level == PasswordStrengthLevel.Default ? DefaultStrengthLevel : level);
    }

    public bool Register(PasswordHashingAlgorithm algo)
    {
      if (algo == null)
        throw new SecurityException(StringConsts.ARGUMENT_ERROR + GetType().Name + ".Register(algo==null)");

      CheckDaemonInactive();

      if (algo.ComponentDirector != this)
        throw new SecurityException(GetType().Name + ".Register(director!=this)");

      return m_Algorithms.Register(algo);
    }

    public bool Unregister(PasswordHashingAlgorithm algo)
    {
      if (algo == null)
        throw new SecurityException(StringConsts.ARGUMENT_ERROR + GetType().Name + ".Register(algo==null)");

      CheckDaemonInactive();

      if (algo.ComponentDirector != this)
        throw new SecurityException(GetType().Name + ".Unregister(director!=this)");

      return m_Algorithms.Unregister(algo);
    }

    public bool Unregister(string algoName)
    {
      if (algoName.IsNullOrWhiteSpace())
        throw new SecurityException(StringConsts.ARGUMENT_ERROR + GetType().Name + ".Register(algoName.IsNullOrWhiteSpace)");

      CheckDaemonInactive();
      return m_Algorithms.Unregister(algoName);
    }

    #endregion

    #region Protected


    protected override void DoConfigure(IConfigSectionNode node)
    {
      base.DoConfigure(node);

      cleanupAlgorithms();

      foreach (var algoNode in node.Children.Where(cn => cn.IsSameName(CONFIG_ALGORITHM_SECTION)))
      {
        var name = algoNode.AttrByName(Configuration.CONFIG_NAME_ATTR).Value;
        var algo = FactoryUtils.MakeAndConfigure<PasswordHashingAlgorithm>(algoNode, args: new object[] { this, name });
        m_Algorithms.Register(algo);
      }
    }

    protected override void DoStart()
    {
      if (m_Algorithms.Count == 0)
      {
        var md5 = new MD5PasswordHashingAlgorithm(this, "MD5");
        m_Algorithms.Register(md5);

        var kdf = new PBKDF2PasswordHashingAlgorithm(this, "KDF");
        m_Algorithms.Register(kdf);

        //#871
        var kdf2023 = new PBKDF2PasswordHashingAlgorithm(this, "KDF2023"){ RevisionYear = 2023 };
        m_Algorithms.Register(kdf2023);

        var nop = new NopPasswordHashingAlgorithm(this, "NOP");
        m_Algorithms.Register(nop);
      }

      foreach (var algo in m_Algorithms)
        algo.Start();
    }

    protected override void DoSignalStop()
    {
      foreach (var algo in m_Algorithms)
        algo.SignalStop();
    }

    protected override void DoWaitForCompleteStop()
    {
      foreach (var algo in m_Algorithms)
        algo.WaitForCompleteStop();
    }

    protected virtual HashedPassword DoComputeHash(PasswordFamily family, SecureBuffer password, PasswordStrengthLevel level)
    {
      var algoFamily = m_Algorithms.Where(al => al.Match(family));
      if (!algoFamily.Any())
        throw new SecurityException(GetType().Name + ".DoComputeHash(family!match)");
      var algs = algoFamily.Where(al => al.StrengthLevel == level);
      if (!algs.Any())
        algs = algoFamily.Where(al => al.StrengthLevel > level).OrderBy(al => al.StrengthLevel);
      if (!algs.Any())
        algs = algoFamily;
      var algo = algs.FirstOrDefault(al => al.IsDefault) ?? algs.First();
      return algo.ComputeHash(family, password);
    }

    protected virtual bool DoVerify(SecureBuffer password, HashedPassword hash, out bool needRehash)
    {
      needRehash = false;
      var algo = m_Algorithms[hash.AlgoName];
      if (algo == null)
        return false;

      bool need = false;
      if (!algo.Verify(password, hash, out need))
        return false;

      needRehash = !algo.IsDefault || need;
      return true;
    }

    protected virtual bool DoAreEquivalent(HashedPassword a, HashedPassword b)
    {
      var algo = m_Algorithms[a.AlgoName];
      if (algo == null)
        return false;
      return algo.AreEquivalent(a, b);
    }

    protected virtual int DoCalculateStrenghtScore(PasswordFamily family, SecureBuffer password)
    {
      var chars = Encoding.UTF8.GetChars(password.Content);
      if (chars.Length == 0) return 0;

      try
      {
        var begin = Array.FindIndex(chars, c => !Char.IsWhiteSpace(c));
        if (begin < 0) return 0;
        if (chars.Length == begin) return 0;
        var end = Array.FindLastIndex(chars, c => !Char.IsWhiteSpace(c)) + 1;

        var score = (end - begin) * CREDIT_CHAR_PRESENT;
        if (score == 0) return 0;

        var wasUpper = false;
        var wasLower = false;
        var wasDigit = false;
        var wasSymbol = false;

        char pc = (char)0;
        for (var i = begin; i < end; i++)
        {
          var c = chars[i];

          if (Char.IsUpper(c)) wasUpper = true;
          if (Char.IsLower(c)) wasLower = true;
          if (Char.IsDigit(c)) wasDigit = true;
          if (isSymbol(c)) wasSymbol = true;

          if (i > 0 &&
              (Char.IsUpper(c) != Char.IsUpper(pc) ||
                Char.IsDigit(c) != Char.IsDigit(pc) ||
                isSymbol(c) != isSymbol(pc))) score += CREDIT_TYPE_TRANSITION;

          if (c == pc) score -= DEBIT_CHAR_REPEAT;

          if (Math.Abs(c - pc) == 1) score -= DEBIT_ADJACENT_CHAR;
          pc = c;
          chars[i] = Char.ToLowerInvariant(c);
        }

        if (wasUpper && wasLower) score += CREDIT_CASE_MIX;
        if (wasDigit && (wasUpper || wasLower || wasSymbol)) score += CREDIT_DIGIT_MIX;
        if (wasSymbol) score += CREDIT_SYMBOL_MIX;

        for (var i = 0; i < DEFAULT_COMMON_WORDS.Length; i++)
        {
          var commonChars = DEFAULT_COMMON_WORDS[i].ToCharArray();
          var from = begin;
          while((from = Array.IndexOf(chars, commonChars[0], from)) >= 0)
          {
            var find = true;
            var j = 0;
            for (; j < commonChars.Length && from + j < chars.Length; j++)
              if (chars[from + j] != commonChars[j])
              {
                find = false;
                break;
              }

            if (find && j == commonChars.Length) score -= DEBIT_COMMON_WORD;
            from++;
          }
        }

        return score < 0 ? 0 : score;
      }
      finally
      {
        Array.Clear(chars, 0, chars.Length);
      }
    }

    protected virtual IEnumerable<PasswordRepresentation> DoGeneratePassword(PasswordFamily family, PasswordRepresentationType type, PasswordStrengthLevel level)
    {
      if (family != PasswordFamily.Text && family != PasswordFamily.PIN)
        yield break;

      if ((type & PasswordRepresentationType.Text) != 0)
      {
        if (family == PasswordFamily.Text)
        {
          int score = 0;
          while (true)
          {
            using (var password = Platform.RandomGenerator.Instance.NextRandomWebSafeSecureBuffer(getMinLengthForLevel(family, level), getMaxLengthForLevel(family, level)))
            {
              score = CalculateStrenghtScore(family, password);

              if (score >= getMinScoreForLevel(family, level))
              {
                var content = password.Content;
                var length = content.Length;
                var reprContent = new byte[length];
                Array.Copy(content, reprContent, length);

                yield return new PasswordRepresentation(PasswordRepresentationType.Text, "plain/text", reprContent);
                break;
              }
            }
          }
        }

        if (family == PasswordFamily.PIN)
        {
          var min = getMinLengthForLevel(family, level);
          var max = getMaxLengthForLevel(family, level);

          var minValue = (int)IntUtils.Pow(10, min - 1);
          var maxValue = (int)IntUtils.Pow(10, max) - 1;
          var value = (uint)Platform.RandomGenerator.Instance.NextScaledRandomInteger(minValue, maxValue);

          var content = value.ToString();
          var reprContent = new byte[content.Length];
          for (int i = 0; i < content.Length; i++)
            reprContent[i] = (byte)content[i];

          yield return new PasswordRepresentation(PasswordRepresentationType.Text, "plain/text", reprContent);
        }
      }
    }

    #endregion

    #region Private


    private void cleanupAlgorithms()
    {
      m_Algorithms.ForEach(a => this.DontLeak(() => a.Dispose(), errorFrom: nameof(cleanupAlgorithms)));
      m_Algorithms.Clear();
    }

    private static bool isSymbol(char c)
    {
      return Char.IsSymbol(c) || Char.IsPunctuation(c);
    }

    private int getMinScoreForLevel(PasswordFamily family, PasswordStrengthLevel level)
    {
      switch (level)
      {
        case PasswordStrengthLevel.Minimum:     return TOP_SCORE_MINIMUM;
        case PasswordStrengthLevel.BelowNormal: return TOP_SCORE_BELOW_NORMAL;
        default:                                return TOP_SCORE_NORMAL;
        case PasswordStrengthLevel.AboveNormal: return TOP_SCORE_ABOVE_NORMAL;
        case PasswordStrengthLevel.Maximum:     return TOP_SCORE_MAXIMUM;
      }
    }

    private int getMinLengthForLevel(PasswordFamily family, PasswordStrengthLevel level)
    {
      switch (level)
      {
        case PasswordStrengthLevel.Minimum:     return 4 -  (family == PasswordFamily.Text ? 0 : 1);
        case PasswordStrengthLevel.BelowNormal: return 5 -  (family == PasswordFamily.Text ? 0 : 1);
        default:                                return 6 -  (family == PasswordFamily.Text ? 0 : 1); // Normal
        case PasswordStrengthLevel.AboveNormal: return 8 -  (family == PasswordFamily.Text ? 0 : 2);
        case PasswordStrengthLevel.Maximum:     return 10 - (family == PasswordFamily.Text ? 0 : 3);
      }
    }

    private int getMaxLengthForLevel(PasswordFamily family, PasswordStrengthLevel level)
    {
      switch (level)
      {
        case PasswordStrengthLevel.Minimum:     return 5 -  (family == PasswordFamily.Text ? 0 : 1);
        case PasswordStrengthLevel.BelowNormal: return 6 -  (family == PasswordFamily.Text ? 0 : 1);
        default:                                return 8 -  (family == PasswordFamily.Text ? 0 : 2); // Normal
        case PasswordStrengthLevel.AboveNormal: return 10 - (family == PasswordFamily.Text ? 0 : 3);
        case PasswordStrengthLevel.Maximum:     return 13 - (family == PasswordFamily.Text ? 0 : 4);
      }
    }

    #endregion
  }
}
