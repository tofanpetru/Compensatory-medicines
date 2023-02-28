namespace excelParser.Entities
{
    public class Medicament
    {
        public Medicament(string grupaMaladiilor, string dci, string doza, decimal sumaCompensata, string dc)
        {
            GrupaMaladiilor = grupaMaladiilor;
            DCI = dci;
            Doza = doza;
            SumaCompensata = sumaCompensata;
            DC = dc;
        }

        public string GrupaMaladiilor { get; set; }
        public string DCI { get; set; }
        public string Doza { get; set; }
        public decimal SumaCompensata { get; set; }
        public string DC { get; set; }
    }

}
