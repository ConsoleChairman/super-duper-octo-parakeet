using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Ripper {
    public class Abandonia {
        private const string RootDownloadURL = "http://www.abandonia.com";
        private static string SiteSavePathRoot;
        public static readonly string Site = "http://www.abandonia.com/en/game/all?page=";

        public static void Rip(string BaseSavePathRoot, int StartPage = 0) {
            List<string> gamePageURLs = new List<string>();

            SiteSavePathRoot = BaseSavePathRoot + @"Abandonia\";

            if (!Directory.Exists(SiteSavePathRoot))
                Directory.CreateDirectory(SiteSavePathRoot);

            //find last page
            int totalPages = -1;
            HtmlNode firstPage = Utility.GetNodeWithWait(Site);
            string lastPageLink = firstPage.SelectSingleNode("//a[@class=\"pager-last active\"]").Attributes["href"].Value;
            int.TryParse(lastPageLink.Split('=')[1], out totalPages);

            //iterate through each page of games
            int startPage = (StartPage > 0) ? StartPage : 0;
            for (int page = startPage; page <= totalPages; page++) {
                Utility.Log("Scraping Listing Page " + (page + 1));
                HtmlNode requestedPage = Utility.GetNodeWithWait(Site + page);

                foreach (HtmlNode node in requestedPage.SelectNodes("//div[@class=\"gamelistimage\"]//a")) {
                    gamePageURLs.Add(node.Attributes["href"].Value);
                }
            }

            foreach (string gamePageURL in gamePageURLs) {
                HtmlNode gamePage = Utility.GetNodeWithWait(RootDownloadURL + gamePageURL);
                var extrasNodes = gamePage.SelectNodes("//div[@class=\"game_extraslink\"]//a");
                string title = gamePage.OwnerDocument.DocumentNode.SelectSingleNode("//title").InnerText.Replace("Download ", "").Replace(" | Abandonia", "").Trim();

                //for each extra, convert to link and metadata and download
                if (extrasNodes != null) {
                    foreach (HtmlNode extrasNode in extrasNodes) {
                        string extrasDownloadURL = extrasNode.Attributes["href"].Value;
                        if (!extrasDownloadURL.Contains("http://") && !extrasDownloadURL.Contains("https://")) {
                            string extraType = WebUtility.HtmlDecode(extrasNode.InnerText).Replace('|', '-').Trim();

                            switch (extraType) {
                                case "boxshots available": DownloadBoxArt(extrasNode, title); break;
                                default: Utility.GetFile(title, extraType, RootDownloadURL + extrasNode.Attributes["href"].Value, SiteSavePathRoot); break;
                            }
                        } else {
                            Utility.Log("Extra not hosted on Abandonia: " + extrasDownloadURL);
                        }
                    }
                }

                var mainDownloadNode = gamePage.SelectSingleNode("//div[@class=\"game_downloadpicture\"]//a");

                if (mainDownloadNode != null) {
                    DownloadGame(mainDownloadNode, title);
                }
            }
            gamePageURLs.Clear();
        }

        private static void DownloadGame(HtmlNode mainDownloadNode, string title) {
            string gameDownloadURL = mainDownloadNode.Attributes["href"].Value;
            if (!gameDownloadURL.Contains("http://") && !gameDownloadURL.Contains("https://")) {
                string imgAlt = mainDownloadNode.FirstChild.Attributes["Alt"].Value;
                if (GameIsDownloadable(imgAlt, title)) {
                    HtmlNode gamePage = Utility.GetNodeWithWait(RootDownloadURL + gameDownloadURL);
                    HtmlNode scriptNode = gamePage.SelectSingleNode("//center/preceding-sibling::script");
                    string downloadUrl = scriptNode.InnerText.Split(new string[] { "function go_to_downloadGame()" }, StringSplitOptions.None)[1].Split('"')[1];

                    Utility.GetFile(title, null, downloadUrl, SiteSavePathRoot);
                }
            } else {
                Utility.Log("Game not hosted on Abandonia: " + gameDownloadURL);
            }
        }

        private static void DownloadBoxArt(HtmlNode extrasNode, string title) {
            Utility.Log("Skipping box art: Not Implemented");
        }

        private static bool GameIsDownloadable(string imgAlt, string title) {
            switch (imgAlt) {
                case "Protected":
                    Utility.Log("Download is ESA Protected: " + title);
                    return false;
                case "Buy it":
                    Utility.Log("Download is for sale: " + title);
                    return false;
                default: return true;
            }
        }
    }
}