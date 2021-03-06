using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using TWCore.Compression;
using TWCore.Serialization;
using TWCore.Serialization.MsgPack;
using TWCore.Serialization.NSerializer;
using TWCore.Serialization.PWSerializer;
using TWCore.Serialization.PWSerializer.Deserializer;
using TWCore.Serialization.Utf8Json;
using TWCore.Serialization.WSerializer;
using TWCore.Services;
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable RedundantAssignment
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedVariable

namespace TWCore.Tests
{
    /// <inheritdoc />
    public class SerializerTest : ContainerParameterService
    {
        public SerializerTest() : base("serializertest", "Serializer Test") { }
        protected override void OnHandler(ParameterHandlerInfo info)
        {
            Core.Log.Warning("Starting Serializer TEST");

            //
            //TWCore.Reflection.AssemblyResolverManager.RegisterDomain(new[] { @"C:\AGSW_GIT\Travel\build\Agsw\Engines\Offer\Service" });
            ////TWCore.Reflection.AssemblyResolverManager.GetAssemblyResolver().app
            //var sObject = SerializedObject.FromFileAsync("c:\\temp\\test.sobj").WaitAndResults();
            //var sObjectValue = sObject.GetValue();
            //RunTest(sObjectValue, 500, false);
            //RunTest(sObjectValue, 500, true);
            //

            var sTest = new STest
            {
                FirstName = "Daniel",
                LastName = "Redondo",
                Age = 33
            };

            var collection = new List<List<STest>>();
            for (var i = 0; i <= 10000; i++)
            {
                var colSTest = new List<STest>
                {
                    sTest,sTest,sTest,sTest,sTest,sTest,
                    sTest,sTest,sTest,sTest,sTest,sTest,
                    new STest { FirstName = "Person" , LastName = "Person" + i + "." + i+0, Age = 1 , Brother = sTest },
                    new STest { FirstName = "Person" , LastName = "Person" + i + "." + i+1, Age =2 , Brother = sTest },
                    new STest { FirstName = "Person" , LastName = "Person" + i + "." + i+2, Age = 3 , Brother = sTest },
                    new STest { FirstName = "Person" , LastName = "Person" + i + "." + i+3, Age = 4 , Brother = sTest },
                    new STest { FirstName = "Person" , LastName = "Person" + i + "." + i+4, Age = 5  , Brother = sTest},
                    new STest { FirstName = "Person" , LastName = "Person" + i + "." + i+5, Age = 6  , Brother = sTest},
                    new STest { FirstName = "Person" , LastName = "Person" + i + "." + i+6, Age = 7  , Brother = sTest},
                    new STest { FirstName = "Person" , LastName = "Person" + i + "." + i+7, Age = 8  , Brother = sTest},
                    new STest { FirstName = "Person" , LastName = "Person" + i + "." + i+8, Age = 9  , Brother = sTest},
                    new STest { FirstName = "Person" , LastName = "Person" + i + "." + i+9, Age = 10 , Brother = sTest },
                    new STest { FirstName = "Person" , LastName = "Person" + i + "." + i+10, Age = 11 },
                    new STest { FirstName = "Person" , LastName = "Person" + i + "." + i+11, Age = 12 },
                    new STest { FirstName = "Person" , LastName = "Person" + i + "." + i+12, Age = 13 },
                    new STest { FirstName = "Person" , LastName = "Person" + i + "." + i+13, Age = 14 },
                    new STest { FirstName = "Person" , LastName = "Person" + i + "." + i+14, Age = 15 },
                    new STest { FirstName = "Person" , LastName = "Person" + i + "." + i+15, Age = 16 },
                    new STest { FirstName = "Person" , LastName = "Person" + i + "." + i+16, Age = 17 },
                    new STest { FirstName = "Person" , LastName = "Person" + i + "." + i+17, Age = 18 },
                    new STest { FirstName = "Person" , LastName = "Person" + i + "." + i+18, Age = 19 },
                    new STest2 { FirstName = "Person" , LastName = "Person" + i + "." + i+19, Age = 20, New = "This is a test" }
                };
                collection.Add(colSTest);
            }

            var lt = new List<STest>
            {
                new STest { FirstName = "Name1" , LastName = "LName1" , Age = 11 },
                new STest2 { FirstName = "Name2" , LastName = "LName2", Age = 20, New = "This is a test" }
            };

            var lt2 = new List<Test3>
            {
                new Test3 { Values = new List<int> {2, 3, 4, 5} },
                new Test3 { Values = new List<int> {10, 11, 12, 13} }
            };

            var dct = new Dictionary<string, int>
            {
                ["Value1"] = 1,
                ["Value2"] = 2,
                ["Value3"] = 3,
            };

            var valSer = collection.Where((item, i) => i % 2 == 0).SelectMany(i => i).OrderBy(i => i.FirstName);
            var valSerData = valSer.SerializeToNBinary();
            var valSer2 = valSerData.DeserializeFromNBinary<object>();

            var valSerData2 = valSer.SerializeToWBinary();
            var valSer3 = valSerData2.DeserializeFromWBinary<object>();

            RunTest(collection[0], 100_000, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RunTest(object value, int times, bool useGZip)
        {
            var vType = value?.GetType() ?? typeof(object);
            var compressor = useGZip ? CompressorManager.GetByEncodingType("gzip") : null;
            var memStream = new MemoryStream();
            var jsonSerializer = new JsonTextSerializer { Compressor = compressor };
            var ut8JsonSerializer = new Utf8JsonTextSerializer { Compressor = compressor };
            //var binaryformatterSerializer = new BinaryFormatterSerializer { Compressor = compressor };
            var nBinarySerializer = new NBinarySerializer { Compressor = compressor };
            var wBinarySerializer = new WBinarySerializer { Compressor = compressor };
            var pwBinarySerializer = new PWBinarySerializer { Compressor = compressor };
            
            Core.Log.Warning("Running Serializer Test. Use GZIP = {0}", useGZip);
            Core.Log.WriteEmptyLine();
            Core.Log.InfoBasic("By size:");
            Core.Log.InfoBasic("\tJson Bytes Count: {0}", SerializerSizeProcess(value, vType, jsonSerializer));
            Core.Log.InfoBasic("\tUtf8Json Bytes Count: {0}", SerializerSizeProcess(value, vType, ut8JsonSerializer));
            //Core.Log.InfoBasic("\tBinaryFormatter Bytes Count: {0}", SerializerSizeProcess(value, vType, binaryformatterSerializer));
            Core.Log.InfoBasic("\tNBinary Bytes Count: {0}", SerializerSizeProcess(value, vType, nBinarySerializer));
            Core.Log.InfoBasic("\tWBinary Bytes Count: {0}", SerializerSizeProcess(value, vType, wBinarySerializer));
            Core.Log.InfoBasic("\tPortable WBinary Bytes Count: {0}", SerializerSizeProcess(value, vType, pwBinarySerializer));
            Core.Log.WriteEmptyLine();
            Core.Log.InfoBasic("By Times: {0}", times);
            SerializerProcess("Json", value, vType, times, jsonSerializer, memStream);
            SerializerProcess("Utf8Json", value, vType, times, ut8JsonSerializer, memStream);
            //SerializerProcess("BinaryFormatter", value, vType, times, binaryformatterSerializer, memStream);
            SerializerProcess("NBinary", value, vType, times, nBinarySerializer, memStream);
            SerializerProcess("WBinary", value, vType, times, wBinarySerializer, memStream);
            SerializerProcess("PWBinary", value, vType, times, pwBinarySerializer, memStream);
            Console.ReadLine();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string SerializerSizeProcess(object value, Type valueType, ISerializer serializer)
        {
            var memStream = new MemoryStream();
            serializer.Serialize(value, valueType, memStream);
            memStream.Position = 0;
            var obj = serializer.Deserialize(memStream, valueType);
            return memStream.Length.ToReadableBytes().Text;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SerializerProcess(string name, object value, Type valueType, int times, ISerializer serializer, MemoryStream memStream)
        {
            double totalValue;
            memStream.Position = 0;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Thread.Sleep(1500);
            using (var w = Watch.Create(name + " SERIALIZER"))
            {
                for (var i = 0; i < times; i++)
                {
                    serializer.Serialize(value, valueType, memStream);
                    memStream.Position = 0;
                }
                totalValue = w.GlobalElapsedMilliseconds;
            }
            Core.Log.InfoBasic("\t" + name + " SERIALIZER - Average Time: {0}ms", totalValue / times);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Thread.Sleep(1500);
            using (var w = Watch.Create(name + " DESERIALIZER"))
            {
                for (var i = 0; i < times; i++)
                {
                    serializer.Deserialize(memStream, valueType);
                    memStream.Position = 0;
                }
                totalValue = w.GlobalElapsedMilliseconds;
            }
            Core.Log.InfoBasic("\t"+ name + " DESERIALIZER - Average Time: {0}ms", totalValue / times);
            Thread.Sleep(1000);
            Core.Log.WriteEmptyLine();
        }
    }

    [Serializable]
    public class STest //: INSerializable
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
        public STest Brother { get; set; }

        //void INSerializable.Serialize(SerializersTable table)
        //{
        //    table.WriteValue(FirstName);
        //    table.WriteValue(LastName);
        //    table.WriteValue(Age);
        //    table.WriteGenericValue(Brother);
        //}
    }
    [Serializable]
    public class STest2 : STest//, INSerializable
    {
        public string New { get; set; }

        //void INSerializable.Serialize(SerializersTable table)
        //{
        //    table.WriteValue(FirstName);
        //    table.WriteValue(LastName);
        //    table.WriteValue(Age);
        //    table.WriteGenericValue(Brother);
        //    table.WriteValue(New);
        //}
    }

    public class Test3
    {
        public List<int> Values { get; set; }
    }
}