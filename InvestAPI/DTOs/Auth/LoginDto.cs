using System.ComponentModel.DataAnnotations;

namespace InvestAPI.DTOs.Auth
{
    public class LoginDto
    {

        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [StringLength(150, ErrorMessage = "O email deve ter no máximo 150 caracteres")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "A senha é obrigatória")]
        [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres")]
        [StringLength(100, ErrorMessage = "A senha deve ter no máximo 100 caracteres")]
        public required string Password { get; set; }
    }
}