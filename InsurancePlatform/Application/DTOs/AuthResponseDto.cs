namespace Application.DTOs
{
    public class AuthResponseDto
    {
        public string? Status { get; set; }
        public string? Message { get; set; }
        public string? Token { get; set; }
        public DateTime? Expiration { get; set; }
        public string? Role { get; set; }
        public string? Email { get; set; }
        public string? Id { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
