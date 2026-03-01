namespace Application.DTOs // dto namespace
{
    // this class is for the reply after user tries to login or register
    public class AuthResponseDto
    {
        // "Success" or "Error"
        public string? Status { get; set; }
        // message like "Login successful"
        public string? Message { get; set; }
        // security token for the user
        // this is the jwt string
        public string? Token { get; set; }
        // when the token expires
        public DateTime? Expiration { get; set; }
        // user role like Admin or Customer
        // important for frontend routing
        public string? Role { get; set; }
        // user email id
        public string? Email { get; set; }
        // unique id of the user
        // guid string from db
        public string? Id { get; set; }
        // full name of the user
        public string? FullName { get; set; }
        // mobile number of the user
        // for contact purpose
        public string? PhoneNumber { get; set; }
    }
}
// auth result object end
