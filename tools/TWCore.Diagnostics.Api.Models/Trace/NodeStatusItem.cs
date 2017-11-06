﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;

namespace TWCore.Diagnostics.Api.Models.Trace
{
    [DataContract]
    public class NodeStatusItem
    {
        [XmlAttribute, DataMember]
        public string Id { get; set; }
        [XmlAttribute, DataMember]
        public string NodeInfoId { get; set; }
        [XmlAttribute, DataMember]
        public DateTime Date { get; set; }
        [XmlAttribute, DataMember]
        public DateTime StartTime { get; set; }
        [XmlAttribute, DataMember]
        public DateTime Timestamp { get; set; }
        [XmlElement("Child"), DataMember]
        public List<NodeStatusChildItem> Children { get; set; }
    }
}