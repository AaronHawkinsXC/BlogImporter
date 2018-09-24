using System.Xml.Linq;
using BlogExporter.ConsoleUI.Extensions;
using BlogExporter.ConsoleUI.System;

namespace BlogExporter.ConsoleUI.Models
{
    public class Taxonomy : SitecoreItem
    {
        public Taxonomy(XElement xmlDoc)
            : base(xmlDoc)
        {
            TagName = xmlDoc.GetField("tag name") ?? xmlDoc.GetField("category name");
        }

        public Field TagName { get; set; }
    }
}
