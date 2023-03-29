using Domain.Entities;
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
        Task<List<Medication>> GetMedicationsAsync();
    }

    public class MedicationService : IMedicationService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _memoryCache;
        private const string CacheKey = "medications";
        private const string ExcelUrl = "http://www.cnam.md/httpdocs/editorDir/file/MedicamenteCompensate/farmacii/2023/1/Lista%20DC%20CNAM%20(28_01_2023)_SITE.xls";

        public MedicationService(HttpClient httpClient, IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _memoryCache = memoryCache;
        }

        public async Task<List<Medication>> GetMedicationsAsync()
        {
            if (_memoryCache.TryGetValue(CacheKey, out List<Medication> medications))
            {
                return medications;
            }

            medications = await DownloadAndParseExcelAsync();
            if (medications != null)
            {
                _memoryCache.Set(CacheKey, medications, TimeSpan.FromDays(1));
            }

            return medications;
        }

        private async Task<List<Medication>> DownloadAndParseExcelAsync()
        {
            var medications = new List<Medication>();

            using var excelStream = await DownloadExcelAsync();
            using var excelReader = ExcelReaderFactory.CreateReader(excelStream, new ExcelReaderConfiguration() { FallbackEncoding = Encoding.UTF8 });

            var dataSet = excelReader.AsDataSet();

            DataTable dataTable = dataSet.Tables[0];

            var columnIndex = new Dictionary<string, int>();
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                var columnName = dataTable.Rows[0][i].ToString();
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

            for (int i = 1; i < dataTable.Rows.Count; i++)
            {
                DataRow row = dataTable.Rows[i];
                DateTime.TryParseExact(row[columnIndex["Data înregistrării"]].ToString(), "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dataInregistrare);

                var medication = new Medication
                {
                    GrupaMaladiilor = row[columnIndex["Grupa maladiilor pentru compensare "]].ToString(),
                    CodDCI = row[columnIndex["Cod DCI"]].ToString(),
                    DenumireComunaInternationala = row[columnIndex["Denumirea Comuna Internationala (DCI)"]].ToString(),
                    Doza = row[columnIndex["Doza în SI MC"]].ToString(),
                    CodDC = row[columnIndex["Cod DC"]].ToString(),
                    SumaFixaCompensataCuTVA = Convert.ToDecimal(row[columnIndex["Suma fixă compensată per unitate de măsură inclusiv TVA"]]),
                    SumaFixaCompensataFaraTVA = Convert.ToDecimal(row[columnIndex["Suma fixă compensată per unitate de măsură fără TVA"]]),
                    DenumireComerciala = row[columnIndex["Denumirea comercială (DC)"]].ToString(),
                    FormaFarmaceutica = row[columnIndex["Forma farmaceutică"]].ToString(),
                    Divizarea = row[columnIndex["Divizarea"]].ToString(),
                    Tara = row[columnIndex["Ţara"]].ToString(),
                    FirmaProducatoare = row[columnIndex["Firma producătoare"]].ToString(),
                    NumarInregistrare = row[columnIndex["Număr de înregistrare"]].ToString(),
                    DataInregistrare = dataInregistrare,
                    CodATC = row[columnIndex["Cod ATC"]].ToString(),
                    CodMedicament = row[columnIndex["Cod medicament (Catalogul național de prețuri)"]].ToString(),
                    DataAprobarePret = row[columnIndex["Data aprobării preţului de Agenția Medicamentului și Dispozitivelor medicale"]].ToString()
                };

                medications.Add(medication);
            }

            return medications;
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
