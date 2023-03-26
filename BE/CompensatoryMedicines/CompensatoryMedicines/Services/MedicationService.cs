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
            // Încercăm să obținem medicamentele din cache
            if (_memoryCache.TryGetValue(CacheKey, out List<Medication> medications))
            {
                return medications;
            }

            // Dacă nu avem medicamentele în cache, le descărcăm și le adăugăm în cache
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

            // Identifică numele coloanelor
            var columnNames = dataTable.Rows[0].ItemArray.Select(x => x.ToString()).ToArray();

            for (int i = 1; i < dataTable.Rows.Count; i++)
            {
                DataRow row = dataTable.Rows[i];
                var medication = new Medication
                {
                    GrupaMaladiilor = GetValueFromColumn<string>(row, "Grupa maladiilor pentru compensare", columnNames),
                    CodDCI = GetValueFromColumn<string>(row, "Cod DCI", columnNames),
                    DenumireComunaInternationala = GetValueFromColumn<string>(row, "Denumirea Comuna Internationala (DCI)", columnNames),
                    Doza = GetValueFromColumn<string>(row, "Doza în SI MC", columnNames),
                    CodDC = GetValueFromColumn<string>(row, "Cod DC", columnNames),
                    SumaFixaCompensataCuTVA = GetValueFromColumn<decimal>(row, "Suma fixă compensată per unitate de măsură inclusiv TVA", columnNames),
                    SumaFixaCompensataFaraTVA = GetValueFromColumn<decimal>(row, "Suma fixă compensată per unitate de măsură fără TVA", columnNames),
                    DenumireComerciala = GetValueFromColumn<string>(row, "Denumirea comercială (DC)", columnNames),
                    FormaFarmaceutica = GetValueFromColumn<string>(row, "Forma farmaceutică", columnNames),
                    Divizarea = GetValueFromColumn<string>(row, "Divizarea", columnNames),
                    Tara = GetValueFromColumn<string>(row, "Țara", columnNames),
                    FirmaProducatoare = GetValueFromColumn<string>(row, "Firma producătoare", columnNames),
                    NumarInregistrare = GetValueFromColumn<string>(row, "Număr de înregistrare", columnNames),
                    DataInregistrare = GetValueFromColumn<DateTime>(row, "Data înregistrării", columnNames),
                    CodATC = GetValueFromColumn<string>(row, "Cod ATC", columnNames),
                    CodMedicament = GetValueFromColumn<string>(row, "Cod medicament (Catalogul național de prețuri)", columnNames),
                    DataAprobarePret = GetValueFromColumn<string>(row, "Data aprobării prețului de Agenția Medicamentului și Dispozitivelor medicale", columnNames)
                };

                medications.Add(medication);
            }

            return medications;
        }

        private T GetValueFromColumn<T>(DataRow row, string columnName, string[] columnNames)
        {
            if (row.Table.Columns.Contains(columnName))
            {
                return GetValue<T>(row[columnName]);
            }
            else if (columnNames != null)
            {
                foreach (string name in columnNames)
                {
                    if (row.Table.Columns.Contains(name))
                    {
                        return GetValue<T>(row[name]);
                    }
                }
            }

            if (typeof(T).IsValueType)
            {
                return default;
            }

            return (T)(object)null;
        }

        private T GetValue<T>(object value)
        {
            if (value != null && value != DBNull.Value)
            {
                if (typeof(T) == typeof(DateTime))
                {
                    return (T)(object)DateTime.ParseExact(value.ToString(), "dd.MM.yyyy", CultureInfo.InvariantCulture);
                }

                return (T)Convert.ChangeType(value, typeof(T));
            }
            else
            {
                return default;
            }
        }

        private async Task<Stream> DownloadExcelAsync()
        {
            var response = await _httpClient.GetAsync(ExcelUrl);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync();
        }
    }

}
