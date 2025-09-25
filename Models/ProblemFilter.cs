namespace ScspApi.Models
{
    public class ProblemFilter
    {
        public ProblemStatus? Status { get; set; } 
        public string? City { get; set; } 
        public string? State { get; set; } 
        public int Page { get; set; } = 1; // Página atual
        public int PageSize { get; set; } = 10; // Quantidade de resultados por página
    }
}
