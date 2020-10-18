namespace SS14.Auth.Shared.Emails
{
    public sealed class SmtpEmailOptions
    {
        public string SendEmail { get; set; }
        public string SendName { get; set; }
        public string Host { get; set; }
        public ushort Port { get; set; } = 0;
        public string User { get; set; }
        public string Pass { get; set; }
    }
}