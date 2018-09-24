using System.Collections.Generic;

namespace BlogExporter.ConsoleUI.Models
{
    public class Database
    {
        public List<Author> Authors = new List<Author>();

        public List<Taxonomy> TaxonomyItems = new List<Taxonomy>();

        public List<BlogPost> BlogPosts = new List<BlogPost>();

        public List<Image> Images = new List<Image>();

        public List<Blob> Blobs = new List<Blob>();
    }
}
