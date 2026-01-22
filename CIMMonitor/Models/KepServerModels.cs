using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace CIMMonitor.Models
{
    [Serializable]
    public class KepServerProject
    {
        [XmlAttribute("xmlns")]
        public string XmlNamespace { get; set; }

        [XmlAttribute("Version")]
        public string Version { get; set; }

        [XmlAttribute("ProjectID")]
        public string ProjectId { get; set; }

        [XmlElement("Properties")]
        public Properties Properties { get; set; }

        [XmlArray("Channels")]
        [XmlArrayItem("Channel")]
        public List<Channel> Channels { get; set; } = new List<Channel>();
    }

    [Serializable]
    public class Properties
    {
        [XmlElement("Property")]
        public List<Property> PropertyList { get; set; } = new List<Property>();
    }

    [Serializable]
    public class Property
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("Value")]
        public string Value { get; set; }
    }

    [Serializable]
    public class Channel
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("Driver")]
        public string Driver { get; set; }

        [XmlElement("Properties")]
        public Properties Properties { get; set; }

        [XmlArray("Devices")]
        [XmlArrayItem("Device")]
        public List<Device> Devices { get; set; } = new List<Device>();
    }

    [Serializable]
    public class Device
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlElement("Properties")]
        public Properties Properties { get; set; }

        [XmlArray("TagGroups")]
        [XmlArrayItem("TagGroup")]
        public List<TagGroup> TagGroups { get; set; } = new List<TagGroup>();
    }

    [Serializable]
    public class TagGroup
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlArray("Tags")]
        [XmlArrayItem("Tag")]
        public List<Tag> Tags { get; set; } = new List<Tag>();
    }

    [Serializable]
    public class Tag
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("Address")]
        public string Address { get; set; }

        [XmlAttribute("DataType")]
        public string DataType { get; set; }

        [XmlAttribute("AccessRights")]
        public string AccessRights { get; set; }

        [XmlAttribute("ScanRate")]
        public int ScanRate { get; set; }

        [XmlAttribute("Description")]
        public string Description { get; set; }
    }

    // 数据变更事件参数
    public class DataChangedEventArgs : EventArgs
    {
        public string TagName { get; set; }
        public object Value { get; set; }
        public DateTime Timestamp { get; set; }
        public string DeviceName { get; set; }
        public string ChannelName { get; set; }
        public string GroupName { get; set; }
    }

    // 数据变更事件委托
    public delegate void DataChangedEventHandler(object sender, DataChangedEventArgs e);

    // OPC DA 事件定义
    public class OpcDaEvent
    {
        public string EventId { get; set; }
        public string EventType { get; set; } // Send or Receive
        public string Description { get; set; }
        public string TriggerTagId { get; set; }
        public string TriggerCondition { get; set; } = "RisingEdge";
        public List<string> TargetGroupIds { get; set; } = new List<string>(); // List of group IDs
        public bool Enabled { get; set; } = true;
    }
}
