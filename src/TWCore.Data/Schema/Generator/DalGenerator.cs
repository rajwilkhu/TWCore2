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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TWCore.Diagnostics.Log;

namespace TWCore.Data.Schema.Generator
{
	/// <summary>
	/// Dal Generator
	/// </summary>
	public class DalGenerator
    {
        CatalogSchema _schema;
        string _namespace;
        IDataAccessDynamicGenerator dataAccessGenerator = null;

        #region Properties
        /// <summary>
        /// Gets the EntityName Delegate
        /// </summary>
        public Func<string, string> GetEntityNameDelegate { get; set; } = new Func<string, string>(tableName =>
        {
            tableName = tableName.Replace("_", " ");
            tableName = tableName.CapitalizeEachWords();
            tableName = tableName.Replace("-", "_");
            return tableName.RemoveSpaces();
        });

        public bool EnableAsync { get; set; } = true;
        public bool EnableCreateEntities { get; set; } = true;
        public bool EnableCreateInterfaces { get; set; } = true;
        public bool EnableCreateSolution { get; set; } = true;
        public bool EnableCreateDal { get; set; } = true;
        public bool EnableDynamicDal { get; set; } = false;
        #endregion

        #region .ctor
        /// <summary>
        /// Dal Generator
        /// </summary>
        /// <param name="schema">Catalog schema</param>
        public DalGenerator(CatalogSchema schema, string @namespace)
        {
            _schema = schema;
            _namespace = @namespace;
            try
            {
                var dagenType = Core.GetType(_schema.AssemblyQualifiedName);
                if (dagenType != null)
                    dataAccessGenerator = Activator.CreateInstance(dagenType) as IDataAccessDynamicGenerator;
            }
            catch (Exception ex)
            {
                Core.Log.Write(LogLevel.Warning, ex);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Create Dal
        /// </summary>
        /// <param name="directory">Folder path</param>
        public void Create(string directory)
        {
            if (dataAccessGenerator == null)
                EnableDynamicDal = true;
            CreateEntities(directory);
            CreateInterfaces(directory);
            CreateDal(directory);
        }
        #endregion

        #region Private Methods
        void CreateEntities(string directory)
        {
            if (!EnableCreateEntities) return;

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            string fName, fContent;

            (fName, fContent) = CreateAbstractionProject();
            WriteToDisk(Path.Combine(directory, fName), fContent);

            foreach (var table in _schema.Tables)
            {
                (fName, fContent) = CreateEntity(table.Name);
                WriteToDisk(Path.Combine(directory, fName), fContent);
            }
        }
        void CreateInterfaces(string directory)
        {
            if (!EnableCreateInterfaces) return;

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            string fName, fContent;

            (fName, fContent) = CreateAbstractionProject();
            WriteToDisk(Path.Combine(directory, fName), fContent);

            foreach (var table in _schema.Tables)
            {
                (fName, fContent) = CreateInterface(table.Name);
                WriteToDisk(Path.Combine(directory, fName), fContent);
                if (EnableAsync)
                {
                    (fName, fContent) = CreateInterfaceAsync(table.Name);
                    WriteToDisk(Path.Combine(directory, fName), fContent);
                }
            }
        }
        void CreateDal(string directory)
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            string fName, fContent;

            if (EnableCreateSolution)
            {
                (fName, fContent) = CreateSolution();
                WriteToDisk(Path.Combine(directory, fName), fContent);
            }

            (fName, fContent) = CreateDalProject();
            WriteToDisk(Path.Combine(directory, fName), fContent);

            (fName, fContent) = CreateDatabaseEntity();
            WriteToDisk(Path.Combine(directory, fName), fContent);

            foreach (var table in _schema.Tables)
            {
                (fName, fContent) = CreateClass(table.Name);
                WriteToDisk(Path.Combine(directory, fName), fContent);
                if (EnableAsync)
                {
                    (fName, fContent) = CreateClassAsync(table.Name);
                    WriteToDisk(Path.Combine(directory, fName), fContent);
                }
            }
        }


        #region Abstractions
        (string, string) CreateAbstractionProject()
        {
            string projFile = DalGeneratorConsts.formatAbstractionsProject;
            projFile = projFile.Replace("($DATAASSEMBLYNAME$)", _schema.Assembly);
            projFile = projFile.Replace("($VERSION$)", Core.FrameworkVersion);
            var filePath = Path.Combine(_schema.Name, "Abstractions", _namespace + "." + _schema.Name + ".Abstractions.csproj");
            return (filePath, projFile);
        }
        (string, string) CreateEntity(string tableName)
        {
            var table = _schema.Tables.FirstOrDefault(t => t.Name == tableName);
            if (table == null) return (null, null);

            string header = DalGeneratorConsts.formatEntityHeader;
            string entityWrapper = DalGeneratorConsts.formatEntityWrapper;
            string columnFormat = DalGeneratorConsts.formatEntityColumn;

            var entityColumns = new List<string>();
            foreach (var column in table.Columns)
            {
                var strColumn = columnFormat;
                bool added = false;

                var columnAttribute = $"\r\n        [ColumnSchema(Name=\"{column.Name}\")]";

                if (!column.IndexesName.Any(i => i.StartsWith("PK")))
                {
                    //We have to check first if the column has a FK
                    foreach (var fk in table.ForeignKeys)
                    {
                        var fkTable = _schema.Tables.FirstOrDefault(t => t.Name == fk.ForeignTable);
                        if (fkTable != null)
                        {
                            var fkColumn = fkTable.Columns.FirstOrDefault(c => c.Name == column.Name);
                            if (fkColumn != null)
                            {
                                var isPK = fkColumn.IndexesName.Any(i => i.StartsWith("PK"));
                                if (isPK)
                                {
                                    strColumn = strColumn.Replace("($COLUMNTYPE$)", "Ent" + GetEntityNameDelegate(fkTable.Name));
                                    var name = column.Name;
                                    if (name.EndsWith("Id"))
                                        name = name.SubstringToLast("Id") + "Item";
                                    else
                                        name = fkTable.Name;
                                    strColumn = strColumn.Replace("($COLUMNNAME$)", GetName(name));

                                    if (!entityColumns.Contains(strColumn))
                                    {
                                        entityColumns.Add(columnAttribute);
                                        entityColumns.Add(strColumn);
                                    }
                                    else
                                    {
                                        var idx = entityColumns.IndexOf(strColumn);
                                        entityColumns.Insert(idx, columnAttribute);
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!added)
                    {
                        //We try to find other entity to match the Id (without FK)
                        if (column.Name != "Id" && column.Name.EndsWith("Id"))
                        {
                            foreach (var t in _schema.Tables)
                            {
                                var iPk = t.Indexes.FirstOrDefault(i => i.Type == IndexType.PrimaryKey);
                                if (iPk?.Columns?.Count == 1)
                                {
                                    if (iPk.Columns[0].ColumnName == column.Name)
                                    {
                                        strColumn = strColumn.Replace("($COLUMNTYPE$)", "Ent" + GetEntityNameDelegate(t.Name));
                                        var name = column.Name.SubstringToLast("Id") + "Item";
                                        strColumn = strColumn.Replace("($COLUMNNAME$)", GetName(name));
                                        strColumn += "          // TODO: This property should have a ForeignKey in DB table.";
                                        entityColumns.Add(columnAttribute);
                                        entityColumns.Add(strColumn);
                                        added = true;
                                        break;

                                    }
                                }
                            }
                        }
                    }
                }
                if (!added)
                {
                    strColumn = strColumn.Replace("($COLUMNTYPE$)", column.DataType);
                    strColumn = strColumn.Replace("($COLUMNNAME$)", GetName(column.Name));
                    entityColumns.Add(columnAttribute);
                    entityColumns.Add(strColumn);
                }
            }


            var body = header + entityWrapper;
            body = body.Replace("($TABLESCHEMA$)", $"TableSchema(Name=\"{tableName}\")");
            body = body.Replace("($NAMESPACE$)", _namespace);
            body = body.Replace("($DATABASENAME$)", _schema.Name);
            body = body.Replace("($TABLENAME$)", GetEntityNameDelegate(table.Name));
            body = body.Replace("($COLUMNS$)", string.Join(string.Empty, entityColumns.ToArray()));

            var filePath = Path.Combine(_schema.Name, "Abstractions", "Entities");
            filePath = Path.Combine(filePath, "Ent" + GetEntityNameDelegate(table.Name) + ".cs");
            return (filePath, body);
        }
        (string, string) CreateInterface(string tableName)
        {
            var table = _schema.Tables.FirstOrDefault(t => t.Name == tableName);
            if (table == null) return (null, null);

            string header = DalGeneratorConsts.formatDalInterfaceHeader;
            string interfaceWrapper = DalGeneratorConsts.formatDalInterfaceWrapper;
            string interfaceMethod = DalGeneratorConsts.formatDalInterfaceMethod;

            var entityTableName = GetEntityNameDelegate(table.Name);
            var entityName = "Ent" + entityTableName;

            var methods = new List<string>();

            methods.Add(interfaceMethod.Replace("($RETURNTYPE$)", $"IEnumerable<{entityName}>").Replace("($METHODNAME$)", "GetAll").Replace("($METHODPARAMETERS$)", ""));

            foreach (var index in table.Indexes)
            {
                var columnNames = new List<string>();
                var names = new List<string>();
                var parameters = new List<string>();
                foreach (var col in index.Columns.OrderBy(c => c.ColumnPosition))
                {
                    var column = table.Columns.FirstOrDefault(c => c.Name == col.ColumnName);
                    columnNames.Add(col.ColumnName);
                    names.Add(GetName(col.ColumnName));
                    parameters.Add(column.DataType + " @" + GetName(col.ColumnName.Substring(0, 1).ToLowerInvariant() + col.ColumnName.Substring(1)));
                }
                var indexAttribute = $"\r\n        [IndexSchema(ColumnsNames=\"{string.Join(", ", columnNames.ToArray())}\")]";
                methods.Add(indexAttribute);
                if (index.Type == IndexType.PrimaryKey || index.Type == IndexType.UniqueKey || index.Type == IndexType.UniqueIndex || index.Type == IndexType.UniqueClusteredIndex)
                {
                    methods.Add(interfaceMethod.Replace("($RETURNTYPE$)", entityName).Replace("($METHODNAME$)", "GetBy" + string.Join("", names.ToArray())).Replace("($METHODPARAMETERS$)", string.Join(", ", parameters.ToArray())));
                }
                else
                {
                    methods.Add(interfaceMethod.Replace("($RETURNTYPE$)", $"IEnumerable<{entityName}>").Replace("($METHODNAME$)", "GetAllBy" + string.Join("", names.ToArray())).Replace("($METHODPARAMETERS$)", string.Join(", ", parameters.ToArray())));
                }
            }

            methods.Add(interfaceMethod.Replace("($RETURNTYPE$)", "int").Replace("($METHODNAME$)", "Insert").Replace("($METHODPARAMETERS$)", entityName + " value"));
            methods.Add(interfaceMethod.Replace("($RETURNTYPE$)", "int").Replace("($METHODNAME$)", "Update").Replace("($METHODPARAMETERS$)", entityName + " value"));


            var body = header + interfaceWrapper;
            body = body.Replace("($NAMESPACE$)", _namespace);
            body = body.Replace("($DATABASENAME$)", _schema.Name);
            body = body.Replace("($TABLENAME$)", entityTableName);
            body = body.Replace("($METHODS$)", string.Join(string.Empty, methods.ToArray()));

            var filePath = Path.Combine(_schema.Name, "Abstractions", "Interfaces");
            filePath = Path.Combine(filePath, "IDal" + entityTableName + ".cs");
            return (filePath, body);
        }
        (string, string) CreateInterfaceAsync(string tableName)
        {
            var table = _schema.Tables.FirstOrDefault(t => t.Name == tableName);
            if (table == null) return (null, null);

            string header = DalGeneratorConsts.formatDalInterfaceHeader;
            header += "using System.Threading.Tasks;\r\n";

            string interfaceWrapper = DalGeneratorConsts.formatDalInterfaceWrapper;
            string interfaceMethod = DalGeneratorConsts.formatDalInterfaceMethod;

            var entityTableName = GetEntityNameDelegate(table.Name);
            var entityName = "Ent" + entityTableName;

            var methods = new List<string>();

            methods.Add(interfaceMethod.Replace("($RETURNTYPE$)", $"Task<IEnumerable<{entityName}>>").Replace("($METHODNAME$)", "GetAllAsync").Replace("($METHODPARAMETERS$)", ""));

            foreach (var index in table.Indexes)
            {
                var columnNames = new List<string>();
                var names = new List<string>();
                var parameters = new List<string>();
                foreach (var col in index.Columns.OrderBy(c => c.ColumnPosition))
                {
                    var column = table.Columns.FirstOrDefault(c => c.Name == col.ColumnName);
                    columnNames.Add(col.ColumnName);
                    names.Add(GetName(col.ColumnName));
                    parameters.Add(column.DataType + " @" + GetName(col.ColumnName.Substring(0, 1).ToLowerInvariant() + col.ColumnName.Substring(1)));
                }
                var indexAttribute = $"\r\n        [IndexSchema(ColumnsNames=\"{string.Join(", ", columnNames.ToArray())}\")]";
                methods.Add(indexAttribute);
                if (index.Type == IndexType.PrimaryKey || index.Type == IndexType.UniqueKey || index.Type == IndexType.UniqueIndex || index.Type == IndexType.UniqueClusteredIndex)
                {
                    methods.Add(interfaceMethod.Replace("($RETURNTYPE$)", $"Task<{entityName}>").Replace("($METHODNAME$)", "GetBy" + string.Join("", names.ToArray()) + "Async").Replace("($METHODPARAMETERS$)", string.Join(", ", parameters.ToArray())));
                }
                else
                {
                    methods.Add(interfaceMethod.Replace("($RETURNTYPE$)", $"Task<IEnumerable<{entityName}>>").Replace("($METHODNAME$)", "GetAllBy" + string.Join("", names.ToArray()) + "Async").Replace("($METHODPARAMETERS$)", string.Join(", ", parameters.ToArray())));
                }
            }

            methods.Add(interfaceMethod.Replace("($RETURNTYPE$)", "Task<int>").Replace("($METHODNAME$)", "InsertAsync").Replace("($METHODPARAMETERS$)", entityName + " value"));
            methods.Add(interfaceMethod.Replace("($RETURNTYPE$)", "Task<int>").Replace("($METHODNAME$)", "UpdateAsync").Replace("($METHODPARAMETERS$)", entityName + " value"));


            var body = header + interfaceWrapper;
            body = body.Replace("($NAMESPACE$)", _namespace);
            body = body.Replace("($DATABASENAME$)", _schema.Name);
            body = body.Replace("($TABLENAME$)", entityTableName + "Async");
            body = body.Replace("($METHODS$)", string.Join(string.Empty, methods.ToArray()));

            var filePath = Path.Combine(_schema.Name, "Abstractions", "Interfaces");
            filePath = Path.Combine(filePath, "IDal" + entityTableName + "Async.cs");
            return (filePath, body);
        }
        #endregion

        #region Dal
        (string, string) CreateSolution()
        {
            var prov = _schema.Provider.Replace("DataAccess", string.Empty);
            string projFile = DalGeneratorConsts.formatSolution;
            projFile = projFile.Replace("($NAMESPACE$)", _namespace);
            projFile = projFile.Replace("($CATALOGNAME$)", _schema.Name);
            projFile = projFile.Replace("($PROVIDERNAME$)", prov);
            var filePath = Path.Combine(_schema.Name, _namespace + "." + _schema.Name + ".sln");
            return (filePath, projFile);
        }
        (string, string) CreateDalProject()
        {
            var prov = _schema.Provider.Replace("DataAccess", string.Empty);
            string projFile = DalGeneratorConsts.formatDalProject;
            projFile = projFile.Replace("($DATAASSEMBLYNAME$)", _schema.Assembly);
            projFile = projFile.Replace("($VERSION$)", Core.FrameworkVersion);
            projFile = projFile.Replace("($NAMESPACE$)", _namespace);
            projFile = projFile.Replace("($CATALOGNAME$)", _schema.Name);
            var filePath = Path.Combine(_schema.Name, "Dal." + prov, _namespace + "." + _schema.Name + "." + prov + ".csproj");
            return (filePath, projFile);
        }
        (string, string) CreateDatabaseEntity()
        {
            var prov = _schema.Provider.Replace("DataAccess", string.Empty);
            string header = DalGeneratorConsts.formatEntityHeader;
            header += "using " + _schema.Assembly + ";\r\n";
            string databaseEntities = DalGeneratorConsts.formatDatabaseEntities;
            databaseEntities = databaseEntities.Replace("($NAMESPACE$)", _namespace);
            databaseEntities = databaseEntities.Replace("($DATABASENAME$)", _schema.Name);
            databaseEntities = databaseEntities.Replace("($CONNECTIONSTRING$)", _schema.ConnectionString);
            databaseEntities = databaseEntities.Replace("($PROVIDER$)", _schema.Provider);
            var filePath = Path.Combine(_schema.Name, "Dal." + prov, _schema.Name + ".cs");
            return (filePath, header + databaseEntities);
        }
        (string, string) CreateClass(string tableName)
        {
            var table = _schema.Tables.FirstOrDefault(t => t.Name == tableName);
            if (table == null) return (null, null);

            string header = DalGeneratorConsts.formatDalHeader;
            string dalWrapper = DalGeneratorConsts.formatDalWrapper;
            string dalSelectMethod = DalGeneratorConsts.formatDalSelectMethod;
            string dalExecuteMethod = DalGeneratorConsts.formatDalExecuteMethod;

            var entityTableName = GetEntityNameDelegate(table.Name);
            var entityName = "Ent" + entityTableName;

            var methods = new List<string>();

            methods.Add(dalSelectMethod
                .Replace("($RETURNTYPE$)", $"IEnumerable<{entityName}>")
                .Replace("($METHODNAME$)", "GetAll")
                .Replace("($METHODPARAMETERS$)", "")
                .Replace("($DATASELECT$)", "SelectElements")
                .Replace("($DATARETURN$)", entityName)
                .Replace("($DATASQL$)", EnableDynamicDal ? $":{entityTableName}.GetAll" : "")
                .Replace("($DATAPARAMETERS$)", "")
                );

            foreach (var index in table.Indexes)
            {
                var columnNames = new List<string>();
                var names = new List<string>();
                var parameters = new List<string>();
                var objParameters = new List<string>();
                foreach (var col in index.Columns.OrderBy(c => c.ColumnPosition))
                {
                    var column = table.Columns.FirstOrDefault(c => c.Name == col.ColumnName);
                    columnNames.Add(col.ColumnName);
                    names.Add(GetName(col.ColumnName));
                    var pName = " @" + GetName(col.ColumnName.Substring(0, 1).ToLowerInvariant() + col.ColumnName.Substring(1));
                    parameters.Add(column.DataType + pName);
                    objParameters.Add($"{GetName(col.ColumnName)} = {pName}");
                }
                var mName = string.Join("", names.ToArray());
                var mParameters = string.Join(", ", parameters.ToArray());
                var oParameters = "new { " + string.Join(", ", objParameters.ToArray()) + " }";
                if (index.Type == IndexType.PrimaryKey || index.Type == IndexType.UniqueKey || index.Type == IndexType.UniqueIndex || index.Type == IndexType.UniqueClusteredIndex)
                {
                    methods.Add(dalSelectMethod
                        .Replace("($RETURNTYPE$)", entityName)
                        .Replace("($METHODNAME$)", "GetBy" + mName)
                        .Replace("($METHODPARAMETERS$)", mParameters)
                        .Replace("($DATASELECT$)", "SelectElement")
                        .Replace("($DATARETURN$)", entityName)
                        .Replace("($DATASQL$)", EnableDynamicDal ? $":{entityTableName}.GetBy" + mName : "")
                        .Replace("($DATAPARAMETERS$)", ", " + oParameters)
                        );
                }
                else
                {
                    methods.Add(dalSelectMethod
                        .Replace("($RETURNTYPE$)", $"IEnumerable<{entityName}>")
                        .Replace("($METHODNAME$)", "GetAllBy" + mName)
                        .Replace("($METHODPARAMETERS$)", mParameters)
                        .Replace("($DATASELECT$)", "SelectElements")
                        .Replace("($DATARETURN$)", entityName)
                        .Replace("($DATASQL$)", EnableDynamicDal ? $":{entityTableName}.GetAllBy" + mName : "")
                        .Replace("($DATAPARAMETERS$)", ", " + oParameters)
                        );
                }
            }

            methods.Add(dalExecuteMethod
                .Replace("($RETURNTYPE$)", "int")
                .Replace("($METHODNAME$)", "Insert")
                .Replace("($DATATYPE$)", entityName)
                .Replace("($DATASQL$)", EnableDynamicDal ? $":{entityTableName}.Insert" : "")
                );

            methods.Add(dalExecuteMethod
                .Replace("($RETURNTYPE$)", "int")
                .Replace("($METHODNAME$)", "Update")
                .Replace("($DATATYPE$)", entityName)
                .Replace("($DATASQL$)", EnableDynamicDal ? $":{entityTableName}.Update" : "")
                );


            var body = header + dalWrapper;

            //FillEntity
            var fillEntities = GetFillMethodSentences(table);

            //PrepareEntity
            var prepareEntities = GetPrepareEntitySentences(table);

			//SelectBaseSQL
			var otherSqls = string.Empty;
			if (!EnableDynamicDal)
			{
				var container = GetSelectColumns(tableName);
				var sbSQL = dataAccessGenerator?.GetSelectFromContainer(container).Replace("\"", "\"\"");
				otherSqls = $"\t\tconst string SelectBaseSql = @\"\n{sbSQL}\";";
			}

			body = body.Replace("($OTHERSQLS$)", otherSqls);
            body = body.Replace("($FILLENTITY$)", string.Join("", fillEntities.ToArray()));
            body = body.Replace("($PREPAREENTITY$)", string.Join("", prepareEntities.ToArray()));
            body = body.Replace("($NAMESPACE$)", _namespace);
            body = body.Replace("($DATABASENAME$)", _schema.Name);
            body = body.Replace("($TABLENAME$)", entityTableName);
            body = body.Replace("($DATATYPE$)", entityName);
            body = body.Replace("($DATATYPE2$)", "ent" + entityTableName);
            body = body.Replace("($METHODS$)", string.Join(string.Empty, methods.ToArray()));
            body = body.Replace("($ASYNC$)", "");

            var prov = _schema.Provider.Replace("DataAccess", string.Empty);
            var filePath = Path.Combine(_schema.Name, "Dal." + prov);
            filePath = Path.Combine(filePath, "Dal" + entityTableName + ".cs");
            return (filePath, body);
        }
        (string, string) CreateClassAsync(string tableName)
        {
            var table = _schema.Tables.FirstOrDefault(t => t.Name == tableName);
            if (table == null) return (null, null);

            string header = DalGeneratorConsts.formatDalHeader;
            header += "using System.Threading.Tasks;\r\n";
            string dalWrapper = DalGeneratorConsts.formatDalWrapper;
            string dalSelectMethod = DalGeneratorConsts.formatDalSelectMethod;
            string dalExecuteMethod = DalGeneratorConsts.formatDalExecuteMethod;

            var entityTableName = GetEntityNameDelegate(table.Name);
            var entityName = "Ent" + entityTableName;

            var methods = new List<string>();

            methods.Add(dalSelectMethod
                .Replace("($RETURNTYPE$)", $"Task<IEnumerable<{entityName}>>")
                .Replace("($METHODNAME$)", "GetAllAsync")
                .Replace("($METHODPARAMETERS$)", "")
                .Replace("($DATASELECT$)", "SelectElementsAsync")
                .Replace("($DATARETURN$)", entityName)
                .Replace("($DATASQL$)", EnableDynamicDal ? $":{entityTableName}.GetAllAsync" : "")
                .Replace("($DATAPARAMETERS$)", "")
                );

            foreach (var index in table.Indexes)
            {
                var columnNames = new List<string>();
                var names = new List<string>();
                var parameters = new List<string>();
                var objParameters = new List<string>();
                foreach (var col in index.Columns.OrderBy(c => c.ColumnPosition))
                {
                    var column = table.Columns.FirstOrDefault(c => c.Name == col.ColumnName);
                    columnNames.Add(col.ColumnName);
                    names.Add(GetName(col.ColumnName));
                    var pName = " @" + GetName(col.ColumnName.Substring(0, 1).ToLowerInvariant() + col.ColumnName.Substring(1));
                    parameters.Add(column.DataType + pName);
                    objParameters.Add($"{GetName(col.ColumnName)} = {pName}");
                }
                var mName = string.Join("", names.ToArray());
                var mParameters = string.Join(", ", parameters.ToArray());
                var oParameters = "new { " + string.Join(", ", objParameters.ToArray()) + " }";
                if (index.Type == IndexType.PrimaryKey || index.Type == IndexType.UniqueKey || index.Type == IndexType.UniqueIndex || index.Type == IndexType.UniqueClusteredIndex)
                {
                    methods.Add(dalSelectMethod
                        .Replace("($RETURNTYPE$)", $"Task<{entityName}>")
                        .Replace("($METHODNAME$)", "GetBy" + mName + "Async")
                        .Replace("($METHODPARAMETERS$)", mParameters)
                        .Replace("($DATASELECT$)", "SelectElementAsync")
                        .Replace("($DATARETURN$)", entityName)
                        .Replace("($DATASQL$)", EnableDynamicDal ? $":{entityTableName}.GetBy" + mName + "Async" : "")
                        .Replace("($DATAPARAMETERS$)", ", " + oParameters)
                        );
                }
                else
                {
                    methods.Add(dalSelectMethod
                        .Replace("($RETURNTYPE$)", $"Task<IEnumerable<{entityName}>>")
                        .Replace("($METHODNAME$)", "GetAllBy" + mName + "Async")
                        .Replace("($METHODPARAMETERS$)", mParameters)
                        .Replace("($DATASELECT$)", "SelectElementsAsync")
                        .Replace("($DATARETURN$)", entityName)
                        .Replace("($DATASQL$)", EnableDynamicDal ? $":{entityTableName}.GetAllBy" + mName + "Async" : "")
                        .Replace("($DATAPARAMETERS$)", ", " + oParameters)
                        );
                }
            }

            methods.Add(dalExecuteMethod
                .Replace("($RETURNTYPE$)", "Task<int>")
                .Replace("($METHODNAME$)", "InsertAsync")
                .Replace("($DATATYPE$)", entityName)
                .Replace("($DATASQL$)", EnableDynamicDal ? $":{entityTableName}.InsertAsync" : "")
                );

            methods.Add(dalExecuteMethod
                .Replace("($RETURNTYPE$)", "Task<int>")
                .Replace("($METHODNAME$)", "UpdateAsync")
                .Replace("($DATATYPE$)", entityName)
                .Replace("($DATASQL$)", EnableDynamicDal ? $":{entityTableName}.UpdateAsync" : "")
                );

            var body = header + dalWrapper;

            /*FillEntity*/
            var fillEntities = GetFillMethodSentences(table);

            /*PrepareEntity*/
            var prepareEntities = GetPrepareEntitySentences(table);

			//SelectBaseSQL
			var otherSqls = string.Empty;
			if (!EnableDynamicDal)
			{
				var container = GetSelectColumns(tableName);
				var sbSQL = dataAccessGenerator?.GetSelectFromContainer(container).Replace("\"", "\"\"");
				otherSqls = $"\t\tconst string SelectBaseSql = @\"\n{sbSQL}\";";
			}

			body = body.Replace("($OTHERSQLS$)", otherSqls);
			body = body.Replace("($FILLENTITY$)", string.Join("", fillEntities.ToArray()));
            body = body.Replace("($PREPAREENTITY$)", string.Join("", prepareEntities.ToArray()));
            body = body.Replace("($NAMESPACE$)", _namespace);
            body = body.Replace("($DATABASENAME$)", _schema.Name);
            body = body.Replace("($TABLENAME$)", entityTableName + "Async");
            body = body.Replace("($DATATYPE$)", entityName);
            body = body.Replace("($DATATYPE2$)", "ent" + entityTableName);
            body = body.Replace("($METHODS$)", string.Join(string.Empty, methods.ToArray()));
            body = body.Replace("($ASYNC$)", "Async");

            var prov = _schema.Provider.Replace("DataAccess", string.Empty);
            var filePath = Path.Combine(_schema.Name, "Dal." + prov);
            filePath = Path.Combine(filePath, "Dal" + entityTableName + "Async.cs");
            return (filePath, body);
        }


        List<string> GetFillMethodSentences(TableSchema table)
        {
            var fillEntities = new List<string>();
            foreach (var column in table.Columns)
            {
                bool added = false;

				if (!column.IndexesName.Any(i => i.StartsWith("PK", StringComparison.OrdinalIgnoreCase)))
                {
                    //We have to check first if the column has a FK
                    foreach (var fk in table.ForeignKeys)
                    {
                        var fkTable = _schema.Tables.FirstOrDefault(t => t.Name == fk.ForeignTable);
                        if (fkTable != null)
                        {
                            var fkColumn = fkTable.Columns.FirstOrDefault(c => c.Name == column.Name);
                            if (fkColumn != null)
                            {
                                var isPK = fkColumn.IndexesName.Any(i => i.StartsWith("PK", StringComparison.OrdinalIgnoreCase));
                                if (isPK)
                                {
                                    var name = column.Name;
                                    if (name.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                                        name = name.SubstringToLast("Id") + "Item";
                                    else
                                        name = fkTable.Name;
                                    name = GetName(name);
                                    var tName = GetEntityNameDelegate(fkTable.Name);
                                    var type = "Ent" + tName;

                                    var fill = $"            ($DATATYPE2$).{name} = binder.Bind<{type}>(rowValues, \"{tName}.%\");\r\n";
                                    if (!fillEntities.Contains(fill))
                                        fillEntities.Add(fill);

                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!added)
                    {
                        //We try to find other entity to match the Id (without FK)
                        if (column.Name != "Id" && column.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (var t in _schema.Tables)
                            {
                                var iPk = t.Indexes.FirstOrDefault(i => i.Type == IndexType.PrimaryKey);
                                if (iPk?.Columns?.Count == 1)
                                {
                                    if (iPk.Columns[0].ColumnName == column.Name)
                                    {
                                        var name = column.Name.SubstringToLast("Id") + "Item";
                                        name = GetName(name);
                                        var tName = GetEntityNameDelegate(t.Name);
                                        var type = "Ent" + tName;

                                        var fill = $"            ($DATATYPE2$).{name} = binder.Bind<{type}>(rowValues, \"{tName}.%\");\r\n";
                                        if (!fillEntities.Contains(fill))
                                            fillEntities.Add(fill);

                                        added = true;
                                        break;

                                    }
                                }
                            }
                        }
                    }
                }

            }

            return fillEntities;
        }
        List<string> GetPrepareEntitySentences(TableSchema table)
        {
            var prepareEntities = new List<string>();
            foreach (var column in table.Columns)
            {
                bool added = false;

                if (!column.IndexesName.Any(i => i.StartsWith("PK", StringComparison.OrdinalIgnoreCase)))
                {
                    //We have to check first if the column has a FK
                    foreach (var fk in table.ForeignKeys)
                    {
                        var fkTable = _schema.Tables.FirstOrDefault(t => t.Name == fk.ForeignTable);
                        if (fkTable != null)
                        {
                            var fkColumn = fkTable.Columns.FirstOrDefault(c => c.Name == column.Name);
                            if (fkColumn != null)
                            {
                                var isPK = fkColumn.IndexesName.Any(i => i.StartsWith("PK", StringComparison.OrdinalIgnoreCase));
                                if (isPK)
                                {
                                    var name = column.Name;
                                    if (name.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                                        name = name.SubstringToLast("Id") + "Item";
                                    else
                                        name = fkTable.Name;
                                    name = GetName(name);
                                    var tName = GetEntityNameDelegate(fkTable.Name);
                                    var type = "Ent" + tName;

                                    var fill = "            param[\"@" + column.Name + "\"] = value." + name + "." + GetName(column.Name) + ";\r\n";
                                    prepareEntities.Add(fill);

                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!added)
                    {
                        //We try to find other entity to match the Id (without FK)
                        if (column.Name != "Id" && column.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (var t in _schema.Tables)
                            {
                                var iPk = t.Indexes.FirstOrDefault(i => i.Type == IndexType.PrimaryKey);
                                if (iPk?.Columns?.Count == 1)
                                {
                                    if (iPk.Columns[0].ColumnName == column.Name)
                                    {
                                        var name = column.Name.SubstringToLast("Id") + "Item";
                                        name = GetName(name);
                                        var tName = GetEntityNameDelegate(t.Name);
                                        var type = "Ent" + tName;

                                        var fill = "            param[\"@" + column.Name + "\"] = value." + name + "." + GetName(column.Name) + ";\r\n";
                                        prepareEntities.Add(fill);

                                        added = true;
                                        break;

                                    }
                                }
                            }
                        }
                    }
                }

                if (!added)
                {
                    var fill = "            param[\"@" + column.Name + "\"] = value." + GetName(column.Name) + ";\r\n";
                    prepareEntities.Add(fill);
                }
            }
            return prepareEntities;
        }
        GeneratorSelectionContainer GetSelectColumns(string tableName)
        {
            var container = new GeneratorSelectionContainer();
            var table = _schema.Tables.FirstOrDefault(t => t.Name == tableName);
            if (table == null) return container;
            container.From = table.Name;

            foreach (var column in table.Columns)
            {
                bool added = false;

                if (!column.IndexesName.Any(i => i.StartsWith("PK", StringComparison.OrdinalIgnoreCase)))
                {
                    foreach (var fk in table.ForeignKeys)
                    {
                        var fkTable = _schema.Tables.FirstOrDefault(t => t.Name == fk.ForeignTable);
                        if (fkTable != null)
                        {
                            var fkColumn = fkTable.Columns.FirstOrDefault(c => c.Name == column.Name);
                            if (fkColumn != null)
                            {
                                var isPK = fkColumn.IndexesName.Any(i => i.StartsWith("PK", StringComparison.OrdinalIgnoreCase));
                                if (isPK)
                                {
                                    foreach (var foreignColumn in fkTable.Columns)
                                        container.Columns.Add(new GeneratorSelectionColumn
                                        {
                                            Table = fkTable.Name,
                                            Column = foreignColumn.Name,
                                            Alias = $"{fkTable.Name}.{foreignColumn.Name}"
                                        });

                                    container.Joins.Add(new GeneratorSelectionJoin
                                    {
                                        Table = fkTable.Name,
                                        TableColumn = fkColumn.Name,
                                        FromColumn = column.Name
                                    });
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!added)
                    {
                        //We try to find other entity to match the Id (without FK)
                        if (column.Name != "Id" && column.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (var t in _schema.Tables)
                            {
                                var iPk = t.Indexes.FirstOrDefault(i => i.Type == IndexType.PrimaryKey);
                                if (iPk?.Columns?.Count == 1)
                                {
                                    if (iPk.Columns[0].ColumnName == column.Name)
                                    {
                                        foreach (var foreignColumn in t.Columns)
                                            container.Columns.Add(new GeneratorSelectionColumn
                                            {
                                                Table = t.Name,
                                                Column = foreignColumn.Name,
                                                Alias = $"{t.Name}.{foreignColumn.Name}"
                                            });
                                        container.Joins.Add(new GeneratorSelectionJoin
                                        {
                                            Table = t.Name,
                                            TableColumn = iPk.Columns[0].ColumnName,
                                            FromColumn = column.Name
                                        });
                                        added = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                if (!added)
                    container.Columns.Add(new GeneratorSelectionColumn
                    {
                        Table = table.Name,
                        Column = column.Name,
                        Alias = column.Name
                    });
            }

			foreach (var index in table.Indexes)
			{
				var whereIdx = new GeneratorWhereIndex()
				{
					Name = index.ConstraintName
				};
				container.Wheres.Add(whereIdx);
				foreach (var column in index.Columns)
				{
					var tColumn = container.Columns.FirstOrDefault(c => c.Column == column.ColumnName);
					if (tColumn == null) continue;

					whereIdx.Fields.Add(new GeneratorWhereField
					{
						FieldName = tColumn.Column,
						TableName = tColumn.Table
					});
				}
			}

            return container;
        }
        #endregion


        void WriteToDisk(string fileName, string content)
        {
            var dname = Path.GetDirectoryName(fileName);
            if (!Directory.Exists(dname))
                Directory.CreateDirectory(dname);
            if (!File.Exists(fileName))
                File.WriteAllText(fileName, content);
        }
        string GetName(string name)
        {
            name = name.Replace("-", "_");
            name = name.Replace(" ", "_");
            name = name.Replace("__", "_");
            return name;
        }
        #endregion
    }
}