using excelParser;
using excelParser.Entities;
using excelParser.Enums;
using OfficeOpenXml;

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
ExcelUtils.DownloadExcelFile();
// Parse the Excel file
string fileName = "Lista DC CNAM (28_02_2023)_SITE.xls";
string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
int tabIndex = (int)ExcelTab.DCCapitalIPartialComp;
Medicament[] medicaments = ExcelUtils.ParseExcelFile(filePath, tabIndex);

// Print the parsed data
Console.WriteLine($"Parsed {medicaments.Length} medicaments:");
foreach (Medicament medicament in medicaments)
{
    Console.WriteLine($"{medicament.GrupaMaladiilor} | {medicament.DCI} | {medicament.Doza} | {medicament.SumaCompensata} | {medicament.DC}");
}

Console.WriteLine("\nPress any key to exit.");
Console.ReadKey();