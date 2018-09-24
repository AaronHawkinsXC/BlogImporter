using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using BlogExporter.ConsoleUI.Helpers;
using BlogExporter.ConsoleUI.Models;
using BlogExporter.ConsoleUI.System;
using HtmlAgilityPack;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Dtos = Project.Website.Models;

namespace BlogExporter.ConsoleUI
{
    class Program
    {
        static void Main(string[] args)
        {
            string sourceFilePath = GetParameterValue(Environment.CurrentDirectory + @"\source.zip", args, 0);

            ZipFile zipFile = null;
            try
            {
                FileStream fs = File.OpenRead(sourceFilePath);
                zipFile = new ZipFile(fs);

                using (MemoryStream packageStream = new MemoryStream())
                {
                    zipFile.GetInputStream(0).CopyTo(packageStream);

                    ZipFile packageFile = new ZipFile(packageStream);

                    Database database = new Database();
                    Console.WriteLine("Reading package file...");
                    foreach (ZipEntry packageEntry in packageFile)
                    {
                        if (!packageEntry.IsFile)
                        {
                            continue;
                        }

                        if (packageEntry.Name.StartsWith("blob"))
                        {
                            ReadBlob(packageFile, packageEntry, database);
                        }
                        if (packageEntry.Name.StartsWith("items"))
                        {
                            ReadItem(packageFile, packageEntry, database);
                        }
                    }
                    Console.WriteLine("Done.");

                    Console.WriteLine("Cleaning database...");
                    CleanDatabase(database);
                    Console.WriteLine("Done.");
                    
                    SubmitDatabase(database);
                }

                Console.WriteLine("Process has completed!  Please press any key to exit.");
                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadKey();
            }
            finally
            {
                if (zipFile != null)
                {
                    zipFile.IsStreamOwner = true;
                    zipFile.Close();
                }
            }
        }

        public static string GetParameterValue(string defaultValue, string[] args, int argIndex)
        {
            string value = defaultValue;
            if (args.Length > argIndex)
            {
                value = args[argIndex];
            }

            return value;
        }

        public static void ReadBlob(ZipFile file, ZipEntry itemEntry, Database destinationDb)
        {
            Stream blobStream = file.GetInputStream(itemEntry);
            using (var ms = new MemoryStream())
            {
                blobStream.CopyTo(ms);

                Blob blob = new Blob()
                {
                    Name = itemEntry.Name.Replace("blob/master/",string.Empty),
                    Data = ms.ToArray()
                };
                destinationDb.Blobs.Add(blob);
            }
        }
        
        public static void ReadItem(ZipFile file, ZipEntry itemEntry, Database destinationDb)
        {
            Stream itemStream = file.GetInputStream(itemEntry);

            try
            {
                XElement xmlDoc = XElement.Load(itemStream);

                string templateName = xmlDoc.Attribute("template")?.Value;
                switch (templateName)
                {
                    case "blog author":
                        Author author = new Author(xmlDoc);
                        destinationDb.Authors.Add(author);
                        break;
                    case "blog category":
                    case "blog tag":
                        Taxonomy taxonomy = new Taxonomy(xmlDoc);
                        destinationDb.TaxonomyItems.Add(taxonomy);
                        break;
                    case "blog post":
                        BlogPost blogPost = new BlogPost(xmlDoc);
                        destinationDb.BlogPosts.Add(blogPost);
                        break;
                    case "image":
                        Image image = new Image(xmlDoc, itemEntry.Name);
                        destinationDb.Images.Add(image);
                        break;
                    case "jpeg":
                        Jpeg jpeg = new Jpeg(xmlDoc, itemEntry.Name);
                        destinationDb.Images.Add(jpeg);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error loading {itemEntry.Name}...");
                Console.WriteLine(e);
            }
        }

        public static void CleanDatabase(Database db)
        {
            List<string> referencedImageIds = new List<string>();

            //Find all references from Blogs.
            referencedImageIds.AddRange(db.BlogPosts.SelectMany(bp => bp.ImageReferences));

            //Find all references from Authors.
            IEnumerable<Field> imageFields = db.Authors.Where(a => a.ProfileImage != null).Select(a => a.ProfileImage);
            foreach (Field imageField in imageFields)
            {
                if (!string.IsNullOrEmpty(imageField.Value))
                {
                    var imageFieldDecoded = WebUtility.HtmlDecode(imageField.Value);
                    var doc = new HtmlDocument();
                    doc.LoadHtml(imageFieldDecoded);

                    var id = doc?.DocumentNode?.SelectSingleNode("/image")?.Attributes["mediaid"]?.Value;
                    referencedImageIds.Add(id);
                }
            }

            List<Image> imagesToRemove = new List<Image>();
            //Remove any non-referenced images.
            foreach (Image img in db.Images)
            {
                if (!referencedImageIds.Contains(img.Id))
                {
                    imagesToRemove.Add(img);
                }
            }

            foreach (Image imageToRemove in imagesToRemove)
            {
                db.Images.Remove(imageToRemove);
            }

            IEnumerable<string> referencedBlobIds = db.Images.Select(i => i.Blob.Value);
            
            List<Blob> blobsToRemove = new List<Blob>();
            //Remove any non-referenced blobs.
            foreach (Blob blob in db.Blobs)
            {
                if (!referencedBlobIds.Contains("{" + blob.Name.ToUpper() + "}"))
                {
                    blobsToRemove.Add(blob);
                }
            }

            foreach (Blob blobToRemove in blobsToRemove)
            {
                db.Blobs.Remove(blobToRemove);
            }

            List<Taxonomy> tagsToRemove = new List<Taxonomy>();
            //Remove Duplicate Tags
            foreach (var duplicateTag in db.TaxonomyItems.Where(dt =>
                db.TaxonomyItems.Count(ti => ti.Name == dt.Name) > 1))
            {
                var lastTag = db.TaxonomyItems.Last(ft => ft.Name == duplicateTag.Name);
                var otherTags = db.TaxonomyItems.Where(ot => ot.Name == duplicateTag.Name && ot.Id != lastTag.Id);

                //Replace references to other tags with the last tag.
                var otherTagList = otherTags as Taxonomy[] ?? otherTags.ToArray();
                foreach (var otherTag in otherTagList)
                {
                    var blogsWithTag = db.BlogPosts.Where(bp => bp.Tags != null && bp.Tags.Value.Contains(otherTag.Id));
                    foreach (var blogPost in blogsWithTag)
                    {
                        //Replace reference to other duplicate tag with the id of the last instance.
                        blogPost.Tags.Value = blogPost.Tags.Value.Replace(otherTag.Id, lastTag.Id);
                    }
                }

                tagsToRemove.AddRange(otherTagList);
            }

            foreach (Taxonomy tagToRemove in tagsToRemove)
            {
                db.TaxonomyItems.Remove(tagToRemove);
            }
        }

        public static void SubmitDatabase(Database db)
        {
            //Console.WriteLine("Importing media items...");
            //SaveMediaItems(db);
            //Console.WriteLine("Done.");

            Console.WriteLine("Importing authors...");
            SaveAuthors(db);
            Console.WriteLine("Done.");

            Console.WriteLine("Importing taxonomy...");
            SaveTaxonomy(db);
            Console.WriteLine("Done.");

            Console.WriteLine("Importing blogs...");
            SaveBlogs(db);
            Console.WriteLine("Done.");
        }

        private static void SaveMediaItems(Database db)
        {
            List<Dtos.Image> images = new List<Dtos.Image>();
            foreach (var sourceImage in db.Images)
            {
                Dtos.Image destinationImage = null;
                if (sourceImage is Jpeg)
                {
                    var destinationJpeg = new Dtos.Jpeg()
                    {
                        Id = sourceImage.Id,
                        Name = sourceImage.Name,
                        CreatedBy = sourceImage.CreatedBy.Value,
                        UpdatedBy = sourceImage.UpdatedBy.Value,
                        Revision = sourceImage.Revision.Value
                    };
                    var sourceJpeg = sourceImage as Jpeg;

                    destinationJpeg.Artist = sourceJpeg.Artist?.Value;
                    destinationJpeg.Copyright = sourceJpeg.Copyright?.Value;
                    destinationJpeg.ImageDescription = sourceJpeg.ImageDescription?.Value;
                    destinationJpeg.Make = sourceJpeg.Make?.Value;
                    destinationJpeg.Model = sourceJpeg.Model?.Value;
                    destinationJpeg.Software = sourceJpeg.Software?.Value;

                    destinationImage = destinationJpeg;
                }
                else
                {
                    destinationImage = new Dtos.Image()
                    {
                        Id = sourceImage.Id,
                        Name = sourceImage.Name,
                        CreatedBy = sourceImage.CreatedBy.Value,
                        UpdatedBy = sourceImage.UpdatedBy.Value,
                        Revision = sourceImage.Revision.Value
                    };
                }

                destinationImage.Created = DateReader.GetDateFromSitecoreFieldValue(sourceImage.Created.Value);
                destinationImage.Updated = DateReader.GetDateFromSitecoreFieldValue(sourceImage.Updated.Value);

                destinationImage.Alt = sourceImage.Alt?.Value;
                destinationImage.Width = sourceImage.Width?.Value;
                destinationImage.Height = sourceImage.Height?.Value;
                destinationImage.Dimensions = sourceImage.Dimensions?.Value;
                destinationImage.MimeType = sourceImage.MimeType?.Value;

                destinationImage.Path = sourceImage.Path;

                Blob blob = db.Blobs.Single(b => "{"+b.Name.ToUpper()+"}" == sourceImage.Blob.Value);
                destinationImage.Blob = blob.Data;
                destinationImage.FileName = sourceImage.Name + "." + sourceImage.Extension?.Value;

                destinationImage.IsAuthor = db.Authors.Any(a => a.ProfileImageMediaID == sourceImage.Id);

                images.Add(destinationImage);
            }

            using (var httpClient = new HttpClient())
            {
                var uri = new Uri("https://saasxm.xcentium.com/sitecore/api/ssc/blogapi/blogimporter/0/savemediaitems");
                var jsonRequest = JsonConvert.SerializeObject(images);
                var stringContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                var task = Task.Run(async () =>
                {
                    HttpResponseMessage msg = await httpClient.PostAsync(uri, stringContent);
                    if (!msg.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Received response failure when saving Media Items. {msg.Content.ToString()}");
                    }
                });
                task.Wait(new TimeSpan(0, 30, 0));
            }
        }

        private static void SaveAuthors(Database db)
        {
            List<Dtos.Author> authors =
                new List<Dtos.Author>();
            foreach (var sourceAuthor in db.Authors)
            {
                Dtos.Author destinationAuthor =
                    new Dtos.Author
                    {
                        Id = sourceAuthor.Id,
                        Name = sourceAuthor.Name,
                        CreatedBy = sourceAuthor.CreatedBy.Value,
                        UpdatedBy = sourceAuthor.UpdatedBy.Value,
                        Revision = sourceAuthor.Revision.Value
                    };

                destinationAuthor.Created = DateReader.GetDateFromSitecoreFieldValue(sourceAuthor.Created.Value);
                destinationAuthor.Updated = DateReader.GetDateFromSitecoreFieldValue(sourceAuthor.Updated.Value);

                destinationAuthor.FullName = sourceAuthor.FullName?.Value;
                destinationAuthor.Title = sourceAuthor.Title?.Value;
                destinationAuthor.Location = sourceAuthor.Location?.Value;
                destinationAuthor.Bio = sourceAuthor.Bio?.Value;
                destinationAuthor.ProfileImage = sourceAuthor.ProfileImage?.Value;
                destinationAuthor.EmailAddress = sourceAuthor.EmailAddress?.Value;
                destinationAuthor.Creator = sourceAuthor.Creator?.Value;

                authors.Add(destinationAuthor);
            }

            using (var httpClient = new HttpClient())
            {
                var uri = new Uri("https://saasxm.xcentium.com/sitecore/api/ssc/blogapi/blogimporter/0/saveauthors");
                var jsonRequest = JsonConvert.SerializeObject(authors);
                var stringContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                var response = httpClient.PostAsync(uri, stringContent).Result;
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Received response failure when saving Authors.");
                }
            }
        }

        private static void SaveTaxonomy(Database db)
        {
            List<Dtos.Taxonomy> tags = new List<Dtos.Taxonomy>();
            foreach (var sourceTag in db.TaxonomyItems)
            {
                Dtos.Taxonomy destinationTag = new Dtos.Taxonomy()
                {
                    Id = sourceTag.Id,
                    Name = sourceTag.Name,
                    CreatedBy = sourceTag.CreatedBy.Value,
                    UpdatedBy = sourceTag.UpdatedBy.Value,
                    Revision = sourceTag.Revision.Value
                };

                destinationTag.Created = DateReader.GetDateFromSitecoreFieldValue(sourceTag.Created.Value);
                destinationTag.Updated = DateReader.GetDateFromSitecoreFieldValue(sourceTag.Updated.Value);

                destinationTag.TagName = sourceTag.TagName?.Value;

                tags.Add(destinationTag);
            }

            using (var httpClient = new HttpClient())
            {
                var uri = new Uri("https://saasxm.xcentium.com/sitecore/api/ssc/blogapi/blogimporter/0/savetags");
                var jsonRequest = JsonConvert.SerializeObject(tags);
                var stringContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                var response = httpClient.PostAsync(uri, stringContent).Result;
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Received response failure when saving Tags.");
                }
            }
        }

        private static void SaveBlogs(Database db)
        {
            List<Dtos.Blog> blogs = new List<Dtos.Blog>();
            foreach (var sourceBlog in db.BlogPosts)
            {
                Dtos.Blog destinationBlog = new Dtos.Blog()
                {
                    Id = sourceBlog.Id,
                    Name = sourceBlog.Name,
                    CreatedBy = sourceBlog.CreatedBy.Value,
                    UpdatedBy = sourceBlog.UpdatedBy.Value,
                    Revision = sourceBlog.Revision.Value
                };

                destinationBlog.Created = DateReader.GetDateFromSitecoreFieldValue(sourceBlog.Created.Value);
                destinationBlog.Updated = DateReader.GetDateFromSitecoreFieldValue(sourceBlog.Updated.Value);

                destinationBlog.Title = sourceBlog.Title?.Value;
                destinationBlog.Image = sourceBlog.Image?.Value;
                destinationBlog.Summary = sourceBlog.Summary?.Value;
                destinationBlog.Body = sourceBlog.Body?.Value;
                destinationBlog.Author = sourceBlog.Author?.Value;

                destinationBlog.PublishingDate =
                    DateReader.GetDateFromSitecoreFieldValue(sourceBlog.PublishingDate?.Value);

                destinationBlog.Tags = sourceBlog.Tags?.Value;
                destinationBlog.BrowserTitle = sourceBlog.BrowserTitle?.Value;
                destinationBlog.MetaDescription = sourceBlog.MetaDescription?.Value;
                destinationBlog.MetaKeywords = sourceBlog.MetaKeywords?.Value;
                destinationBlog.SearchTitle = sourceBlog.SearchTitle?.Value;
                destinationBlog.SearchDescription = sourceBlog.SearchDescription?.Value;
                destinationBlog.SearchImage = sourceBlog.SearchImage?.Value;

                blogs.Add(destinationBlog);
            }

            using (var httpClient = new HttpClient())
            {
                var uri = new Uri("https://saasxm.xcentium.com/sitecore/api/ssc/blogapi/blogimporter/0/saveblogs");
                var jsonRequest = JsonConvert.SerializeObject(blogs);
                var stringContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                var response = httpClient.PostAsync(uri, stringContent).Result;
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Received response failure when saving Blogs.");
                }
            }
        }
    }
}
