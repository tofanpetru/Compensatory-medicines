using excelParser.Entities;
using OfficeOpenXml;
using System.Net;

namespace excelParser
{
    public static class ExcelUtils
    {
        public static void DownloadExcelFile()
        {
            string url = "http://www.cnam.md/httpdocs/editorDir/file/MedicamenteCompensate/2023/2/Lista%20DC%20CNAM%20(28_02_2023)_SITE.xls";
            string fileName = "Lista DC CNAM (28_02_2023)_SITE.xls";
            string savePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

            // Check if the file already exists
            if (File.Exists(savePath))
            {
                Console.WriteLine($"File already exists at {savePath}.\n");
                return;
            }

            using (WebClient webClient = new())
            {
                webClient.DownloadFile(url, savePath);
            }

            Console.WriteLine($"File downloaded to {savePath}.\n");
        }


        /*public static Medicament[] ParseExcelFile(string fileName, int tabIndex)
        {
            using (ExcelPackage package = new ExcelPackage(new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName))))
            {
                Console.WriteLine("Number of worksheets : " + package.Workbook.Worksheets.Count);
                ExcelWorksheet worksheet = package.Workbook.Worksheets[tabIndex];

                // Load the data range into a 2D array
                object[,]? dataArray = worksheet.Cells[2, 1, worksheet.Dimension.End.Row, 5].Value as object[,];

                // Parse the data into an array of Medicament objects
                Medicament[] medicaments = Enumerable.Range(1, dataArray.GetLength(0))
                    .Select(i => new Medicament
                    {
                        GrupaMaladiilor = dataArray[i, 1]?.ToString(),
                        DCI = dataArray[i, 2]?.ToString(),
                        Doza = dataArray[i, 3]?.ToString(),
                        SumaCompensata = decimal.TryParse(dataArray[i, 4]?.ToString(), out decimal suma) ? suma : 0,
                        DC = dataArray[i, 5]?.ToString()
                    })
                    .ToArray();

                return medicaments;
            }
        }*/

        public static Medicament[] ParseExcelFile(string filePath, int tabIndex)
        {
            // Open the Excel file using EPPlus
            using (ExcelPackage package = new ExcelPackage(new FileInfo(filePath)))
            {
                Console.WriteLine("Number of worksheets : " + package.Workbook.Worksheets.Count);

                // Get the worksheet at the specified tab index
                ExcelWorksheet worksheet = package.Workbook.Worksheets[tabIndex];

                // Find the first row that contains data
                int row = 2; // Start at row 2 to skip the header row
                while (string.IsNullOrWhiteSpace(worksheet.Cells[row, 1].Value?.ToString()))
                {
                    row++;
                }

                // Parse the data into Medicament objects
                List<Medicament> medicaments = new();
                while (!string.IsNullOrWhiteSpace(worksheet.Cells[row, 1].Value?.ToString()))
                {
                    string grupaMaladiilor = worksheet.Cells[row, 1].Value.ToString().Trim();
                    string dci = worksheet.Cells[row, 2].Value.ToString().Trim();
                    string doza = worksheet.Cells[row, 3].Value.ToString().Trim();
                    decimal sumaCompensata = decimal.Parse(worksheet.Cells[row, 4].Value.ToString().Trim());
                    string dc = worksheet.Cells[row, 5].Value.ToString().Trim();

                    Medicament medicament = new Medicament(grupaMaladiilor, dci, doza, sumaCompensata, dc);
                    medicaments.Add(medicament);

                    row++;
                }

                return medicaments.ToArray();
            }
        }


    }
}
