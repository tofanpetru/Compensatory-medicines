using Domain.Entities;
using Domain.Enums;
using ExcelDataReader;
using Microsoft.Extensions.Caching.Memory;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CompensatoryMedicines.Services
{
    public interface IMedicationService
    {
        Task<List<Medication>> GetMedicationsAsync(DCTabs tab);
    }

    public class MedicationService : IMedicationService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _memoryCache;
        public MedicationService(HttpClient httpClient, IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _memoryCache = memoryCache;
        }

        public async Task<List<Medication>> GetMedicationsAsync(DCTabs tab)
        {
            // Generate a cache key based on the selected DCTabs value
            var cacheKey = $"medications_{tab}";

            if (!_memoryCache.TryGetValue(cacheKey, out List<Medication> medications))
            {
                using var excelStream = await GetExcelStreamAsync();
                medications = GetMedicationFromExcel(excelStream, tab);

                if (medications != null)
                {
                    TimeSpan cacheExpirationTime = tab switch
                    {
                        DCTabs.FullCompensated => TimeSpan.FromHours(6),
                        DCTabs.PartialCompensated => TimeSpan.FromDays(1),
                        DCTabs.Covid19 => TimeSpan.FromDays(7),
                        _ => throw new ArgumentException("Invalid DCTabs value", nameof(tab))
                    };

                    _memoryCache.Set(cacheKey, medications, cacheExpirationTime);
                }
            }

            if (medications == null)
            {
                throw new Exception($"No medications found for the {tab} tab.");
            }

            return medications;
        }

        private async Task<Stream> GetExcelStreamAsync()
        {
            const string excelCacheKey = "excel_file";
            if (!_memoryCache.TryGetValue(excelCacheKey, out Stream excelStream))
            {
                excelStream = await DownloadExcelAsync();
                _memoryCache.Set(excelCacheKey, excelStream, TimeSpan.FromHours(6));
            }
            return excelStream;
        }

        private async Task<List<Medication>> DownloadAndParseExcelAsync(DCTabs tab)
        {
            using var excelStream = await DownloadExcelAsync();

            return GetMedicationFromExcel(excelStream, tab);
        }

        private static List<Medication> GetMedicationFromExcel(Stream excelStream, DCTabs tab)
        {
            IExcelDataReader excelReader = ExcelReaderFactory.CreateReader(
                excelStream,
                new ExcelReaderConfiguration() { FallbackEncoding = Encoding.UTF8 });

            DataTable dataTable = excelReader.AsDataSet().Tables[(int)tab];

            var columnIndex = new Dictionary<string, int>();
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                var columnName = dataTable.Rows[0][i].ToString().TrimEnd();
                if (columnIndex.ContainsKey(columnName))
                {
                    var suffix = 2;
                    while (columnIndex.ContainsKey(columnName + " " + suffix))
                    {
                        suffix++;
                    }
                    columnName = columnName + " " + suffix;
                }
                columnIndex.Add(columnName, i);
            }

            var medications = new List<Medication>();
            for (int i = 1; i < dataTable.Rows.Count; i++)
            {
                DataRow row = dataTable.Rows[i];
                DateTime.TryParseExact(row[columnIndex["Data înregistrării"]].ToString(), "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dataInregistrare);

                var medication = new Medication
                {
                    GrupaMaladiilor = GetColumnValue<string>(row, columnIndex, "Grupa maladiilor pentru compensare"),
                    CodDCI = GetColumnValue<string>(row, columnIndex, "Cod DCI"),
                    DenumireComunaInternationala = GetColumnValue<string>(row, columnIndex, "Denumirea Comuna Internationala (DCI)"),
                    Doza = GetColumnValue<string>(row, columnIndex, "Doza în SI MC"),
                    CodDC = GetColumnValue<string>(row, columnIndex, "Cod DC"),
                    SumaFixaCompensataCuTVA = GetColumnValue<decimal>(row, columnIndex, "Suma fixă compensată per unitate de măsură inclusiv TVA"),
                    SumaFixaCompensataFaraTVA = GetColumnValue<decimal>(row, columnIndex, "Suma fixă compensată per unitate de măsură fără TVA"),
                    DenumireComerciala = GetColumnValue<string>(row, columnIndex, "Denumirea comercială (DC)"),
                    FormaFarmaceutica = GetColumnValue<string>(row, columnIndex, "Forma farmaceutică"),
                    Divizarea = GetColumnValue<string>(row, columnIndex, "Divizarea"),
                    Tara = GetColumnValue<string>(row, columnIndex, "Ţara"),
                    FirmaProducatoare = GetColumnValue<string>(row, columnIndex, "Firma producătoare"),
                    NumarInregistrare = GetColumnValue<string>(row, columnIndex, "Număr de înregistrare"),
                    DataInregistrare = dataInregistrare,
                    CodATC = GetColumnValue<string>(row, columnIndex, "Cod ATC"),
                    CodMedicament = GetColumnValue<string>(row, columnIndex, "Cod medicament (Catalogul național de prețuri)"),
                    DataAprobarePret = GetColumnValue<string>(row, columnIndex, "Data aprobării preţului de Agenția Medicamentului și Dispozitivelor medicale")
                };


                medications.Add(medication);
            }

            return medications;
        }

        private static T GetColumnValue<T>(DataRow row, Dictionary<string, int> columnIndex, string columnName, T defaultValue = default)
        {
            if (columnIndex.TryGetValue(columnName, out int index) && row.ItemArray.Length > index)
            {
                var value = row[columnIndex[columnName]];
                if (value != null && value != DBNull.Value)
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
            }

            return defaultValue;
        }

        private async Task<Stream> DownloadExcelAsync()
        {
            var url = "http://www.cnam.md/index.php?page=295";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var htmlDocument = new HtmlAgilityPack.HtmlDocument();
            htmlDocument.LoadHtml(content);

            var linkNode = htmlDocument.DocumentNode.SelectSingleNode("//a[contains(., 'Lista Denumirilor Comerciale compensate')]");

            if (linkNode == null)
            {
                throw new InvalidOperationException("Link-ul cu denumirile comerciale compensate nu a fost găsit.");
            }

            var excelUrl = linkNode.GetAttributeValue("href", string.Empty);
            var dateRegex = new Regex(@"\((\d{2}\.\d{2}\.\d{4})\)");
            var match = dateRegex.Match(linkNode.InnerText);
            if (match.Success)
            {
                DateTime.TryParseExact(match.Groups[1].Value, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime excelDate);

                if (excelDate > DateTime.Now)
                {
                    throw new InvalidOperationException("Data din link-ul Excel este mai mare decât data actuală.");
                }
            }
            else
            {
                throw new InvalidOperationException("Data nu a fost găsită în link-ul Excel.");
            }

            // Acum descarcă Excel-ul
            var excelResponse = await _httpClient.GetAsync("http://www.cnam.md" + excelUrl);
            excelResponse.EnsureSuccessStatusCode();

            return await excelResponse.Content.ReadAsStreamAsync();
        }
    }
}
