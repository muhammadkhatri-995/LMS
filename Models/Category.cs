namespace LMS.API.Models
{
    public class Category
    {
        public Guid Id { get; set; }
        public String Name { get; set; }
        
        public DateTime CreatedAt { get; set; }

        public ICollection<Course>Courses { get; set; } = new List<Course>();
    }
}
