using System.Xml.Linq;
using BlogExporter.ConsoleUI.Extensions;
using BlogExporter.ConsoleUI.System;

namespace BlogExporter.ConsoleUI.Models
{
    public class Image : SitecoreItem
    {
        public Image(XElement xmlDoc, string xmlPath)
            : base(xmlDoc)
        {
            Alt = xmlDoc.GetField("alt");
            Width = xmlDoc.GetField("width");
            Height = xmlDoc.GetField("height");
            Dimensions = xmlDoc.GetField("dimensions");
            Blob = xmlDoc.GetField("blob");
            MimeType = xmlDoc.GetField("mime type");
            Extension = xmlDoc.GetField("extension");
            ParsePath(xmlPath);
        }

        private void ParsePath(string xmlPath)
        {
            if (xmlPath.Contains("Blog Images"))
            {
                string path = xmlPath.Replace("items/master/sitecore/media library/Images/Blog Images", string.Empty);
                int index = path.IndexOf(Name);
                if (index != -1)
                {
                    Path = path.Substring(0, index);
                }
            }
            else if (xmlPath.Contains("Blog Files"))
            {
                string path = xmlPath.Replace("items/master/sitecore/media library/Files/Blog Files", string.Empty);
                int index = path.IndexOf(Name);
                if (index != -1)
                {
                    Path = path.Substring(0, index);
                }
            }
            else
            {
                string path = xmlPath.Replace("items/master/sitecore/media library", string.Empty);
                int index = path.IndexOf(Name);
                if (index != -1)
                {
                    Path = path.Substring(0, index);
                }
            }
        }

        public Field Alt { get; set; }
        public Field Width { get; set; }
        public Field Height { get; set; }
        public Field Dimensions { get; set; }
        public Field Blob { get; set; }
        public Field MimeType { get; set; }
        public Field Extension { get; set; }
        public string Path { get; set; }
    }
}
