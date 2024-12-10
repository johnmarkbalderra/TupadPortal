using System.Collections.Generic;
using tupadportal.Models;

namespace tupadportal.ViewModels
{
    public class DashboardViewModel
    {
        public List<Applicants> Applicants { get; set; } = new List<Applicants>();
        public List<string> Barangays { get; set; } = new List<string>();
        public List<Batch> Batches { get; set; } = new List<Batch>();
        public int TotalApplicants { get; set; } // Count of all applicants
        public int ApprovedApplicants { get; set; } // Count of approved applicants
    }




    public class BarangayApplicantCount
    {
        public string Barangay { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
