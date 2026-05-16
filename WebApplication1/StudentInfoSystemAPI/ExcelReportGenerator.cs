using System.Data;
using System.Drawing;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;

namespace StudentInfoSystemAPI;

public class ExcelReportGenerator
{
    private const string CompanyName = "Student Information System";
    private const string CompanyAddress = "123 University Avenue, Education City";
    private const string CompanyPhone = "(555) 123-4567";

    public byte[] GenerateDataTableReport(DataTable table, string reportTitle, string signerName)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        var reportSheet = package.Workbook.Worksheets.Add("Sheet 1 - Report");
        var summarySheet = package.Workbook.Worksheets.Add("Sheet 2 - Graph");

        AddReportHeader(reportSheet, reportTitle, signerName, Math.Max(table.Columns.Count, 6));
        AddDataTable(reportSheet, table, 13);
        AddDataTableGraph(summarySheet, table, reportTitle);

        return package.GetAsByteArray();
    }

    public byte[] GenerateTransactionReport(List<TransactionReportData> transactions, string reportTitle, string signerName)
    {
        var table = new DataTable();
        table.Columns.Add("transaction_id", typeof(int));
        table.Columns.Add("student_id", typeof(int));
        table.Columns.Add("student_name", typeof(string));
        table.Columns.Add("transaction_type", typeof(string));
        table.Columns.Add("description", typeof(string));
        table.Columns.Add("transaction_date", typeof(DateTime));
        table.Columns.Add("amount", typeof(decimal));
        table.Columns.Add("status", typeof(string));

        foreach (var transaction in transactions)
        {
            table.Rows.Add(
                transaction.TransactionId,
                transaction.StudentId,
                transaction.StudentName,
                transaction.TransactionType,
                transaction.Description,
                transaction.TransactionDate,
                transaction.Amount ?? 0,
                transaction.Status);
        }

        return GenerateDataTableReport(table, reportTitle, signerName);
    }

    private static void AddReportHeader(ExcelWorksheet ws, string reportTitle, string signerName, int columnCount)
    {
        var lastColumn = Math.Max(columnCount, 6);

        ws.Row(1).Height = 24;
        ws.Row(2).Height = 24;
        ws.Row(3).Height = 20;
        ws.Row(4).Height = 12;
        ws.Column(1).Width = 18;

        using var logoStream = CreateLogoImage();
        var logo = ws.Drawings.AddPicture("CompanyLogo", logoStream);
        logo.SetPosition(0, 6, 0, 8);
        logo.SetSize(82, 58);

        ws.Cells[1, 2, 1, lastColumn].Merge = true;
        ws.Cells[1, 2].Value = CompanyName;
        ws.Cells[1, 2].Style.Font.Bold = true;
        ws.Cells[1, 2].Style.Font.Size = 18;
        ws.Cells[1, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        ws.Cells[2, 2, 2, lastColumn].Merge = true;
        ws.Cells[2, 2].Value = CompanyAddress;
        ws.Cells[2, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        ws.Cells[3, 2, 3, lastColumn].Merge = true;
        ws.Cells[3, 2].Value = CompanyPhone;
        ws.Cells[3, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        ws.Cells[5, 1, 5, lastColumn].Merge = true;
        ws.Cells[5, 1].Value = reportTitle;
        ws.Cells[5, 1].Style.Font.Bold = true;
        ws.Cells[5, 1].Style.Font.Size = 15;
        ws.Cells[5, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        ws.Cells[5, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
        ws.Cells[5, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(221, 235, 247));

        ws.Cells[7, 1].Value = "Generated On:";
        ws.Cells[7, 2].Value = DateTime.Now;
        ws.Cells[7, 2].Style.Numberformat.Format = "yyyy-mm-dd hh:mm";

        ws.Cells[8, 1].Value = "Prepared By:";
        ws.Cells[8, 2].Value = signerName;

        ws.Cells[10, 1].Value = "Signature:";
        ws.Cells[10, 2, 10, 4].Merge = true;
        ws.Cells[10, 2].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        ws.Cells[11, 2].Value = "Authorized Signatory";
        ws.Cells[11, 2].Style.Font.Italic = true;

        ws.Cells[1, 1, 4, lastColumn].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        ws.Cells[1, 1, 4, lastColumn].Style.Border.Bottom.Color.SetColor(Color.FromArgb(180, 180, 180));
    }

    private static MemoryStream CreateLogoImage()
    {
        var stream = new MemoryStream();
        using var bitmap = new Bitmap(220, 150);
        using var graphics = Graphics.FromImage(bitmap);
        using var background = new SolidBrush(Color.FromArgb(13, 110, 253));
        using var accent = new SolidBrush(Color.FromArgb(25, 135, 84));
        using var textBrush = new SolidBrush(Color.White);
        using var font = new Font("Segoe UI", 34, FontStyle.Bold, GraphicsUnit.Pixel);
        using var smallFont = new Font("Segoe UI", 14, FontStyle.Bold, GraphicsUnit.Pixel);

        graphics.Clear(Color.Transparent);
        graphics.FillEllipse(background, 16, 12, 118, 118);
        graphics.FillRectangle(accent, 108, 82, 86, 28);
        graphics.DrawString("SIS", font, textBrush, 42, 43);
        graphics.DrawString("REPORT", smallFont, textBrush, 119, 87);
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        stream.Position = 0;

        return stream;
    }

    private static void AddDataTable(ExcelWorksheet ws, DataTable table, int startRow)
    {
        if (table.Columns.Count == 0)
        {
            ws.Cells[startRow, 1].Value = "No columns found.";
            return;
        }

        for (var col = 0; col < table.Columns.Count; col++)
        {
            var cell = ws.Cells[startRow, col + 1];
            cell.Value = ToTitle(table.Columns[col].ColumnName);
            cell.Style.Font.Bold = true;
            cell.Style.Font.Color.SetColor(Color.White);
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(52, 73, 94));
        }

        for (var row = 0; row < table.Rows.Count; row++)
        {
            for (var col = 0; col < table.Columns.Count; col++)
            {
                var value = table.Rows[row][col];
                ws.Cells[startRow + row + 1, col + 1].Value = value == DBNull.Value ? null : value;

                if (value is DateTime)
                {
                    ws.Cells[startRow + row + 1, col + 1].Style.Numberformat.Format = "yyyy-mm-dd";
                }

                if (table.Columns[col].ColumnName.Contains("amount", StringComparison.OrdinalIgnoreCase) ||
                    table.Columns[col].ColumnName.Contains("fee", StringComparison.OrdinalIgnoreCase))
                {
                    ws.Cells[startRow + row + 1, col + 1].Style.Numberformat.Format = "#,##0.00";
                }
            }
        }

        var endRow = Math.Max(startRow + table.Rows.Count, startRow + 1);
        var endCol = table.Columns.Count;
        var dataRange = ws.Cells[startRow, 1, endRow, endCol];
        dataRange.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.LightGray);
        ws.View.FreezePanes(startRow + 1, 1);
        ws.Cells[ws.Dimension.Address].AutoFitColumns(12, 40);
    }

    private static void AddDataTableGraph(ExcelWorksheet ws, DataTable table, string reportTitle)
    {
        ws.Cells["A1:B1"].Merge = true;
        ws.Cells["A1"].Value = $"{reportTitle} Graph";
        ws.Cells["A1"].Style.Font.Bold = true;
        ws.Cells["A1"].Style.Font.Size = 16;
        ws.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        ws.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(221, 235, 247));

        var labelColumn = FindFirstColumn(table, "transaction_type", "course_name", "semester", "student_name", "status");
        var valueColumn = FindFirstNumericColumn(table, "amount", "registration_fee", "enrolled_students", "total_enrollments", "courses_taken", "average_grade", "average_gpa");

        ws.Cells[3, 1].Value = "Category";
        ws.Cells[3, 2].Value = "Value";
        ws.Cells[3, 1, 3, 2].Style.Font.Bold = true;
        ws.Cells[3, 1, 3, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
        ws.Cells[3, 1, 3, 2].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

        var groupedRows = BuildChartRows(table, labelColumn, valueColumn);
        var row = 4;
        foreach (var item in groupedRows)
        {
            ws.Cells[row, 1].Value = item.Label;
            ws.Cells[row, 2].Value = item.Value;
            row++;
        }

        if (!groupedRows.Any())
        {
            ws.Cells[4, 1].Value = "No data";
            ws.Cells[4, 2].Value = 0;
            row = 5;
        }

        var chart = ws.Drawings.AddChart("SummaryChart", eChartType.ColumnClustered);
        chart.Title.Text = reportTitle;
        chart.SetPosition(2, 0, 4, 0);
        chart.SetSize(760, 420);
        chart.Series.Add(ws.Cells[4, 2, row - 1, 2], ws.Cells[4, 1, row - 1, 1]);
        chart.Legend.Remove();
        chart.YAxis.Title.Text = "Generated Data";
        chart.XAxis.Title.Text = "Category";

        ws.Cells[ws.Dimension.Address].AutoFitColumns(14, 35);
    }

    private static List<(string Label, double Value)> BuildChartRows(DataTable table, string? labelColumn, string? valueColumn)
    {
        if (labelColumn is null || table.Rows.Count == 0)
        {
            return [];
        }

        return table.Rows.Cast<DataRow>()
            .GroupBy(row => Convert.ToString(row[labelColumn]) ?? "N/A")
            .Select(group => (
                Label: group.Key,
                Value: valueColumn is null
                    ? group.Count()
                    : group.Sum(row => TryGetDouble(row[valueColumn]))))
            .OrderByDescending(item => item.Value)
            .Take(12)
            .ToList();
    }

    private static string? FindFirstColumn(DataTable table, params string[] names) =>
        names.FirstOrDefault(name => table.Columns.Contains(name)) ?? table.Columns.Cast<DataColumn>().FirstOrDefault()?.ColumnName;

    private static string? FindFirstNumericColumn(DataTable table, params string[] names)
    {
        foreach (var name in names)
        {
            if (table.Columns.Contains(name))
            {
                return name;
            }
        }

        return table.Columns.Cast<DataColumn>()
            .FirstOrDefault(column => column.DataType == typeof(int) || column.DataType == typeof(decimal) || column.DataType == typeof(double))
            ?.ColumnName;
    }

    private static double TryGetDouble(object value)
    {
        if (value == DBNull.Value || value is null)
        {
            return 0;
        }

        return double.TryParse(Convert.ToString(value), out var parsed) ? parsed : 0;
    }

    private static string ToTitle(string value) =>
        string.Join(" ", value.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
}
