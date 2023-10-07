using System.Globalization;
using System.Text;
using csv_diff;

namespace csv_diff_report;

public class Html : Report
{
    // Generare a diff report in HTML format.
    public string HtmlOutput(string output)
    {
        var content = new List<string>();
        content.Add("<html>");
        content.Add("<head>");
        content.Add("<title>Diff Report</title>");
        content.Add("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">");
        content.Add(HtmlStyles());
        content.Add("</head>");
        content.Add("<body>");

        var lcsAvailable = false;
        // var lcsAvailable = typeof(Diff.LCS) != null;

        HtmlSummary(content);
        foreach (var fileDiff in diffs)
        {
            if (fileDiff.Diffs.Count > 0)
            {
                HtmlDiff(content, fileDiff);
            }
        }

        content.Add("</body>");
        content.Add("</html>");

        // Save page
        var path = $"{Path.GetDirectoryName(output)}/{Path.GetFileNameWithoutExtension(output)}.html";
        File.WriteAllLines(path, content);
        return path;
    }

    // Returns the HTML head content, which contains the styles used for diffing.
    public string HtmlStyles()
    {
        var style = new StringBuilder();
        style.AppendLine("<style>");
        style.AppendLine("@font-face {font-family: Calibri, Helvetica, sans-serif;}");
        style.AppendLine("h1 {font-family: Calibri, Helvetica, sans-serif; font-size: 16pt;}");
        style.AppendLine("h2 {font-family: Calibri, Helvetica, sans-serif; font-size: 14pt; margin: 1em 0em .2em;}");
        style.AppendLine("h3 {font-family: Calibri, Helvetica, sans-serif; font-size: 12pt; margin: 1em 0em .2em;}");
        style.AppendLine("body {font-family: Calibri, Helvetica, sans-serif; font-size: 11pt;}");
        style.AppendLine("p {margin: .2em 0em;}");
        style.AppendLine("code {font-size: 8pt; white-space: pre;}");
        style.AppendLine("table {font-family: Calibri, Helvetica, sans-serif; font-size: 10pt; line-height: 13pt; border-collapse: collapse;}");
        style.AppendLine("th {background-color: #00205B; color: white; font-size: 11pt; font-weight: bold; text-align: left; border: 1px solid #DDDDFF; padding: 1px 5px;}");
        style.AppendLine("td {border: 1px solid #DDDDFF; padding: 1px 5px;}");
        style.AppendLine(".summary {font-size: 13pt;}");
        style.AppendLine(".add {background-color: white; color: #33A000;}");
        style.AppendLine(".delete {background-color: white; color: #FF0000; text-decoration: line-through;}");
        style.AppendLine(".update {background-color: white; color: #0000A0;}");
        style.AppendLine(".move {background-color: white; color: #0000A0;}");
        style.AppendLine(".matched {background-color: white; color: #A0A0A0;}");
        style.AppendLine(".bold {font-weight: bold;}");
        style.AppendLine(".center {text-align: center;}");
        style.AppendLine(".right {text-align: right;}");
        style.AppendLine(".separator {width: 200px; border-bottom: 1px gray solid;}");
        style.AppendLine("</style>");
        return style.ToString();
    }

    public void HtmlSummary(List<string> body)
    {
        body.Add("<h2>Summary</h2>");
        body.Add($"<p>Diff report generated at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}.</p>");
        body.Add("<h3>Source Locations</h3>");
        body.Add("<table>");
        body.Add("<tbody>");
        body.Add($"<tr><th>From:</th><td>{Left}</td></tr>");
        body.Add($"<tr><th>To:</th><td>{Right}</td></tr>");
        body.Add("</tbody>");
        body.Add("</table>");
        body.Add("<br>");
        body.Add("<h3>Files</h3>");
        body.Add("<table>");
        body.Add("<thead>");
        body.Add("<tr><th rowspan=2>File</th><th colspan=2 class='center'>Lines</th><th colspan=4 class='center'>Diffs</th></tr>");
        body.Add("<tr><th>From</th><th>To</th><th>Adds</th><th>Deletes</th><th>Updates</th><th>Moves</th></tr>");
        body.Add("</thead>");
        body.Add("<tbody>");
        foreach (var fileDiff in diffs)
        {
            var label = Path.GetFileName(fileDiff.Left.Path ?? fileDiff.Right.Path);
            body.Add("<tr>");
            if (fileDiff.Diffs.Count > 0)
            {
                body.Add($"<td><a href='#{label}'>{label}</a></td>");
            }
            else
            {
                body.Add($"<td>{label}</td>");
            }
            body.Add($"<td class='right'>{fileDiff.Left.LineCount}</td>");
            body.Add($"<td class='right'>{fileDiff.Right.LineCount}</td>");
            body.Add($"<td class='right'>{fileDiff.Summary["Add"]}</td>");
            body.Add($"<td class='right'>{fileDiff.Summary["Delete"]}</td>");
            body.Add($"<td class='right'>{fileDiff.Summary["Update"]}</td>");
            body.Add($"<td class='right'>{fileDiff.Summary["Move"]}</td>");
            body.Add("</tr>");
        }
        body.Add("</tbody>");
        body.Add("</table>");
    }

    public void HtmlDiff(List<string> body, CSVDiff fileDiff)
    {
        var label = Path.GetFileName(fileDiff.Left.Path ?? fileDiff.Right.Path);
        body.Add($"<h2 id='{label}'>{label}</h2>");
        body.Add("<p>");
        var count = 0;
        if (fileDiff.Summary["Add"] > 0)
        {
            body.Add($"<span class='add'>{fileDiff.Summary["Add"]} Adds</span>");
            count += 1;
        }
        if (fileDiff.Summary["Delete"] > 0)
        {
            if (count > 0) body.Add(", ");
            body.Add($"<span class='delete'>{fileDiff.Summary["Delete"]} Deletes</span>");
            count += 1;
        }
        if (fileDiff.Summary["Update"] > 0)
        {
            if (count > 0) body.Add(", ");
            body.Add($"<span class='update'>{fileDiff.Summary["Update"]} Updates</span>");
            count += 1;
        }
        if (fileDiff.Summary["Move"] > 0)
        {
            if (count > 0) body.Add(", ");
            body.Add($"<span class='move'>{fileDiff.Summary["Move"]} Moves</span>");
        }
        body.Add("</p>");

        var outFields = OutputFields(fileDiff);
        body.Add("<table>");
        body.Add("<thead><tr>");
        foreach (var fld in outFields)
        {
            body.Add($"<th>{(fld is string ? (string)fld : Titleize((string)fld))}</th>");
        }
        body.Add("</tr></thead>");
        body.Add("<tbody>");
        foreach (var diff in fileDiff.Diffs)
        {
            body.Add("<tr>");
            var chg = (string)diff.Value["action"];
            for (var i = 0; i < outFields.Count; i++)
            {
                var field = outFields[i];
                var old = "";
                var newDiff = "";
                var style = "";
                var d = diff.Value.Fields[field.ToString()];
                if (d is List<object> diffList)
                {
                    old = diffList[0]?.ToString();
                    newDiff = diffList[1]?.ToString();
                    if (old == null)
                    {
                        style = "add";
                    }
                    else
                    {
                        style = chg.ToLower();
                    }
                }
                else if (d != null)
                {
                    newDiff = d.ToString();
                    if (i == 1)
                    {
                        style = chg.ToLower();
                    }
                }
                else if (fileDiff.Options.TryGetValue("include_matched", out object include_matched) && (bool)include_matched)
                {
                    style = "matched";
                    newDiff = ((Source)fileDiff.Right)[diff.Key][(string)field]?.ToString();
                }
                body.Add("<td>");
                // if (style == "update" && lcsAvailable && old != null && newDiff != null &&
                //     (old.Split('\n').Length > 1 || newDiff.Split('\n').Length > 1))
                // {
                //     var lcsDiffs = Diff.LCS.Diff(old.Split('\n'), newDiff.Split('\n'));
                //     for (var j = 0; j < lcsDiffs.Count; j++)
                //     {
                //         if (j > 0) body.Add("<br>...<br>");
                //         for (var l = 0; l < lcsDiffs[j].Count; l++)
                //         {
                //             if (l > 0) body.Add("<br>");
                //             body.Add($"{lcsDiffs[j][l].Position + 1}&nbsp;&nbsp;<span class='{(lcsDiffs[j][l].Action == '+' ? "add" : "delete")}'>" +
                //                 $"<code>{System.Web.HttpUtility.HtmlEncode(lcsDiffs[j][l].Element.ToString().TrimEnd('\n'))}</code></span>");
                //         }
                //     }
                // }
                // else
                // {
                    if (old != null)
                    {
                        body.Add($"<span class='delete'><code>{System.Web.HttpUtility.HtmlEncode(old)}</code></span>");
                    }
                    if (old != null && old.Length > 10)
                    {
                        body.Add("<br>");
                    }
                    body.Add($"<span{(string.IsNullOrEmpty(style) ? "" : $" class='{style}'")}><code>{System.Web.HttpUtility.HtmlEncode(newDiff)}</code></span>");
                // }
                body.Add("</td>");
            }
            body.Add("</tr>");
        }
        body.Add("</tbody>");
        body.Add("</table>");
    }

    public string Titleize(string text)
    {
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower());
    }

    public List<object> OutputFields(CSVDiff fileDiff)
    {
        var outFields = new List<object>();
        foreach (var field in fileDiff.Left.FieldNames)
        {
            if (!fileDiff.Options.ContainsKey("ignore_fields"))
            {
                outFields.Add(field);
            }
        }
        foreach (var field in fileDiff.Right.FieldNames)
        {
            if (!fileDiff.Options.ContainsKey("ignore_fields") && !outFields.Contains(field))
            {
                outFields.Add(field);
            }
        }
        return outFields;
    }
}