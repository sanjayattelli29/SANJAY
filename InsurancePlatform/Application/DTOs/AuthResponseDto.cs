namespace Application.DTOs
{
    // this class is for the reply after user tries to login or register
    public class AuthResponseDto
    {
        // "Success" or "Error"
        public string? Status { get; set; }
        // message like "Login successful"
        public string? Message { get; set; }
        // security token for the user
        public string? Token { get; set; }
        // when the token expires
        public DateTime? Expiration { get; set; }
        // user role like Admin or Customer
        public string? Role { get; set; }
        // user email id
        public string? Email { get; set; }
        // unique id of the user
        public string? Id { get; set; }
        // full name of the user
        public string? FullName { get; set; }
        // mobile number of the user
        public string? PhoneNumber { get; set; }
    }
}
