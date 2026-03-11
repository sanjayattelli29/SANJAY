using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace Application.DTOs
{
    public class RaiseClaimRequest
    {
        public string PolicyApplicationId { get; set; } = string.Empty;
        public string IncidentType { get; set; } = string.Empty;
        public string IncidentLocation { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public DateTime IncidentDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string HospitalName { get; set; } = string.Empty;
        public bool HospitalizationRequired { get; set; }
        public decimal RequestedAmount { get; set; }

        public TimeSpan? IncidentTime { get; set; }
        public string? AccidentCause { get; set; }
        public bool PoliceCaseFiled { get; set; }
        public string? FirNumber { get; set; }
        public string? InjuryType { get; set; }
        public string? BodyPartInjured { get; set; }
        public DateTime? AdmissionDate { get; set; }
        public DateTime? DischargeDate { get; set; }
        public decimal EstimatedMedicalCost { get; set; }
        public decimal HospitalBill { get; set; }
        public decimal Medicines { get; set; }
        public decimal OtherExpenses { get; set; }

        // Family Info
        public string? AffectedMemberName { get; set; }
        public string? AffectedMemberRelation { get; set; }

        public List<IFormFile>? Documents { get; set; }
    }
}
