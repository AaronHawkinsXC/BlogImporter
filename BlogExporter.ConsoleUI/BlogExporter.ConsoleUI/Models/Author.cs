using System;
using System.Xml.Linq;
using BlogExporter.ConsoleUI.Extensions;
using BlogExporter.ConsoleUI.System;
using HtmlAgilityPack;

namespace BlogExporter.ConsoleUI.Models
{
    public class Author : SitecoreItem
    {
        public Author(XElement xmlDoc)
            : base(xmlDoc)
        {
            FullName = xmlDoc.GetField("full name");
            Title = xmlDoc.GetField("title");
            Location = xmlDoc.GetField("location");
            Bio = xmlDoc.GetField("bio");
            ProfileImage = xmlDoc.GetField("profile image");
            EmailAddress = xmlDoc.GetField("email address");
            Creator = xmlDoc.GetField("creator");
            ParseProfileImageMediaID();
        }

        private void ParseProfileImageMediaID()
        {
            if (!string.IsNullOrEmpty(ProfileImage?.Value))
            {
                var doc = new HtmlDocument();
                try
                {
                    doc.LoadHtml(ProfileImage.Value);

                    var node = doc?.DocumentNode?.SelectSingleNode("/image");
                    ProfileImageMediaID = node?.GetAttributeValue("mediaid", null);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public Field FullName { get; set; }
        public Field Title { get; set; }
        public Field Location { get; set; }
        public Field Bio { get; set; }
        public Field ProfileImage { get; set; }
        public Field EmailAddress { get; set; }
        public Field Creator { get; set; }
        public string ProfileImageMediaID { get; set; }
    }
}
