﻿/*
Copyright 2015-2018 Daniel Adrian Redondo Suarez

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

using System.Collections.Generic;

namespace TWCore.Data.Schema.Generator
{
    /// <summary>
    /// Generator Selection Container
    /// </summary>
    public class GeneratorSelectionContainer
    {
        public List<GeneratorSelectionColumn> Columns { get; } = new List<GeneratorSelectionColumn>();
        public string From { get; set; }
        public List<GeneratorSelectionJoin> Joins { get; } = new List<GeneratorSelectionJoin>();
		public List<GeneratorWhereIndex> Wheres { get; } = new List<GeneratorWhereIndex>();
        public List<(string, string)> TableColumns { get; } = new List<(string, string)>();
    }
}
