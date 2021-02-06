using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace Ripper {
    class Program {
        private const string RootDownloadURL = @"files.replacementdocs.com/";
        private const string SavePathRoot = @"E:\Site Rips\replacementdocs\";
        public static readonly string Site = @"http://www.replacementdocs.com/request.php?";
        public static readonly bool Wait = false;
        //public static readonly int TotalPages = 141;

        static void Main(string[] args) {
            HtmlWeb web = new HtmlWeb();
            WebClient client = new WebClient();
            List<string> gamePageURLs = new List<string>();
            List<HtmlNode> extrasLinks = new List<HtmlNode>();
            int gameCount = 0;

            bool skipPage = false;

            //iterate through each page of games
            for (int page = 1; page > 0; page++)
            {
                skipPage = false;

                Debug.WriteLine("Scraping Listing Page " + (page));
                HtmlNode rawPageNode = web.Load(Site + page).DocumentNode;

                //replacementdocs returns "File Not Found" when you ask for a bad page
                foreach (HtmlNode node in rawPageNode.Descendants(1))
                {
                    if (node.HasClass("bodymain"))
                    {
                        if (node.InnerHtml.Contains("File Not Found"))
                        {
                            skipPage = true;
                            break;
                        }
                    }
                }

                if (skipPage)
                    continue;

                //CONTINUE FROM HERE, Load page, correct URL, download into Platform folder

                gamePageURLs.AddRange(BuildGameNodeList(rawPageNode.Descendants(1)));

                gameCount = 1;
                foreach (string URL in gamePageURLs)
                {
                    Debug.WriteLine("Scraping Page #" + (page) + " Game #" + gameCount); gameCount++;
                    HtmlNode gamePage = web.Load(RootDownloadURL + URL).DocumentNode;

                    //find extras and add to list
                    foreach (HtmlNode gameNode in gamePage.Descendants(1))
                    {
                        if (gameNode.Name == "a")
                        {
                            if (gameNode.OuterHtml.Contains("ExtraID"))
                            {
                                extrasLinks.Add(gameNode);
                            }
                        }
                    }
                    if (Wait) System.Threading.Thread.Sleep(500);

                    //for each extra, convert to link and metadata and download
                    foreach (HtmlNode extrasNode in extrasLinks)
                    {
                        string downloadUrl = RootDownloadURL + extrasNode.OuterHtml.Split('"')[1];

                        //build filename
                        string gameTitle = extrasNode.OwnerDocument.DocumentNode.ChildNodes["html"].ChildNodes["head"].ChildNodes["title"].InnerHtml.Replace(" @  Reloaded.org", "").Trim();
                        string extraType = WebUtility.HtmlDecode(extrasNode.InnerText).Replace('|', '-').Trim();
                        //string extension = Path.GetExtension(downloadUrl);
                        string filename = Utility.GetSafeFilename(gameTitle + "_" + extraType);

                        //build savepath
                        string subfolder = Utility.GetSafeFilename(gameTitle) + "\\";
                        string savePath = SavePathRoot + subfolder;

                        //Have to determine extension after the fact
                        //if (extension == "")
                        //    continue;

                        if (!Directory.Exists(savePath))
                            Directory.CreateDirectory(savePath);

                        HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(downloadUrl);
                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        {
                            var contentName = response.Headers["Content-Disposition"].Split(new string[] { "=" }, StringSplitOptions.None)[1];
                            if (contentName[contentName.Length - 1] == ';') contentName = contentName.TrimEnd(';');
                            string extension = Path.GetExtension(contentName);
                            if (!File.Exists(savePath + filename + extension))
                            {
                                Debug.WriteLine("Downloading " + filename + extension);
                                var responseStream = response.GetResponseStream();
                                using (var fileStream = File.Create(Path.Combine(savePath, filename + extension)))
                                {
                                    responseStream.CopyTo(fileStream);
                                }
                            }
                            else
                                Debug.WriteLine("Skipping " + filename + extension);

                        }

                        //if (!File.Exists(savePath + filename))
                        //{
                        //    Debug.WriteLine("Downloading " + filename);
                        //    try
                        //    {
                        //        client.DownloadFile(downloadUrl, savePath + filename);
                        //    }
                        //    catch (WebException ex)
                        //    {
                        //        if(ex.Message.Contains("The operation has timed out"))
                        //        {
                        //            Debug.WriteLine(filename + " timed out");
                        //            continue;
                        //        }
                        //    }

                        //    string extension = "";
                        //    string mimeType = Utility.GetMimeFromFile(savePath + filename);
                        //    switch (mimeType)
                        //    {
                        //        case "application/x-zip-compressed":
                        //            extension = "zip";
                        //            break;
                        //        case "image/pjpeg":
                        //            extension = "jpeg";
                        //            break;
                        //        case "text/richtext":
                        //            extension = "rtf";
                        //            break;
                        //        case "text/plain":
                        //            extension = "txt";
                        //            break;
                        //        case "application/pdf":
                        //            extension = "pdf";
                        //            break;
                        //        case "image/x-png":
                        //            extension = "png";
                        //            break;
                        //        default:
                        //            break;
                        //    }
                        //    if (Wait) System.Threading.Thread.Sleep(3000);
                        //}
                        //else
                        //    Debug.WriteLine("Skipping " + filename);
                    }

                    extrasLinks.Clear();
                }
                gamePageURLs.Clear();
                if (Wait) System.Threading.Thread.Sleep(500);
            }
        }

        /// <summary>
        /// Gets all links to games from page of game links
        /// </summary>
        /// <param name="descendants"></param>
        /// <returns></returns>
        private static List<string> BuildGameNodeList(IEnumerable<HtmlNode> descendants) {
            List<HtmlNode> rawGameNodes = new List<HtmlNode>();
            List<string> gamePageURLs = new List<string>();

            //find each game listed on page
            foreach (HtmlNode node in descendants)
            {
                //Reloaded uses the same class on the span and the anchor
                if (node.HasClass("list_title") && node.Name == "a")
                {
                    rawGameNodes.Add(node);
                }
            }

            //find link to game from node on page
            foreach (HtmlNode node in rawGameNodes)
            {
                gamePageURLs.Add(node.OuterHtml.Split('"')[1]);
            }

            return gamePageURLs;
        }
    }
}