/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;

namespace Azos.Serialization.BSON
{

  /// <summary>
  /// Specifies the purpose of BSON serialization so the level of detail may be dynamically adjusted.
  /// Depending on this parameter IBSONWritable implementors may include additional details
  /// that are otherwise not needed
  /// </summary>
  [Flags]
  public enum BSONSerializationFlags
  {
      Unspecified = 0,

      /// <summary>
      /// UI Only - minimum data only needed for ui
      /// </summary>
      UIOnly      = 0x1,

      /// <summary>
      /// Include as much data as possible for remote object reconstruction
      /// </summary>
      KeepNull    = 0x2
  }


  /// <summary>
  /// Denotes entities which can write/serialize their state directly into BSONDocument
  /// </summary>
  public interface IBSONSerializable
  {
    void SerializeToBSON(BSONSerializer serializer, BSONDocument doc, IBSONSerializable parent, ref object context);

    /// <summary>
    /// Return true to state that the supplied type is known by IBSONDeserializable implementation
    /// and its type id should not be added to the document
    /// </summary>
    bool IsKnownTypeForBSONDeserialization(Type type);
  }


  /// <summary>
  /// Denotes entities which can read/deserialize their state directly from BSONDocument
  /// </summary>
  public interface IBSONDeserializable
  {
    void DeserializeFromBSON(BSONSerializer serializer, BSONDocument doc, ref object context);
  }

}
