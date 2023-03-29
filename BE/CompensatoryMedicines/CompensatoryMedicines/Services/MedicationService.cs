using Domain.Entities;
using ExcelDataReader;
using Microsoft.Extensions.Caching.Memory;
using System.Data;
using System.Globalization;
using System.Text;

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
            var response = await _httpClient.GetAsync(ExcelUrl);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync();
        }
    }
}
