using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace Application.DTOs
{
    /// <summary>
    /// This class is used when a customer wants to file a claim because something happened.
    /// It contains all details about the incident, costs, and supporting documents.
    /// </summary>
    public class RaiseClaimRequest
    {
        // The ID of the policy under which the claim is being filed
        public string PolicyApplicationId { get; set; } = string.Empty;
        
        // What happened? (e.g., 'Accident', 'Illness', 'Death')
        public string IncidentType { get; set; } = string.Empty;
        
        // Where did it happen?
        public string IncidentLocation { get; set; } = string.Empty;
        
        // Exact GPS coordinates of the incident
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        
        // When did it happen?
        public DateTime IncidentDate { get; set; }
        
        // A detailed story of what happened
        public string Description { get; set; } = string.Empty;
        
        // If hospital related, the name of the hospital
        public string HospitalName { get; set; } = string.Empty;
        
        // True if the person had to stay in the hospital overnight
        public bool HospitalizationRequired { get; set; }
        
        // How much money the user is asking from the insurance company
        public decimal RequestedAmount { get; set; }

        // --- Extra details for specific types of claims ---
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

        // If it's a family plan, which member is affected?
        public string? AffectedMemberName { get; set; }
        public string? AffectedMemberRelation { get; set; }

        // Photos or PDFs of bills, prescriptions, and reports
        public List<IFormFile>? Documents { get; set; }
    }
}
