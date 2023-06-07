/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System.Collections.Generic;

using Microsoft.Data.SqlClient;

namespace Azos.Data.Access.MsSql
{
  public sealed class MsSqlCursor : Cursor
  {
    internal MsSqlCursor(MsSqlCRUDQueryExecutionContext context,
                         SqlCommand command,
                         SqlDataReader reader,
                         IEnumerable<Doc> source) : base(source)
    {
      m_Context = context;
      m_Command = command;
      m_Reader = reader;
    }

    protected override void Destructor()
    {
      base.Destructor();

      DisposableObject.DisposeAndNull(ref m_Reader);
      DisposableObject.DisposeAndNull(ref m_Command);

      if (m_Context.Transaction==null)
        m_Context.Connection.Dispose();
    }

    private MsSqlCRUDQueryExecutionContext m_Context;
    private SqlCommand m_Command;
    private SqlDataReader m_Reader;
  }
}
