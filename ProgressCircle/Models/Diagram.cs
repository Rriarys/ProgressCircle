namespace ProgressCircle.Models
{
    public class Diagram
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int TargetMinutes { get; set; }
        public int AccumulatedMinutes { get; set; }
        public DateTime LastUpdatedUtc { get; set; }
        public string AccessKey { get; set; }
    }
}
