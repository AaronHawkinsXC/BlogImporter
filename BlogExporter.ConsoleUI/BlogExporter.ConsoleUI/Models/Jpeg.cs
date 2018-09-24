using System.Xml.Linq;
using BlogExporter.ConsoleUI.Extensions;
using BlogExporter.ConsoleUI.System;

namespace BlogExporter.ConsoleUI.Models
{
    public class Jpeg : Image
    {
        public Jpeg(XElement xmlDoc, string xmlPath)
            : base(xmlDoc, xmlPath)
        {
            Artist = xmlDoc.GetField("artist");
            Copyright = xmlDoc.GetField("copyright");
            DateTime = xmlDoc.GetField("datetime");
            ImageDescription = xmlDoc.GetField("imagedescription");
            Make = xmlDoc.GetField("make");
            Model = xmlDoc.GetField("model");
            Software = xmlDoc.GetField("software");
        }

        public Field Artist { get; set; }
        public Field Copyright { get; set; }
        public Field DateTime { get; set; }
        public Field ImageDescription { get; set; }
        public Field Make { get; set; }
        public Field Model { get; set; }
        public Field Software { get; set; }
    }
}
