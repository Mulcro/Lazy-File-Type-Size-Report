// Program.cs
//
// CECS 342 Assignment 2
// File Type Report
// Solution Template

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace FileTypeReport
{
    internal static class Program
    {
        // 1. Enumerate all files in a folder recursively
        private static IEnumerable<string> EnumerateFilesRecursively(string path)
        {
            if (!Directory.Exists(path))
                yield break;

            string[] files;
            try
            {
                files = Directory.GetFiles(path);
            }
            catch (UnauthorizedAccessException)
            {
                yield break;
            }
            catch (IOException)
            {
                yield break;
            }

            foreach (var file in files)
                yield return file;

            string[] subDirs;
            try
            {
                subDirs = Directory.GetDirectories(path);
            }
            catch (UnauthorizedAccessException)
            {
                yield break;
            }
            catch (IOException)
            {
                yield break;
            }

            foreach (var dir in subDirs)
            {
                foreach (var nestedFile in EnumerateFilesRecursively(dir))
                    yield return nestedFile;
            }
        }

        // Human readable byte size
        private static string FormatByteSize(long byteSize)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };

            double size = byteSize;

            int i = 0;

            while (size >= 1024 && i < suffixes.Length - 1)
            {
                size /= 1024;
                i++;
            }

            return $"{size:F2} {suffixes[i]}";

        }

        // Create an HTML report file
        private static XDocument CreateReport(IEnumerable<string> files)
        {
            // 2. Process data
            var query =
              from file in files
              let ext = Path.GetExtension(file)
              let type = string.IsNullOrEmpty(ext) ? "No Extension" : ext.ToLowerInvariant()
              group file by type into g

              select new
              {
                  Type = g.Key,
                  Count = g.Count(),
                  TotalSize = g.Sum(file => new FileInfo(file).Length)
              };

            // 3. Functionally construct XML
            var alignment = new XAttribute("align", "right");
            var style = "table, th, td { border: 1px solid black; }";
            var tableRows =
              from entry in query
              select new XElement("tr",
                new XElement("td", entry.Type),
                new XElement("td", entry.Count.ToString(), new XAttribute("align", "right")),
                new XElement("td", FormatByteSize(entry.TotalSize), new XAttribute("align", "right"))
              );

            var table = new XElement("table",
              new XElement("thead",
                new XElement("tr",
                  new XElement("th", "Type"),
                  new XElement("th", "Count"),
                  new XElement("th", "Total Size"))),
              new XElement("tbody", tableRows));

            return new XDocument(
              new XDocumentType("html", null, null, null),
                new XElement("html",
                  new XElement("head",
                    new XElement("title", "File Report"),
                    new XElement("style", style)),
                  new XElement("body", table)));
        }

        // Console application with two arguments
        public static void Main(string[] args)
        {
            try
            {
                string inputFolder = args[0];
                string reportFile = args[1];
                CreateReport(EnumerateFilesRecursively(inputFolder)).Save(reportFile);
            }
            catch
            {
                Console.WriteLine("Usage: FileTypeReport <folder> <report file>");
            }
        }
    }
}