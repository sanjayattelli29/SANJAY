using Application.DTOs;
using Application.Interfaces.Services;
using Application.Interfaces.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    // this handles general stuff about insurance policies
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PolicyController : ControllerBase
    {
        private readonly IPolicyManager _policyManager;
        private readonly IFileStorageService _fileStorageService;

        public PolicyController(IPolicyManager policyManager, IFileStorageService fileStorageService)
        {
            _policyManager = policyManager;
            _fileStorageService = fileStorageService;
        }

        // see the plans and category details
        [HttpGet("configuration")]
        [AllowAnonymous]
        public async Task<IActionResult> GetConfiguration()
        {
            var config = await _policyManager.GetConfigurationAsync();
            return Ok(config);
        }

        // calculate how much premium user has to pay
        [HttpPost("calculate-premium")]
        public async Task<IActionResult> CalculatePremium([FromBody] PolicyApplicationRequest request)
        {
            try
            {
                var premium = await _policyManager.CalculatePremiumAsync(request);
                return Ok(new { Premium = premium });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // customer applies for a new insurance plan
        [HttpPost("apply")]
        public async Task<IActionResult> Apply([FromBody] PolicyApplicationRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                var result = await _policyManager.ApplyForPolicyAsync(userId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("my-policies")]
        public async Task<IActionResult> GetMyPolicies()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var policies = await _policyManager.GetUserPoliciesAsync(userId);
            return Ok(policies);
        }

        // customer uploads documents for their application
        [HttpPost("submit-documents")]
        public async Task<IActionResult> SubmitDocuments([FromForm] SubmitPolicyDocumentsRequest request)
        {
            try
            {
                var result = await _policyManager.SubmitPolicyDocumentsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    
        [HttpPost("upload-document")]
        public async Task<IActionResult> UploadDocument(IFormFile file, [FromForm] string folder = "general")
        {
            if (file == null || file.Length == 0) return BadRequest(new { Message = "No file uploaded" });
            
            try
            {
                using var stream = file.OpenReadStream();
                var url = await _policyManager.UploadGeneralFileAsync(stream, file.FileName, folder);
                return Ok(new { Url = url });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        [HttpPost("upload-invoice")]
        public async Task<IActionResult> UploadInvoice([FromBody] UploadInvoiceDto dto)
        {
            try
            {
                var base64Data = dto.Base64Pdf.Contains(",")
                    ? dto.Base64Pdf.Substring(dto.Base64Pdf.IndexOf(',') + 1)
                    : dto.Base64Pdf;

                var pdfBytes = Convert.FromBase64String(base64Data);
                using var stream = new MemoryStream(pdfBytes);

                var uploadResult = await _fileStorageService.UploadFileAsync(stream, dto.FileName, "/invoices");
                
                await _policyManager.UpdateInvoiceUrlAsync(dto.ApplicationId, uploadResult.FileUrl);

                return Ok(new { invoiceUrl = uploadResult.FileUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Invoice upload failed: {ex.Message}" });
            }
        }

        [HttpPost("upload-analysis")]
        public async Task<IActionResult> UploadAnalysis([FromBody] UploadAnalysisDto dto)
        {
            try
            {
                var base64Data = dto.Base64Pdf.Contains(",")
                    ? dto.Base64Pdf.Substring(dto.Base64Pdf.IndexOf(',') + 1)
                    : dto.Base64Pdf;

                var pdfBytes = Convert.FromBase64String(base64Data);
                using var stream = new MemoryStream(pdfBytes);

                var uploadResult = await _fileStorageService.UploadFileAsync(stream, dto.FileName, "/analysis");
                
                await _policyManager.UpdateAnalysisUrlAsync(dto.ApplicationId, uploadResult.FileUrl);

                return Ok(new { analysisUrl = uploadResult.FileUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Analysis upload failed: {ex.Message}" });
            }
        }
    }
}

