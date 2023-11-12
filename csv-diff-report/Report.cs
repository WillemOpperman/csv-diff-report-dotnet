using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using csv_diff;
using CsvHelper;
using CsvHelper.Configuration;

namespace csv_diff_report
{
    public class Report
    {
        internal List<CSVDiff> diffs;
        private bool color;

        // Accessors to describe the left and right sides of the diff
        public string Left { get; set; }
        public string Right { get; set; }

        // Controls whether output should be in color or not
        public bool Color { get => color; set => color = value; }

        // Accessor for each file diff in this report
        public IReadOnlyList<CSVDiff> Diffs => diffs;

        // Constructor
        public Report(string left = null, string right = null)
        {
            Left = left;
            Right = right;
            diffs = new List<CSVDiff>();
            color = true;
        }

        public void Echo(params object[] args)
        {
            if (!color)
            {
                List<string> chunks = new List<string>();
                foreach (var chunk in args)
                {
                    if (chunk is string text)
                        chunks.Add(text);
                    else if (chunk is KeyValuePair<string, ConsoleColor> colorChunk)
                        chunks.Add(colorChunk.Key);
                }
                Console.WriteLine(string.Join("", chunks));
            }
            else
            {
                foreach (var arg in args)
                {
                    if (arg is string text)
                        Console.Write(text);
                    else if (arg is KeyValuePair<string, ConsoleColor> colorChunk)
                    {
                        Console.ForegroundColor = colorChunk.Value;
                        Console.Write(colorChunk.Key);
                        Console.ResetColor();
                    }
                }
                Console.WriteLine();
            }
        }

        public void Add(CSVDiff diff)
        {
            if (ReferenceEquals(diff, null))
                throw new ArgumentNullException(nameof(diff));

            diffs.Add(diff);
            if (ReferenceEquals(Left, null) || ReferenceEquals(diff.Left.Path, null))
            {
                Left = diff.Left.Path;
                Right = diff.Right.Path;
            }
            foreach (var warn in diff.DiffWarnings)
                Echo(new KeyValuePair<string, ConsoleColor>(warn, ConsoleColor.Yellow));
            Echo($"Found {diff.Diffs.Count} differences");
            int i = 0;
            foreach (var pair in diff.Summary)
            {
                Echo(i == 0 ? ": " : ", ");
                var k = pair.Key;
                var v = pair.Value;
                ConsoleColor color;
                switch (k)
                {
                    case "Add":
                        color = ConsoleColor.Green;
                        break;
                    case "Delete":
                        color = ConsoleColor.Red;
                        break;
                    case "Update":
                        color = ConsoleColor.Cyan;
                        break;
                    case "Move":
                        color = ConsoleColor.Magenta;
                        break;
                    case "Warning":
                        color = ConsoleColor.Yellow;
                        break;
                    default:
                        color = ConsoleColor.White;
                        break;
                }
                Echo(new KeyValuePair<string, ConsoleColor>($"{v} {k}s", color));
                i++;
            }
        }

        public void Diff(string left, string right, Dictionary<string, object> options = null)
        {
            Left = left;
            Right = right;

            if (File.Exists(Left) && File.Exists(Right))
            {
                Echo("Performing file diff:");
                Echo($"  From File:    {Left}");
                Echo($"  To File:      {Right}");
                var optFile = LoadOptFile(options?.ContainsKey("options_file") == true ? options["options_file"].ToString() : Path.GetDirectoryName(Left));
                DiffFile(Left, Right, options, optFile);
            }
            else if (Directory.Exists(Left) && Directory.Exists(Right))
            {
                Echo("Performing directory diff:");
                Echo($"  From directory:  {Left}");
                Echo($"  To directory:    {Right}");
                var optFile = LoadOptFile(options?.ContainsKey("options_file") == true ? options["options_file"].ToString() : Left);
                if (options?.ContainsKey("file_types") == true)
                {
                    throw new Exception("TODO");
                    // var fileTypes = FindMatchingFileTypes(options["file_types"] as string[], optFile);
                    // foreach (var fileType in fileTypes)
                    // {
                    //     var hsh = optFile["file_types"][fileType] as Dictionary<string, object>;
                    //     var ftOpts = new Dictionary<string, object>(options);
                    //     foreach (var kvp in hsh)
                    //     {
                    //         ftOpts[kvp.Key] = kvp.Value;
                    //     }
                    //     DiffDir(Left, Right, ftOpts, optFile);
                    // }
                }
                else
                {
                    DiffDir(Left, Right, options, optFile);
                }
            }
            else
            {
                if (!File.Exists(Left))
                    Echo($"From path '{Left}' not found", ConsoleColor.Red);
                if (!File.Exists(Right))
                    Echo($"To path '{Right}' not found", ConsoleColor.Red);
                throw new ArgumentException("Left and right must both exist and be of the same type (files or directories)");
            }
        }
        
        // Helper method to find matching file types based on patterns
        private List<string> FindMatchingFileTypes(string[] fileTypes, Dictionary<string, object> optFile)
        {
            var matchedFileTypes = new List<string>();

            if (optFile != null && optFile.ContainsKey("file_types") && optFile["file_types"] is Dictionary<string, object> fileTypesDict)
            {
                foreach (var fileType in fileTypes)
                {
                    var re = new System.Text.RegularExpressions.Regex(
                        fileType.Replace(".", "\\.").Replace("?", ".").Replace("*", ".*"), 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    var matchedKeys = fileTypesDict.Keys
                        .Where(key => re.IsMatch(key))
                        .ToList();

                    if (matchedKeys.Count > 0)
                    {
                        matchedFileTypes.AddRange(matchedKeys);
                    }
                    else
                    {
                        Echo($"No file type matching '{fileType}' defined in .csvdiff", ConsoleColor.Yellow);
                        Echo($"Known file types are: {string.Join(", ", fileTypesDict.Keys)}", ConsoleColor.Yellow);
                    }
                }
            }
            else
            {
                if (optFile != null)
                {
                    Echo("No file types are defined in .csvdiff", ConsoleColor.Yellow);
                }
                else
                {
                    Echo("The file_types option can only be used when a .csvdiff file is present in the FROM or current directory", ConsoleColor.Yellow);
                }
            }

            return matchedFileTypes.Distinct().ToList();
        }

        public void Output(string path, string format = "html")
        {
            path = GetOutputPath(path, format);
            switch (format.ToLower())
            {
                case "xlsx":
                case "xls":
                    //XlOutput(path);
                    break;
                case "html":
                    //HtmlOutput(path);
                    break;
                case "txt":
                case "csv":
                    //TextOutput(path);
                    break;
                default:
                    throw new ArgumentException($"Unrecognized output format: {format}");
            }
            Echo($"Diff report saved to '{path}'");
        }

        private string GetOutputPath(string path, string format)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            if (format.Equals("xlsx", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(path).Equals(".xlsx", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(path).Equals(".xls", StringComparison.OrdinalIgnoreCase))
                return path;
            if (format.Equals("html", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(path).Equals(".html", StringComparison.OrdinalIgnoreCase))
                return path;
            if (format.Equals("txt", StringComparison.OrdinalIgnoreCase) || format.Equals("csv", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(path).Equals(".txt", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(path).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                return path;
            throw new ArgumentException($"Unrecognized output format: {format}");
        }

        private Dictionary<string, object> LoadOptFile(string dir)
        {
            var optPath = new DirectoryInfo(dir);
            if (optPath.Name != "")
                optPath = new DirectoryInfo(Path.Combine(optPath.FullName, ".csvdiff"));
            if (!optPath.Exists)
                optPath = new DirectoryInfo(".csvdiff");
            if (optPath.Exists)
            {
                Echo($"Loading options from '{optPath.FullName}'");
                var optFile = new YamlDotNet.Serialization.Deserializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(optPath.FullName));
                SymbolizeKeys(optFile);
                return optFile;
            }
            return null;
        }

        private void SymbolizeKeys(Dictionary<string, object> hsh)
        {
            foreach (var kvp in hsh)
            {
                hsh[kvp.Key.ToLower()] = kvp.Value;
                if (kvp.Value is Dictionary<string, object> nestedHsh)
                    SymbolizeKeys(nestedHsh);
            }
        }

        private void DiffDir(string left, string right, Dictionary<string, object> options, Dictionary<string, object> optFile)
        {
    	    var pattern = options?.TryGetValue("pattern", out var option) is true ? option.ToString() : "*";
            var exclude = options?.TryGetValue("exclude_pattern", out var option1) is true ? option1.ToString() : null;

            Echo($"  Include Pattern: {pattern}");
            if (!string.IsNullOrEmpty(exclude))
                Echo($"  Exclude Pattern: {exclude}");

            var leftFiles = Directory.GetFiles(left, pattern);
            var excludes = exclude != null ? Directory.GetFiles(left, exclude) : new string[0];
            foreach (var file in leftFiles)
            {
                var fileName = Path.GetFileName(file);
                if (!excludes.Contains(file))
                {
                    var rightFile = Path.Combine(right, fileName);
                    if (File.Exists(rightFile))
                    {
                        DiffFile(file, rightFile, options, optFile);
                    }
                    else
                    {
                        Echo($"Skipping file '{fileName}', as there is no corresponding TO file", ConsoleColor.Yellow);
                    }
                }
            }
        }

        private void DiffFile(string left, string right, Dictionary<string, object> options, Dictionary<string, object> optFile)
        {
            var settings = FindFileTypeSettings(left, optFile);
            if (settings.ContainsKey("ignore") && (bool)settings["ignore"])
            {
                Echo($"Ignoring file {left}");
                return;
            }

            var mergedOptions = new Dictionary<string, object>(settings);
            if (options != null)
            {
                foreach (var kvp in options)
                {
                    if (!mergedOptions.ContainsKey(kvp.Key))
                    {
                        mergedOptions[kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        // Merge the file-specific option with the default option from .csvdiff file
                        if (kvp.Value is Dictionary<string, object> nestedOptions && mergedOptions[kvp.Key] is Dictionary<string, object> nestedMergedOptions)
                        {
                            var mergedNestedOptions = new Dictionary<string, object>(nestedMergedOptions);
                            foreach (var nestedKvp in nestedOptions)
                            {
                                if (!mergedNestedOptions.ContainsKey(nestedKvp.Key))
                                {
                                    mergedNestedOptions[nestedKvp.Key] = nestedKvp.Value;
                                }
                            }
                            mergedOptions[kvp.Key] = mergedNestedOptions;
                        }
                    }
                }
            }

            var from = OpenSource(left, "FROM", mergedOptions);
            var to = OpenSource(right, "TO", mergedOptions);
            var diff = new CSVDiff(from, to, mergedOptions);
            Add(diff);
        }

        private Dictionary<string, object> FindFileTypeSettings(string left, Dictionary<string, object> optFile)
        {
            var leftPath = left.Replace("\\", "/");
            var settings = optFile?.ContainsKey("defaults") == true ? (Dictionary<string, object>)optFile["defaults"] : new Dictionary<string, object>();
            if (optFile?.ContainsKey("file_types") == true)
            {
                var fileTypes = optFile["file_types"] as Dictionary<string, object>;
                foreach (var fileType in fileTypes)
                {
                    var hsh = fileType.Value as Dictionary<string, object>;
                    if (hsh != null && hsh.ContainsKey("pattern") && !hsh["pattern"].ToString().Equals("-"))
                    {
                        if (!hsh.ContainsKey("matched_files"))
                        {
                            var matchedFiles = Directory.GetFiles(Path.GetDirectoryName(left), hsh["pattern"].ToString()).ToList();
                            if (hsh.ContainsKey("exclude_pattern"))
                            {
                                matchedFiles.RemoveAll(file => Directory.GetFiles(Path.GetDirectoryName(left), hsh["exclude_pattern"].ToString()).Contains(file));
                            }
                            hsh["matched_files"] = matchedFiles;
                        }
                        var matchedFilesList = hsh["matched_files"] as List<string>;
                        if (matchedFilesList.Contains(leftPath))
                        {
                            Echo($"Matched file {left} to file type {fileType.Key}");
                            settings = MergeDictionary(settings, hsh);
                            settings.Remove("pattern");
                            settings.Remove("exclude_pattern");
                            settings.Remove("matched_files");
                            break;
                        }
                    }
                }
            }
            return settings;
        }

        private Dictionary<string, object> MergeDictionary(Dictionary<string, object> dict1, Dictionary<string, object> dict2)
        {
            var mergedDict = new Dictionary<string, object>(dict1);
            foreach (var kvp in dict2)
            {
                if (!mergedDict.ContainsKey(kvp.Key))
                {
                    mergedDict[kvp.Key] = kvp.Value;
                }
                else
                {
                    if (kvp.Value is Dictionary<string, object> nestedDict2 && mergedDict[kvp.Key] is Dictionary<string, object> nestedDict1)
                    {
                        mergedDict[kvp.Key] = MergeDictionary(nestedDict1, nestedDict2);
                    }
                }
            }
            return mergedDict;
        }

        private CSVSource OpenSource(string src, string leftRight, Dictionary<string, object> options)
        {
            var outChunks = new List<KeyValuePair<string, ConsoleColor>>
            {
        	    new KeyValuePair<string, ConsoleColor>($"Opening {leftRight} file '{Path.GetFileName(src)}'...", ConsoleColor.White)
            };

            CSVSource csvSrc = null;
            try
            {
                csvSrc = new CSVSource(src, options);
                outChunks.Add(new KeyValuePair<string, ConsoleColor>($"  {csvSrc.Lines.Count} lines read", ConsoleColor.White));
                if (csvSrc.SkipCount > 0)
                {
                    outChunks.Add(new KeyValuePair<string, ConsoleColor>($" ({csvSrc.SkipCount} lines skipped)", ConsoleColor.Yellow));
                }
                Echo(outChunks.ToArray());
                foreach (var warn in csvSrc.Warnings)
                {
                    Echo(new KeyValuePair<string, ConsoleColor>(warn, ConsoleColor.Yellow));
                }
            }
            catch (Exception ex)
            {
                Echo(outChunks.ToArray());
                Echo(new KeyValuePair<string, ConsoleColor>($"An error occurred opening file {src}: {ex}", ConsoleColor.Red));
                throw;
            }

            return csvSrc;
        }

        internal string[] OutputFields(CSVDiff diff)
        {
            var outputFields = new List<string>();
            if (diff.Options.ContainsKey("output_fields") && diff.Options["output_fields"] is string[] fields)
            {
                foreach (var fld in fields)
                {
                    if (int.TryParse(fld, out var fieldIndex))
                    {
                        if (fieldIndex >= 0 && fieldIndex < diff.DiffFields.Count)
                        {
                            outputFields.Add(diff.DiffFields[fieldIndex]);
                        }
                    }
                    else
                    {
                        outputFields.Add(fld);
                    }
                }
            }
            else
            {
                outputFields.Add("Row");
                outputFields.Add("Action");
                if (!diff.Options.ContainsKey("ignore_moves") || !(bool)diff.Options["ignore_moves"])
                {
                    outputFields.Add("SiblingPosition");
                }
                outputFields.AddRange(diff.DiffFields);
            }
            return outputFields.ToArray();
        }

        public void Diff(string left, string right, Action<object[]> echoHandler = null)
        {
            Left = left;
            Right = right;

            var optFile = LoadOptFile(Left);

            echoHandler = Echo;

            if (File.Exists(Left) && File.Exists(Right))
            {
                echoHandler(new object[] { "Performing file diff:", null });
                echoHandler(new object[] { "  From File:    ", Left });
                echoHandler(new object[] { "  To File:      ", Right });
                var options = new Dictionary<string, object> { { "echo_handler", echoHandler } };
                DiffFile(Left, Right, options, optFile);
            }
            else if (Directory.Exists(Left) && Directory.Exists(Right))
            {
                echoHandler(new object[] { "Performing directory diff:", null });
                echoHandler(new object[] { "  From directory:  ", Left });
                echoHandler(new object[] { "  To directory:    ", Right });
                var options = new Dictionary<string, object> { { "echo_handler", echoHandler } };
                DiffDir(Left, Right, options, optFile);
            }
            else
            {
                if (!File.Exists(Left))
                    echoHandler(new object[] { $"From path '{Left}' not found", ConsoleColor.Red });
                if (!File.Exists(Right))
                    echoHandler(new object[] { $"To path '{Right}' not found", ConsoleColor.Red });
                throw new ArgumentException("Left and right must both exist and be of the same type (files or directories)");
            }
        }

        // Helper method to convert a string to title case.
        internal string Titleize(string input)
        {
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input);
        }
    }
}
