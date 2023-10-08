using System.Text.RegularExpressions;
using ClosedXML.Excel;
using csv_diff;

namespace csv_diff_report;

public class Excel : Report
{
    private Dictionary<string, object> _xlStyles;

    public Excel(string left = null, string right = null):  base(left, right)
    {
    }

    public void XLOutput(string output)
    {
		// Create a new workbook
        var workbook = new XLWorkbook();

        // Add a summary sheet and diff sheets for each diff
        XLSummarySheet(workbook);

        // Save the workbook
        var path = $"{System.IO.Path.GetDirectoryName(output)}/{System.IO.Path.GetFileNameWithoutExtension(output)}.xlsx";
        XLSave(workbook, path);
    }

    private void XLSummarySheet(XLWorkbook workbook)
    {
        string compareFrom = Left;
        string compareTo = Right;

        var summarySheet = workbook.Worksheets.Add("Summary");

        // Add headers
        summarySheet.Cell("A1").Value = "From:";
        summarySheet.Cell("A1").Style.Font.SetBold();
        summarySheet.Cell("B1").Value = compareFrom;

        summarySheet.Cell("A2").Value = "To:";
        summarySheet.Cell("A2").Style.Font.SetBold();
        summarySheet.Cell("B2").Value = compareTo;

        summarySheet.Cell("A3").Value = string.Empty; // Spacer row

        summarySheet.Cell("A4").Value = "Sheet";
        summarySheet.Cell("B4").Value = "Adds";
        summarySheet.Cell("C4").Value = "Deletes";
        summarySheet.Cell("D4").Value = "Updates";
        summarySheet.Cell("E4").Value = "Moves";

        // Set column widths
        summarySheet.Column("A").Width = 20;
        summarySheet.Columns("B:E").Width = 10;

        var row = 5;

        foreach (var fileDiff in Diffs)
        {
            var sheetName = fileDiff.Options.TryGetValue("sheet_name", out var value)
                ? value.ToString()
                : System.IO.Path.GetFileNameWithoutExtension(fileDiff.Left.Path);

            var adds = fileDiff.Summary["Add"];
            var deletes = fileDiff.Summary["Delete"];
            var updates = fileDiff.Summary["Update"];
            var moves = fileDiff.Summary["Move"];

            summarySheet.Cell($"A{row}").Value = sheetName;
            summarySheet.Cell($"B{row}").Value = adds;
            summarySheet.Cell($"C{row}").Value = deletes;
            summarySheet.Cell($"D{row}").Value = updates;
            summarySheet.Cell($"E{row}").Value = moves;

            if (fileDiff.Diffs.Count > 0)
            {
                XLDiffSheet(workbook, fileDiff);
            }

            row++;
        }
    }

    private void XLDiffSheet(XLWorkbook workbook, CSVDiff fileDiff)
    {
        var sheetName = fileDiff.Options.TryGetValue("sheet_name", out var sheetNameValue)
        ? sheetNameValue.ToString()
        : System.IO.Path.GetFileNameWithoutExtension(fileDiff.Left.Path);

		var outFields = OutputFields(fileDiff);
		var freezeCols = fileDiff.Options.TryGetValue("freeze_cols", out var freezeColsValue)
			? Convert.ToInt32(freezeColsValue)
			: (outFields.Count(f => f is string) + fileDiff.Left.KeyFields.Count);

		var diffSheet = workbook.Worksheets.Add(sheetName);

		// Add column headers
		for (int i = 1; i <= outFields.Length; i++)
		{
			var header = outFields[i - 1] is string ? outFields[i - 1].ToString() : Titleize(outFields[i - 1].ToString());
			diffSheet.Cell(1, i).Value = header;
			diffSheet.Cell(1, i).Style.Font.SetBold();
		}

		int row = 2; // Start from the second row for data

		foreach (var keyDiff in fileDiff.Diffs)
		{
			var key = keyDiff.Key;
			var diff = keyDiff.Value;
			var chg = diff["action"].ToString();

			int col = 1;

			foreach (var field in outFields)
			{
				// Determine cell value based on field and diff data
				var newValue = ""; // Set the appropriate value here
				var oldValue = ""; // Set the appropriate value here

				// Determine background color based on change action
				var fgColor = XLColor.Black; // Default color
				var bgColor = XLColor.White; // Default color
				var strike = false;
				if (chg == "Add")
				{
					fgColor = XLColor.FromHtml("#00A000"); // Green
				}
				else if (chg == "Delete")
				{
					fgColor = XLColor.FromHtml("#FF0000"); // Red
					strike = true;
				}
				else if (chg == "Update")
				{
					fgColor = XLColor.FromHtml("#0000A0"); // Blue
					bgColor = XLColor.FromHtml("#F0F0FF"); // Blue
				}
				else if (chg == "Move")
				{
					fgColor = XLColor.FromHtml("#4040FF"); // Purple
				}

				// Determine if you need to add a comment
				string comment = null;
				var diffValue = diff[field];
				if (diffValue is object[])
				{
					var diffList = diffValue as object[];
					oldValue = diffList[0].ToString();
					newValue = diffList[1].ToString();

					if (string.IsNullOrEmpty(oldValue))
					{
						fgColor = XLColor.FromHtml("#00A000"); // Green
					}
					else
					{
						comment = oldValue;
					}
					
				}
				else if (diffValue is not null)
				{
					newValue = diffValue.ToString();
				}
				else if (false)
				{
					// TODO: include_matched logic
				}

				var cell = diffSheet.Cell(row, col);
				cell.Value = newValue;
				cell.Style.Font.FontColor = fgColor;
				cell.Style.Font.Strikethrough = strike;
				cell.Style.Fill.BackgroundColor = bgColor;
				// cell.DataType = XLDataType.Text;

				// Add comment if present
				if (!string.IsNullOrEmpty(comment))
				{
					var commentCell = cell.CreateComment();
					commentCell.Visible = false;
					commentCell.AddText(comment);
				}

				col++;
			}

			row++;
		}

		// Apply auto-filter and freeze rows/columns
		XLFilterAndFreeze(diffSheet, freezeCols);
		
		// Auto-size columns
		diffSheet.Columns().AdjustToContents();
    }

    private void XLFilterAndFreeze(IXLWorksheet sheet, int freezeCols = 0)
    {
        // Implement auto-filter and freeze logic here
		// Add auto-filter to the appropriate row/column
		sheet.RangeUsed().SetAutoFilter();

		// Freeze rows and columns based on the freezeCols parameter
		if (freezeCols > 0)
		{
			sheet.SheetView.Freeze(freezeCols, 1);
		}
    }

    private void XLSave(XLWorkbook workbook, string path)
    {
        try
        {
            workbook.SaveAs(path);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw new Exception($"Unable to replace existing Excel file {path} - is it already open in Excel?");
        }
    }
}