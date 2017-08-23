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
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using TWCore.Reflection;

namespace TWCore.Serialization.WSerializer.Serializer
{
    internal abstract class SerializerPlanItem
    {
        #region Static
        static ConcurrentDictionary<Type, string> typeNames = new ConcurrentDictionary<Type, string>();
        static ConcurrentDictionary<Type, Tuple<string, string, string>> typeNamesTuples = new ConcurrentDictionary<Type, Tuple<string, string, string>>();
        #endregion

        #region Fields and Properties
        public byte PlanType;
        public Type Type;
        public byte[] ValueBytes;
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetTypeName(Type type)
        {
            return typeNames.GetOrAdd(type, _type =>
            {
                var typeInfo = _type.GetTypeInfo();
                var assembly = typeInfo.Assembly;
                var assemblyName = new AssemblyName(assembly.FullName);

                string mType = null;
                if (typeInfo.IsGenericType)
                    mType = string.Format("{0}.{1}[{2}]", typeInfo.Namespace, typeInfo.Name, typeInfo.GenericTypeArguments.Select(a => "[" + GetTypeName(a) + "]").ToArray().Join(","));
                else
                    mType = type.FullName;
                if (mType != null && assemblyName.Name != "mscorlib")
                    mType += "," + assemblyName.Name;
                return mType;
            });
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Tuple<string, string, string> GetTypeNameTuple(Type type)
        {
            return typeNamesTuples.GetOrAdd(type, _type =>
            {
                var typeInfo = _type.GetTypeInfo();
                var assembly = typeInfo.Assembly;
                var assemblyName = new AssemblyName(assembly.FullName);

                string mNamespace = null;
                string mType = null;
                string asmName = null;


                if (typeInfo.IsGenericType)
                {
                    mNamespace = typeInfo.Namespace;
                    if (typeInfo.GenericTypeArguments.Length > 0)
                        mType = string.Format("{0}[{1}]", typeInfo.Name, typeInfo.GenericTypeArguments.Select(a => "[" + GetTypeName(a) + "]").ToArray().Join(","));
                    else
                        mType = typeInfo.Name;
                }
                else
                {
                    mNamespace = type.Namespace;
                    mType = type.Name;
                }
                if (mType != null && assemblyName.Name != "mscorlib")
                    asmName = assemblyName.Name;

                if (type.DeclaringType != null)
                {
                    mType = type.DeclaringType.Name + "+" + mType;
                }
                return Tuple.Create(mNamespace, mType, asmName);
            });
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetDataType(Type type)
        {
            if (type == typeof(string))
                return DataType.TypeNameString;
            else if (type == typeof(int))
                return DataType.TypeNameInt;
            else if (type == typeof(Guid))
                return DataType.TypeNameGuid;
            else if (type == typeof(DateTime))
                return DataType.TypeNameDateTime;
            else if (type == typeof(TimeSpan))
                return DataType.TypeNameTimeSpan;
            else if (type == typeof(decimal))
                return DataType.TypeNameDecimal;
            else if (type == typeof(bool))
                return DataType.TypeNameBool;
            else if (type == typeof(double))
                return DataType.TypeNameDouble;
            else if (type == typeof(float))
                return DataType.TypeNameFloat;
            else if (type == typeof(ulong))
                return DataType.TypeNameULong;
            else if (type == typeof(long))
                return DataType.TypeNameLong;
            else if (type == typeof(uint))
                return DataType.TypeNameUInt;
            else if (type == typeof(ushort))
                return DataType.TypeNameUShort;
            else if (type == typeof(short))
                return DataType.TypeNameShort;
            else if (type == typeof(sbyte))
                return DataType.TypeNameSByte;
            else if (type == typeof(byte))
                return DataType.TypeNameByte;
            else if (type == typeof(char))
                return DataType.TypeNameChar;
            else if (type == typeof(DateTimeOffset))
                return DataType.TypeNameDateTimeOffset;
            return DataType.Unknown;
        }
        #endregion




        #region Nested Classes

        #region WriteByte
        public class WriteBytes : SerializerPlanItem
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public WriteBytes(byte[] values)
            {
                PlanType = SerializerPlanItemType.WriteBytes;
                ValueBytes = values;
            }
        }
        #endregion

        #region TypeStart
        public class TypeStart : SerializerPlanItem
        {
            public string TypeNamespace;
            public string TypeName;
            public string TypeAssembly;
            public int Quantity;
            public Type[] Types;
            public byte[] DTypes;
            public string[] TypeNamespaces;
            public string[] TypeNames;
            public string[] TypeAssemblies;
            public bool IsArray;
            public string[] Properties;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TypeStart(Type type, TypeInfo typeInfo)
            {
                PlanType = SerializerPlanItemType.TypeStart;
                Type = type;
                IsArray = type.IsArray;
                if (typeInfo.IsGenericType)
                {
                    var gtype = typeInfo.GetGenericTypeDefinition();
                    var _ta = GetTypeNameTuple(gtype);
                    TypeNamespace = _ta.Item1;
                    TypeName = _ta.Item2;
                    TypeAssembly = _ta.Item3;

                    var types = typeInfo.GenericTypeArguments;
                    Quantity = types.Length;
                    Types = types;
                    DTypes = new byte[Quantity];
                    TypeNamespaces = new string[Quantity];
                    TypeNames = new string[Quantity];
                    TypeAssemblies = new string[Quantity];
                    for (var i = 0; i < Quantity; i++)
                    {
                        DTypes[i] = GetDataType(Types[i]);
                        var _takey = GetTypeNameTuple(Types[i]);
                        TypeNamespaces[i] = _takey.Item1;
                        TypeNames[i] = _takey.Item2;
                        TypeAssemblies[i] = _takey.Item3;
                    }
                }
                else
                {
                    var _ta = GetTypeNameTuple(type);
                    TypeNamespace = _ta.Item1;
                    TypeName = _ta.Item2;
                    TypeAssembly = _ta.Item3;
                }
            }
        }
        #endregion

        #region ListStart
        public class ListStart : SerializerPlanItem
        {
            public Type InnerType;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ListStart(Type listType, Type innerType)
            {
                PlanType = SerializerPlanItemType.ListStart;
                Type = listType;
                InnerType = innerType;
            }
        }
        #endregion

        #region DictionaryStart
        public class DictionaryStart : SerializerPlanItem
        {
            public Type KeyType;
            public Type KeySerializerType;
            public bool KeyIsNullable;

            public Type ValueType;
            public Type ValueSerializerType;
            public bool ValueIsNullable;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DictionaryStart(Type type, Type keyType, Type keySerializerType, bool keyIsNullable, Type valueType, Type valueSerializerType, bool valueIsNullable)
            {
                PlanType = SerializerPlanItemType.DictionaryStart;
                Type = type;

                KeyType = keyType;
                KeySerializerType = keySerializerType;
                KeyIsNullable = keyIsNullable;

                ValueType = valueType;
                ValueSerializerType = valueSerializerType;
                ValueIsNullable = valueIsNullable;
            }
        }
        #endregion

        #region KeyValueStart
        public class KeyValueStart : SerializerPlanItem
        {
            public Type KeyType;
            public FastPropertyInfo Key;
            public Type KeySerializerType;
            public bool KeyIsNullable;

            public Type ValueType;
            public FastPropertyInfo Value;
            public Type ValueSerializerType;
            public bool ValueIsNullable;

            public byte KeyDType;
            public string KeyTypeNamespace;
            public string KeyTypeName;
            public string KeyTypeAssembly;
            
            public byte ValueDType;
            public string ValueTypeNamespace;
            public string ValueTypeName;
            public string ValueTypeAssembly;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public KeyValueStart(Type type, Type keyType, Type keySerializerType, bool keyIsNullable, Type valueType, Type valueSerializerType, bool valueIsNullable)
            {
                PlanType = SerializerPlanItemType.KeyValueStart;
                Type = type;
                var props = type.GetProperties();

                KeyType = keyType;
                KeySerializerType = keySerializerType;
                KeyIsNullable = keyIsNullable;
                Key = props.First(p => p.Name == "Key").GetFastPropertyInfo();
                KeyDType = GetDataType(KeyType);
                var _takey = GetTypeNameTuple(KeyType);
                KeyTypeNamespace = _takey.Item1;
                KeyTypeName = _takey.Item2;
                KeyTypeAssembly = _takey.Item3;

                ValueType = valueType;
                ValueSerializerType = valueSerializerType;
                ValueIsNullable = valueIsNullable;
                Value = props.First(p => p.Name == "Value").GetFastPropertyInfo();
                ValueDType = GetDataType(ValueType);
                var _tavalue = GetTypeNameTuple(ValueType);
                ValueTypeNamespace = _tavalue.Item1;
                ValueTypeName = _tavalue.Item2;
                ValueTypeAssembly = _tavalue.Item3;
            }
        }
        #endregion

        #region PropertyValue
        public class PropertyValue : SerializerPlanItem
        {
            public string Name;
            public FastPropertyInfo Property;
            public Type SerializerType;
            public object DefaultValue;
            public bool IsNullable;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public PropertyValue(PropertyInfo pInfo, Type serializer, bool isNullable)
            {
                PlanType = SerializerPlanItemType.PropertyValue;
                Type = pInfo.PropertyType;
                DefaultValue = isNullable ? null : Type.GetTypeInfo().IsValueType ? Activator.CreateInstance(Type) : null;
                IsNullable = isNullable;
                Property = pInfo.GetFastPropertyInfo();
                Name = pInfo.Name;
                SerializerType = serializer;
            }
        }
        #endregion

        #region PropertyReference
        public class PropertyReference : SerializerPlanItem
        {
            public string Name;
            public FastPropertyInfo Property;
            public object DefaultValue;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public PropertyReference(PropertyInfo pInfo)
            {
                PlanType = SerializerPlanItemType.PropertyReference;
                Type = pInfo.PropertyType;
                DefaultValue = Type.GetTypeInfo().IsValueType ? Activator.CreateInstance(Type) : null;
                Property = pInfo.GetFastPropertyInfo();
                Name = pInfo.Name;
            }
        }
        #endregion

        #region Value
        public class ValueItem : SerializerPlanItem
        {
            public Type SerializerType;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ValueItem(Type type, Type serializer)
            {
                PlanType = SerializerPlanItemType.Value;
                Type = type;
                SerializerType = serializer;
            }
        }
        #endregion

        #region RuntimeValue
        public class RuntimeValue : SerializerPlanItem
        {
            public Type SerializerType;
            public object Value;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public RuntimeValue(Type type, Type serializer, object value)
            {
                PlanType = SerializerPlanItemType.RuntimeValue;
                Type = type;
                SerializerType = serializer;
                Value = value;
            }
        }
        #endregion

        #region TupleStart
        public class TupleStart : SerializerPlanItem
        {
            public int Quantity;
            public Type[] Types;
            public FastPropertyInfo[] Props;
            public Type[] SerializerTypes;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TupleStart(Type tupleType, Type[] types, Type[] serializerTypes)
            {
                PlanType = SerializerPlanItemType.TupleStart;
                Type = tupleType;
                Quantity = types.Length;
                Types = types;
                SerializerTypes = serializerTypes;
                Props = new FastPropertyInfo[Quantity];
                for (var i = 0; i < Quantity; i++)
                    Props[i] = tupleType.GetRuntimeProperty("Item" + (i + 1)).GetFastPropertyInfo();
            }
        }
        #endregion

        #endregion
    }
}