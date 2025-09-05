using System.Collections.Generic;

namespace code
{
    public class CvData
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public List<Experience> Experiences { get; set; } = new();
        public List<Education> Educations { get; set; } = new();
        public List<string> Skills { get; set; } = new();
        public List<string> Competitions { get; set; } = new();
        public List<Project> Projects { get; set; } = new();
    }

    public class Experience
    {
        public string? Role { get; set; }
        public string? Company { get; set; }
        public string? Period { get; set; }
        public string? Description { get; set; }
    }

    public class Education
    {
        public string? Degree { get; set; }
        public string? Institution { get; set; }
        public string? Period { get; set; }
        public string? Description { get; set; }
    }

    public class Project
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
}
