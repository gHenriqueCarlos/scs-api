using Microsoft.AspNetCore.Identity;

namespace ScspApi.Models
{
    public class User : IdentityUser
    {
        public string FullName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastActivity { get; set; } = null;
        public string? Cpf { get; set; } = "";
        public string? Cnpj { get; set; } = "";
        public int ProblemnsReported { get; set; } = 0;
        public int ProblemnsSolved { get; set; } = 0;

        // Eu iria usar os roles diretamente, mas ai não teria como especificar um motivo do banimento tendo que salvar de qualquer forma.
        public bool IsBanned { get; set; } = false;
        public string? BanReason { get; set; } = null;
    }
}
