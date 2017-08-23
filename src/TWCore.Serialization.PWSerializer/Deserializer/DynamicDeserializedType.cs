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
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TWCore.Serialization.PWSerializer.Deserializer
{
    /// <summary>
    /// Dynamic Deserialized Type
    /// </summary>
    public class DynamicDeserializedType : DynamicObject
    {
        #region Fields
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private object _tmpDictionary;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _itemDictionaryIdx;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string[] PropertiesNames;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly object[] PropertiesValues;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Dictionary<string, object> _properties = null;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int PropertiesLength;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int CurrentPropertyIndex;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool isIList;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int ItemListLength;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool isIDictionary;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private byte Operation;
        #endregion

        public string ValueType { get; private set; }
        public Dictionary<string, object> Properties
        {
            get
            {
                if (_properties != null) return _properties;
                _properties = new Dictionary<string, object>();
                for (var i = 0; i < PropertiesLength; i++)
                    _properties.Add(PropertiesNames[i], PropertiesValues[i]);
                return _properties;
            }
        }
        public List<object> List;
        public Dictionary<object, object> Dictionary;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DynamicDeserializedType(string valueType, string[] properties, int length)
        {
            ValueType = valueType;
            if (properties?.Length > 0)
            {
                PropertiesNames = properties;
                PropertiesLength = properties.Length;
                PropertiesValues = new object[PropertiesLength];
            }
            ItemListLength = length;
        }

        #region Internals
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ListStart()
        {
            Operation = 1;
            isIList = true;
            List = new List<object>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ListEnd()
        {
            Operation = 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DictionaryStart()
        {
            _itemDictionaryIdx = 0;
            Operation = 2;
            isIDictionary = true;
            Dictionary = new Dictionary<object, object>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DictionaryEnd()
        {
            Operation = 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddValue(object lastObject)
        {
            switch (Operation)
            {
                case 0:
                    PropertiesValues[CurrentPropertyIndex++] = lastObject;
                    break;
                case 1:
                    List.Add(lastObject);
                    break;
                case 2:
                    if (_itemDictionaryIdx == 0)
                    {
                        _tmpDictionary = lastObject;
                        _itemDictionaryIdx++;
                    }
                    else
                    {
                        Dictionary.Add(_tmpDictionary, lastObject);
                        _tmpDictionary = null;
                        _itemDictionaryIdx = 0;
                    }
                    break;
            }
        }
        #endregion

        #region Dynamic Methods
        public bool IsList => isIList;
        public bool IsDictionary => isIDictionary;
        public bool HasProperties => PropertiesLength > 0;
        public int Count => isIList ? List.Count : isIDictionary ? Dictionary.Count : -1;
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            result = null;
            if (isIList && indexes[0] is int index)
            {
                var value = List[index];
                result = value;
                return true;
            }
            if (!isIDictionary) return false;
            result = Dictionary[indexes[0]];
            return true;
        }
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;
            if (PropertiesLength <= 0)
                return false;
            var name = binder.Name;
            var idx = PropertiesNames.IndexOf(name);
            if (idx < 0)
                return false;
            result = PropertiesValues[idx];
            return true;
        }
        #endregion

        #region Public
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetObject<T>() where T: class, new()
        {
            object obj;
            if (ValueType != null)
                obj = Activator.CreateInstance(Core.GetType(ValueType));
            else
                obj = Activator.CreateInstance<T>();
            BindObject(ref obj);
            return (T)obj;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BindObject(ref object obj)
        {
            var typeInfo = GetDeserializationTypeInfo(obj.GetType());
            foreach(var prop in typeInfo.Properties)
            {
                if (Properties.TryGetValue(prop.Key, out var rawValue))
                {
                    var underType = prop.Value.UnderlyingType;
                    var value = GetValue(rawValue, underType);
                    prop.Value.Property.SetValue(obj, value);
                }
            }
            if (isIList && typeInfo.IsIList)
            {
                var underType = typeInfo.InnerTypes[0];
                if (typeInfo.IsArray)
                    obj = Array.CreateInstance(underType, ItemListLength);
                var ilistObj = (IList)obj;
                var idx = 0;
                foreach (var item in List)
                {
                    object value = null;
                    try
                    {
                        value = GetValue(item, underType);
                        if (!typeInfo.IsArray)
                            ilistObj.Add(value);
                        else
                            ilistObj[idx] = value;
                        idx++;
                    }
                    catch (Exception ex)
                    {
                        Core.Log.Error("UnderlayingType   = {0}", underType.AssemblyQualifiedName);
                        Core.Log.Error("ValueType         = {0}", value?.GetType().AssemblyQualifiedName);
                        Core.Log.Error("ValueTypeBaseType = {0}", value?.GetType().BaseType.AssemblyQualifiedName);
                        Core.Log.Error("UnderlayingType Location   = {0}", underType.Assembly.Location);
                        Core.Log.Error("ValueType Location         = {0}", value?.GetType().Assembly.Location);
                        Core.Log.Error("TypeInfo          = {0}", typeInfo.Type.AssemblyQualifiedName);
                        Core.Log.Error("UnderlayingType.AssignableFrom(ValueType) = {0}", underType.IsAssignableFrom(value?.GetType()));
                        Core.Log.Error("ValueType.AssignableFrom(UnderlayingType) = {0}", value?.GetType().IsAssignableFrom(underType));
                        Core.Log.Write(ex);
                        throw;
                    }
                }
            }
            if (isIDictionary && typeInfo.IsIDictionary)
            {
                var keyType = typeInfo.InnerTypes[0];
                var valueType = typeInfo.InnerTypes[1];
                var iDctObj = (IDictionary) obj;
                foreach (var item in Dictionary)
                {
                    var kValue = GetValue(item.Key, keyType);
                    var vValue = GetValue(item.Value, valueType);
                    iDctObj.Add(kValue, vValue);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object GetValue(object item, Type underType)
        {
            object value = item;
            if (item is DynamicDeserializedType ddtValue)
            {
                if (ddtValue.ValueType != null)
                    value = Activator.CreateInstance(Core.GetType(ddtValue.ValueType));
                else
                    value = Activator.CreateInstance(underType);
                ddtValue.BindObject(ref value);
            }
            value = DataTypeHelper.Change(value, underType);
            return value;
        }

        #endregion

        private static ConcurrentDictionary<Type, DeserializerTypeInfo> DeserializationTypeInfo = new ConcurrentDictionary<Type, DeserializerTypeInfo>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DeserializerTypeInfo GetDeserializationTypeInfo(Type type)
        {
            if (type == null) return null;
            return DeserializationTypeInfo.GetOrAdd(type, valueType =>
            {
                if (valueType != null)
                {
                    var typeInfo = valueType.GetTypeInfo();
                    bool isGenericType = typeInfo.IsGenericType;

                    var tinfo = new DeserializerTypeInfo()
                    {
                        Type = valueType,
                        Properties = valueType.GetRuntimeProperties().Where(p =>
                        {
                            bool ok = !p.IsSpecialName && p.CanRead && p.CanWrite;
                            if (ok)
                            {
                                if (p.GetIndexParameters().Length > 0)
                                    return false;
                                var cAttr = p.GetCustomAttributes();
                            }
                            return ok;
                        }).Select(p => new DeserializerTypeInfo.FPropertyInfo
                        {
                            Property = p.GetFastPropertyInfo(),
                            IsEnum = p.PropertyType.GetTypeInfo().IsEnum,
                            UnderlyingType = p.PropertyType.GetUnderlyingType()
                        }).ToDictionary(k => k.Property.Name, v => v)
                    };
                    var constructor = typeInfo.DeclaredConstructors.First();
                    tinfo.Activator = Factory.Accessors.CreateActivator(constructor);
                    tinfo.ActivatorParametersTypes = constructor.GetParameters().Select(p => p.ParameterType).ToArray();
                    tinfo.IsArray = valueType.IsArray;

                    //
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
                                if (iListType?.GenericTypeArguments.Length > 0)
                                    innerType = iListType.GenericTypeArguments[0];
                            }
                        }
                        tinfo.IsIList = true;
                        tinfo.InnerTypes = new Type[] { innerType };
                    }

                    var idictio = ifaces.FirstOrDefault(i => i == typeof(IDictionary) || (i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>)));
                    if (idictio != null)
                    {
                        tinfo.IsIDictionary = true;
                        tinfo.InnerTypes = idictio.GenericTypeArguments;
                    }

                    if (!tinfo.IsIList && !tinfo.IsIDictionary && isGenericType)
                        tinfo.InnerTypes = typeInfo.GenericTypeArguments;

                    if (tinfo.IsIList && type.GenericTypeArguments.Length == 1 && type.GetGenericTypeDefinition() == typeof(List<>))
                        tinfo.IsTypeList = true;
                    else if (tinfo.IsIDictionary && type.GenericTypeArguments.Length == 2 && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                        tinfo.IsTypeDictionary = true;

                    return tinfo;
                }
                return null;
            });
        }
    }
}