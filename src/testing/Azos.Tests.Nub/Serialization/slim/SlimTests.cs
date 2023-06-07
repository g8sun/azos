/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Azos.Scripting;
using Azos.Apps;
using Azos.Data;
using Azos.Conf;
using Azos.Collections;
using Azos.IO;
using Azos.Serialization.Slim;

namespace Azos.Tests.Nub.Serialization
{

#pragma warning disable SYSLIB0011 // Type or member is obsolete

  [Runnable]
  public class SlimTests
  {
    [Run]
    public void Configuration_1()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var conf = "app{a=2 b=3 sumka=27 child1{c=4} child2=XXX{c=5} z=999}".AsLaconicConfig(handling: ConvertErrorHandling.Throw);

        s.Serialize(ms, conf);

        ms.Position.See();

        ms.Seek(0, SeekOrigin.Begin);

        var deser = s.Deserialize(ms);

        deser.ToString().SeeArgs(deser.GetType().FullName);

        var conf2 = deser as ConfigSectionNode;

        Aver.IsFalse(conf2.Configuration.EmptySection.Exists);
        Aver.IsFalse(conf2.Configuration.EmptyAttr.Exists);

        Aver.AreEqual("app", conf2.Name);
        Aver.AreEqual(2, conf2.Children.Count());
        Aver.AreEqual(4, conf2.Attributes.Count());

        Aver.AreEqual(2, conf2.AttrByName("a").ValueAsInt());
        Aver.AreEqual(3, conf2.AttrByName("b").ValueAsInt());
        Aver.AreEqual(27, conf2.AttrByName("sumka").ValueAsInt());
        Aver.AreEqual(999, conf2.AttrByName("z").ValueAsInt());

        Aver.AreEqual(4, conf2["child1"].AttrByName("c").ValueAsInt());
        Aver.AreEqual(5, conf2["child2"].AttrByName("c").ValueAsInt());
        Aver.AreEqual("XXX", conf2["child2"].Value);
      }
    }

    [Run]
    public void Configuration_2_VarResolver()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var conf = "app{a=2 b=3 sumka=27 child1{c=$(~SLAVA)} child2=XXX{c=$(~CITY)} z=999}".AsLaconicConfig(handling: ConvertErrorHandling.Throw);

        conf.Configuration.EnvironmentVarResolver = new Vars(new VarsDictionary
            {
               {"Slava", "KPSS"}, {"City","MOCKBA"}
            });

        s.Serialize(ms, conf);

        ms.Position.See();

        ms.Seek(0, SeekOrigin.Begin);

        var deser = s.Deserialize(ms);

        deser.ToString().SeeArgs(deser.GetType().FullName);

        var conf2 = deser as ConfigSectionNode;

        Aver.IsFalse(conf2.Configuration.EmptySection.Exists);
        Aver.IsFalse(conf2.Configuration.EmptyAttr.Exists);

        Aver.AreEqual("app", conf2.Name);
        Aver.AreEqual(2, conf2.Children.Count());
        Aver.AreEqual(4, conf2.Attributes.Count());

        Aver.AreEqual(2, conf2.AttrByName("a").ValueAsInt());
        Aver.AreEqual(3, conf2.AttrByName("b").ValueAsInt());
        Aver.AreEqual(27, conf2.AttrByName("sumka").ValueAsInt());
        Aver.AreEqual(999, conf2.AttrByName("z").ValueAsInt());

        Aver.AreEqual("KPSS", conf2["child1"].AttrByName("c").Value);
        Aver.AreEqual("MOCKBA", conf2["child2"].AttrByName("c").Value);
        Aver.AreEqual("XXX", conf2["child2"].Value);
      }
    }

    [Run]
    public void Configuration_3_DefaultMacroRunner()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var conf = "app{a=2 b=3 sumka=27 child{c='So, it is $(~FLAG::as-bool)!'} }".AsLaconicConfig(handling: ConvertErrorHandling.Throw);

        conf.Configuration.EnvironmentVarResolver = new Vars(new VarsDictionary
            {
               {"FLAG", "1"}
            });

        s.Serialize(ms, conf);

        ms.Position.See();

        ms.Seek(0, SeekOrigin.Begin);

        var deser = s.Deserialize(ms);

        deser.ToString().SeeArgs(deser.GetType().FullName);

        var conf2 = deser as ConfigSectionNode;

        Aver.AreEqual("So, it is True!", conf2["child"].AttrByName("c").Value);
      }
    }

    internal class TeztZhabifyMacroRunner : IMacroRunner
    {
      public string Run(IConfigSectionNode node, string inputValue, string macroName, IConfigSectionNode macroParams, object context = null)
      {
        if (macroName == "zhabify") return inputValue + "zhabenko";
        return DefaultMacroRunner.Instance.Run(node, inputValue, macroName, macroParams, context);
      }
    }

    [Run]
    public void Configuration_4_CUSTOMMacroRunner()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var conf = "app{child{c='So, it is $(~FLAG::as-bool)!' g='Dear $(~MEMBER), we have zhabified you into $(~MEMBER::zhabify)'} }".AsLaconicConfig(handling: ConvertErrorHandling.Throw);

        conf.Configuration.EnvironmentVarResolver = new Vars(new VarsDictionary
            {
               {"FLAG", "1"},
               {"MEMBER", "Xitro"}
            });

        conf.Configuration.MacroRunner = new TeztZhabifyMacroRunner();

        s.Serialize(ms, conf);

        ms.Position.See();

        ms.Seek(0, SeekOrigin.Begin);

        var deser = s.Deserialize(ms);

        deser.ToString().SeeArgs(deser.GetType().FullName);

        var conf2 = deser as ConfigSectionNode;

        Aver.AreEqual("So, it is True!", conf2["child"].AttrByName("c").Value);
        Aver.AreEqual("Dear Xitro, we have zhabified you into Xitrozhabenko", conf2["child"].AttrByName("g").Value);
      }
    }

    [Run]
    public void JSONDataMap_1_CaseSensitive()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var map = new Azos.Serialization.JSON.JsonDataMap(true)
            {
               {"flag", "1"},
               {"FLAG", "22"},
               {"MEMBER", "Xitro"}
            };

        s.Serialize(ms, map);

        ms.Position.See();

        ms.Seek(0, SeekOrigin.Begin);

        var deser = s.Deserialize(ms);

        deser.ToString().See();
        Aver.IsTrue(deser is Azos.Serialization.JSON.JsonDataMap);

        var map2 = deser as Azos.Serialization.JSON.JsonDataMap;

        Aver.AreEqual("1", map2["flag"].AsString());
        Aver.AreEqual("22", map2["FLAG"].AsString());
        Aver.AreEqual("Xitro", map2["MEMBER"].AsString());
      }
    }

    [Run]
    public void JSONDataMap_2_CaseInSensitive()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var map = new Azos.Serialization.JSON.JsonDataMap(false)
            {
               {"FLAG", "22"},
               {"MEMBER", "Xitro"}
            };

        s.Serialize(ms, map);

        ms.Position.See();

        ms.Seek(0, SeekOrigin.Begin);

        var deser = s.Deserialize(ms);

        deser.ToString().See();
        Aver.IsTrue(deser is Azos.Serialization.JSON.JsonDataMap);

        var map2 = deser as Azos.Serialization.JSON.JsonDataMap;

        map.Comparer.GetType().DisplayNameWithExpandedGenericArgs().See();
        map2.Comparer.GetType().DisplayNameWithExpandedGenericArgs().See();

        Aver.AreEqual("22", map2["flag"].AsString());
        Aver.AreEqual("22", map2["FLAG"].AsString());
        Aver.AreEqual("Xitro", map2["MEMBER"].AsString());
      }
    }

    [Run]
    public void RootSimpleTypes()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        s.Serialize(ms, 125);
        s.Serialize(ms, true);
        s.Serialize(ms, TimeSpan.FromHours(45.11));
        s.Serialize(ms, "I am here");
        s.Serialize(ms, 1212.8920M);

        var ds = new DataStruct();
        ds.fInt = 8902;
        ds.fString = "hello";
        s.Serialize(ms, ds);
        s.Serialize(ms, new DateTime(2011, 05, 22));

        ms.Seek(0, SeekOrigin.Begin);

        Aver.AreEqual(125, s.Deserialize(ms).AsInt());
        Aver.IsTrue(s.Deserialize(ms).AsBool());
        Aver.AreObjectsEqual(TimeSpan.FromHours(45.11), s.Deserialize(ms));
        Aver.AreEqual("I am here", s.Deserialize(ms).AsString());
        Aver.AreEqual(1212.8920M, s.Deserialize(ms).AsDecimal());
        Aver.AreObjectsEqual(ds, s.Deserialize(ms));
        Aver.AreObjectsEqual(new DateTime(2011, 05, 22), s.Deserialize(ms));
      }
    }


    public enum SomeCategory { CatA, CatB, CatC }


    [Run]
    public void RootEnums()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        s.Serialize(ms, SomeCategory.CatA);
        s.Serialize(ms, SomeCategory.CatB);
        s.Serialize(ms, SomeCategory.CatC);
        s.Serialize(ms, SomeCategory.CatC);

        ms.Seek(0, SeekOrigin.Begin);

        Aver.AreObjectsEqual(SomeCategory.CatA, s.Deserialize(ms));
        Aver.AreObjectsEqual(SomeCategory.CatB, s.Deserialize(ms));
        Aver.AreObjectsEqual(SomeCategory.CatC, s.Deserialize(ms));
        Aver.AreObjectsEqual(SomeCategory.CatC, s.Deserialize(ms));
        Aver.IsTrue(ms.Position == ms.Length);
      }
    }

    [Run]
    public void RootType()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        s.Serialize(ms, typeof(int));

        ms.GetBuffer().ToDumpString(DumpFormat.Printable, 0, (int)ms.Length).See();

        s.Serialize(ms, typeof(bool));
        s.Serialize(ms, GetType());

        ms.Seek(0, SeekOrigin.Begin);

        var t1 = s.Deserialize(ms) as Type;
        var t2 = s.Deserialize(ms) as Type;
        var t3 = s.Deserialize(ms) as Type;

        $"t1: {t1.FullName}".See();
        $"t2: {t2.FullName}".See();
        $"t3: {t3.FullName}".See();

        Aver.AreObjectsEqual(typeof(int), t1);
        Aver.AreObjectsEqual(typeof(bool), t2);
        Aver.AreObjectsEqual(GetType(), t3);
        Aver.IsTrue(ms.Position == ms.Length);
      }
    }

    [Run]
    public void ObjectArrayEnumsAndTypes()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var o1 = new object[] { 1, SomeCategory.CatA, typeof(int), SomeCategory.CatC, typeof(bool) };

        s.Serialize(ms, o1);

        ms.Seek(0, SeekOrigin.Begin);

        var o2 = s.Deserialize(ms) as object[];

        Aver.AreEqual(5, o2.Length);
        Aver.AreEqual(1, o2[0].AsInt());
        Aver.AreObjectsEqual(SomeCategory.CatA, o2[1]);
        Aver.AreObjectsEqual(typeof(int), o2[2]);
        Aver.AreObjectsEqual(SomeCategory.CatC, o2[3]);
        Aver.AreObjectsEqual(typeof(bool), o2[4]);
      }
    }


    public class ClassWithTypeFieldsAndInts
    {
      public Type T1;
      public Type T2;
      public string Text;

      public int INT;
      public uint UINT;
      public short SHORT;
      public ushort USHORT;
      public long LONG;
      public ulong ULONG;
    }


    [Run]
    public void ClassTypeFields()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);
        var o1 = new ClassWithTypeFieldsAndInts { T1 = typeof(int), T2 = typeof(string), Text = "Hello" };
        s.Serialize(ms, o1);

        ms.Seek(0, SeekOrigin.Begin);

        var o2 = s.Deserialize(ms) as ClassWithTypeFieldsAndInts;

        Aver.AreObjectsEqual(typeof(int), o2.T1);
        Aver.AreObjectsEqual(typeof(string), o2.T2);
        Aver.AreEqual("Hello", o2.Text);
        Aver.IsTrue(ms.Position == ms.Length);
      }
    }

    [Run]
    public void IntCompressionEdgeCases_MinValues()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);
        var o1 = new ClassWithTypeFieldsAndInts
        {
          INT = Int32.MinValue,
          UINT = UInt32.MinValue,
          SHORT = Int16.MinValue,
          USHORT = UInt16.MinValue,
          LONG = Int64.MinValue,
          ULONG = UInt64.MinValue
        };
        s.Serialize(ms, o1);

        ms.Seek(0, SeekOrigin.Begin);

        var o2 = s.Deserialize(ms) as ClassWithTypeFieldsAndInts;

        Aver.AreEqual(Int32.MinValue, o2.INT);
        Aver.AreEqual(UInt32.MinValue, o2.UINT);
        Aver.AreEqual(Int16.MinValue, o2.SHORT);
        Aver.AreEqual(UInt16.MinValue, o2.USHORT);
        Aver.AreEqual(Int64.MinValue, o2.LONG);
        Aver.AreEqual(UInt64.MinValue, o2.ULONG);
      }
    }

    [Run]
    public void IntCompressionEdgeCases_MaxValues()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);
        var o1 = new ClassWithTypeFieldsAndInts
        {
          INT = Int32.MaxValue,
          UINT = UInt32.MaxValue,
          SHORT = Int16.MaxValue,
          USHORT = UInt16.MaxValue,
          LONG = Int64.MaxValue,
          ULONG = UInt64.MaxValue
        };
        s.Serialize(ms, o1);

        ms.Seek(0, SeekOrigin.Begin);

        var o2 = s.Deserialize(ms) as ClassWithTypeFieldsAndInts;

        Aver.AreEqual(Int32.MaxValue, o2.INT);
        Aver.AreEqual(UInt32.MaxValue, o2.UINT);
        Aver.AreEqual(Int16.MaxValue, o2.SHORT);
        Aver.AreEqual(UInt16.MaxValue, o2.USHORT);
        Aver.AreEqual(Int64.MaxValue, o2.LONG);
        Aver.AreEqual(UInt64.MaxValue, o2.ULONG);
      }
    }

    [Run]
    public void GenericTupleWithReadonlyFields()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        s.Serialize(ms, new Tuple<int, string>(5, "yez"));

        ms.Seek(0, SeekOrigin.Begin);

        var tuple = (Tuple<int, string>)s.Deserialize(ms);

        Aver.AreEqual(5, tuple.Item1);
        Aver.AreEqual("yez", tuple.Item2);
      }
    }

    [Run]
    public void SingleObjectWithAllSupportedTypes()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var obj1 = new DataObject();
        obj1.Populate();

        s.Serialize(ms, obj1);
        ms.Seek(0, SeekOrigin.Begin);

        var obj2 = s.Deserialize(ms);

        Aver.AreObjectsEqual(obj1, obj2);
      }
    }

    [Run]
    public void MultipleObjectsWithAllSupportedTypes()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var obj1A = new DataObject();
        obj1A.Populate();
        s.Serialize(ms, obj1A);
        var obj1B = new DataObject();
        obj1B.Populate();
        obj1B.fLong = -99892323;
        s.Serialize(ms, obj1B);

        ms.Seek(0, SeekOrigin.Begin);

        var obj2A = s.Deserialize(ms);
        var obj2B = s.Deserialize(ms);

        Aver.AreObjectsEqual(obj1A, obj2A);
        Aver.AreObjectsEqual(obj1B, obj2B);
      }
    }

    [Run]
    public void SingleStruct()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var str1 = new DataStruct();
        str1.fInt = 89098;
        str1.fString = "hahaha!";
        str1.fNullableInt = null;

        s.Serialize(ms, str1);
        ms.Seek(0, SeekOrigin.Begin);

        var str2 = (DataStruct)s.Deserialize(ms);

        Aver.AreEqual(str1.fInt, str2.fInt);
        Aver.AreEqual(str1.fString, str2.fString);
        Aver.AreEqual(false, str2.fNullableInt.HasValue);
      }
    }

    [Run]
    public void SingleStructWithObject()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var str1 = new DataStruct();
        str1.fInt = 89098;
        str1.fString = "hahaha!";
        str1.fObject = -190;

        s.Serialize(ms, str1);
        ms.Seek(0, SeekOrigin.Begin);

        var str2 = (DataStruct)s.Deserialize(ms);

        Aver.AreEqual(str1.fInt, str2.fInt);
        Aver.AreEqual(str1.fString, str2.fString);
        Aver.AreEqual(-190, str2.fObject.AsInt());
      }
    }

    [Run]
    public void SingleStructWith1DArray()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var str1 = new DataStruct();
        str1.fString = "hahaha!";
        str1.fObjectArray1D = new object[300];
        str1.fObjectArray1D[297] = -180;
        str1.fObjectArray1D[298] = true;
        str1.fObjectArray1D[299] = "moon";

        s.Serialize(ms, str1);
        ms.Seek(0, SeekOrigin.Begin);

        var str2 = (DataStruct)s.Deserialize(ms);

        Aver.AreEqual(str1.fString, str2.fString);
        Aver.AreEqual(300, str2.fObjectArray1D.Length);
        Aver.AreEqual(-180, str2.fObjectArray1D[297].AsInt());
        Aver.IsTrue(str2.fObjectArray1D[298].AsBool());
        Aver.AreEqual("moon", str2.fObjectArray1D[299].AsString());
      }
    }

    [Run]
    public void SingleStructWith2DArray()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var str1 = new DataStruct();
        str1.fString = "hahaha!";
        str1.fObjectArray2D = new object[300, 2];
        str1.fObjectArray2D[297, 0] = -180;
        str1.fObjectArray2D[298, 1] = true;
        str1.fObjectArray2D[299, 0] = "moon";

        s.Serialize(ms, str1);
        ms.Seek(0, SeekOrigin.Begin);

        var str2 = (DataStruct)s.Deserialize(ms);

        Aver.AreEqual(str1.fString, str2.fString);
        Aver.AreEqual(300 * 2, str2.fObjectArray2D.Length);
        Aver.AreEqual(-180, str2.fObjectArray2D[297, 0].AsInt());
        Aver.IsTrue(str2.fObjectArray2D[298, 1].AsBool());
        Aver.AreEqual("moon", str2.fObjectArray2D[299, 0].AsString());
      }
    }

    [Run]
    public void Array_1D_OfDataObject()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var ar1 = new DataObject[10];
        for (int i = 0; i < ar1.Length; i++)
        {
          ar1[i] = new DataObject();
          ar1[i].Populate();
          ar1[i].fString = "My number is " + i.ToString();
        }

        s.Serialize(ms, ar1);
        ms.Seek(0, SeekOrigin.Begin);

        var ar2 = (DataObject[])s.Deserialize(ms);

        Aver.IsTrue(ar1.SequenceEqual(ar2));
      }
    }

    [Run]
    public void Array_2D_OfDataObject()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var ar1 = new DataObject[5, 10];
        for (int i = 0; i < 5; i++)
          for (int j = 0; j < 10; j++)
          {
            ar1[i, j] = new DataObject();
            ar1[i, j].Populate();
            ar1[i, j].fString = "My number is " + i.ToString() + ":" + j.ToString();
          }

        s.Serialize(ms, ar1);
        ms.Seek(0, SeekOrigin.Begin);

        var ar2 = (DataObject[,])s.Deserialize(ms);

        for (int i = 0; i < 5; i++)
          for (int j = 0; j < 10; j++)
          {
            Aver.AreEqual(ar1[i, j], ar2[i, j]);
          }
      }
    }


    [Run]
    public void ListOfDataObject()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var lst1 = new List<DataObject>();
        for (int i = 0; i < 10; i++)
        {
          var obj = new DataObject();
          obj.Populate();
          obj.fString = "My number is " + i.ToString();
          lst1.Add(obj);
        }

        s.Serialize(ms, lst1);
        ms.Seek(0, SeekOrigin.Begin);

        var lst2 = (List<DataObject>)s.Deserialize(ms);

        Aver.IsTrue(lst1.SequenceEqual(lst2));
      }
    }

    [Run]
    public void SingleObjectWithAllSupportedTypesAndListOfOtherObjects()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var obj1 = new DataObjectWithList();
        obj1.Populate();
        obj1.OtherObjects.Add(new DataObject().Populate());

        s.Serialize(ms, obj1);
        ms.Seek(0, SeekOrigin.Begin);

        var obj2 = s.Deserialize(ms);

        Aver.AreObjectsEqual(obj1, obj2);
      }
    }

    [Run]
    public void NodeGraph()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var root = new DataNode();
        root.fValue = "I am root";
        root.fChildren = new DataNode[12];
        for (int i = 0; i < root.fChildren.Length; i++)
        {
          root.fChildren[i] = new DataNode();
          root.fChildren[i].fParent = root;
          root.fChildren[i].fValue = "child " + i.ToString();
        }

        s.Serialize(ms, root);
        ms.Seek(0, SeekOrigin.Begin);

        var obj2 = (DataNode)s.Deserialize(ms);

        Aver.AreObjectsEqual(root, obj2);
        foreach (var n in obj2.fChildren)
          Aver.IsTrue(object.ReferenceEquals(n.fParent, obj2));
      }
    }

    [Run]
    public void NodeGraph2()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var root = new DataNode();
        root.fValue = "I am root";
        root.fChildren = new DataNode[2];
        root.fChildren[0] = new DataNode();
        root.fChildren[0].fParent = root;
        root.fChildren[0].fValue = "I am child 1";
        root.fChildren[1] = new DataNode();
        root.fChildren[1].fParent = root;
        root.fChildren[1].fValue = "I am child 2";

        root.fLeft = new DataNode { fValue = "I am left child of root", fParent = root };
        root.fRight = new DataNode { fValue = "I am right child of root", fParent = root, fLeft = root };//cycle

        s.Serialize(ms, root);
        ms.Seek(0, SeekOrigin.Begin);

        var root2 = (DataNode)s.Deserialize(ms);

        Aver.AreEqual("I am root", root2.fValue);
        Aver.IsNull(root2.fParent);
        Aver.AreEqual(2, root2.fChildren.Length);
        Aver.AreEqual("I am child 1", root2.fChildren[0].fValue);
        Aver.AreEqual("I am child 2", root2.fChildren[1].fValue);
        Aver.AreEqual("I am left child of root", root2.fLeft.fValue);
        Aver.AreEqual("I am right child of root", root2.fRight.fValue);
        Aver.AreObjectsEqual(root2, root2.fLeft.fParent);
        Aver.AreObjectsEqual(root2, root2.fRight.fParent);
        Aver.AreObjectsEqual(root2, root2.fRight.fLeft);//cycle
      }
    }

    [Run]
    public void ArrayOfNodeGraph()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var arr = new DataNode[25];
        for (int z = 0; z < arr.Length; z++)
        {
          var root = new DataNode();
          root.fValue = "I am root";
          root.fChildren = new DataNode[12];
          for (int i = 0; i < root.fChildren.Length; i++)
          {
            root.fChildren[i] = new DataNode();
            root.fChildren[i].fParent = root;
            root.fChildren[i].fValue = "child " + i.ToString();
          }
          arr[z] = root;
        }

        s.Serialize(ms, arr);
        ms.Seek(0, SeekOrigin.Begin);

        var obj2 = s.Deserialize(ms) as DataNode[];

        Aver.AreArrayObjectsEquivalent(arr, obj2);
      }
    }

    [Run]
    public void Dictionary_ISerializable_NoComparer()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var d1 = new Dictionary<string, object>();

        d1["A"] = 1;
        d1["B"] = "dva";

        s.Serialize(ms, d1);

        ms.Seek(0, SeekOrigin.Begin);

        var d2 = (Dictionary<string, object>)s.Deserialize(ms);

        Aver.AreObjectsEqual(1, d2["A"]);
        Aver.AreObjectsEqual("dva", d2["B"]);
      }
    }

    [Run]
    public void Dictionary_ISerializable_WITHComparer_InvariantCultureIgnoreCase()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var d1 = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

        d1["A"] = 1;
        d1["B"] = "dva";

        s.Serialize(ms, d1);

        ms.Seek(0, SeekOrigin.Begin);

        var d2 = (Dictionary<string, object>)s.Deserialize(ms);

        Aver.AreObjectsEqual(1, d2["A"]);
        Aver.AreObjectsEqual("dva", d2["B"]);
      }
    }

    [Run(@"message=$'
 20171223 DKh This test failed on net-core <2.0.3 because OrdinalIgnoreCase
 comparer does not have a .ctor() for deserialization. See:
   https://github.com/dotnet/coreclr/issues/15626
 #/> dotnet --info
   '")]
    public void Dictionary_ISerializable_WITHComparer_OrdinalIgnoreCase()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var d1 = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        d1["A"] = 1;
        d1["B"] = "dva";

        s.Serialize(ms, d1);

        ms.Seek(0, SeekOrigin.Begin);

        var d2 = (Dictionary<string, object>)s.Deserialize(ms);

        Aver.AreObjectsEqual(1, d2["A"]);
        Aver.AreObjectsEqual("dva", d2["B"]);
      }
    }

    [Run("message='This is to test MS stuff on dot-core, does not really test Slim, but tests Dictionary<> with OrdinalIgnoreCase comparer'")]
    public void Dictionary_BinaryFormatter()
    {
      var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

      dict.Add("A", 1);
      dict.Add("B", true);

      var stream = new MemoryStream();

      var bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

      bf.Serialize(stream, dict);


      stream.Position = 0;

      var got = bf.Deserialize(stream) as Dictionary<string, object>;

      $" got A =  {got["A"]}".See();
      $" got B =  {got["B"]}".See();
    }

    private class ZNamed : INamed { public string Name { get; set; } }

    [Run]
    public void Dictionary_Registry()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var r1 = new Registry<ZNamed>();

        r1.Register(new ZNamed { Name = "A" });
        r1.Register(new ZNamed { Name = "B" });

        s.Serialize(ms, r1);

        ms.Seek(0, SeekOrigin.Begin);

        var r2 = (Registry<ZNamed>)s.Deserialize(ms);

        Aver.AreEqual(2, r2.Count);
        Aver.AreEqual("A", r2["A"].Name);
        Aver.AreEqual("B", r2["B"].Name);
      }
    }


    public class TeztDictDerived : Dictionary<string, object>
    {
      public TeztDictDerived() : base(StringComparer.InvariantCultureIgnoreCase) { }
      protected TeztDictDerived(System.Runtime.Serialization.SerializationInfo info,
                                System.Runtime.Serialization.StreamingContext context) : base(info, context)
      {

      }
    }


    [Run]
    public void DerivedDictionary_ISerializable()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var d1 = new TeztDictDerived();

        d1["A"] = 1;
        d1["B"] = "dva";

        s.Serialize(ms, d1);

        ms.Seek(0, SeekOrigin.Begin);

        var d2 = (TeztDictDerived)s.Deserialize(ms);

        Aver.AreObjectsEqual(1, d2["A"]);
        Aver.AreObjectsEqual("dva", d2["B"]);
      }
    }

    [Run]
    public void ConcurrentDictionaryThatUsesOnAttributes()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);


        var d = new System.Collections.Concurrent.ConcurrentDictionary<string, object>();
        d.TryAdd("a", 1);
        d.TryAdd("b", true);

        s.Serialize(ms, d);

        ms.Seek(0, SeekOrigin.Begin);

        var d2 = s.Deserialize(ms) as System.Collections.Concurrent.ConcurrentDictionary<string, object>;

        Aver.AreObjectsEqual(1, d2["a"]);
        Aver.AreObjectsEqual(true, d2["b"]);
      }
    }

    [Run]
    public void ConcurrentDictionaryThatUsesOnAttributesWithTypeRegistry()
    {
      using (var ms = new MemoryStream())
      {
        var treg = new TypeRegistry(TypeRegistry.CommonCollectionTypes,
                                    TypeRegistry.BoxedCommonTypes,
                                    TypeRegistry.BoxedCommonNullableTypes);

        var s = new SlimSerializer(SlimFormat.Instance, treg);


        var d = new System.Collections.Concurrent.ConcurrentDictionary<string, object>();
        d.TryAdd("a", 1);
        d.TryAdd("b", true);

        s.Serialize(ms, d);

        ms.Seek(0, SeekOrigin.Begin);

        var d2 = s.Deserialize(ms) as System.Collections.Concurrent.ConcurrentDictionary<string, object>;

        Aver.AreObjectsEqual(1, d2["a"]);
        Aver.AreObjectsEqual(true, d2["b"]);
      }
    }

    [Run]
    public void BaseSessionWithConcurrentDictionaryThatUsesOnAttributes()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);


        var session = new BaseSession(Guid.NewGuid(), 12345);

        session.Items["a"] = 1;
        session.Items["b"] = true;


        s.Serialize(ms, session);

        ms.Seek(0, SeekOrigin.Begin);

        var session2 = s.Deserialize(ms) as BaseSession;

        Aver.AreObjectsEqual(1, session2.Items["a"]);
        Aver.AreObjectsEqual(true, session2.Items["b"]);
      }
    }

    [Run]
    public void BaseSessionWithConcurrentDictionaryThatUsesOnAttributesWithTypeRegistry()
    {
      using (var ms = new MemoryStream())
      {
        var treg = new TypeRegistry(TypeRegistry.CommonCollectionTypes,
                                    TypeRegistry.BoxedCommonTypes,
                                    TypeRegistry.BoxedCommonNullableTypes);

        var s = new SlimSerializer(SlimFormat.Instance, treg);

        var session = new BaseSession(Guid.NewGuid(), 54321);

        session.Items["a"] = 1;
        session.Items["b"] = true;

        s.Serialize(ms, session);

        ms.Seek(0, SeekOrigin.Begin);

        var session2 = s.Deserialize(ms) as BaseSession;

        Aver.AreObjectsEqual(1, session2.Items["a"]);
        Aver.AreObjectsEqual(true, session2.Items["b"]);
      }
    }

    [Run]
    public void Arrays_1D_int()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var arr1 = new int[8];
        arr1[0] = -100;
        arr1[5] = 987;
        arr1[7] = 1000;

        s.Serialize(ms, arr1);
        ms.Seek(0, SeekOrigin.Begin);

        var arr2 = (int[])s.Deserialize(ms);

        Aver.AreEqual(arr1.Length, arr2.Length);
        Aver.AreEqual(-100, arr2[0]);
        Aver.AreEqual(987, arr2[5]);
        Aver.AreEqual(1000, arr2[7]);
      }
    }

    [Run]
    public void Arrays_2D_int()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var arr1 = new int[8, 2];
        arr1[0, 0] = -100;
        arr1[5, 1] = 987;
        arr1[7, 1] = 1000;

        s.Serialize(ms, arr1);
        ms.Seek(0, SeekOrigin.Begin);

        var arr2 = (int[,])s.Deserialize(ms);

        Aver.AreEqual(arr1.Length, arr2.Length);
        Aver.AreEqual(16, arr2.Length);
        Aver.AreEqual(-100, arr2[0, 0]);
        Aver.AreEqual(987, arr2[5, 1]);
        Aver.AreEqual(1000, arr2[7, 1]);
      }
    }

    [Run]
    public void Arrays_3D_int()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var arr1 = new int[8, 2, 4];
        arr1[0, 0, 0] = -100;
        arr1[5, 1, 2] = 987;
        arr1[7, 1, 3] = 1000;

        s.Serialize(ms, arr1);
        ms.Seek(0, SeekOrigin.Begin);

        var arr2 = (int[,,])s.Deserialize(ms);

        Aver.AreEqual(arr1.Length, arr2.Length);
        Aver.AreEqual(64, arr2.Length);
        Aver.AreEqual(-100, arr2[0, 0, 0]);
        Aver.AreEqual(987, arr2[5, 1, 2]);
        Aver.AreEqual(1000, arr2[7, 1, 3]);
      }
    }

    [Run]
    public void Arrays_3D_object()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var arr1 = new object[8, 2, 4];
        arr1[0, 0, 0] = -100;
        arr1[5, 1, 2] = "its good";
        arr1[7, 1, 3] = new DateTime(1990, 2, 12);

        s.Serialize(ms, arr1);
        ms.Seek(0, SeekOrigin.Begin);

        var arr2 = (object[,,])s.Deserialize(ms);

        Aver.AreEqual(arr1.Length, arr2.Length);
        Aver.AreEqual(64, arr2.Length);
        Aver.AreObjectsEqual(-100, arr2[0, 0, 0]);
        Aver.AreObjectsEqual("its good", arr2[5, 1, 2]);
        Aver.AreObjectsEqual(new DateTime(1990, 2, 12), arr2[7, 1, 3]);
      }
    }

    [Run]
    public void CtorChaining()
    {
      using (var ms = new MemoryStream())
      {
        var obj1 = new ClassB();
        obj1.Age = 789000;

        var s = new SlimSerializer(SlimFormat.Instance);

        s.Serialize(ms, obj1);
        ms.Seek(0, SeekOrigin.Begin);

        var obj2 = s.Deserialize(ms) as ClassB;

        Aver.AreEqual(78, obj2.Magic1);
        Aver.AreEqual(123, obj2.Magic2);
        Aver.AreEqual(789000, obj2.Age);
        Aver.IsNotNull(obj2.Magic3);
        Aver.AreEqual("Marat", obj2.Name);
      }
    }


    public class WithoutCTORSkip
    {

      public WithoutCTORSkip()
      {
        MSG = "Was CALLED";
      }

      [NonSerialized]
      public string MSG;
    }

    public class WithCTORSkip : WithoutCTORSkip
    {
      [SlimDeserializationCtorSkip]
      public WithCTORSkip() : base()
      {

      }
    }


    [Run]
    public void CtorSkip()
    {
      using (var ms = new MemoryStream())
      {
        var without = new WithoutCTORSkip();
        var with = new WithCTORSkip();

        var s = new SlimSerializer(SlimFormat.Instance);

        s.Serialize(ms, with);
        s.Serialize(ms, without);
        ms.Seek(0, SeekOrigin.Begin);

        var with2 = s.Deserialize(ms) as WithCTORSkip;
        var without2 = s.Deserialize(ms) as WithoutCTORSkip;

        Aver.IsNotNull(with2);
        Aver.IsNotNull(without2);

        Aver.AreEqual(null, with2.MSG);
        Aver.AreEqual("Was CALLED", without2.MSG);
      }
    }

    [Run]
    public void WithInterfaceFields_1()
    {
      using (var ms = new MemoryStream())
      {
        var objA = new ClassWithInterfaceFieldsA();
        objA.ID = 9890;
        objA.Data1 = new SomeDataA();
        objA.Data2 = new SomeDataB();

        var s = new SlimSerializer(SlimFormat.Instance);

        s.Serialize(ms, objA);
        ms.Seek(0, SeekOrigin.Begin);

        var objA2 = s.Deserialize(ms) as ClassWithInterfaceFieldsA;

        Aver.AreEqual(9890, objA2.ID);
        Aver.AreEqual(null, objA2.Data1.Data);
        Aver.AreEqual("Unspecified", objA2.Data2.Data);
      }
    }

    [Run]
    public void WithInterfaceFields_2()
    {
      using (var ms = new MemoryStream())
      {
        var objB = new ClassWithInterfaceFieldsB();
        objB.ID = 19890;
        objB.Data1 = new SomeDataA() { Data = "hahaha!" };
        objB.Data2 = new SomeDataB();
        objB.Data3 = new SomeDataB() { Data = "hohoho!" };

        var s = new SlimSerializer(SlimFormat.Instance);

        s.Serialize(ms, objB);
        ms.Seek(0, SeekOrigin.Begin);

        var objB2 = s.Deserialize(ms) as ClassWithInterfaceFieldsB;

        Aver.AreEqual(19890, objB2.ID);
        Aver.AreEqual("hahaha!", objB2.Data1.Data);
        Aver.AreEqual("Unspecified", objB2.Data2.Data);
        Aver.AreEqual("hohoho!", objB2.Data3.Data);
        Aver.AreEqual(true, objB2.WasCtor);
      }
    }

    [Run]
    public void ObjectField_ByteArray()
    {
      using (var ms = new MemoryStream())
      {
        var objA = new OneObjectField();
        objA.Data = new byte[2] { 10, 20 };

        var s = new SlimSerializer(SlimFormat.Instance);

        s.Serialize(ms, objA);
        ms.Seek(0, SeekOrigin.Begin);

        var objB = s.Deserialize(ms) as OneObjectField;

        Aver.IsTrue(objB.Data is byte[]);
        Aver.AreEqual(2, ((byte[])objB.Data).Length);
        Aver.AreEqual(10, ((byte[])objB.Data)[0]);
        Aver.AreEqual(20, ((byte[])objB.Data)[1]);
      }
    }

    [Run]
    public void ObjectField_StringArray()
    {
      using (var ms = new MemoryStream())
      {
        var objA = new OneObjectField();
        objA.Data = new string[6] { "a", "b", "c", "d", "e", "f" };

        var s = new SlimSerializer(SlimFormat.Instance);

        s.Serialize(ms, objA);

        ms.GetBuffer().ToDumpString(DumpFormat.Printable).See();

        ms.Seek(0, SeekOrigin.Begin);

        var objB = s.Deserialize(ms) as OneObjectField;

        Aver.IsTrue(objB.Data is string[]);
        Aver.AreEqual(6, ((string[])objB.Data).Length);
        Aver.AreEqual("a", ((string[])objB.Data)[0]);
        Aver.AreEqual("b", ((string[])objB.Data)[1]);
      }
    }

    [Run]
    public void TwoStreamerSupportedRefFields_SameReference()
    {
      using (var ms = new MemoryStream())
      {
        var objA = new TwoByteArrayRefFields();
        objA.Data1 = new byte[2] { 1, 2 };
        objA.Data2 = objA.Data1;

        Aver.IsTrue(object.ReferenceEquals(objA.Data1, objA.Data2));

        var s = new SlimSerializer(SlimFormat.Instance);

        s.Serialize(ms, objA);

        ms.GetBuffer().ToDumpString(DumpFormat.Printable).See();

        ms.Seek(0, SeekOrigin.Begin);

        var objB = s.Deserialize(ms) as TwoByteArrayRefFields;

        Aver.IsTrue(objB.Data1 is byte[]);
        Aver.IsTrue(objB.Data2 is byte[]);
        Aver.AreEqual(2, objB.Data1.Length);
        Aver.AreEqual(2, objB.Data2.Length);
        Aver.IsTrue(object.ReferenceEquals(objB.Data1, objB.Data2));
      }
    }

    [Run]
    public void TwoObjectArrayRefFields_SameReference()
    {
      using (var ms = new MemoryStream())
      {
        var objA = new TwoObjectArrayRefFields();
        objA.Data1 = new object[2] { 1, true };
        objA.Data2 = objA.Data1;

        Aver.IsTrue(object.ReferenceEquals(objA.Data1, objA.Data2));

        var s = new SlimSerializer(SlimFormat.Instance);

        s.Serialize(ms, objA);

        ms.GetBuffer().ToDumpString(DumpFormat.Printable).See();

        ms.Seek(0, SeekOrigin.Begin);

        var objB = s.Deserialize(ms) as TwoObjectArrayRefFields;

        Aver.IsTrue(objB.Data1 is object[]);
        Aver.IsTrue(objB.Data2 is object[]);
        Aver.AreEqual(2, objB.Data1.Length);
        Aver.AreEqual(2, objB.Data2.Length);
        Aver.IsTrue(object.ReferenceEquals(objB.Data1, objB.Data2));
      }
    }

    [Run]
    public void ByteArrayArray_SameReference()
    {
      using (var ms = new MemoryStream())
      {
        var objA = new ByteArrayArray();
        objA.Data1 = new byte[][] { new byte[] { 1, 2 }, new byte[] { 129 }, new byte[] { 250, 240, 100 } };
        objA.Data2 = objA.Data1;

        Aver.IsTrue(object.ReferenceEquals(objA.Data1, objA.Data2));
        Aver.AreEqual(2, objA.Data1[0].Length);
        Aver.AreEqual(1, objA.Data1[1].Length);
        Aver.AreEqual(3, objA.Data1[2].Length);

        var s = new SlimSerializer(SlimFormat.Instance);

        s.Serialize(ms, objA);

        //  ms.GetBuffer().ToDumpString(DumpFormat.Printable).See();

        ms.Seek(0, SeekOrigin.Begin);

        var objB = s.Deserialize(ms) as ByteArrayArray;

        Aver.IsNotNull(objB);

        Aver.IsTrue(objB.Data1 is byte[][]);
        Aver.IsTrue(objB.Data2 is byte[][]);
        Aver.AreEqual(3, objB.Data1.Length);
        Aver.AreEqual(3, ((byte[][])objB.Data2).Length);
        Aver.IsTrue(object.ReferenceEquals(objB.Data1, objB.Data2));

        Aver.AreEqual(2, objB.Data1[0].Length);
        Aver.AreEqual(1, objB.Data1[1].Length);
        Aver.AreEqual(3, objB.Data1[2].Length);
      }
    }

    [Run]
    public void SequentialManyWritesReads_1()
    {
      using (var ms = new MemoryStream())
      {
        var objA = new ByteArrayArray();
        objA.Data1 = new byte[][] { new byte[] { 1, 2 }, new byte[] { 129 }, new byte[] { 250, 240, 100 } };
        objA.Data2 = objA.Data1;

        Aver.IsTrue(object.ReferenceEquals(objA.Data1, objA.Data2));
        Aver.AreEqual(2, objA.Data1[0].Length);
        Aver.AreEqual(1, objA.Data1[1].Length);
        Aver.AreEqual(3, objA.Data1[2].Length);

        var s = new SlimSerializer(SlimFormat.Instance);

        //NOTICE MANY serializes into the same stream
        s.Serialize(ms, new OneObjectField { Data = "Yes, I am here" });
        s.Serialize(ms, new OneObjectField { Data = 124567891 });
        s.Serialize(ms, objA);
        s.Serialize(ms, new OneObjectField { Data = new TestPerson { Name = "Koloboko", Respect = PersonRespect.Guru, Assets = 250333111m } });
        s.Serialize(ms, new OneObjectField { Data = -2345d });

        ms.Seek(0, SeekOrigin.Begin); //<========================================================================================================

        //NOTICE MANY deserializes into the same stream
        Aver.AreEqual("Yes, I am here", (string)((s.Deserialize(ms) as OneObjectField).Data));
        Aver.AreEqual(124567891, (int)((s.Deserialize(ms) as OneObjectField).Data));

        var objB = s.Deserialize(ms) as ByteArrayArray;

        Aver.IsNotNull(objB);

        Aver.IsTrue(objB.Data1 is byte[][]);
        Aver.IsTrue(objB.Data2 is byte[][]);
        Aver.AreEqual(3, objB.Data1.Length);
        Aver.AreEqual(3, ((byte[][])objB.Data2).Length);
        Aver.IsTrue(object.ReferenceEquals(objB.Data1, objB.Data2));

        Aver.AreEqual(2, objB.Data1[0].Length);
        Aver.AreEqual(1, objB.Data1[1].Length);
        Aver.AreEqual(3, objB.Data1[2].Length);

        var pb = (TestPerson)(s.Deserialize(ms) as OneObjectField).Data;
        Aver.AreEqual("Koloboko", pb.Name);
        Aver.IsTrue(PersonRespect.Guru == pb.Respect);
        Aver.AreEqual(250333111m, pb.Assets);

        Aver.AreEqual(-2345d, (double)((s.Deserialize(ms) as OneObjectField).Data));
      }
    }

    [Run]
    public void SequentialManyWritesReads_2_ROOTPrimitives()
    {
      using (var ms = new MemoryStream())
      {
        var objA = new ByteArrayArray();
        objA.Data1 = new byte[][] { new byte[] { 1, 2 }, new byte[] { 129 }, new byte[] { 250, 240, 100 } };
        objA.Data2 = objA.Data1;

        Aver.IsTrue(object.ReferenceEquals(objA.Data1, objA.Data2));
        Aver.AreEqual(2, objA.Data1[0].Length);
        Aver.AreEqual(1, objA.Data1[1].Length);
        Aver.AreEqual(3, objA.Data1[2].Length);

        var s = new SlimSerializer(SlimFormat.Instance);

        //NOTICE MANY serializes into the same stream
        s.Serialize(ms, "Yes, I am here");  //Notice ROOT primitives
        s.Serialize(ms, 124567891);
        s.Serialize(ms, objA);
        s.Serialize(ms, new TestPerson { Name = "Koloboko", Respect = PersonRespect.Guru, Assets = 250333111m });
        s.Serialize(ms, -2345d);

        ms.Seek(0, SeekOrigin.Begin); //<========================================================================================================

        //NOTICE MANY deserializes into the same stream
        Aver.AreEqual("Yes, I am here", s.Deserialize(ms) as string);
        Aver.AreEqual(124567891, (int)s.Deserialize(ms));

        var objB = s.Deserialize(ms) as ByteArrayArray;

        Aver.IsNotNull(objB);

        Aver.IsTrue(objB.Data1 is byte[][]);
        Aver.IsTrue(objB.Data2 is byte[][]);
        Aver.AreEqual(3, objB.Data1.Length);
        Aver.AreEqual(3, ((byte[][])objB.Data2).Length);
        Aver.IsTrue(object.ReferenceEquals(objB.Data1, objB.Data2));

        Aver.AreEqual(2, objB.Data1[0].Length);
        Aver.AreEqual(1, objB.Data1[1].Length);
        Aver.AreEqual(3, objB.Data1[2].Length);

        var pb = (TestPerson)s.Deserialize(ms);
        Aver.AreEqual("Koloboko", pb.Name);
        Aver.IsTrue(PersonRespect.Guru == pb.Respect);
        Aver.AreEqual(250333111m, pb.Assets);

        Aver.AreEqual(-2345d, (double)s.Deserialize(ms));
      }
    }


    public class ClassWithSelfReference
    {
      public string S1;
      public ClassWithSelfReference Ref1;
      public ClassWithSelfReference[] Array1;
      public object[] Array2;
    }


    [Run]
    public void ClassWithSelfReference_1()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var o1 = new ClassWithSelfReference
        {
          S1 = "Some Data"
        };
        o1.Ref1 = o1;//Field in self pointing to self

        s.Serialize(ms, o1);

        ms.GetBuffer().ToDumpString(DumpFormat.Printable, 0, (int)ms.Length).See();
        $"Position: {ms.Position}".See();

        ms.Seek(0, SeekOrigin.Begin);

        var o2 = s.Deserialize(ms) as ClassWithSelfReference;

        Aver.AreEqual("Some Data", o2.S1);
        Aver.IsTrue(object.ReferenceEquals(o2, o2.Ref1));
      }
    }

    [Run]
    public void ClassWithSelfReference_2_Arrays()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var o1 = new ClassWithSelfReference
        {
          S1 = "Some Data"
        };
        o1.Ref1 = o1;//Field in self pointing to self
        o1.Array1 = new ClassWithSelfReference[] { o1, o1, null };
        o1.Array2 = new object[] { null, 1, o1, true };

        s.Serialize(ms, o1);

        ms.GetBuffer().ToDumpString(DumpFormat.Printable, 0, (int)ms.Length).See();
        $"Position: {ms.Position}".See();

        ms.Seek(0, SeekOrigin.Begin);

        var o2 = s.Deserialize(ms) as ClassWithSelfReference;

        Aver.AreEqual("Some Data", o2.S1);
        Aver.IsTrue(object.ReferenceEquals(o2, o2.Ref1));

        Aver.AreEqual(3, o2.Array1.Length);
        Aver.IsTrue(object.ReferenceEquals(o2, o2.Array1[0]));
        Aver.IsTrue(object.ReferenceEquals(o2, o2.Array1[1]));
        Aver.IsNull(o2.Array1[2]);

        Aver.AreEqual(4, o2.Array2.Length);
        Aver.IsNull(o2.Array2[0]);
        Aver.AreObjectsEqual(1, o2.Array2[1]);
        Aver.IsTrue(object.ReferenceEquals(o2, o2.Array2[2]));
        Aver.AreObjectsEqual(true, o2.Array2[3]);
      }
    }

    [Run("cnt=10 sz=512000")]
    [Run("cnt=10 sz=1512000")]
    public void VeryLargeStrings(int cnt, int sz)
    {
      var data = new List<string>();

      for (var i = 0; i < cnt; i++)
      {
        var sb = new StringBuilder(sz);
        while (sb.Length < sz)
          sb.Append(Text.NaturalTextGenerator.Generate(50));

        data.Add(sb.ToString());
      }

      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer();

        s.Serialize(ms, data);

        $"Serialized bytes: {ms.Position}".See();
        $"Serialized strings: {data.Count}".See();

        ms.Position = 0;

        var got = s.Deserialize(ms) as List<string>;

        $"DeSerialized bytes: {ms.Position}".See();
        $"DeSerialized strings: {got.Count}".See();

        Aver.IsTrue(data.SequenceEqual(got));
      }
    }


    private class binwrap
    {
      public byte[] bin;
    }


    [Run]
    public void RootByteArrayEfficiency()
    {
      using (var ms = new MemoryStream())
      {
        var s = new SlimSerializer(SlimFormat.Instance);

        var ar1 = new byte[32000];

        const int CNT = 1000;

        for (var i = 0; i < 250000; i++)
        {
          ms.Position = 0;
          s.Serialize(ms, ar1);//warmup
          ms.Position = 0;
          s.Deserialize(ms);
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (var i = 0; i < CNT; i++)
        {
          ms.Position = 0;
          s.Serialize(ms, ar1);
          ms.Position = 0;
          var ar2 = s.Deserialize(ms) as byte[];
          Aver.AreEqual(ar1.Length, ar2.Length);
        }
        var e1 = sw.ElapsedMilliseconds;

        var wo1 = new binwrap { bin = ar1 };

        ms.Position = 0;
        s.Serialize(ms, wo1);//warmup

        sw.Restart();
        for (var i = 0; i < CNT; i++)
        {
          ms.Position = 0;
          s.Serialize(ms, wo1);
          ms.Position = 0;
          var wo2 = s.Deserialize(ms) as binwrap;
          Aver.AreEqual(wo1.bin.Length, wo2.bin.Length);
        }
        var e2 = sw.ElapsedMilliseconds;

        "Did {0}  byte[] root: {1}ms ({2}ops/sec);  wrap obj: {3}ms({4} ops/sec)".SeeArgs(CNT, e1, CNT / (e1 / 1000d), e2, CNT / (e2 / 1000d));

        Aver.IsTrue(e1 < 60);
        Aver.IsTrue(e2 < 60);

        var ratio = e1 / (double)e2;
        Aver.IsTrue(ratio > 0.33d && ratio < 3.0d);
      }
    }


    public class OneObjectField
    {
      public object Data;
    }


    public class TwoByteArrayRefFields
    {
      public byte[] Data1;
      public byte[] Data2;
    }


    public class TwoObjectArrayRefFields
    {
      public object[] Data1;
      public object[] Data2;
    }


    public class ByteArrayArray
    {
      public byte[][] Data1;
      public object Data2;
      public string Z;
    }


    public class ClassA
    {
      [NonSerialized]
      public int Magic1 = 78;

      public int Age;
    }


    public class ClassB : ClassA
    {
      public ClassB()
      {
        Name = "Marat";
      }
      [NonSerialized]
      public int Magic2 = 123;

      [NonSerialized]
      public object Magic3 = new object();

      public string Name;
    }


    public interface ISomeData
    {
      string Data { get; }
    }


    public class SomeDataA : ISomeData
    {
      public string Data { get; set; }
    }


    public class SomeDataB : ISomeData
    {
      public SomeDataB()
      {
        Data = "Unspecified";
      }
      public string Data { get; set; }
    }


    public class ClassWithInterfaceFieldsA
    {
      public int ID;
      public ISomeData Data1;
      public ISomeData Data2;
    }


    public class ClassWithInterfaceFieldsB : ClassWithInterfaceFieldsA
    {
      public ClassWithInterfaceFieldsB()
      {
        WasCtor = true;
      }

      public int Age;
      public ISomeData Data3;

      [NonSerialized]
      public bool WasCtor;
    }

#pragma warning restore SYSLIB0011 // Type or member is obsolete
  }
}
