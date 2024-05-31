namespace MonitoringService.Domain.Models
{
    /// <summary>
    /// This class represents the settings retrieved from the appsettings.json file to keep settings configurable without a code change.
    /// </summary>
    public class ConfigurableSettings
    {
        public string Ongoing { get; set; }
        public string Approved { get; set; }
        public string ApprovedCsv { get; set; }
        public int MsBetweenRuns { get; set; }
    }
}
