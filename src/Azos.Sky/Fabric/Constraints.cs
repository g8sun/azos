﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;

namespace Azos.Sky.Fabric
{
  public static class Constraints
  {
    /// <summary>
    /// Memory format version describes the methods/protocols/serializers used to pack/unpack
    /// memory representations into/from byte[].
    /// Writers take version  parameter to write out in specific format.
    /// Readers read the first version tag and then change their reading strategy as of that version
    /// Assumption: versions are always backward-compatible, e.g. a written version 9 can be processed by reader version 9 or higher, but not lower than 9.
    /// </summary>
    public const int MEMORY_FORMAT_VERSION = 1;

    public const int MAX_TAG_COUNT = 32;
    public const int MAX_STATE_SLOT_COUNT = 128;
    public const int MAX_IMPERSONATE_LEN = 255;

    public const int MAX_GROUP_LEN = 127;
    public const int MAX_INITIATOR_LEN = 127;
    public const int MAX_OWNER_LEN = 127;

    public const int MAX_DESCRIPTION_LEN = 100;

    public const float PRIORITY_MIN =   0.01f;
    public const float PRIORITY_MAX = 100.00f;


    public const float PROCESSING_FACTOR_DEFAULT = 1f;
    public const float PROCESSING_FACTOR_MIN = 0.0f;
    public const float PROCESSING_FACTOR_MAX = 10f;

    public const float ALLOCATION_FACTOR_DEFAULT = 1f;
    public const float ALLOCATION_FACTOR_MIN = 0.0f;
    public const float ALLOCATION_FACTOR_MAX = 4f;


    public static readonly Atom SEC_CREDENTIALS_BASIC = Atom.Encode("basic");
  }
}
