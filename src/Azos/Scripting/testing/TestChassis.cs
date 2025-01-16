﻿using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

using Azos.Apps;
using Azos.Apps.Injection;
using Azos.Conf;
using Azos.Data;
using Azos.Security;

namespace Azos.Scripting
{
  /// <summary>
  /// Provides uniform base for unit/integration test fixtures which contain App chassis reference and test assumption functionality
  /// </summary>
  public abstract class TestChassis : IRunnableHook
  {
    public const string CONFIG_ASSUMPTIONS_SECTION = "assumptions";
    public const string CONFIG_GLOBAL_NS_ATTR = "global-ns";

    [Inject] IApplication m_App;

    /// <summary>
    /// Application container executing the test
    /// </summary>
    protected IApplication App => m_App;

    void IRunnableHook.Prologue(Runner runner, FID id)
    {
      runner.App.InjectInto(this); //perform default dependency injection
      DoPrologue(runner, id);
    }

    bool IRunnableHook.Epilogue(Runner runner, FID id, Exception error) => DoEpilogue(runner, id, error);

    /// <summary>
    /// Override to perform extra steps on tests harness setup before methods start to execute
    /// </summary>
    protected virtual void DoPrologue(Runner runner, FID id)
    {

    }


    /// <summary>
    /// Override to perform extra steps on tests execution  finish.
    /// Return true if exception is already handled and should not be reported to script runner
    /// </summary>
    protected virtual bool DoEpilogue(Runner runner, FID id, Exception error) => false;

    /// <summary>
    /// Override to return a different SKY home path root, by default this gets assigned from `$SKY_HOME` env variable.
    /// The getter does not ensure that this directory exists
    /// </summary>
    public virtual string SkyHomePath => Environment.GetEnvironmentVariable(CoreConsts.SKY_HOME);

    /// <summary>
    /// Override to return a different home path root, by default this gets assigned as `SkyHomePath.get()+'/testing'`.
    /// This property ensures that this root directory exists by creating it on get.
    /// This property is not designed to be accessed many times (1000s) in tight loops. Callers must cache the value instead
    /// </summary>
    public virtual string TestingHomePath
    {
      get
      {
        var path = Path.Combine(SkyHomePath, CoreConsts.SKY_TESTING_HOME_DIR);
        var di = Directory.CreateDirectory(path);
        return di.FullName;
      }
    }


    /// <summary>
    /// Returns assumptions config section used for the specific test class.
    /// This property may be  overridden for custom config location,  by default assumptions/ns/(calling type name) sub section is used.
    /// You can use `global-ns` attribute on the `assumptions` section to set common namespace name
    /// </summary>
    public virtual IConfigSectionNode ClassAssumptions
    {
      get
      {
        var nass = App.ConfigRoot[CONFIG_ASSUMPTIONS_SECTION].NonEmpty("/{0}".Args(CONFIG_ASSUMPTIONS_SECTION));
        var t = GetType();

        var result = nass[t.Namespace][t.Name];
        if (result.Exists) return result;

        var gns = nass.ValOf(CONFIG_GLOBAL_NS_ATTR).NonBlank("/{0}/${1}".Args(CONFIG_ASSUMPTIONS_SECTION, CONFIG_GLOBAL_NS_ATTR)).Trim();
        if (!gns.EndsWith(".")) gns = gns + ".";

        var ns = t.Namespace.Replace(gns, string.Empty).Trim();

        return nass[ns][t.Name].NonEmpty("/{0}/{1}/{2} configuration".Args(CONFIG_ASSUMPTIONS_SECTION, ns, t.Name));
      }
    }


    /// <summary> Returns test assumptions for named test case, inferring test case name form a calling method by default  </summary>
    public virtual Assumptions AssumptionsForCaller([CallerMemberName]string testName = null)
     => new Assumptions(AssumptionsType.Test, ClassAssumptions[testName.NonBlank(nameof(testName))].NonEmpty("Class assumptions for test `{0}`".Args(testName)));

    /// <summary> Returns test assumptions for a named story within a named test case, inferring test case name form a calling method by default </summary>
    public virtual Assumptions AssumptionsForCallerStory(string story, [CallerMemberName]string testName = null)
     => new Assumptions(AssumptionsType.Story, ClassAssumptions[testName.NonBlank(nameof(testName))][story.NonBlank(nameof(story))].NonEmpty("Class assumptions for test `{0}` story `{1}`".Args(testName, story)));

    /// <summary> Provides type of assumption: test | story </summary>
    public enum AssumptionsType { Test = 0, Story = 1 };

    /// <summary>
    /// Facilitates access to test assumption data supplied as a configuration section
    /// </summary>
    public struct Assumptions
    {
      /// <summary>
      /// Typically you would not call this .ctor directly unless you are temporarily mocking assumptions.
      /// Call <see cref="AssumptionsForCaller(string)"/> or <see cref="AssumptionsForCallerStory(string, string)"/> instead
      /// </summary>
      public Assumptions(AssumptionsType tp, IConfigSectionNode data)
      {
        Type = tp;
        Data = data.NonEmpty(nameof(data));
      }

      /// <summary> Provides the type of assumptions: test | story </summary>
      public readonly AssumptionsType Type;

      /// <summary> Accesses the underlying test data config </summary>
      public readonly IConfigSectionNode Data;


      /// <summary>
      /// Gets attribute by name with optional requirement guard check (true by default)
      /// </summary>
      public IConfigAttrNode this[string attr, bool req = true]
      {
        get
        {
          var result = Data.AttrByName(attr);
          if (req) result.NonEmpty(attr);
          return result;
        }
      }

      /// <summary>
      /// Takes standard config path relative to the calling method/story and navigates it. Pass "!" at the beginning to make path required
      /// </summary>
      public IConfigNode Nav(string path) => Data.Navigate(path);


      public sbyte   GetSByte(string attr)   => this[attr].Value.AsSByte(handling: ConvertErrorHandling.Throw);
      public int     GetInt(string attr)     => this[attr].Value.AsInt(handling: ConvertErrorHandling.Throw);
      public short   GetShort(string attr)   => this[attr].Value.AsShort(handling: ConvertErrorHandling.Throw);
      public long    GetLong(string attr)    => this[attr].Value.AsLong(handling: ConvertErrorHandling.Throw);
      public float   GetFloat(string attr)   => this[attr].Value.AsFloat(handling: ConvertErrorHandling.Throw);
      public double  GetDouble(string attr)  => this[attr].Value.AsDouble(handling: ConvertErrorHandling.Throw);
      public decimal GetDecimal(string attr) => this[attr].Value.AsDecimal(handling: ConvertErrorHandling.Throw);

      public byte    GetByte(string attr)   => this[attr].Value.AsByte(handling: ConvertErrorHandling.Throw);
      public ushort  GetUShort(string attr) => this[attr].Value.AsUShort(handling: ConvertErrorHandling.Throw);
      public uint    GetUInt(string attr)   => this[attr].Value.AsUInt(handling: ConvertErrorHandling.Throw);
      public ulong   GetULong(string attr)  => this[attr].Value.AsULong(handling: ConvertErrorHandling.Throw);

      public sbyte?   GetNullableSByte(string attr)   => this[attr].Value.AsNullableSByte(handling: ConvertErrorHandling.Throw);
      public int?     GetNullableInt(string attr)     => this[attr].Value.AsNullableInt(handling: ConvertErrorHandling.Throw);
      public short?   GetNullableShort(string attr)   => this[attr].Value.AsNullableShort(handling: ConvertErrorHandling.Throw);
      public long?    GetNullableLong(string attr)    => this[attr].Value.AsNullableLong(handling: ConvertErrorHandling.Throw);
      public float?   GetNullableFloat(string attr)   => this[attr].Value.AsNullableFloat(handling: ConvertErrorHandling.Throw);
      public double?  GetNullableDouble(string attr)  => this[attr].Value.AsNullableDouble(handling: ConvertErrorHandling.Throw);
      public decimal? GetNullableDecimal(string attr) => this[attr].Value.AsNullableDecimal(handling: ConvertErrorHandling.Throw);

      public byte?    GetNullableByte(string attr)    => this[attr].Value.AsNullableByte(handling: ConvertErrorHandling.Throw);
      public ushort?  GetNullableUShort(string attr)  => this[attr].Value.AsNullableUShort(handling: ConvertErrorHandling.Throw);
      public uint?    GetNullableUInt(string attr)    => this[attr].Value.AsNullableUInt(handling: ConvertErrorHandling.Throw);
      public ulong?   GetNullableULong(string attr)   => this[attr].Value.AsNullableULong(handling: ConvertErrorHandling.Throw);

      public byte[]    GetByteArray(string attr)      => this[attr].Value.AsByteArray(null).NonNull(attr);

      public DateTime  GetDateTime(string attr, DateTimeStyles styles = CoreConsts.UTC_TIMESTAMP_STYLES)         => this[attr].Value.AsDateTime(new DateTime(), handling: ConvertErrorHandling.Throw, styles: styles);
      public DateTime? GetNullableDateTime(string attr, DateTimeStyles styles = CoreConsts.UTC_TIMESTAMP_STYLES) => this[attr].Value.AsNullableDateTime(null, handling: ConvertErrorHandling.Throw, styles: styles);

      public bool      GetBool(string attr)          => this[attr].Value.AsBool(handling: ConvertErrorHandling.Throw);
      public bool?     GetNullableBool(string attr)  => this[attr].Value.AsNullableBool(handling: ConvertErrorHandling.Throw);

      /// <summary>
      /// Returns a string attribute which is required to be present, but may have a null or empty value
      /// </summary>
      public string    GetString(string attr)         => this[attr].Value;

      /// <summary>
      /// Returns a string value which must be a non-blank value (non-null, non-whitespace)
      /// </summary>
      public string    GetRequiredString(string attr) => GetString(attr).NonBlank(attr);

      public Atom      GetAtom(string attr)          => this[attr].Value.AsAtom(Atom.ZERO, handling: ConvertErrorHandling.Throw);
      public Atom?     GetNullableAtom(string attr)  => this[attr].Value.AsNullableAtom(null, handling: ConvertErrorHandling.Throw);

      public EntityId  GetEntityId(string attr) => this[attr].Value.AsEntityId(EntityId.EMPTY, handling: ConvertErrorHandling.Throw);
      public EntityId? GetNullableEntityId(string attr) => this[attr].Value.AsNullableEntityId(null, handling: ConvertErrorHandling.Throw);

      public GDID      GetGdid(string attr)          => this[attr].Value.AsGDID(GDID.ZERO, handling: ConvertErrorHandling.Throw);
      public GDID?     GetNullableGdid(string attr)  => this[attr].Value.AsNullableGDID(null, handling: ConvertErrorHandling.Throw);

      public Guid      GetGuid(string attr)          => this[attr].Value.AsGUID(Guid.Empty, handling: ConvertErrorHandling.Throw);
      public Guid?     GetNullableGuid(string attr)  => this[attr].Value.AsNullableGUID(null, handling: ConvertErrorHandling.Throw);
    }

    /// <summary>
    /// Impersonates the current ambient session flow. Returns the session that was injected
    /// </summary>
    protected virtual ISession Impersonate(Credentials credentials)
    {
      var user = App.SecurityManager.Authenticate(credentials);
      var session = MakeImpersonationSession();
      session.User = user;
      ExecutionContext.__SetThreadLevelSessionContext(session);
      return session;
    }

    /// <summary>
    /// Override to create custom impersonation session type. By default BaseSession is used.
    /// You can also override this to set specific session context before injecting it into ExecutionContext
    /// </summary>
    protected virtual ISession MakeImpersonationSession() => new BaseSession(Guid.NewGuid(), App.Random.NextRandomUnsignedLong);
  }
}
