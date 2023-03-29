namespace Domain.Entities
{
    public class Medication
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? GrupaMaladiilor { get; set; }
        public string? CodDCI { get; set; }
        public string? DenumireComunaInternationala { get; set; }
        public string? Doza { get; set; }
        public string? CodDC { get; set; }
        public decimal SumaFixaCompensataCuTVA { get; set; }
        public decimal SumaFixaCompensataFaraTVA { get; set; }
        public string? DenumireComerciala { get; set; }
        public string? FormaFarmaceutica { get; set; }
        public string? Divizarea { get; set; }
        public string? Tara { get; set; }
        public string? FirmaProducatoare { get; set; }
        public string? NumarInregistrare { get; set; }
        public DateTime DataInregistrare { get; set; }
        public string? CodATC { get; set; }
        public string? CodMedicament { get; set; }
        public string? DataAprobarePret { get; set; }
    }

}
