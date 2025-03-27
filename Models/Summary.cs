namespace AmestoCandidateTask.Models
{
    public class Summary
    {
        public int CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public List<Order>? Order { get; set; }
    }
}