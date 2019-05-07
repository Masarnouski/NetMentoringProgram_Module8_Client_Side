using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using AngleSharp.Parser.Html;
using AngleSharp.Dom.Html;
using AngleSharp.Dom;
using System.Text;
using System.Text.RegularExpressions;

namespace Async_await_Task
{
    public class HtmlParser
    {
        private string[] fileExtensions = new string[] { ".jpg", ".png", ".gif", ".pdf" };
        private string url;
        private string initPath;
        private int loadLevel;
        private Regex regexForSavedAddress = new Regex(@"^https://.+\..+");
        private Regex regexForUnsavedAddress = new Regex(@"^http://.+\..+");

        public HtmlParser(string url, string path, int loadLevel)
        {
            this.url = string.IsNullOrEmpty(url) ? throw new ArgumentNullException() : url;
            this.initPath = string.IsNullOrEmpty(path) || !Directory.Exists(path) ? throw new ArgumentNullException() : path;
            this.loadLevel = loadLevel;
        }

        public async Task StartSavingAsync()
        {
            IEnumerable<char> urlPath = "";

            MatchCollection matchesForSavedAddress = regexForSavedAddress.Matches(url);
            if (matchesForSavedAddress.Count > 0)
            {
                urlPath = url.Skip(8);
            }

            MatchCollection matchesForUnsavedAddress = regexForUnsavedAddress.Matches(url);
            if (matchesForUnsavedAddress.Count > 0)
            {
                urlPath = url.Skip(7);
            }

            var builder = new StringBuilder();
            foreach (var a in urlPath)
            {
                builder.Append(a);
            }

            var path = CreateFolder(initPath, builder.ToString());
            await StartSavingAsync(loadLevel, null, path);
        }

        private async Task StartSavingAsync(int loadLevel, List<IElement> list, string startPath)
        {
            var path = startPath;

            if (loadLevel >= 0)
            {

                Console.WriteLine($"Processing {url}" + '\n' + "Downloading html");
                string htmlDoc = await DownloadHtmlAsync(url);

                var parsedHtml = await ParseHtmlAsync(htmlDoc);

                await SaveHtmlAsync(path, htmlDoc);

                Console.WriteLine("Html downloaded");

                string resourcePath = CreateFolder(path, "resources");

                Console.WriteLine("Created folder: resources" + '\n' + "Downloading resources");
                await DownloadResourcesAsync(parsedHtml, resourcePath);

                Console.WriteLine("All resources are downloaded");

                path = CreateFolder(path, "links");
                Console.WriteLine("Created folder: links" + '\n');

                var elements = parsedHtml.QuerySelectorAll("a").Where(c => !string.IsNullOrEmpty(c.GetAttribute("href"))).ToList(); //&& c.GetAttribute("href").Contains(url)).ToList();

                foreach (var b in elements.Take(3))
                {
                    string subPath = path;
                    url = b.GetAttribute("href");
                    subPath = CreateFolder(path, Path.GetRandomFileName());
                    await StartSavingAsync(loadLevel - 1, elements, subPath);
                }

                Console.WriteLine("Work is done!");
            }
        }

        
        private string CreateFolder(string currentPath, string folderName)
        {
            string path = currentPath + @"\" + folderName;

            if (!Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (Exception e) { }
            }

            return path;
        }

        private async Task DownloadResourcesAsync(IHtmlDocument document, string path)
        {          
            var resources = from element in document.All
                            from attribute in element.Attributes
                            where fileExtensions.Any(e => attribute.Value.EndsWith(e) && attribute.Value.StartsWith("http"))
                            select attribute;

            using (var client = new HttpClient())
            {
                foreach (var item in resources)
                {
                    using (var response = await client.GetAsync(item.Value))
                    {
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        {
                            using (var streamWrite = File.Open(path + @"\" + GetFilename(item.Value), FileMode.Create))
                            {
                                await stream.CopyToAsync(streamWrite);
                            }
                        }
                    }
                }
            }
        }

        private string GetFilename(string hreflLink)
        {
            return Path.GetFileName(hreflLink);
        }

        private async Task<string> DownloadHtmlAsync(string url)
        {
            try
            {
                using (var req = new HttpClient())
                {
                    using (var response = await req.GetAsync(url))
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                }
            }
            catch (Exception e) { return null; }
        }

        private async Task<IHtmlDocument> ParseHtmlAsync(string html)
        {
            var parser = new AngleSharp.Parser.Html.HtmlParser();
            var document = await parser.ParseAsync(html);
            return document;
        }

        private async Task SaveHtmlAsync(string path, string htmlDocument)
        {
            using (var outputFile = new StreamWriter(Path.Combine(path, "Html.htm")))
            {
                await outputFile.WriteAsync(htmlDocument);
            }
        }
    }
}
