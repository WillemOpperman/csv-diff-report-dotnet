using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using csv_diff;
using csv_diff_report;
using CsvHelper;
using Xunit;

namespace tests
{
    public class TestReport
    {
        private static readonly List<string[]> Data1 = new List<string[]>()
        {
            new[] { "Parent", "Child", "Description" },
            new[] { "A", "A1", "Account1" },
            new[] { "A", "A2", "Account 2" },
            new[] { "A", "A3", "Account 3" },
            new[] { "A", "A4", "Account 4" },
            new[] { "A", "A6", "Account 6" }
        };

        private static readonly List<string[]> Data2 = new List<string[]>()
        {
            new[] { "Parent", "Child", "Description" },
            new[] { "A", "A1", "Account1" },
            new[] { "A", "A2", "Account2" },
            new[] { "A", "a3", "ACCOUNT 3" },
            new[] { "A", "A5", "Account 5" },
            new[] { "B", "A6", "Account 6" },
            new[] { "C", "A6", "Account 6c" }
        };

        private readonly CSVSource _source1;
        private readonly CSVSource _source2;
        private readonly CSVDiff _diff;

        public TestReport()
        {
            _source1 = new CSVSource(Data1, new Dictionary<string, object>
            {
                { "key_fields", new List<int> { 0, 1 } },
                { "exclude", new Dictionary<string, Regex> { { "Description", new Regex("Account\\d") } } }
            });

            _source2 = new CSVSource(Data2, new Dictionary<string, object>
            {
                { "key_fields", new List<int> { 0, 1 } },
                { "exclude", new Dictionary<string, Regex> { { "2", new Regex("^ACC") } } }
            });

            _diff = new CSVDiff(_source1, _source2, new Dictionary<string, object>
            {
                { "parent_field", 0 },
                { "child_field", 1 }
            });
        }
        
        [Fact]
        public void TestExcludeFilter()
        {
            var textOutputFilePath = Path.GetTempFileName();
            var textReport = new Text();
            textReport.Add(_diff);
            textReport.TextOutput(textOutputFilePath);
            
            var htmlOutputFilePath = Path.GetTempFileName();
            var htmlReport = new Html();
            htmlReport.Add(_diff);
            htmlReport.HtmlOutput(htmlOutputFilePath);

            var excelOutputFilePath = Path.GetTempFileName();
            var excelReport = new Excel();
            excelReport.Add(_diff);
            excelReport.XLOutput(excelOutputFilePath);

            Assert.Equal(1, _source1.SkipCount);
            Assert.Equal(1, _source2.SkipCount);
        }
    }
}
