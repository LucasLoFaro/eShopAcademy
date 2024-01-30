namespace Data.Interfaces
{
    public interface IDatabaseSettingsProvider
    {
        public string GetConnectionString();
        public string GetConnectionBundlePath();
        public string GetClient();
        public string GetSecret();
    }
}