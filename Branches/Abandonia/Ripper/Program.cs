using CommandLine;
using System.IO;

namespace Ripper {
    public class Options {
        //[Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        //public bool Verbose { get; set; }

        [Option('s', "startPage", Required = false, HelpText = "Page to start downloads at. (Int)")]
        public int StartPage { get; set; }
        [Option('r', "rip", Required = true, HelpText = "Site to rip downloads from. (String)")]
        public string SiteToRip { get; set; }
        [Option('p', "path", Required = true, HelpText = "Root path to save ripped files. (String)")]
        public string RootPath { get; set; }
    }
    class Program {
        static void Main(string[] args) {
            Parser.Default.ParseArguments<Options>(args).WithParsed(Run);
        }
        static void Run(Options opts) {
            if (opts.StartPage < 0)
                opts.StartPage = 0;

            if (!opts.RootPath.EndsWith(@"\"))
                opts.RootPath += @"\";

            if (!Directory.Exists(opts.RootPath))
                Directory.CreateDirectory(opts.RootPath);

            switch (opts.SiteToRip.ToLower()) {
                case "a":
                case "abandonia": Abandonia.Rip(opts.RootPath, opts.StartPage); break;
                case "reloaded": break;
                case "replacementdocs": break;
            }
        }
    }
}