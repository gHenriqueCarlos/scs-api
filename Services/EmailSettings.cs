namespace ScspApi.Services
{
    public class EmailSettings
    {
        public string FromEmail { get; set; } = default!;
        public string SmtpServer { get; set; } = default!;
        public int SmtpPort { get; set; }    
        public string SmtpUser { get; set; } = default!;
        public string SmtpPassword { get; set; } = default!;
        public bool UseSsl { get; set; } = true; // 465 = SSL; 587 = STARTTLS (UseSsl=false)
    }
}
