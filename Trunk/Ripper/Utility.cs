using HtmlAgilityPack;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Text;

namespace Ripper {
    class Utility {
        //Don't be a dick, wait so you don't DOS
        public static readonly bool DoCourtesyWait = true;
        public static readonly int CourtestWaitTime = 5000;

        public static string GetSafeFilename(string filename) {
            return string.Join("-", filename.Split(Path.GetInvalidFileNameChars()));
        }

        public static HtmlNode GetNodeWithWait(string URL) {
            HtmlNode node = (new HtmlWeb()).Load(URL).DocumentNode;
            if (DoCourtesyWait) System.Threading.Thread.Sleep(CourtestWaitTime);
            return node;
        }

        public static void Log(string logMessage) {
            //TODO: Add text file logging?
            Debug.WriteLine(logMessage);
            Console.WriteLine(logMessage);
        }

        public static void GetFile(string title, string filenameSubtype, string URL, string savePathRoot) {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(URL);
            try {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                    //build filename
                    string extension = GetExtension(response);

                    if (extension != null) {
                        string filename = BuildFilename(title, extension, filenameSubtype);

                        //build savepath
                        string subfolder = Utility.GetSafeFilename(title) + "\\";
                        string savePath = savePathRoot + subfolder;

                        if (!File.Exists(Path.Combine(savePath, filename))) {
                            Log("Downloading " + filename);
                            if (!Directory.Exists(savePath))
                                Directory.CreateDirectory(savePath);

                            var responseStream = response.GetResponseStream();

                            using (var fileStream = File.Create(Path.Combine(savePath, filename))) {
                                responseStream.CopyTo(fileStream);
                            }
                            if (DoCourtesyWait) System.Threading.Thread.Sleep(CourtestWaitTime);
                        } else {
                            Log("Skipping " + filename);
                        }
                    }
                }
            } catch (WebException ex) {
                Log("Encountered error fetching file: " + ex.Message);
            }
        }

        public static string BuildFilename(string title, string extension, params string[] filenameSubtypes) {
            StringBuilder sb = new StringBuilder();
            sb.Append(title);
            foreach (string s in filenameSubtypes) {
                if (!string.IsNullOrWhiteSpace(s))
                    sb.Append("_" + s);
            }
            sb.Append(extension);
            return Utility.GetSafeFilename(sb.ToString());
        }

        public static string GetExtension(HttpWebResponse response) {
            string extension = "";
            string contentDisposition = response.Headers["Content-Disposition"];
            string responseUriLocalPath = response.ResponseUri.LocalPath;

            if (!string.IsNullOrWhiteSpace(contentDisposition)) {
                extension = Path.GetExtension(new ContentDisposition(contentDisposition).FileName);
            } else if (!string.IsNullOrWhiteSpace(responseUriLocalPath)) {
                extension = Path.GetExtension(responseUriLocalPath);
            } else {
                Log("Failed to get extension");
            }

            return extension;
        }
    }
}