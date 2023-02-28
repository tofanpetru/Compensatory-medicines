using Domain.Entities;
using ExcelDataReader;
using System.Data;

namespace CompensatoryMedicines.Services
{
    public interface IMedicationManager
    {
        Task<List<Medication>> GetMedicationsAsync();
    }

    public class MedicationService : IMedicationManager
    {
        public async Task<List<Medication>> GetMedicationsAsync()
        {
            using var client = new HttpClient();
            var stream = await client.GetStreamAsync("http://www.cnam.md/httpdocs/editorDir/file/MedicamenteCompensate/farmacii/2023/1/Lista%20DC%20CNAM%20(28_01_2023)_SITE.xls");

            using var reader = ExcelReaderFactory.CreateReader(stream);
            var result = reader.AsDataSet(new ExcelDataSetConfiguration()
            {
                ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                {
                    UseHeaderRow = true
                }
            });

            var medications = new List<Medication>();

            foreach (DataRow row in result.Tables[0].Rows)
            {
                var medication = new Medication
                {
                    Name = row["DCI"].ToString(),
                    DosageForm = row["Forma Farmaceutică"].ToString(),
                    Strength = row["Concentrația"].ToString(),
                    Company = row["Furnizor"].ToString(),
                    Price = decimal.Parse(row["Prețul cu TVA, MDL"].ToString())
                };

                medications.Add(medication);
            }

            return medications;
        }


    }
}
