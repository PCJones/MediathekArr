﻿using System.Xml;
using System.Xml.Serialization;

namespace MediathekArrLib.Models.Newznab;

public class Response
{
    [XmlAttribute("offset")]
    public int Offset { get; set; }

    [XmlAttribute("total")]
    public int Total { get; set; }
}