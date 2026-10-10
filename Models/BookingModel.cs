namespace MedLinkPortal.Models
{
    public class BookingModel
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Type { get; set; }
        public string Note { get; set; }
        public Doctor SelectedDoctor { get; set; }
    }
}
