﻿/*
Copyright 2015-2017 Daniel Adrian Redondo Suarez

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

	http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
 */

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using TWCore.IO;
using TWCore.Serialization.PWSerializer.Deserializer;
using TWCore.Serialization.PWSerializer.Serializer;
using TWCore.Serialization.PWSerializer.Types;
// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable ForCanBeConvertedToForeach

// ReSharper disable InconsistentNaming

namespace TWCore.Serialization.PWSerializer
{
    /// <summary>
    /// Portable Wanhjör Serializer
    /// </summary>
    public class PWSerializerCore
    {
        private static readonly Encoding DefaultUtf8Encoding = new UTF8Encoding(false);
        private static readonly ArrayEqualityComparer<string> StringArrayComparer = new ArrayEqualityComparer<string>(StringComparer.Ordinal);
        private static readonly ObjectPool<(SerializerCache<string[]>, SerializerCache<object>, StringSerializer)> OPool = new ObjectPool<(SerializerCache<string[]>, SerializerCache<object>, StringSerializer)>(pool =>
            {
                var serCacheArr = new SerializerCache<string[]>(SerializerMode.CachedUShort, StringArrayComparer);
                var serCacheObj = new SerializerCache<object>(SerializerMode.CachedUShort);
                var serString = new StringSerializer();
                serString.Init(SerializerMode.CachedUShort);
                serString.Encoding = Encoding.ASCII;
                return (serCacheArr, serCacheObj, serString);
            }
            ,
            i => 
            {
                i.Item1.Clear(SerializerMode.CachedUShort);
                i.Item2.Clear(SerializerMode.CachedUShort);
                i.Item3.Init(SerializerMode.CachedUShort);
            },
            1,
            PoolResetMode.AfterUse);


        /// <summary>
        /// Serializer Mode
        /// </summary>
        public SerializerMode Mode = SerializerMode.Cached2048;

        public PWSerializerCore() { }
        public PWSerializerCore(SerializerMode mode)
        {
            Mode = mode;
        }

        #region Serializer
        private static readonly ConcurrentDictionary<Type, SerializerPlan> SerializationPlans = new ConcurrentDictionary<Type, SerializerPlan>();
        private static readonly SerializerPlanItem[] EndPlan = { new SerializerPlanItem.WriteBytes(new[] { DataType.TypeEnd }) };
        private static readonly ObjectPool<(HashSet<Type>, Stack<SerializerScope>)> SerPool = new ObjectPool<(HashSet<Type>, Stack<SerializerScope>)>(pool => (new HashSet<Type>(), new Stack<SerializerScope>()), (item) =>
        {
            item.Item1.Clear();
            item.Item2.Clear();
        }, 1, PoolResetMode.AfterUse);
        private static readonly ReferencePool<SerializerScope> SerializerScopePool = new ReferencePool<SerializerScope>(1, scope => scope.Init(), null, PoolResetMode.AfterUse);
        private static readonly ReferencePool<SerializerPlanItem.RuntimeValue> SerializerRuntimePool = new ReferencePool<SerializerPlanItem.RuntimeValue>(1, p => p.Init(), null, PoolResetMode.AfterUse);
        private readonly byte[] _bufferSer = new byte[4];

        #region Public Methods
        /// <summary>
        /// Serialize an object value in a Portable Tony Wanhjör format
        /// </summary>
        /// <param name="stream">Stream where the data is going to be stored</param>
        /// <param name="value">Value to be serialized</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Serialize(Stream stream, object value)
        {
            var type = value?.GetType() ?? typeof(object);
            var serPool = SerPool.New();
            var currentSerializerPlanTypes = serPool.Item1;
            var serializersTable = SerializersTable.GetTable(Mode);
            var numberSerializer = serializersTable.NumberSerializer;
            var tuple = OPool.New();
            var typesCache = tuple.Item1;
            var objectCache = tuple.Item2;
            var propertySerializer = tuple.Item3;

            var plan = GetSerializerPlan(currentSerializerPlanTypes, serializersTable, type);
            var scopeStack = serPool.Item2;
            var scope = SerializerScopePool.New();
            scope.Init(plan, type, value);
            scopeStack.Push(scope);

            var bw = new FastBinaryWriter(stream, DefaultUtf8Encoding, true);
            _bufferSer[0] = DataType.PWFileStart;
            _bufferSer[1] = (byte)Mode;
            bw.Write(_bufferSer, 0, 2);
            do
            {
                var item = scope.NextIfAvailable();

                #region Get the Current Scope
                if (item == null)
                {
                    SerializerScopePool.Store(scopeStack.Pop());
                    scope = (scopeStack.Count > 0) ? scopeStack.Peek() : null;
                    continue;
                }
                #endregion

                #region Switch Plan Type

                switch (item.PlanType)
                {
                    #region WriteBytes

                    case SerializerPlanItemType.WriteBytes:
                        bw.Write(item.ValueBytes);
                        continue;

                    #endregion

                    #region TypeStart

                    case SerializerPlanItemType.TypeStart:
                        if (scope.Value == null)
                        {
                            bw.Write(DataType.TypeStart);
                            numberSerializer.WriteValue(bw, 0);
                            numberSerializer.WriteValue(bw, -1);
                            continue;
                        }
                        var oidx = objectCache.SerializerGet(scope.Value);
                        if (oidx > -1)
                        {
                            #region Object Reference
                            switch (oidx)
                            {
                                case 0:
                                    bw.Write(DataType.RefObjectByte0);
                                    break;
                                case 1:
                                    bw.Write(DataType.RefObjectByte1);
                                    break;
                                case 2:
                                    bw.Write(DataType.RefObjectByte2);
                                    break;
                                case 3:
                                    bw.Write(DataType.RefObjectByte3);
                                    break;
                                case 4:
                                    bw.Write(DataType.RefObjectByte4);
                                    break;
                                case 5:
                                    bw.Write(DataType.RefObjectByte5);
                                    break;
                                case 6:
                                    bw.Write(DataType.RefObjectByte6);
                                    break;
                                case 7:
                                    bw.Write(DataType.RefObjectByte7);
                                    break;
                                case 8:
                                    bw.Write(DataType.RefObjectByte8);
                                    break;
                                case 9:
                                    bw.Write(DataType.RefObjectByte9);
                                    break;
                                case 10:
                                    bw.Write(DataType.RefObjectByte10);
                                    break;
                                case 11:
                                    bw.Write(DataType.RefObjectByte11);
                                    break;
                                case 12:
                                    bw.Write(DataType.RefObjectByte12);
                                    break;
                                case 13:
                                    bw.Write(DataType.RefObjectByte13);
                                    break;
                                case 14:
                                    bw.Write(DataType.RefObjectByte14);
                                    break;
                                case 15:
                                    bw.Write(DataType.RefObjectByte15);
                                    break;
                                case 16:
                                    bw.Write(DataType.RefObjectByte16);
                                    break;
                                case 17:
                                    bw.Write(DataType.RefObjectByte17);
                                    break;
                                case 18:
                                    bw.Write(DataType.RefObjectByte18);
                                    break;
                                case 19:
                                    bw.Write(DataType.RefObjectByte19);
                                    break;
                                case 20:
                                    bw.Write(DataType.RefObjectByte20);
                                    break;
                                default:
                                    if (oidx <= byte.MaxValue)
                                        Write(bw, DataType.RefObjectByte, (byte)oidx);
                                    else
                                        Write(bw, DataType.RefObjectUShort, (ushort)oidx);
                                    break;
                            }
                            #endregion
                            SerializerScopePool.Store(scopeStack.Pop());
                            scope = (scopeStack.Count > 0) ? scopeStack.Peek() : null;
                        }
                        else
                        {
                            objectCache.SerializerSet(scope.Value);
                            var tStartItem = (SerializerPlanItem.TypeStart)item;
                            //var valType = scope.Value.GetType();
                            if (item.Type != scope.Type)
                            {
                                var tParts = tStartItem.TypeParts;
                                Write(bw, DataType.TypeName, (byte)tParts.Length);
                                for (var i = 0; i < tParts.Length; i++)
                                    propertySerializer.WriteValue(bw, tParts[i]);
                            }

                            var typeIdx = typesCache.SerializerGet(tStartItem.Properties);
                            if (typeIdx < 0)
                            {
                                #region TypeStart write
                                bw.Write(DataType.TypeStart);
                                var props = tStartItem.Properties;
                                var propsLength = props.Length;
                                numberSerializer.WriteValue(bw, propsLength);
                                for (var i = 0; i < propsLength; i++)
                                    propertySerializer.WriteValue(bw, props[i]);
                                typesCache.SerializerSet(props);
                                #endregion
                            }
                            else
                            {
                                switch (typeIdx)
                                {
                                    case 0: bw.Write(DataType.TypeRefByte0); break;
                                    case 1: bw.Write(DataType.TypeRefByte1); break;
                                    case 2: bw.Write(DataType.TypeRefByte2); break;
                                    case 3: bw.Write(DataType.TypeRefByte3); break;
                                    case 4: bw.Write(DataType.TypeRefByte4); break;
                                    case 5: bw.Write(DataType.TypeRefByte5); break;
                                    case 6: bw.Write(DataType.TypeRefByte6); break;
                                    case 7: bw.Write(DataType.TypeRefByte7); break;
                                    case 8: bw.Write(DataType.TypeRefByte8); break;
                                    case 9: bw.Write(DataType.TypeRefByte9); break;
                                    case 10: bw.Write(DataType.TypeRefByte10); break;
                                    case 11: bw.Write(DataType.TypeRefByte11); break;
                                    case 12: bw.Write(DataType.TypeRefByte12); break;
                                    case 13: bw.Write(DataType.TypeRefByte13); break;
                                    case 14: bw.Write(DataType.TypeRefByte14); break;
                                    case 15: bw.Write(DataType.TypeRefByte15); break;
                                    case 16: bw.Write(DataType.TypeRefByte16); break;
                                    case 17: bw.Write(DataType.TypeRefByte17); break;
                                    case 18: bw.Write(DataType.TypeRefByte18); break;
                                    case 19: bw.Write(DataType.TypeRefByte19); break;
                                    case 20: bw.Write(DataType.TypeRefByte20); break;
                                    case 21: bw.Write(DataType.TypeRefByte21); break;
                                    case 22: bw.Write(DataType.TypeRefByte22); break;
                                    case 23: bw.Write(DataType.TypeRefByte23); break;
                                    case 24: bw.Write(DataType.TypeRefByte24); break;
                                    default:
                                        if (typeIdx <= byte.MaxValue)
                                            Write(bw, DataType.TypeRefByte, (byte)typeIdx);
                                        else
                                            Write(bw, DataType.TypeRefUShort, (ushort)typeIdx);
                                        break;
                                }
                            }

                            if (tStartItem.IsIList)
                                numberSerializer.WriteValue(bw, ((IList)scope.Value).Count);
                            else if (tStartItem.IsIDictionary)
                                numberSerializer.WriteValue(bw, ((IDictionary)scope.Value).Count);
                            else
                                numberSerializer.WriteValue(bw, 0);
                        }
                        continue;

                    #endregion

                    #region ListStart

                    case SerializerPlanItemType.ListStart:
                        if (scope.Value != null)
                        {
                            var lType = (SerializerPlanItem.ListStart)item;
                            var iList = (IList)scope.Value;
                            var iListCount = iList.Count;
                            if (iListCount > 0)
                            {
                                bw.Write(DataType.ListStart);
                                var aPlan = new SerializerPlanItem.RuntimeValue[iListCount];
                                for (var i = 0; i < iListCount; i++)
                                {
                                    var itemList = iList[i];
                                    itemList = ResolveLinqEnumerables(itemList);
                                    var itemType = itemList?.GetType() ?? lType.InnerType;
                                    var srpVal = SerializerRuntimePool.New();
                                    var serValue = serializersTable.GetSerializerByValueType(itemType);
                                    srpVal.Init(lType.InnerType, serValue?.GetType(), itemList);
                                    aPlan[i] = srpVal;
                                }
                                scope = SerializerScopePool.New();
                                scope.Init(aPlan, scope.Type);
                                scopeStack.Push(scope);
                            }
                            else
                            {
                                scope.ReplacePlan(EndPlan);
                            }
                        }
                        continue;

                    #endregion

                    #region DictionaryStart

                    case SerializerPlanItemType.DictionaryStart:
                        if (scope.Value != null)
                        {
                            var dictioItem = (SerializerPlanItem.DictionaryStart)item;
                            var iDictio = (IDictionary)scope.Value;
                            var iDictioCount = iDictio.Count;
                            if (iDictioCount > 0)
                            {
                                bw.Write(DataType.DictionaryStart);
                                var aPlan = new SerializerPlanItem.RuntimeValue[iDictioCount * 2];
                                var aIdx = 0;
                                foreach (var keyValue in iDictio.Keys)
                                {
                                    var kv = ResolveLinqEnumerables(keyValue);
                                    var valueValue = iDictio[keyValue];
                                    valueValue = ResolveLinqEnumerables(valueValue);
                                    var aPlanKeyVal = SerializerRuntimePool.New();
                                    aPlanKeyVal.Init(dictioItem.KeyType, dictioItem.KeySerializerType, kv);
                                    aPlan[aIdx++] = aPlanKeyVal;

                                    var aPlanValVal = SerializerRuntimePool.New();
                                    aPlanValVal.Init(dictioItem.ValueType, dictioItem.ValueSerializerType, valueValue);
                                    aPlan[aIdx++] = aPlanValVal;
                                }
                                scope = SerializerScopePool.New();
                                scope.Init(aPlan, scope.Type);
                                scopeStack.Push(scope);
                            }
                            else
                            {
                                scope.ReplacePlan(EndPlan);
                            }
                        }
                        continue;

                    #endregion

                    #region PropertyValue

                    case SerializerPlanItemType.PropertyValue:
                        var cItem = (SerializerPlanItem.PropertyValue)item;
                        var pVal = cItem.Property.GetValue(scope.Value);
                        if (pVal == cItem.DefaultValue)
                            bw.Write(DataType.ValueNull);
                        else
                            serializersTable.Write(cItem.SerializerType, bw, pVal);
                        continue;

                    #endregion

                    #region PropertyReference

                    case SerializerPlanItemType.PropertyReference:
                        var rItem = (SerializerPlanItem.PropertyReference)item;
                        var rVal = rItem.Property.GetValue(scope.Value);
                        if (rVal == null)
                            bw.Write(DataType.ValueNull);
                        else
                        {
                            rVal = ResolveLinqEnumerables(rVal);
                            scope = SerializerScopePool.New();
                            scope.Init(GetSerializerPlan(currentSerializerPlanTypes, serializersTable, rVal?.GetType() ?? rItem.Type), rItem.Type, rVal);
                            scopeStack.Push(scope);
                        }
                        continue;

                    #endregion

                    #region Value

                    case SerializerPlanItemType.Value:
                        var vItem = (SerializerPlanItem.ValueItem)item;
                        if (scope.Value == null)
                            bw.Write(DataType.ValueNull);
                        else if (vItem.SerializerType != null)
                            serializersTable.Write(vItem.SerializerType, bw, scope.Value);
                        else
                        {
                            scope = SerializerScopePool.New();
                            scope.Init(GetSerializerPlan(currentSerializerPlanTypes, serializersTable, scope.Value?.GetType() ?? vItem.Type), vItem.Type, scope.Value);
                            scopeStack.Push(scope);
                        }
                        continue;

                    #endregion

                    #region RuntimeValue

                    case SerializerPlanItemType.RuntimeValue:
                        var rvItem = (SerializerPlanItem.RuntimeValue)item;
                        if (rvItem.Value == null)
                            bw.Write(DataType.ValueNull);
                        else if (rvItem.SerializerType != null)
                            serializersTable.Write(rvItem.SerializerType, bw, rvItem.Value);
                        else
                        {
                            scope = SerializerScopePool.New();
                            scope.Init(GetSerializerPlan(currentSerializerPlanTypes, serializersTable, rvItem.Value?.GetType() ?? rvItem.Type), rvItem.Type, rvItem.Value);
                            scopeStack.Push(scope);
                        }
                        SerializerRuntimePool.Store(rvItem);
                        continue;

                    #endregion

                    default:
                        break;
                }

                #endregion

            } while (scope != null);
            SerPool.Store(serPool);
            SerializersTable.ReturnTable(serializersTable);
            OPool.Store(tuple);
        }
        #endregion

        #region Private Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write(FastBinaryWriter bw, byte type, byte value)
        {
            _bufferSer[0] = type;
            _bufferSer[1] = value;
            bw.Write(_bufferSer, 0, 2);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void Write(FastBinaryWriter bw, byte type, ushort value)
        {
            _bufferSer[0] = type;
            fixed (byte* b = &_bufferSer[1])
                *((ushort*)b) = value;
            bw.Write(_bufferSer, 0, 3);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static SerializerPlan GetSerializerPlan(HashSet<Type> currentSerializerPlanTypes, SerializersTable serializerTable, Type type)
        {
            return SerializationPlans.GetOrAdd(type, iType =>
            {
                var plan = new List<SerializerPlanItem>();
                var typeInfo = iType.GetTypeInfo();
                var genTypeDefinition = typeInfo.IsGenericType ? typeInfo.GetGenericTypeDefinition() : null;
                var serBase = serializerTable.GetSerializerByValueType(iType);
                var isIList = false;
                var isIDictionary = false;

                if (serBase != null)
                {
                    //Value type
                    plan.Add(new SerializerPlanItem.ValueItem(iType, serBase.GetType()));
                }
                else if (genTypeDefinition == typeof(Nullable<>))
                {
                    //Nullable type
                    iType = Nullable.GetUnderlyingType(iType);
                    serBase = serializerTable.GetSerializerByValueType(iType);
                    plan.Add(new SerializerPlanItem.ValueItem(iType, serBase.GetType()));
                }
                else
                {
                    currentSerializerPlanTypes.Add(iType);
                    var tStart = new SerializerPlanItem.TypeStart(iType);
                    plan.Add(tStart);
                    var endBytes = new List<byte>();

                    if (typeInfo.ImplementedInterfaces.Any(i => i == typeof(IList) || i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>)))
                        isIList = true;
                    if (typeInfo.ImplementedInterfaces.Any(i => i == typeof(IDictionary) || i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
                        isIDictionary = true;

                    tStart.IsIList = isIList;
                    tStart.IsIDictionary = isIDictionary;

                    #region Properties
                    var properties = type.GetRuntimeProperties().OrderBy(n => n.Name).ToArray();
                    var propNames = new List<string>();
                    foreach (var prop in properties)
                    {
                        if (!prop.CanRead || !prop.CanWrite) continue;

                        if (isIList && prop.Name == "Capacity")
                            continue;
                        if (prop.GetAttribute<NonSerializeAttribute>() != null)
                            continue;
                        if (prop.GetIndexParameters().Length > 0)
                            continue;
                        var propType = prop.PropertyType;
                        var propTypeInfo = propType.GetTypeInfo();
                        var propIsNullable = propTypeInfo.IsGenericType && propTypeInfo.GetGenericTypeDefinition() == typeof(Nullable<>);
                        if (propIsNullable)
                            propType = Nullable.GetUnderlyingType(propType);
                        var serType = serializerTable.GetSerializerByValueType(propType)?.GetType();
                        propNames.Add(prop.Name);
                        if (serType == null)
                        {
                            plan.Add(new SerializerPlanItem.PropertyReference(prop));
                            if (!currentSerializerPlanTypes.Contains(propType))
                                GetSerializerPlan(currentSerializerPlanTypes, serializerTable, propType);
                        }
                        else
                            plan.Add(new SerializerPlanItem.PropertyValue(prop, serType, propIsNullable));
                    }
                    tStart.Properties = propNames.ToArray();
                    #endregion

                    #region ListInfo
                    if (isIList)
                    {
                        var ifaces = typeInfo.ImplementedInterfaces;
                        var ilist = ifaces.FirstOrDefault(i => i == typeof(IList) || (i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>)));
                        if (ilist != null)
                        {
                            Type innerType = null;
                            if (type.IsArray)
                                innerType = type.GetElementType();
                            else
                            {
                                var gargs = ilist.GenericTypeArguments;
                                if (gargs.Length == 0)
                                    gargs = type.GenericTypeArguments;
                                if (gargs.Length > 0)
                                    innerType = gargs[0];
                                else
                                {
                                    var iListType = typeInfo.ImplementedInterfaces.FirstOrDefault(m => (m.GetTypeInfo().IsGenericType && m.GetGenericTypeDefinition() == typeof(IList<>)));
                                    if (iListType != null && iListType.GenericTypeArguments.Length > 0)
                                        innerType = iListType.GenericTypeArguments[0];
                                }
                            }
                            plan.Add(new SerializerPlanItem.ListStart(type, innerType));
                            if (!currentSerializerPlanTypes.Contains(innerType))
                                GetSerializerPlan(currentSerializerPlanTypes, serializerTable, innerType);
                            endBytes.Add(DataType.ListEnd);
                        }
                    }
                    #endregion

                    #region DictionaryInfo
                    if (isIDictionary)
                    {
                        var ifaces = typeInfo.ImplementedInterfaces;
                        var idictio = ifaces.FirstOrDefault(i => i == typeof(IDictionary) || i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
                        if (idictio != null)
                        {
                            //KeyValye Type
                            var types = idictio.GenericTypeArguments;
                            var keyType = types[0];
                            var keyTypeInfo = keyType.GetTypeInfo();
                            var keyIsNullable = keyTypeInfo.IsGenericType && keyTypeInfo.GetGenericTypeDefinition() == typeof(Nullable<>);
                            var keySer = keyIsNullable ? serializerTable.GetSerializerByValueType(Nullable.GetUnderlyingType(keyType)) : serializerTable.GetSerializerByValueType(keyType);

                            var valueType = types[1];
                            var valueTypeInfo = valueType.GetTypeInfo();
                            var valueIsNullable = valueTypeInfo.IsGenericType && valueTypeInfo.GetGenericTypeDefinition() == typeof(Nullable<>);
                            var valueSer = valueIsNullable ? serializerTable.GetSerializerByValueType(Nullable.GetUnderlyingType(valueType)) : serializerTable.GetSerializerByValueType(valueType);

                            if (keySer == null && !currentSerializerPlanTypes.Contains(keyType))
                                GetSerializerPlan(currentSerializerPlanTypes, serializerTable, keyType);
                            if (valueSer == null && !currentSerializerPlanTypes.Contains(valueType))
                                GetSerializerPlan(currentSerializerPlanTypes, serializerTable, valueType);

                            plan.Add(new SerializerPlanItem.DictionaryStart(type, keyType, keySer?.GetType(), keyIsNullable, valueType, valueSer?.GetType(), valueIsNullable));
                            endBytes.Add(DataType.DictionaryEnd);
                        }
                    }
                    #endregion

                    endBytes.Add(DataType.TypeEnd);
                    plan.Add(new SerializerPlanItem.WriteBytes(endBytes.ToArray()));
                    currentSerializerPlanTypes.Remove(iType);
                }
                var sPlan = new SerializerPlan();
                sPlan.Init(plan.ToArray(), iType, isIList, isIDictionary);
                return sPlan;
            });
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object ResolveLinqEnumerables(object value)
        {
            if (value is IEnumerable ieValue && !(value is string))
                value = ieValue.Enumerate();
            return value;
        }
        #endregion

        #endregion

        #region Deserializer
        private static readonly ObjectPool<(Dictionary<Type, Type[]>, Stack<DeserializerType>)> DesPool = new ObjectPool<(Dictionary<Type, Type[]>, Stack<DeserializerType>)>(pool => (new Dictionary<Type, Type[]>(), new Stack<DeserializerType>()), (item) =>
         {
             item.Item1.Clear();
             item.Item2.Clear();
         }, 1, PoolResetMode.AfterUse);
        private static readonly ReferencePool<DeserializerType> DesarializerTypePool = new ReferencePool<DeserializerType>(1, d => d.Clear());
        private static readonly ReferencePool<Stack<DynamicDeserializedType>> GdStackPool = new ReferencePool<Stack<DynamicDeserializedType>>(1, s => s.Clear());
        private readonly byte[] _bufferDes = new byte[8];

        /// <summary>
        /// Deserialize a Portable Tony Wanhjor stream into a object value
        /// </summary>
        /// <param name="stream">Stream where the data is going to be readed (source data)</param>
        /// <param name="type">Declared type of the value to be serialized</param>
        /// <returns>Deserialized object instance</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Deserialize(Stream stream, Type type)
        {
            if (type == null)
                return Deserialize(stream);
            var br = new FastBinaryReader(stream, DefaultUtf8Encoding, true);
            if (br.Read(_bufferDes, 0, 2) != 2)
                throw new EndOfStreamException("Error reading the PW header.");
            var fStart = _bufferDes[0];
            var sMode = (SerializerMode)_bufferDes[1];
            if (fStart != DataType.PWFileStart)
                throw new FormatException(string.Format("The stream is not in PWBinary format. Byte {0} was expected, received: {1}", DataType.PWFileStart, fStart));
            Mode = sMode;

            var desPool = DesPool.New();
            var serializersTable = DeserializersTable.GetTable(sMode);
            var numberSerializer = serializersTable.NumberSerializer;
            var tuple = OPool.New();
            var typesCache = tuple.Item1;
            var objectCache = tuple.Item2;
            var propertySerializer = tuple.Item3;
            var propertiesTypes = desPool.Item1;
            var desStack = desPool.Item2;
            DeserializerType typeItem = null;
            Type valueType = null;

            do
            {
                var currentType = valueType ?? typeItem?.CurrentType ?? type;
                var currentByte = br.ReadByte();
                switch (currentByte)
                {
                    #region TypeStart
                    case DataType.TypeName:
                        var vTypePartsLength = (int)br.ReadByte();
                        var vTypeParts = new string[vTypePartsLength];
                        for (var i = 0; i < vTypePartsLength; i++)
                            vTypeParts[i] = propertySerializer.ReadValue(br);
                        var vType = string.Join(",", vTypeParts);
                        valueType = Core.GetType(vType, false) ?? valueType;
                        continue;
                    case DataType.TypeStart:
                        var typePropertiesLength = numberSerializer.ReadValue(br);
                        var typeProperties = new string[typePropertiesLength];
                        for (var i = 0; i < typePropertiesLength; i++)
                            typeProperties[i] = propertySerializer.ReadValue(br);
                        var length = numberSerializer.ReadValue(br);
                        typesCache.DeserializerSet(typeProperties);
                        typeItem = DesarializerTypePool.New();
                        typeItem.SetProperties(currentType, typeProperties, propertiesTypes, length);
                        objectCache.DeserializerSet(typeItem.Value);
                        desStack.Push(typeItem);
                        valueType = null;
                        continue;
                    case DataType.TypeRefByte0:
                    case DataType.TypeRefByte1:
                    case DataType.TypeRefByte2:
                    case DataType.TypeRefByte3:
                    case DataType.TypeRefByte4:
                    case DataType.TypeRefByte5:
                    case DataType.TypeRefByte6:
                    case DataType.TypeRefByte7:
                    case DataType.TypeRefByte8:
                    case DataType.TypeRefByte9:
                    case DataType.TypeRefByte10:
                    case DataType.TypeRefByte11:
                    case DataType.TypeRefByte12:
                    case DataType.TypeRefByte13:
                    case DataType.TypeRefByte14:
                    case DataType.TypeRefByte15:
                    case DataType.TypeRefByte16:
                    case DataType.TypeRefByte17:
                    case DataType.TypeRefByte18:
                    case DataType.TypeRefByte19:
                    case DataType.TypeRefByte20:
                    case DataType.TypeRefByte21:
                    case DataType.TypeRefByte22:
                    case DataType.TypeRefByte23:
                    case DataType.TypeRefByte24:
                        var byteIdx = currentByte - DataType.TypeRefByte0;
                        typeProperties = typesCache.DeserializerGet(byteIdx);
                        length = numberSerializer.ReadValue(br);
                        typeItem = DesarializerTypePool.New();
                        typeItem.SetProperties(currentType, typeProperties, propertiesTypes, length);
                        objectCache.DeserializerSet(typeItem.Value);
                        desStack.Push(typeItem);
                        valueType = null;
                        continue;
                    case DataType.TypeRefByte:
                        var refByteVal = br.ReadByte();
                        typeProperties = typesCache.DeserializerGet(refByteVal);
                        length = numberSerializer.ReadValue(br);
                        typeItem = DesarializerTypePool.New();
                        typeItem.SetProperties(currentType, typeProperties, propertiesTypes, length);
                        objectCache.DeserializerSet(typeItem.Value);
                        desStack.Push(typeItem);
                        valueType = null;
                        continue;
                    case DataType.TypeRefUShort:
                        var refUshortVal = br.ReadUInt16();
                        typeProperties = typesCache.DeserializerGet(refUshortVal);
                        length = numberSerializer.ReadValue(br);
                        typeItem = DesarializerTypePool.New();
                        typeItem.SetProperties(currentType, typeProperties, propertiesTypes, length);
                        objectCache.DeserializerSet(typeItem.Value);
                        desStack.Push(typeItem);
                        valueType = null;
                        continue;
                    #endregion

                    #region TypeEnd
                    case DataType.TypeEnd:
                        var lastItem = desStack.Pop();
                        if (desStack.Count > 0)
                        {
                            typeItem = desStack.Peek();
                            typeItem.AddValue(lastItem.Value);
                            DesarializerTypePool.Store(lastItem);
                        }
                        continue;
                    #endregion

                    #region ListStart
                    case DataType.ListStart:
                        typeItem?.ListStart();
                        continue;
                    #endregion

                    #region ListEnd
                    case DataType.ListEnd:
                        typeItem?.ListEnd();
                        continue;
                    #endregion

                    #region DictionaryStart
                    case DataType.DictionaryStart:
                        typeItem?.DictionaryStart();
                        continue;
                    #endregion

                    #region DictionaryEnd
                    case DataType.DictionaryEnd:
                        typeItem?.DictionaryEnd();
                        continue;
                    #endregion

                    #region Ref Object
                    case DataType.RefObjectByte0:
                    case DataType.RefObjectByte1:
                    case DataType.RefObjectByte2:
                    case DataType.RefObjectByte3:
                    case DataType.RefObjectByte4:
                    case DataType.RefObjectByte5:
                    case DataType.RefObjectByte6:
                    case DataType.RefObjectByte7:
                    case DataType.RefObjectByte8:
                    case DataType.RefObjectByte9:
                    case DataType.RefObjectByte10:
                    case DataType.RefObjectByte11:
                    case DataType.RefObjectByte12:
                    case DataType.RefObjectByte13:
                    case DataType.RefObjectByte14:
                    case DataType.RefObjectByte15:
                    case DataType.RefObjectByte16:
                    case DataType.RefObjectByte17:
                    case DataType.RefObjectByte18:
                    case DataType.RefObjectByte19:
                    case DataType.RefObjectByte20:
                    case DataType.RefObjectUShort:
                    case DataType.RefObjectByte:
                        var objRef = -1;
                        #region Get Object Reference

                        switch (currentByte)
                        {
                            case DataType.RefObjectByte0:
                                objRef = 0;
                                break;
                            case DataType.RefObjectByte1:
                                objRef = 1;
                                break;
                            case DataType.RefObjectByte2:
                                objRef = 2;
                                break;
                            case DataType.RefObjectByte3:
                                objRef = 3;
                                break;
                            case DataType.RefObjectByte4:
                                objRef = 4;
                                break;
                            case DataType.RefObjectByte5:
                                objRef = 5;
                                break;
                            case DataType.RefObjectByte6:
                                objRef = 6;
                                break;
                            case DataType.RefObjectByte7:
                                objRef = 7;
                                break;
                            case DataType.RefObjectByte8:
                                objRef = 8;
                                break;
                            case DataType.RefObjectByte9:
                                objRef = 9;
                                break;
                            case DataType.RefObjectByte10:
                                objRef = 10;
                                break;
                            case DataType.RefObjectByte11:
                                objRef = 11;
                                break;
                            case DataType.RefObjectByte12:
                                objRef = 12;
                                break;
                            case DataType.RefObjectByte13:
                                objRef = 13;
                                break;
                            case DataType.RefObjectByte14:
                                objRef = 14;
                                break;
                            case DataType.RefObjectByte15:
                                objRef = 15;
                                break;
                            case DataType.RefObjectByte16:
                                objRef = 16;
                                break;
                            case DataType.RefObjectByte17:
                                objRef = 17;
                                break;
                            case DataType.RefObjectByte18:
                                objRef = 18;
                                break;
                            case DataType.RefObjectByte19:
                                objRef = 19;
                                break;
                            case DataType.RefObjectByte20:
                                objRef = 20;
                                break;
                            case DataType.RefObjectByte:
                                objRef = br.ReadByte();
                                break;
                            case DataType.RefObjectUShort:
                                objRef = br.ReadUInt16();
                                break;
                        }
                        #endregion
                        var objValue = objectCache.DeserializerGet(objRef);
                        typeItem?.AddValue(objValue);
                        continue;
                    #endregion

                    #region ValueNull
                    case DataType.ValueNull:
                        typeItem?.AddValue(null);
                        continue;
                    #endregion

                    default:
                        var value = serializersTable.Read(br, currentByte);
                        if (typeItem != null)
                            typeItem.AddValue(value);
                        else
                            return DataTypeHelper.Change(value, type);
                        break;
                }
            }
            while (desStack.Count > 0);

            OPool.Store(tuple);
            DesPool.Store(desPool);
            DeserializersTable.ReturnTable(serializersTable);
            return typeItem?.Value;
        }

        /// <summary>
        /// Generic Deserialize a Portable Tony Wanhjor stream into a object value
        /// </summary>
        /// <param name="stream">Stream where the data is going to be readed (source data)</param>
        /// <returns>Deserialized object instance</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Deserialize(Stream stream)
        {
            var br = new FastBinaryReader(stream, DefaultUtf8Encoding, true);
            if (br.Read(_bufferDes, 0, 2) != 2)
                throw new EndOfStreamException("Error reading the PW header.");
            var fStart = _bufferDes[0];
            var sMode = (SerializerMode)_bufferDes[1];
            Mode = sMode;
            if (fStart != DataType.PWFileStart)
                throw new FormatException(string.Format("The stream is not in PWBinary format. Byte {0} was expected, received: {1}", DataType.PWFileStart, fStart));

            var serializersTable = DeserializersTable.GetTable(sMode);
            var numberSerializer = serializersTable.NumberSerializer;
            var tuple = OPool.New();
            var typesCache = tuple.Item1;
            var objectCache = tuple.Item2;
            var propertySerializer = tuple.Item3;

            var desStack = GdStackPool.New();
            DynamicDeserializedType typeItem = null;
            string valueType = null;
            do
            {
                var currentByte = br.ReadByte();
                switch (currentByte)
                {
                    #region TypeStart
                    case DataType.TypeName:
                        var vTypePartsLength = (int)br.ReadByte();
                        var vTypeParts = new string[vTypePartsLength];
                        for (var i = 0; i < vTypePartsLength; i++)
                            vTypeParts[i] = propertySerializer.ReadValue(br);
                        valueType = string.Join(",", vTypeParts);
                        continue;
                    case DataType.TypeStart:
                        var typePropertiesLength = numberSerializer.ReadValue(br);
                        var typeProperties = new string[typePropertiesLength];
                        for (var i = 0; i < typePropertiesLength; i++)
                            typeProperties[i] = propertySerializer.ReadValue(br);
                        var length = numberSerializer.ReadValue(br);
                        typesCache.DeserializerSet(typeProperties);
                        typeItem = new DynamicDeserializedType(valueType, typeProperties, length);
                        objectCache.DeserializerSet(typeItem);
                        desStack.Push(typeItem);
                        valueType = null;
                        continue;
                    case DataType.TypeRefByte0:
                    case DataType.TypeRefByte1:
                    case DataType.TypeRefByte2:
                    case DataType.TypeRefByte3:
                    case DataType.TypeRefByte4:
                    case DataType.TypeRefByte5:
                    case DataType.TypeRefByte6:
                    case DataType.TypeRefByte7:
                    case DataType.TypeRefByte8:
                    case DataType.TypeRefByte9:
                    case DataType.TypeRefByte10:
                    case DataType.TypeRefByte11:
                    case DataType.TypeRefByte12:
                    case DataType.TypeRefByte13:
                    case DataType.TypeRefByte14:
                    case DataType.TypeRefByte15:
                    case DataType.TypeRefByte16:
                    case DataType.TypeRefByte17:
                    case DataType.TypeRefByte18:
                    case DataType.TypeRefByte19:
                    case DataType.TypeRefByte20:
                    case DataType.TypeRefByte21:
                    case DataType.TypeRefByte22:
                    case DataType.TypeRefByte23:
                    case DataType.TypeRefByte24:
                        var byteIdx = currentByte - DataType.TypeRefByte0;
                        typeProperties = typesCache.DeserializerGet(byteIdx);
                        length = numberSerializer.ReadValue(br);
                        typeItem = new DynamicDeserializedType(valueType, typeProperties, length);
                        objectCache.DeserializerSet(typeItem);
                        desStack.Push(typeItem);
                        valueType = null;
                        continue;
                    case DataType.TypeRefByte:
                        var refByteVal = br.ReadByte();
                        typeProperties = typesCache.DeserializerGet(refByteVal);
                        length = numberSerializer.ReadValue(br);
                        typeItem = new DynamicDeserializedType(valueType, typeProperties, length);
                        objectCache.DeserializerSet(typeItem);
                        desStack.Push(typeItem);
                        valueType = null;
                        continue;
                    case DataType.TypeRefUShort:
                        var refUshortVal = br.ReadUInt16();
                        typeProperties = typesCache.DeserializerGet(refUshortVal);
                        length = numberSerializer.ReadValue(br);
                        typeItem = new DynamicDeserializedType(valueType, typeProperties, length);
                        objectCache.DeserializerSet(typeItem);
                        desStack.Push(typeItem);
                        valueType = null;
                        continue;
                    #endregion

                    #region TypeEnd

                    case DataType.TypeEnd:
                        var lastItem = desStack.Pop();
                        if (desStack.Count > 0)
                        {
                            typeItem = desStack.Peek();
                            typeItem.AddValue(lastItem);
                        }
                        continue;

                    #endregion

                    #region ListStart

                    case DataType.ListStart:
                        typeItem?.ListStart();
                        continue;

                    #endregion

                    #region ListEnd

                    case DataType.ListEnd:
                        typeItem?.ListEnd();
                        continue;

                    #endregion

                    #region DictionaryStart

                    case DataType.DictionaryStart:
                        typeItem?.DictionaryStart();
                        continue;

                    #endregion

                    #region DictionaryEnd

                    case DataType.DictionaryEnd:
                        typeItem?.DictionaryEnd();
                        continue;

                    #endregion

                    #region Ref Object

                    case DataType.RefObjectByte0:
                    case DataType.RefObjectByte1:
                    case DataType.RefObjectByte2:
                    case DataType.RefObjectByte3:
                    case DataType.RefObjectByte4:
                    case DataType.RefObjectByte5:
                    case DataType.RefObjectByte6:
                    case DataType.RefObjectByte7:
                    case DataType.RefObjectByte8:
                    case DataType.RefObjectByte9:
                    case DataType.RefObjectByte10:
                    case DataType.RefObjectByte11:
                    case DataType.RefObjectByte12:
                    case DataType.RefObjectByte13:
                    case DataType.RefObjectByte14:
                    case DataType.RefObjectByte15:
                    case DataType.RefObjectByte16:
                    case DataType.RefObjectByte17:
                    case DataType.RefObjectByte18:
                    case DataType.RefObjectByte19:
                    case DataType.RefObjectByte20:
                    case DataType.RefObjectUShort:
                    case DataType.RefObjectByte:
                        var objRef = -1;

                        #region Get Object Reference

                        switch (currentByte)
                        {
                            case DataType.RefObjectByte0:
                                objRef = 0;
                                break;
                            case DataType.RefObjectByte1:
                                objRef = 1;
                                break;
                            case DataType.RefObjectByte2:
                                objRef = 2;
                                break;
                            case DataType.RefObjectByte3:
                                objRef = 3;
                                break;
                            case DataType.RefObjectByte4:
                                objRef = 4;
                                break;
                            case DataType.RefObjectByte5:
                                objRef = 5;
                                break;
                            case DataType.RefObjectByte6:
                                objRef = 6;
                                break;
                            case DataType.RefObjectByte7:
                                objRef = 7;
                                break;
                            case DataType.RefObjectByte8:
                                objRef = 8;
                                break;
                            case DataType.RefObjectByte9:
                                objRef = 9;
                                break;
                            case DataType.RefObjectByte10:
                                objRef = 10;
                                break;
                            case DataType.RefObjectByte11:
                                objRef = 11;
                                break;
                            case DataType.RefObjectByte12:
                                objRef = 12;
                                break;
                            case DataType.RefObjectByte13:
                                objRef = 13;
                                break;
                            case DataType.RefObjectByte14:
                                objRef = 14;
                                break;
                            case DataType.RefObjectByte15:
                                objRef = 15;
                                break;
                            case DataType.RefObjectByte16:
                                objRef = 16;
                                break;
                            case DataType.RefObjectByte17:
                                objRef = 17;
                                break;
                            case DataType.RefObjectByte18:
                                objRef = 18;
                                break;
                            case DataType.RefObjectByte19:
                                objRef = 19;
                                break;
                            case DataType.RefObjectByte20:
                                objRef = 20;
                                break;
                            case DataType.RefObjectByte:
                                objRef = br.ReadByte();
                                break;
                            case DataType.RefObjectUShort:
                                objRef = br.ReadUInt16();
                                break;
                        }

                        #endregion

                        var objValue = objectCache.DeserializerGet(objRef);
                        typeItem?.AddValue(objValue);
                        continue;

                    #endregion

                    #region ValueNull

                    case DataType.ValueNull:
                        typeItem?.AddValue(null);
                        continue;

                    #endregion

                    default:
                        var value = serializersTable.Read(br, currentByte);
                        if (typeItem != null)
                            typeItem.AddValue(value);
                        else
                            return value;
                        break;
                }
            }
            while (desStack.Count > 0);

            OPool.Store(tuple);
            GdStackPool.Store(desStack);
            DeserializersTable.ReturnTable(serializersTable);
            return typeItem;
        }
        #endregion
    }
}
