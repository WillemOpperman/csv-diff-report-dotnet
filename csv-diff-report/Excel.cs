using System.Text.RegularExpressions;
using csv_diff;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace csv_diff_report;

public class Excel : Report
{
    private Dictionary<string, object> _xlStyles;

    public Excel(object left, object right, Dictionary<string, object> options = null)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    private void XLOutput(string output)
    {
        FileInfo newFile = new FileInfo(output);
        using (ExcelPackage xl = new ExcelPackage(newFile))
        {
            _xlStyles = new Dictionary<string, object>();

            // Add a summary sheet and diff sheets for each diff
            XLSummarySheet(xl);

            // Save workbook
            xl.Save();
        }
    }

    private void XLSummarySheet(ExcelPackage xl)
    {
        string compareFrom = Left.ToString();
        string compareTo = Right.ToString();

        ExcelWorksheet sheet = xl.Workbook.Worksheets.Add("Summary");

        sheet.Cells[1, 1].Value = "From:";
        sheet.Cells[1, 1].Style.Font.Bold = true;
        sheet.Cells[1, 2].Value = compareFrom;

        sheet.Cells[2, 1].Value = "To:";
        sheet.Cells[2, 1].Style.Font.Bold = true;
        sheet.Cells[2, 2].Value = compareTo;

        sheet.Cells[3, 1].Value = string.Empty;

        sheet.Cells[4, 1].Value = "Sheet";
        sheet.Cells[4, 2].Value = "Adds";
        sheet.Cells[4, 3].Value = "Deletes";
        sheet.Cells[4, 4].Value = "Updates";
        sheet.Cells[4, 5].Value = "Moves";

        // foreach (var ci in sheet.Column(1, 5))
        foreach (var ci in sheet.Columns[0, 4])
        {
            ci.Style.Font.Bold = true;
            ci.Style.WrapText = true;
            ci.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            ci.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            ci.Style.Font.Size = 9;
        }

        int row = 5;
        foreach (var fileDiff in Diffs)
        {
            sheet.Cells[row, 1].Value = fileDiff.Options["sheet_name"] ?? Path.GetFileNameWithoutExtension(fileDiff.Left.Path);

            sheet.Cells[row, 2].Value = fileDiff.Summary["Add"];
            sheet.Cells[row, 3].Value = fileDiff.Summary["Delete"];
            sheet.Cells[row, 4].Value = fileDiff.Summary["Update"];
            sheet.Cells[row, 5].Value = fileDiff.Summary["Move"];

            if (fileDiff.Diffs.Count > 0)
            {
                XLDiffSheet(xl, fileDiff);
            }

            row++;
        }
    }

    private void XLDiffSheet(ExcelPackage xl, CSVDiff fileDiff)
    {
        string sheetName = fileDiff.Options["sheet_name"]?.ToString() ?? Path.GetFileNameWithoutExtension(fileDiff.Left.Path);
        List<object> outFields = OutputFields(fileDiff).Cast<object>().ToList();
        int freezeCols = (int?)fileDiff.Options["freeze_cols"] ?? (outFields.FindAll(f => f is string).Count + fileDiff.Left.KeyFields.Count);

        ExcelWorksheet sheet = xl.Workbook.Worksheets.Add(sheetName);

        int columnIndex = 1;
        foreach (var field in outFields)
        {
            ExcelRange cell = sheet.Cells[1, columnIndex];
            cell.Value = field is string ? Titleize((string)field) : field.ToString();
            cell.Style.Font.Bold = true;
            columnIndex++;
        }

        int rowIndex = 2;
        foreach (var diff in fileDiff.Diffs)
        {
            ExcelRange row = sheet.Cells[rowIndex, 1, rowIndex, outFields.Count];
            string chg = (string)diff.Value["action"];
            foreach (var cell in row)
            {
                object cellValue = null;
                string comment = null;
                string old = null;
                ExcelStyle style = null;
                throw new Exception("TODO");
                object d = null;
                // object d = diff.Value[cell.Start.Column - 1];
                int index = cell.Start.Column - 1;

                if (d is string[])
                {
                    string[] diffArray = (string[])d;
                    old = diffArray[0];
                    string newDiffValue = diffArray[1];
                    if (old == null)
                    {
                        style = _xlStyles["Add"] as ExcelStyle;
                    }
                    else
                    {
                        style = _xlStyles[chg] as ExcelStyle;
                        comment = old;
                    }
                    cellValue = newDiffValue;
                }
                else
                {
                    cellValue = d;
                    if ((bool)fileDiff.Options["include_matched"])
                    {
                        style = _xlStyles["Matched"] as ExcelStyle;
                        throw new Exception("TODO");
                        // d = fileDiff.Right[diff.Key][index];
                    }
                    else
                    {
                        switch (chg)
                        {
                            case "Add":
                            case "Delete":
                                style = _xlStyles[chg] as ExcelStyle;
                                break;
                            default:
                                throw new Exception("TODO");
                                // style = new ExcelStyle();
                                break;
                        }
                    }
                }

                switch (cellValue)
                {
                    case string strValue:
                        if (Regex.IsMatch(strValue, "^0+\\d+(\\.\\d+)?"))
                        {
                            // Don't let Excel auto-convert this to a number, as that
                            // will remove the leading zero(s)
                            cell.Value = strValue;
                        }
                        else
                        {
                            cell.Value = strValue;
                        }
                        break;
                    default:
                        cell.Value = cellValue;
                        break;
                }

                if (style != null)
                {
                    throw new Exception("TODO");
                    // cell.Style.Font.Color.SetColor(style.Font.Color.Rgb);
                    cell.Style.Fill.PatternType = style.Fill.PatternType;
                    // cell.Style.Fill.BackgroundColor.SetColor(style.Fill.BackgroundColor.Rgb);
                    cell.Style.Font.Strike = style.Font.Strike;
                }

                if (comment != null)
                {
                    ExcelComment xlComment = sheet.Comments.Add(cell.Current, comment, "Current");
                    xlComment.Visible = false;
                }

                columnIndex++;
            }
            rowIndex++;
        }

        foreach (var ci in sheet.Columns[1, outFields.Count])
        {
            ci.AutoFit(80);
        }

        XLFilterAndFreeze(sheet, freezeCols);
    }

    private void XLFilterAndFreeze(ExcelWorksheet sheet, int freezeCols = 0)
    {
        sheet.Cells[sheet.Dimension.Address].AutoFilter = true;
        sheet.View.FreezePanes(2, freezeCols + 1);
    }

    private void XLSave(ExcelPackage xl, string path)
    {
        try
        {
            xl.Save();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw new Exception($"Unable to replace existing Excel file {path} - is it already open in Excel?");
        }
    }
}