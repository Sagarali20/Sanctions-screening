using NPOI.SS.UserModel;
using System.Globalization;

namespace Nec.Web.Config
{
    public class Excel
    {
        public static bool IsRowCompletelyEmpty(IRow row)
        {
            if (row == null) return true;

            foreach (var cell in row.Cells)
            {
                if (cell.CellType != CellType.Blank &&
                    !string.IsNullOrWhiteSpace(cell.ToString()))
                    return false;
            }

            return true;
        }

        public static string GetCell(IRow row, int idx)
        {
            var cell = row.GetCell(idx);
            if (cell == null) return string.Empty;

            return cell.CellType switch
            {
                CellType.String => cell.StringCellValue?.Trim() ?? string.Empty,

                CellType.Numeric => DateUtil.IsCellDateFormatted(cell)
                    ? DateUtil.GetJavaDate(cell.NumericCellValue, false)
                              .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : cell.NumericCellValue.ToString(CultureInfo.InvariantCulture),

                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Formula => GetFormulaResult(cell),
                _ => cell.ToString()?.Trim() ?? string.Empty
            };
        }

        public static string GetFormulaResult(ICell cell)
        {
            return cell.CachedFormulaResultType switch
            {
                CellType.Numeric => DateUtil.IsCellDateFormatted(cell)
                    ? DateUtil.GetJavaDate(cell.NumericCellValue, false)
                            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : cell.NumericCellValue
                            .ToString(CultureInfo.InvariantCulture),

                CellType.String => cell.StringCellValue?.Trim() ?? string.Empty,
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                _ => cell.ToString()?.Trim() ?? string.Empty
            };
        }

    }
}
