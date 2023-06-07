/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Azos.Conf;
using Azos.Data;

namespace Azos.Serialization.JSON
{
  /// <summary>
    /// Represents a JSON-serializable structure that keys [N]ame and [D]escription on language ISO.
    /// It respects JSONWritingOptions.NLSMapLanguageISO and NLSMapLanguageISODefault.
    /// The ISO code is stored as Atom for maximum efficiency.
    /// Warning: ISO codes are CASE sensitive
    /// </summary>
    [Serializable]
    public struct NLSMap : IEnumerable<KeyValuePair<string, NLSMap.NDPair>>, IEquatable<NLSMap>,  IJsonWritable, IJsonReadable, IRequiredCheck, IConfigurationPersistent
    {
      //There are roughly 6,500 spoken languages in the world today.
      //However, about 2,000 of those languages have fewer than 1,000 speakers
      public const int MAX_ISO_COUNT = 10000;//safeguard for serialization and abuse

      /// <summary>
      /// Facilitates the population of NLSMap from code
      /// </summary>
      public struct Builder
      {
        private List<NDPair> m_Data;

        public Builder Add(string langIso, string n, string d)
        {
          if (langIso.IsNullOrWhiteSpace()) return this;
          return Add(Atom.Encode(langIso), n, d);
        }

        public Builder Add(Atom langIso, string n, string d)
        {
           if (m_Data == null)
             m_Data = new List<NDPair>();
           else
             if (m_Data.Count == MAX_ISO_COUNT) throw new AzosException("Exceeded NLSMap.MAX_ISO_COUNT");

           m_Data.Add( new NDPair(langIso, n, d) );
           return this;
        }

        /// <summary>
        /// Returns the built map
        /// </summary>
        public NLSMap Map
        {
          get
          {
            var result = new NLSMap(m_Data==null ? null : m_Data.ToArray());
            return result;
          }
        }
      }


      /// <summary>
      /// Localized Name:Description pair
      /// </summary>
      public struct NDPair : IJsonWritable, IEquatable<NDPair>
      {
        internal NDPair(Atom iso, string name, string descr){ISO = iso; Name = name; Description = descr;}

        public bool IsAssigned => !ISO.IsZero;

        public readonly Atom ISO;
        public readonly string Name;
        public readonly string Description;

        void IJsonWritable.WriteAsJson(TextWriter wri, int nestingLevel, JsonWritingOptions options)
        {
          JsonWriter.WriteMap(wri, nestingLevel, options, new System.Collections.DictionaryEntry("n", Name),
                                                          new System.Collections.DictionaryEntry("d", Description));
        }

        public override int GetHashCode() => ISO.GetHashCode();
        public override bool Equals(object obj) => obj is NDPair pair ? this.Equals(pair) : false;

        public bool Equals(NDPair other) => this.ISO == other.ISO &&
                                            this.Name.EqualsOrdSenseCase(other.Name) &&
                                            this.Description.EqualsOrdSenseCase(other.Description);

        public static bool operator ==(NDPair a, NDPair b) => a.Equals(b);
        public static bool operator !=(NDPair a, NDPair b) => !a.Equals(b);
      }

      //used by ser
      internal NLSMap(NDPair[] data)
      {
        m_Data = data;
      }

      /// <summary>
      /// Makes NLSMap out of JSON string: {eng: {n: 'Cucumber',d: 'It is green'}, deu: {n='Gurke',d='Es ist grün'}}
      /// </summary>
      public NLSMap(string nlsConf)
      {
        m_Data = null;
        if (nlsConf.IsNullOrWhiteSpace()) return;
        var nlsNode = ("{r:"+nlsConf+"}").AsJSONConfig(wrapRootName: null, handling: ConvertErrorHandling.Throw);
        ctor(nlsNode);
      }

      /// <summary>
      /// Makes NLSMap out of conf node: eng{n='Cucumber' d='It is green'} deu{n='Gurke' d='Es ist grün'}
      /// </summary>
      [ConfigCtor]
      public NLSMap(IConfigSectionNode nlsNode)
      {
        m_Data = null;
        if (nlsNode==null || !nlsNode.Exists) return;
        ctor(nlsNode);
      }

      public ConfigSectionNode PersistConfiguration(ConfigSectionNode parentNode, string name)
      {
        var node = parentNode.NonNull(nameof(parentNode))
                             .AddChildNode(name.NonBlank(nameof(name)));

        foreach(var pair in m_Data)
        {
          var entry = node.AddChildNode(pair.ISO.Value);
          entry.AddAttributeNode("n", pair.Name);
          entry.AddAttributeNode("d", pair.Description);
        }
        return node;
      }


      private void ctor(IConfigSectionNode nlsNode)
      {
        if (!nlsNode.HasChildren) return;

        var cnt = nlsNode.ChildCount;
        if (cnt > MAX_ISO_COUNT) throw new AzosException("Exceeded NLSMap.MAX_ISO_COUNT");
        m_Data = new NDPair[cnt];
        for(var i=0; i<cnt; i++)
        {
          var node = nlsNode[i];
          m_Data[i] = new NDPair( Atom.Encode(node.Name) , node.AttrByName("n").Value, node.AttrByName("d").Value );
        }
      }

      internal NDPair[] m_Data;

      public bool IsAssigned => m_Data != null && m_Data.Length > 0;

      public bool CheckRequired(string targetName) => IsAssigned;

      public NDPair this[string langIso]
      {
        get
        {
          return m_Data != null
                  ? this[Atom.Encode(langIso)]
                  : new NDPair();
        }
      }

      public NDPair this[Atom iso]
      {
        get
        {
          if (m_Data != null)
          {
            //the sequential search is used because most systems have < 6 entries (typically 2-4 e.g.: "eng", "fra", "spa", "chi" etc.)
            //hashtables start to pay off when collection has 8 or more items (when doing simple key search like here)
            for(var i=0; i<m_Data.Length; i++)
              if (m_Data[i].ISO == iso) return m_Data[i];
          }

          return new NDPair();
        }
      }


      public int Count => m_Data == null ? 0 : m_Data.Length;


      /// <summary>
      /// Takes entries from this instance and overrides them by ISO keys from another instance returning the new instance
      /// </summary>
      public NLSMap OverrideBy(NLSMap other)
      {
        if (m_Data==null) return other;
        if (other.m_Data==null) return this;

        var lst = new List<NDPair>(m_Data);

        for(var j=0; j<other.m_Data.Length; j++)
        {
          var found = false;
          for(var i=0; i<lst.Count; i++)
          {
            if (lst[i].ISO==other.m_Data[j].ISO)
            {
              lst[i] = other.m_Data[j];
              found = true;
              break;
            }
          }
          if (!found)
           lst.Add(other.m_Data[j]);
        }
        if (lst.Count > MAX_ISO_COUNT) throw new AzosException("Exceeded NLSMap.MAX_ISO_COUNT");

        return new NLSMap( lst.ToArray() );
      }

      public override string ToString() => JsonWriter.Write(this, JsonWritingOptions.Compact);


      public override int GetHashCode()
      {
        if (m_Data == null) return 0;

        var result = 0;
        for(var i=0; i < m_Data.Length; i++)
        {
          result ^= m_Data[i].GetHashCode();
        }
        return result;
      }

      public override bool Equals(object obj) => obj is NLSMap other ? this.Equals(other) : false;

      public bool Equals(NLSMap other)
      {
        if (this.m_Data == null)
        {
          if (other.m_Data == null) return true;
          return false;
        }
        if (other.m_Data == null) return false;

        if (m_Data.Length != other.m_Data.Length) return false;

        return m_Data.SequenceEqual(other.m_Data);
      }

      public static bool operator ==(NLSMap a, NLSMap b) => a.Equals(b);
      public static bool operator !=(NLSMap a, NLSMap b) => !a.Equals(b);


      public enum GetParts{ Name, Description, NameOrDescription, DescriptionOrName, NameAndDescription, DescriptionAndName}

      /// <summary>
      /// Tries to get the specified part(s) from the map defaulting to another lang if requested lang is not found.
      /// Returns null if nothing is found
      /// </summary>
      public string Get(GetParts tp, string langIso = null, string dfltLangIso = null, string concat = null)
      {
        if (langIso.IsNullOrWhiteSpace()) langIso = CoreConsts.ISO_LANG_ENGLISH;
        if (concat.IsNullOrWhiteSpace()) concat = " - ";

        var p = this[langIso];
        string result = getSwitch(p, tp, concat);
        if (result.IsNotNullOrWhiteSpace()) return result;

        if (dfltLangIso.IsNullOrWhiteSpace()) dfltLangIso = CoreConsts.ISO_LANG_ENGLISH;
        if (langIso.EqualsIgnoreCase(dfltLangIso)) return null;

        p = this[dfltLangIso];
        result = getSwitch(p, tp, concat);
        if (result.IsNotNullOrWhiteSpace()) return result;

        return null;
      }

      /// <summary>
      /// Tries to get the specified part(s) from the JSON content that represents map defaulting to another lang if requested lang is not found.
      /// Returns null if nothing is found
      /// </summary>
      public static bool TryGet(string json, out string result, GetParts tp, string langIso = null, string dfltLangIso = null, string concat = null)
      {
        try
        {
          var nls = new NLSMap(json);
          result = nls.Get(tp, langIso, dfltLangIso, concat);
          return true;
        }
        catch
        {
          result = null;
          return false;
        }
      }


          private string getSwitch(NDPair p, GetParts tp, string concat)
          {
            switch(tp)
            {
              case GetParts.Name:               { return p.Name; }
              case GetParts.Description:        { return p.Description; }
              case GetParts.NameOrDescription:  {
                                                 if (p.Name.IsNotNullOrWhiteSpace()) return p.Name;
                                                 return p.Description;
                                                }
              case GetParts.DescriptionOrName:  {
                                                 if (p.Description.IsNotNullOrWhiteSpace()) return p.Description;
                                                 return p.Name;
                                                }
              case GetParts.NameAndDescription: {
                                                 var isName = p.Name.IsNotNullOrWhiteSpace();
                                                 var isDescr = p.Description.IsNotNullOrWhiteSpace();

                                                 if (isName && isDescr)
                                                  return p.Name+concat+p.Description;

                                                 if (isName) return p.Name;
                                                 if (isDescr) return p.Description;
                                                 return null;
                                                }
              case GetParts.DescriptionAndName: {
                                                 var isName = p.Name.IsNotNullOrWhiteSpace();
                                                 var isDescr = p.Description.IsNotNullOrWhiteSpace();

                                                 if (isName && isDescr)
                                                  return p.Description+concat+p.Name;

                                                 if (isDescr) return p.Description;
                                                 if (isName) return p.Name;
                                                 return null;
                                                }
            }

            return null;
          }


      /// <summary>
      /// Writes NLSMap either as a dict or as a {n:"", d: ""} pair as Options.NLSMapLanguageISO filter dictates
      /// </summary>
      void IJsonWritable.WriteAsJson(TextWriter wri, int nestingLevel, JsonWritingOptions options)
      {
        if (m_Data==null)
        {
          wri.Write("{}");
          return;
        }

        if (options==null ||
            options.Purpose==JsonSerializationPurpose.Marshalling ||
            options.NLSMapLanguageISO.IsZero)
        {
          JsonWriter.WriteMap(wri, nestingLevel, options,
                              m_Data.Select
                              (
                                e => new System.Collections.DictionaryEntry(e.ISO.Value, e)
                              ).ToArray() );

          return;
        }

        var pair = this[options.NLSMapLanguageISO];

        if (!pair.IsAssigned && options.NLSMapLanguageISODefault != options.NLSMapLanguageISO)
          pair = this[options.NLSMapLanguageISODefault];

        if (pair.IsAssigned)
          JsonWriter.WriteMap(wri, nestingLevel, options, new System.Collections.DictionaryEntry("n", pair.Name),
                                                          new System.Collections.DictionaryEntry("d", pair.Description));
        else
          JsonWriter.WriteMap(wri, nestingLevel, options, new System.Collections.DictionaryEntry("n", null),
                                                          new System.Collections.DictionaryEntry("d", null));
      }

      (bool match, IJsonReadable self) IJsonReadable.ReadAsJson(object data, bool fromUI, JsonReader.DocReadOptions? options)
      {
        if (data is JsonDataMap map)
        {
           var builder = new NLSMap.Builder();
           foreach(var pair in map)
           {
             if (pair.Value is JsonDataMap map2)
             {
               builder.Add(pair.Key, map2["n"].AsString(), map2["d"].AsString());
             }
           }
           return (true, builder.Map);
        }

        return (false, null);
      }

      public IEnumerator<KeyValuePair<string, NLSMap.NDPair>> GetEnumerator()
      {
        return m_Data==null ? Enumerable.Empty<KeyValuePair<string, NLSMap.NDPair>>().GetEnumerator()
                            : m_Data.Select( nd => new KeyValuePair<string, NLSMap.NDPair>( nd.ISO.Value, nd)).GetEnumerator()
        ;
      }

      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
      {
        return this.GetEnumerator();
      }
    }
}
