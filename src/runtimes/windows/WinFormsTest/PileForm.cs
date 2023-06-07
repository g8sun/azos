/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;


using Azos;
using Azos.Apps;
using Azos.Pile;
using Azos.Data;
using Azos.Serialization.JSON;



namespace WinFormsTest
{
  public partial class PileForm : System.Windows.Forms.Form
  {
    public PileForm()
    {
      InitializeComponent();
    }

    public PileForm(bool mmf)
    {
      InitializeComponent();
      MMF = mmf;
    }



    #region PERZON

      private static ulong _id;

      public class Person : TypedDoc
      {
        public static Person MakeFake()
        {
          return new Person
          {
            ID = new GDID(0, 0, _id++),
            FirstName = "Gavial-"+_id.ToString("0000"),
            LastName = "Buxarinovich-"+_id.ToString("00000"),
            DOB = _id%7==0 ? (DateTime?)null : DateTime.UtcNow,
            Balance = 2131m,
           // Data = new float[]{12.388f, -32.9090f, 2343434.893f}
          };
        }


        [Field]public GDID ID { get; set;}               //4+8 = 12
        [Field]public string FirstName { get; set;}      //16 hdr + 4 + (11*2) = 42
        [Field]public string LastName { get; set;}       //16 hdr + 4 + (18*2) = 56
        [Field]public DateTime? DOB { get; set;}         //10
        [Field]public decimal Balance { get; set;}       //8
        [Field]public float[] Data { get; set;}          //8

        [Field]public byte[] BinData { get; set;}        //8
                                                         //-------------
                                                         // around at least 144 bytes in native CLR
        public override string ToString()
        {
          if (BinData==null || BinData.Length<50)
           return JsonWriter.Write(this, JsonWritingOptions.PrettyPrintRowsAsMap);

          return "ID = {0} FirstName={1} LastName={2} BinData[{3}]....".Args(ID, FirstName, LastName, BinData.Length);
        }
      }


      public class Person2 : Person
      {
        public static Person2 MakeFake2()
        {
          return new Person2
          {
            ID = new GDID(0, 0, _id++),
            FirstName = "Adam-"+_id.ToString(),
            LastName = "Lokomaz-"+_id.ToString(),
            DOB = _id%7==0 ? (DateTime?)null : DateTime.UtcNow,
            Balance = 2131m,
            Data = new float[]{1f, 2f, 3f},

            Flag1 = true,
            Flag2 = true,

            Int1 = (int)_id,
            Int2 = (int)_id,
            Int3 = (int)_id,

            Long1 = 123,
            Long2 = (long)_id,
            Long3 = 134324324334,

            S1 = "This is a very long and longer and long line of text that takes many bytes indeed it does but how does it affect perf?"
            /*ObjectArray = new object[]{ Person.MakeFake(), new object[]{Person.MakeFake()}, Person.MakeFake(), Person.MakeFake(), Person.MakeFake(), Person.MakeFake(),
                                        Person.MakeFake(), new object[]{Person.MakeFake()}, Person.MakeFake(), Person.MakeFake(), Person.MakeFake(), Person.MakeFake(),
                                        Person.MakeFake(), new object[]{Person.MakeFake()}, Person.MakeFake(), Person.MakeFake(), Person.MakeFake(), Person.MakeFake(),
                                        Person.MakeFake(), new object[]{Person.MakeFake()}, Person.MakeFake(), Person.MakeFake(), Person.MakeFake(), Person.MakeFake(),
                                        Person.MakeFake(), new object[]{Person.MakeFake()}, Person.MakeFake(), Person.MakeFake(), Person.MakeFake(), Person.MakeFake(),
                                        Person.MakeFake(), new object[]{Person.MakeFake()}, Person.MakeFake(), Person.MakeFake(), Person.MakeFake(), Person.MakeFake(),
                                        Person.MakeFake(), new object[]{Person.MakeFake()}, Person.MakeFake(), Person.MakeFake(), Person.MakeFake(), Person.MakeFake(),
                                        Person.MakeFake(), new object[]{Person.MakeFake()}, Person.MakeFake(), Person.MakeFake(), Person.MakeFake(), Person.MakeFake(),
                                        Person.MakeFake(), new object[]{Person.MakeFake()}, Person.MakeFake(), Person.MakeFake(), Person.MakeFake(), Person.MakeFake(),
                                        Person.MakeFake(), new object[]{Person.MakeFake()}, Person.MakeFake(), Person.MakeFake(), Person.MakeFake(), Person.MakeFake(),
                                        Person.MakeFake(), new object[]{Person.MakeFake()}, Person.MakeFake(), Person.MakeFake(), Person.MakeFake(), Person.MakeFake(),
                                        1, true, "dausyhiduhaiushdahsuidhuia", false, null, null, null }
             */
          };
        }


        [Field]public bool Flag1 { get; set;}
        [Field]public bool Flag2 { get; set;}
        [Field]public bool Flag3 { get; set;}

        [Field]public int Int1 { get; set;}
        [Field]public int Int2 { get; set;}
        [Field]public int Int3 { get; set;}

        [Field]public long Long1 { get; set;}
        [Field]public long Long2 { get; set;}
        [Field]public long Long3 { get; set;}

        [Field]public double Dbl1 { get; set;}
        [Field]public double Dbl2 { get; set;}
        [Field]public double Dbl3 { get; set;}

        [Field]public string S1 { get; set;}

        [Field]public object[] ObjectArray{get; set;}
      }


    #endregion


    public bool MMF;

   // private DefaultPile m_Pile;
    private DefaultPileBase m_Pile;


    private void PileForm_Load(object sender, EventArgs e)
    {
      if (!MMF)
       m_Pile = new DefaultPile(NOPApplication.Instance);
      else
      {
        m_Pile = new MMFPile(NOPApplication.Instance);
        ((MMFPile)m_Pile).DataDirectoryRoot = @"c:\Azos";
      }

      m_Pile.Configure(null);

      chkSpeed_CheckedChanged(null, null);
    }

    private void PileForm_FormClosed(object sender, FormClosedEventArgs e)
    {
      m_Pile.WaitForCompleteStop();
    }

    private long pCount = 0;
    private DateTime pNow = DateTime.UtcNow;

    private void tmrStatus_Tick(object sender, EventArgs e)
    {
      btnStart.Enabled = m_Pile.Status==DaemonStatus.Inactive;
      tbMaxMemoryMb.Enabled = tbSegmentSize.Enabled = m_Pile.Status== DaemonStatus.Inactive;
      btnStop.Enabled = m_Pile.Status== DaemonStatus.Active;
      btnPurge.Enabled = m_Pile.Status== DaemonStatus.Active;
      btnCrawl.Enabled = m_Pile.Status== DaemonStatus.Active;
      btnCompact.Enabled = m_Pile.Status== DaemonStatus.Active;
      chkSpeed.Enabled = m_Pile.Status== DaemonStatus.Active;

      btnPersonPut.Enabled = btnPersonParaPut.Enabled = btnPersonParaGet.Enabled =
      btnStruct.Enabled = btnPersonGet.Enabled = btnPersonDelete.Enabled = m_Pile.Status== DaemonStatus.Active;

      sbTraxDeletes.Enabled = sbTraxWrites.Enabled = m_Pile.Status== DaemonStatus.Active;


      var count = m_Pile.ObjectCount;
      var utilized = m_Pile.UtilizedBytes;
      var overhead = m_Pile.OverheadBytes;

      stbMemBytes.Text = m_Pile.AllocatedMemoryBytes.ToString("n0");
      stbObjectCount.Text = count.ToString("n0");
      stbOverheadBytes.Text = overhead.ToString("n0");
      stbOverheadBytesObject.Text = count>0 ? (overhead / count).ToString("n0") : "<n/a>";
      stbUtilizedBytes.Text = utilized.ToString("n0");
      stbUtilizedBytesObject.Text = count>0 ? (utilized / count).ToString("n0") : "<n/a>";
      stbSegments.Text = m_Pile.SegmentCount.ToString("n0");
      stbSegTotalCount.Text = m_Pile.SegmentTotalCount.ToString("n0");
      stbMemCapacityBytes.Text = m_Pile.MemoryCapacityBytes.ToString("n0");

      trax();

      string err;
      while(m_Errors.TryDequeue(out err)) log( err );

      var now = DateTime.UtcNow;
      var dif = (now - pNow).TotalMilliseconds;
      if (dif>=1000)
      {
        lbSpeed.Items.Add( "{0:n0}/s".Args((count-pCount) / (dif / 1000d)));
        if (lbSpeed.Items.Count>25) lbSpeed.Items.RemoveAt(0);
        pNow = now;
        pCount = count;
      }
    }

    private void btnStart_Click(object sender, EventArgs e)
    {
      lbErrors.Items.Clear();

      var sz = tbSegmentSize.Text.AsInt(0);
      if (sz > 0)
      {
        sz = sz * 1024 * 1024;
        m_Pile.SegmentSize = sz;
      }

      var mm = tbMaxMemoryMb.Text.AsLong(0);
      if (mm > 0)
      {
        mm = mm * 1024 * 1024;
        m_Pile.MaxMemoryLimit = mm;
      }
      else
        m_Pile.MaxMemoryLimit = 0;

      m_Pile.Start();
    }

    private void btnStop_Click(object sender, EventArgs e)
    {
      chkTraxer.Checked = false;
      m_Pile.WaitForCompleteStop();
    }


//    private List<Person> m_Crap = new List<Person>();

    private void btnPersonPut_Click(object sender, EventArgs e)
    {
      lbPerson.Items.Clear();
      var cnt = tbPersonCount.Text.AsInt(10);
      var w = Stopwatch.StartNew();
      var pv = tbPersonVariance.Text.AsInt(0);
      for(var i=0; i<cnt;i++)
      {
        var obj = i%5==0? Person2.MakeFake2() : Person.MakeFake();
//m_Crap.Add( obj );
        if (pv>0) obj.BinData = new byte[pv];

        var pp = m_Pile.Put( obj );

        if (i<25)
         lbPerson.Items.Add( pp );
      }

      var elps = w.ElapsedMilliseconds;
      Text = "Added {0:n0} in {1:n0}ms at {2:n0}/sec".Args(cnt, elps, cnt /(elps/1000d));

    }


    private void btnPersonDelete_Click(object sender, EventArgs e)
    {
      if (lbPerson.SelectedItem==null) return;
      var pp = (PilePointer)lbPerson.SelectedItem;

      m_Pile.Delete( pp );
      lbPerson.Items.Remove( pp );
    }

    private void btnPersonGet_Click(object sender, EventArgs e)
    {
      if (lbPerson.SelectedItem==null) return;
      var pp = (PilePointer)lbPerson.SelectedItem;

      var raw = chkRaw.Checked;
      var cnt = tbPersonCount.Text.AsInt(10);
      var w = Stopwatch.StartNew();
      object person = null;
      byte sver;
      for(var i=0; i<cnt;i++)
      {
        person = raw ? m_Pile.GetRawBuffer( pp, out sver) : m_Pile.Get( pp );
      }

      var elps = w.ElapsedMilliseconds;
      MessageBox.Show( "Read {0:n0} in {1:n0}ms at {2:n0}/sec \r\n {3}".Args(cnt, elps, cnt /(elps/1000d), person==null ? "[null]" : person.ToString()) );
    }

    private void btnPersonSizeOf_Click(object sender, EventArgs e)
    {
         if (lbPerson.SelectedItem==null) return;
          var pp = (PilePointer)lbPerson.SelectedItem;

         Text = "Size of {0} is {1:n} bytes".Args(pp,  m_Pile.SizeOf(pp));
    }

    private void btnPurge_Click(object sender, EventArgs e)
    {
      m_Pile.Purge();
    }

    private void btnCompact_Click(object sender, EventArgs e)
    {
      var w = Stopwatch.StartNew();
      var freed = m_Pile.Compact();
      Text = "Compacted {0:n3} in {1:n0} ms".Args(freed, w.ElapsedMilliseconds);
    }

    private void btnCrawl_Click(object sender, EventArgs e)
    {
      var w = Stopwatch.StartNew();
      var status = m_Pile.Crawl( true );
      Text = "Crawl {0} in {1:n0} ms".Args(status, w.ElapsedMilliseconds);
    }


    private void btnGC_Click(object sender, EventArgs e)
    {
      var was = GC.GetTotalMemory(false);
      var w = Stopwatch.StartNew();
      GC.Collect();
      Text = "GC Freed {0:n0} bytes in {1:n0} ms".Args(was - GC.GetTotalMemory(false), w.ElapsedMilliseconds);
    }

    private void btnPersonParaPut_Click(object sender, EventArgs e)
    {
      var cnt = tbPersonCount.Text.AsInt(10);
      var threads = tbPersonThreads.Text.AsInt(1);

      var tasks = new List<Task>();

      var w = Stopwatch.StartNew();

      for(var c=0;c<threads;c++)
        tasks.Add(Task.Factory.StartNew(()=>
        {
          for(var i=0; i<cnt;i++)
          {
            var obj = Person.MakeFake();
            var pp = m_Pile.Put( obj );
          }
        }));

      Task.WaitAll( tasks.ToArray());


      var elps = w.ElapsedMilliseconds;
      var total = cnt*threads;
      Text = "Added {0:n0} in {1:n0}ms at {2:n0}/sec".Args(total, elps, total /(elps/1000d));
    }

    private void chkSpeed_CheckedChanged(object sender, EventArgs e)
    {
      m_Pile.AllocMode = chkSpeed.Checked ? AllocationMode.FavorSpeed : AllocationMode.ReuseSpace;
    }

      private void sbTraxWrites_Scroll(object sender, ScrollEventArgs e)
      {
        tbTraxWrites.Text = sbTraxWrites.Value.ToString();
      }

      private void sbTraxDeletes_Scroll(object sender, ScrollEventArgs e)
      {
        tbTraxDeletes.Text = sbTraxDeletes.Value.ToString();
      }

      private void log(string text)
      {
        lbErrors.Items.Insert(0, text );
        if (lbErrors.Items.Count>255) lbErrors.Items.RemoveAt(lbErrors.Items.Count-1);
      }

      private List<tcontext> m_Traxers = new List<tcontext>();
      private class tcontext
      {
        public bool STOP;
      }


      private enum ptype{Person, Person2, String, ByteArray}

      private int __threadCount;
      private int __writesSec;
      private int __delSec;
      private int __payloadVariance;
      private ptype __payloadType;


      private ConcurrentQueue<string> m_Errors = new ConcurrentQueue<string>();

      private void trax()
      {
        __threadCount = tbTraxThreads.Text.AsInt(1);
        __writesSec = tbTraxWrites.Text.AsInt(0);
        __delSec = tbTraxDeletes.Text.AsInt(0);
        __payloadVariance = tbTraxVariance.Text.AsInt(0);
        __payloadType = rbtPerson.Checked ? ptype.Person : rbtPerson2.Checked ? ptype.Person2 : rbtString.Checked ? ptype.String : ptype.ByteArray;

        var added = 0;
        while (chkTraxer.Checked &&  m_Traxers.Count < __threadCount)
        {
          var context = new tcontext();
          var thread = new Thread(
          (ctx) =>
          {
            try
            {
              var pps = new PilePointer[4000000];
              for(var i=0; i<pps.Length; i++) pps[i] = PilePointer.Invalid;
              var ppi = 0;
              var di=0;

              while(m_Pile.Running && !((tcontext)ctx).STOP)
              {
                var tc = __threadCount;
                if (tc==0) return;

                var wc = __writesSec / tc;
                var dc = __delSec / tc;

                var w=0;
                var d=0;
                while(w<wc || d<dc)
                {
                      if (w<wc)
                      {
                          object payload;

                          if (__payloadType== ptype.Person)
                          {
                            var p = Person.MakeFake();
                            if (__payloadVariance>0)
                              p.BinData = new byte[ IntUtils.ChangeByRndPct(__payloadVariance, -0.25f) ];
                            payload = p;
                          }
                          else if (__payloadType== ptype.Person2)
                          {
                            var p = Person2.MakeFake2();
                            if (__payloadVariance>0)
                              p.BinData = new byte[IntUtils.ChangeByRndPct(__payloadVariance, -0.25f) ];
                            payload = p;
                          }
                          else if (__payloadType== ptype.String) payload = new string(' ', __payloadVariance);
                          else payload = new byte[__payloadVariance];



                          var pp = m_Pile.Put( payload );
                          pps[ppi] = pp;
                          ppi++;
                          if (ppi==pps.Length) ppi = 0;
                          w++;
                      }

                      if (d<dc)
                      {
                          if (di==pps.Length) di = 0;
                          var pp = pps[di];
                          if (pp.Address>=0)
                          {
                            m_Pile.Delete( pp );
                            pps[di] = PilePointer.Invalid;
                          }
                          d++;
                          di++;
                      }
                }
              }
            }
            catch(Exception error)//abort etc..
            {
              m_Errors.Enqueue( error.ToMessageWithType() );
            }
          });
          m_Traxers.Add(context);
          thread.Start(context);
          added++;
        }

        if (added>0)
         log("{0} added {1} threads".Args(DateTime.Now, added));

        var removed = 0;
        while ((!chkTraxer.Checked&&m_Traxers.Count>0) || m_Traxers.Count > __threadCount)
        {
          m_Traxers[0].STOP = true;
          m_Traxers.RemoveAt(0);
          removed++;
        }

        if (removed>0)
         log("{0} removed {1} threads".Args(DateTime.Now, removed));


      }

      private void btnStruct_Click(object sender, EventArgs e)
      {
         lbPerson.Items.Clear();
      var cnt = tbPersonCount.Text.AsInt(10);
      var w = Stopwatch.StartNew();
      var pv = tbPersonVariance.Text.AsInt(0);
      for(var i=0; i<cnt;i++)
      {
        var pp = m_Pile.Put( new PilePointer(2,1,2) );

        if (i<25)
         lbPerson.Items.Add( pp );
      }

      var elps = w.ElapsedMilliseconds;
      Text = "Added structs {0:n0} in {1:n0}ms at {2:n0}/sec".Args(cnt, elps, cnt /(elps/1000d));
      }

      private void btnPersonParaGet_Click(object sender, EventArgs e)
      {
        if (lbPerson.SelectedItem==null) return;
        var pp = (PilePointer)lbPerson.SelectedItem;


        var cnt = tbPersonCount.Text.AsInt(10);
        var threads = tbPersonThreads.Text.AsInt(1);

        var tasks = new List<Task>();

        var w = Stopwatch.StartNew();

        for(var c=0;c<threads;c++)
          tasks.Add(Task.Factory.StartNew(()=>
          {
            for(var i=0; i<cnt;i++)
            {
              var obj = m_Pile.Get( pp );
            }
          }, TaskCreationOptions.LongRunning));

        Task.WaitAll( tasks.ToArray());


        var elps = w.ElapsedMilliseconds;
        var total = cnt*threads;
        Text = "Got {0:n0} in {1:n0}ms at {2:n0}/sec".Args(total, elps, total /(elps/1000d));
      }









  }
}
