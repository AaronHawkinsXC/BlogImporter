using System.Xml.Linq;
using BlogExporter.ConsoleUI.Extensions;
using BlogExporter.ConsoleUI.System;

namespace BlogExporter.ConsoleUI.Models
{
    public class Category : SitecoreItem
    {
        public Category(XElement xmlDoc)
            : base(xmlDoc)
        {
            CategoryName = xmlDoc.GetField("category name");
        }
        
        public Field CategoryName { get; set; }
    }
}
