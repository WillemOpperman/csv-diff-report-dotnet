using System.Globalization;
using csv_diff;
using CsvHelper;
using CsvHelper.Configuration;

namespace csv_diff_report;

public class Text : Report
{
    // Generate a diff report in TEXT format.
    public string TextOutput(string output)
    {
        string path = $"{Path.GetDirectoryName(output)}/{Path.GetFileNameWithoutExtension(output)}.csv";
        using (var writer = new StreamWriter(path))
        using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
        {
            foreach (var fileDiff in diffs)
            {
                TextDiff(csv, fileDiff);
            }
            
            csv.Flush();
        }
        return path;
    }

    private void TextDiff(CsvWriter csv, CSVDiff fileDiff)
    {
        var outFields = OutputFields(fileDiff);
        var columnHeaders = outFields.Select(fld => fld is string ? (string)fld : Titleize((string)fld));

        foreach (var header in columnHeaders)
        {
            csv.WriteField(header);
        }
        
        csv.NextRecord();
        
        foreach (var diff in fileDiff.Diffs)
        {
            foreach (var field in outFields)
            {
                var d = diff.Value[field];
                if (d is object[] dList)
                {
                    d = dList.Last();
                }
                if (d == null && fileDiff.Options.TryGetValue("include_matched", out object include_matched) && (bool)include_matched)
                {
                    throw new Exception("TODO");
                    // d = fileDiff.Right[diff.Key] && fileDiff.Right[diff.Key][field];
                }
                csv.WriteField(d?.ToString() ?? "");
            }
            // var rows = outFields.Select(field =>
            // {
            //     var d = diff.Value.Fields[field];
            //     if (d is List<string> dList)
            //     {
            //         d = dList.Last();
            //     }
            //     if (d == null && fileDiff.Options.TryGetValue("include_matched", out object include_matched) && (bool)include_matched)
            //     {
            //         throw new Exception("TODO");
            //         // d = fileDiff.Right[diff.Key] && fileDiff.Right[diff.Key][field];
            //     }
            //     return d?.ToString() ?? "";;
            // });
            //
            // csv.WriteRecords(rows);
            csv.NextRecord();
        }
    }
}