using System.Xml.Linq;
using BlogExporter.ConsoleUI.Extensions;
using BlogExporter.ConsoleUI.System;

namespace BlogExporter.ConsoleUI.Models
{
    public class SitecoreItem
    {
        public SitecoreItem(XElement xmlDoc)
        {
            Id = xmlDoc.Attribute("id")?.Value;
            Name = xmlDoc.Attribute("name")?.Value;
            CreatedBy = xmlDoc.GetField("__created by");
            UpdatedBy = xmlDoc.GetField("__updated by");
            Revision = xmlDoc.GetField("__revision");
            Created = xmlDoc.GetField("__created");
            Updated = xmlDoc.GetField("__updated");
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public Field CreatedBy { get; set; }
        public Field UpdatedBy { get; set; }
        public Field Revision { get; set; }
        public Field Created { get; set; }
        public Field Updated { get; set; }

        public override string ToString()
        {
            return Name ?? base.ToString();
        }
    }
}
