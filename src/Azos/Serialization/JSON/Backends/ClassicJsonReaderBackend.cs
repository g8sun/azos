﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azos.CodeAnalysis.JSON;
using Azos.CodeAnalysis.Source;

namespace Azos.Serialization.JSON.Backends
{
  /// <summary>
  /// Implements a Json Backend based on classic Azos technologies such as Azos.CodeAnalysis
  /// </summary>
  public sealed class ClassicJsonReaderBackend : IJsonReaderBackend
  {
    public object DeserializeFromJson(string json, bool caseSensitiveMaps)
    {
      var source = new StringSource(json, JsonLanguage.Instance);
      return DeserializeFromJson(source, caseSensitiveMaps);
    }

    public object DeserializeFromJson(Stream stream, bool caseSensitiveMaps, Encoding encoding, bool useBom)
    {
      using (var source = new StreamSource(stream, encoding, useBom, JsonLanguage.Instance))
      {
        return DeserializeFromJson(source, caseSensitiveMaps);
      }
    }

    public object DeserializeFromJson(ISourceText source, bool caseSensitiveMaps)
    {
      var lexer = new JsonLexer(source, throwErrors: true);
      var parser = new JsonParser(lexer, throwErrors: true, caseSensitiveMaps: caseSensitiveMaps);

      parser.Parse();

      return parser.ResultContext.ResultObject;
    }

    public ValueTask<object> DeserializeFromJsonAsync(Stream stream, bool caseSensitiveMaps, Encoding encoding, bool useBom)
      => new ValueTask<object>(DeserializeFromJson(stream, caseSensitiveMaps, encoding, useBom));

    public ValueTask<object> DeserializeFromJsonAsync(ISourceText source, bool caseSensitiveMaps)
      => new ValueTask<object>(DeserializeFromJson(source, caseSensitiveMaps));
  }
}
