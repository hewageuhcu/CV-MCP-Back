using System;
using System.Collections.Generic;
using System.IO;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Text.RegularExpressions;

namespace code
{
    public static class CvParser
    {
        public static CvData ParsePdf(string pdfPath)
        {
            var cv = new CvData();
            string? author = null;
            string text = string.Empty;
            using (var document = PdfDocument.Open(pdfPath))
            {
                // Try to get Author metadata
                if (document.Information != null && !string.IsNullOrWhiteSpace(document.Information.Author))
                {
                    author = document.Information.Author.Trim();
                }
                // Extract all text
                foreach (Page page in document.GetPages())
                {
                    text += page.Text + "\n";
                }
            }
            // Prefer author metadata, fallback to text parsing
            cv.Name = !string.IsNullOrWhiteSpace(author) ? author : ParseName(text);
            // Extract ABOUT section for email
            string aboutSection = "";
            var aboutMatch = Regex.Match(text, @"ABOUT(.+?)(EXPERIENCE|EDUCATION|SKILLS|COMPETITIONS|PROJECTS|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (aboutMatch.Success)
                aboutSection = aboutMatch.Groups[1].Value;
            cv.Email = ParseEmail(aboutSection);

            // Extract sections
            var sections = SplitSections(text);
            if (sections.TryGetValue("EXPERIENCE", out var expText))
                cv.Experiences = ParseExperiences(expText);
            if (sections.TryGetValue("EDUCATION", out var eduText))
                cv.Educations = ParseEducations(eduText);
            if (sections.TryGetValue("SKILLS", out var skillsText))
                cv.Skills = ParseSkills(skillsText);
            if (sections.TryGetValue("COMPETITIONS", out var compText))
                cv.Competitions = ParseCompetitions(compText);
            if (sections.TryGetValue("PROJECTS", out var projText))
                cv.Projects = ParseProjects(projText);
            return cv;
        }

    // No longer needed: ExtractTextFromPdf handled inline

        private static string? ParseName(string text)
        {
            // Try to find the first line that looks like a real name (letters, spaces, not email/section)
            var lines = text.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                // Skip empty lines, lines with @ (emails), or lines with digits
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Contains("@") || Regex.IsMatch(trimmed, "\\d"))
                    continue;
                // Heuristic: 2-4 words, all words start with uppercase, no special chars
                var words = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length >= 2 && words.Length <= 4 && Array.TrueForAll(words, w => char.IsUpper(w[0])))
                {
                    return trimmed;
                }
            }
            // fallback: first non-empty line
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                    return trimmed;
            }
            return null;
        }

        private static string? ParseEmail(string text)
        {
            var match = Regex.Match(text, @"[\w\.-]+@[\w\.-]+\.[a-zA-Z]{2,}");
            return match.Success ? match.Value : null;
        }

        // Split text into sections by headers (e.g., EXPERIENCE, EDUCATION, SKILLS, etc.)
        private static Dictionary<string, string> SplitSections(string text)
        {
            var sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var sectionHeaders = new[] { "EXPERIENCE", "EDUCATION", "SKILLS", "COMPETITIONS", "PROJECTS" };
            var lines = text.Split('\n');
            string? currentHeader = null;
            var buffer = new List<string>();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (Array.Exists(sectionHeaders, h => trimmed.ToUpper().StartsWith(h)))
                {
                    if (currentHeader != null)
                        sections[currentHeader] = string.Join("\n", buffer);
                    currentHeader = trimmed.ToUpper().Split(' ')[0];
                    buffer.Clear();
                }
                else if (currentHeader != null)
                {
                    buffer.Add(line);
                }
            }
            if (currentHeader != null)
                sections[currentHeader] = string.Join("\n", buffer);
            return sections;
        }

        private static List<Experience> ParseExperiences(string text)
        {
            var experiences = new List<Experience>();
            var lines = text.Split('\n');
            Experience? current = null;
            foreach (var line in lines)
            {
                var l = line.Trim();
                if (string.IsNullOrWhiteSpace(l)) continue;
                // Heuristic: Role at Company (Period)
                var match = Regex.Match(l, @"^(?<role>.+?) at (?<company>.+?) \((?<period>.+?)\)$");
                if (match.Success)
                {
                    if (current != null) experiences.Add(current);
                    current = new Experience
                    {
                        Role = match.Groups["role"].Value.Trim(),
                        Company = match.Groups["company"].Value.Trim(),
                        Period = match.Groups["period"].Value.Trim(),
                        Description = ""
                    };
                }
                else if (current != null)
                {
                    current.Description += (current.Description == "" ? "" : " ") + l;
                }
            }
            if (current != null) experiences.Add(current);
            return experiences;
        }

        private static List<Education> ParseEducations(string text)
        {
            var educations = new List<Education>();
            var lines = text.Split('\n');
            Education? current = null;
            foreach (var line in lines)
            {
                var l = line.Trim();
                if (string.IsNullOrWhiteSpace(l)) continue;
                // Heuristic: Degree, Institution (Period)
                var match = Regex.Match(l, @"^(?<degree>.+?), (?<inst>.+?) \((?<period>.+?)\)$");
                if (match.Success)
                {
                    if (current != null) educations.Add(current);
                    current = new Education
                    {
                        Degree = match.Groups["degree"].Value.Trim(),
                        Institution = match.Groups["inst"].Value.Trim(),
                        Period = match.Groups["period"].Value.Trim(),
                        Description = ""
                    };
                }
                else if (current != null)
                {
                    current.Description += (current.Description == "" ? "" : " ") + l;
                }
            }
            if (current != null) educations.Add(current);
            return educations;
        }

        private static List<string> ParseSkills(string text)
        {
            // Split by comma or new line
            return text.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        private static List<string> ParseCompetitions(string text)
        {
            // Each line is a competition name
            return text.Split('\n')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        private static List<Project> ParseProjects(string text)
        {
            var projects = new List<Project>();
            var lines = text.Split('\n');
            Project? current = null;
            foreach (var line in lines)
            {
                var l = line.Trim();
                if (string.IsNullOrWhiteSpace(l)) continue;
                // Heuristic: Project name (bold/first line), then description
                if (current == null || !string.IsNullOrWhiteSpace(current.Description))
                {
                    if (current != null) projects.Add(current);
                    current = new Project { Name = l, Description = "" };
                }
                else if (current != null)
                {
                    current.Description += (current.Description == "" ? "" : " ") + l;
                }
            }
            if (current != null) projects.Add(current);
            return projects;
        }
    }
}
