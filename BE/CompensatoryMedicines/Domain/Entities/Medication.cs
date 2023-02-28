namespace Domain.Entities
{
    public class Medication
    {
        public string Name { get; set; }

        public string InternationalCommonName { get; set; }

        public string Dose { get; set; }

        public string PharmaceuticalForm { get; set; }

        public string PackagingQuantity { get; set; }

        public string Concentration { get; set; }

        public string Manufacturer { get; set; }

        public string ManufacturerCountry { get; set; }

        public string ExpiryDate { get; set; }

        public string NationalCode { get; set; }
        public string? DosageForm { get; set; }
        public string? Strength { get; set; }
        public string? Company { get; set; }
        public decimal Price { get; set; }
    }
}
