namespace AmestoCandidateTask.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public int ItemId { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
    }
}