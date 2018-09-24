using System.Linq;
using System.Xml.Linq;
using BlogExporter.ConsoleUI.System;

namespace BlogExporter.ConsoleUI.Extensions
{
    public static class XmlDocumentExtensions
    {
        public static Field GetField(this XElement xmlDoc, string fieldName)
        {
            XElement element = xmlDoc.Descendants("field").SingleOrDefault(d => d.Attribute("key")?.Value == fieldName);
            if (element != null)
                return new Field(element);

            return null;
        }

        public static MultiListField GetMultiListField(this XElement xmlDoc, string fieldName)
        {
            XElement element = xmlDoc.Descendants("field").SingleOrDefault(d => d.Attribute("key")?.Value == fieldName);
            if (element != null)
                return new MultiListField(element);

            return null;
        }
    }
}
