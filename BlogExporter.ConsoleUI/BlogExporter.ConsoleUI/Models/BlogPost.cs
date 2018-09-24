using System;
using System.Collections.Generic;
using System.Net;
using System.Xml.Linq;
using BlogExporter.ConsoleUI.Extensions;
using BlogExporter.ConsoleUI.System;
using HtmlAgilityPack;

namespace BlogExporter.ConsoleUI.Models
{
    public class BlogPost : SitecoreItem
    {
        public BlogPost(XElement xmlDoc)
            : base(xmlDoc)
        {
            Title = xmlDoc.GetField("title");
            Image = null;
            Summary = xmlDoc.GetField("summary");
            Body = xmlDoc.GetField("body");
            Author = xmlDoc.GetMultiListField("author");
            PublishingDate = xmlDoc.GetField("publish") ?? xmlDoc.GetField("publish date");

            var tagsField = xmlDoc.GetMultiListField("tags");
            var categoryField = xmlDoc.GetMultiListField("category");
            if (tagsField != null)
            {
                Tags = tagsField;
                if (categoryField != null)
                {
                    Tags.AddItemsFromMultiListField(categoryField);
                }
            }
            else
            {
                Tags = categoryField;
            }

            SearchTitle = xmlDoc.GetField("title");
            SearchDescription = xmlDoc.GetField("summary");
            SearchImage = null;
            BrowserTitle = xmlDoc.GetField("browser title");
            ParseMetaTags(xmlDoc.GetField("meta tags")?.Value);
            ImageReferences = new List<string>();
            ParseBodyForImageReferences();
        }

        private void ParseMetaTags(string metaTags)
        {
            if (string.IsNullOrEmpty(metaTags))
            {
                return;
            }

            var metaTagsDecoded = WebUtility.HtmlDecode(metaTags);

            var doc = new HtmlDocument();
            doc.LoadHtml(metaTagsDecoded);
            
            var keywordsValue = doc?.DocumentNode?.SelectSingleNode("/*/meta[@name='keywords']")?
                .GetAttributeValue("content", null);
            if (keywordsValue != null)
            {
                Field keywords = Field.Create(
                    name: "MetaKeywords",
                    type: "Single-Line Text",
                    value: keywordsValue);
                MetaKeywords = keywords;
            }

            var descriptionValue = doc?.DocumentNode?.SelectSingleNode("/*/meta[@name='description']")?
                .GetAttributeValue("content", null);
            if (descriptionValue != null)
            {
                Field description = Field.Create(
                    name: "MetaDescription",
                    type: "Single-Line Text",
                    value: descriptionValue);
                MetaDescription = description;
            }
    }

        private void ParseBodyForImageReferences()
        {
            if (string.IsNullOrEmpty(Body?.Value))
                return;

            var doc = new HtmlDocument();
            try
            {
                doc.LoadHtml(Body.Value);

                var nodes = doc?.DocumentNode?.SelectNodes("//img");
                if (nodes != null)
                    foreach (var node in nodes)
                    {
                        var src = node.GetAttributeValue("src", null);
                        if (!string.IsNullOrEmpty(src) && src.Contains("-/media"))
                        {
                            var mediaStripped = src.Replace("-/media/", string.Empty);
                            var mediaId = mediaStripped.Substring(0, mediaStripped.IndexOf('.'));
                            if (Guid.TryParse(mediaId, out var mediaGuid))
                            {
                                ImageReferences.Add(mediaGuid.ToString("B").ToUpper());
                            }
                        }
                    }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public Field Title { get; set; }
        public Field Image { get; set; }
        public Field Summary { get; set; }
        public Field Body { get; set; }
        public MultiListField Author { get; set; }
        public Field PublishingDate { get; set; }
        public MultiListField Category { get; set; }
        public MultiListField Tags { get; set; }
        public Field SearchTitle { get; set; }
        public Field SearchDescription { get; set; }
        public Field SearchImage { get; set; }
        public Field BrowserTitle { get; set; }
        public Field MetaDescription { get; set; }
        public Field MetaKeywords { get; set; }
        public Field CanonicalTagValue { get; set; }
        public Field CustomMetadata { get; set; }
        public List<string> ImageReferences { get; set; }
    }
}
