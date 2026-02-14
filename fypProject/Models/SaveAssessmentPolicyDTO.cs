using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace fypProject.Models
{
    public class SaveAssessmentPolicyDTO
    {
        public List<CloWeightageDTO> CloWeightage { get; set; }
        public List<DifficultyPolicyDTO> DifficultyPolicy { get; set; }
    }

}