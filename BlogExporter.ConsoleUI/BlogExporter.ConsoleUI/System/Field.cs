using System.Xml;
using System.Xml.Linq;

namespace BlogExporter.ConsoleUI.System
{
    public class Field
    {
        public Field(XElement node)
        {
            if (node.Name.LocalName == "field")
            {
                Name = node.Attribute("key")?.Value;
                Type = node.Attribute("type")?.Value;
                Value = node.Element("content")?.Value;
            }
        }

        private Field() { }

        public static Field Create(string name, string type, string value)
        {
            return new Field()
            {
                Name = name,
                Type = type,
                Value = value
            };
        }

        public string Name { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }

        public override string ToString()
        {
            return Value;
        }
    }
}
