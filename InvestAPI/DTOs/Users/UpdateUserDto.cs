using System.ComponentModel.DataAnnotations;

namespace InvestAPI.DTOs.Users
{
    public class UpdateUserDto
    {
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres")]
        public string? Name { get; set; }

        [EmailAddress(ErrorMessage = "Email inválido")]
        [StringLength(150, ErrorMessage = "O email deve ter no máximo 150 caracteres")]
        public string? Email { get; set; }

        [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres")]
        [StringLength(100, ErrorMessage = "A senha deve ter no máximo 100 caracteres")]
        public string? Password { get; set; }
    }
}
