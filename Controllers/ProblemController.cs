using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.X509;
using ScspApi.Data;
using ScspApi.Helpers;
using ScspApi.Infrastructure;
using ScspApi.Models;
namespace ScspApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProblemController : ApiControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<User> _userManager;

        public ProblemController(ApplicationDbContext context, IWebHostEnvironment env, UserManager<User> userManager)
        {
            _context = context;
            _env = env;
            _userManager = userManager;
        }

        // TODO: Mover para um helper/util
        public static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Obrigado stackoverflow
            const double R = 6371; // Raio da Terra em quilômetros
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c; // Distância em quilômetros
        }

        private static double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        [Authorize]
        [HttpPost("report")]
        public async Task<ActionResult<ApiResponse<object>>> ReportProblem([FromForm] Problem problem, [FromForm] IFormFile? file)
        {
            if (problem == null)
                return ApiBadRequest("Problema inválido.");

            if(file == null)
                return ApiBadRequest("Imagem inválida.");

            if (file.Length > 10 * 1024 * 1024)
                return ApiBadRequest("O arquivo é muito grande. O tamanho máximo permitido é 10MB.");

            var imageHelper = new ImageHelper(_env);
            if (!ImageHelper.IsValidImage(file.FileName, file.OpenReadStream()))
                return ApiBadRequest("Formato de imagem inválido. Apenas JPG, PNG e GIF são permitidos.");

            if (problem.Latitude == 0 || problem.Longitude == 0)
                return ApiBadRequest("Localização inválida.");

            if(string.IsNullOrWhiteSpace(problem.City) || string.IsNullOrWhiteSpace(problem.State))
            {
                // Tentar preencher cidade e estado via google maps mais tarde
            }

            // Verificar problemas semelhantes nas proximidades (dentro de 40 metros)
            var radiusInKm = 0.04; // 40 metros
            var problemsNearby = await _context.Problems
                .Where(p => p.Status == ProblemStatus.Open && p.Type == problem.Type)
                .Where(p => HaversineDistance(p.Latitude, p.Longitude, problem.Latitude, problem.Longitude) <= radiusInKm)
                .ToListAsync();

            if (problemsNearby.Count > 0)
                return ApiBadRequest("Problema semelhante já reportado nas proximidades.");

            var userId = User.FindFirst("UserId")?.Value;

            if (userId == null)
                return ApiUnauthorized("Usuário não autenticado.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ApiNotFound("Usuário não encontrado.");

            problem.UserId = user.Id;
            problem.UserIdSolved = "";
            problem.CreatedAt = DateTime.UtcNow;
            problem.Status = ProblemStatus.Open;
            
            problem.ImageUrl = await imageHelper.SaveImageAsync(file, "problems"); 

            _context.Problems.Add(problem);
            await _context.SaveChangesAsync();

            return ApiOk((object)problem, "Problema reportado com sucesso.");
        }

        [Authorize]
        [HttpGet("get-all")]
        public async Task<ActionResult<ApiResponse<object>>> GetAllProblems([FromQuery] ProblemFilter filter)
        {
            var query = _context.Problems.AsQueryable();

            // Filtros
            if (filter.Status.HasValue)
                query = query.Where(p => p.Status == filter.Status);

            if (!string.IsNullOrWhiteSpace(filter.City))
                query = query.Where(p => p.City.ToLower().Contains(filter.City.ToLower()));

            if (!string.IsNullOrWhiteSpace(filter.State))
                query = query.Where(p => p.State.ToLower().Contains(filter.State.ToLower()));

            // Paginação
            var totalItems = await query.CountAsync();
            var problems = await query
                .Skip((filter.Page - 1) * filter.PageSize) 
                .Take(filter.PageSize) 
                .ToListAsync();

            // Retornar total de itens e a lista paginada
            var response = new
            {
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling((double)totalItems / filter.PageSize),
                CurrentPage = filter.Page,
                Problems = problems
            };

            return ApiOk((object)response, "Dados dos problemas.");
        }

        // Obter um problema específico
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> GetProblem(int id)
        {
            var problem = await _context.Problems.FindAsync(id);
            if (problem == null)
                return ApiNotFound("Problema não encontrado.");

            return ApiOk((object)problem, "Dados do problema.");
        }

        // Atualizar o status de um problema (ex: Resolver)
        [Authorize]
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateProblemStatus(int id, [FromBody] ProblemStatus status, [FromForm] IFormFile? statusImage)
        {
            var problem = await _context.Problems.FindAsync(id);
            if (problem == null)
                return ApiNotFound("Problema não encontrado.");

            // Não confiar no ID do usuario que vem no formulario, pegar direto do token logado.
            var userId = User.FindFirst("UserId")?.Value; 
            if (string.IsNullOrEmpty(userId))
                return ApiUnauthorized("Usuário não autenticado.");

            if(statusImage == null)
                return ApiBadRequest("Imagem inválida.");

            problem.Status = status;

            if (status == ProblemStatus.Solved){
                problem.SolvedAt = DateTime.UtcNow;
                problem.UserIdSolved = userId; 

                var imageHelper = new ImageHelper(_env); 
                problem.ImageSolvedUrl = await imageHelper.SaveImageAsync(statusImage, "solved"); 
            }
            else if (status == ProblemStatus.Recurring)
            {
                problem.RecurringAt = DateTime.UtcNow;
                problem.UserIdRecurring = userId; 

                var imageHelper = new ImageHelper(_env);
                problem.ImageRecurringUrl = await imageHelper.SaveImageAsync(statusImage, "recurring"); 
            }

            await _context.SaveChangesAsync();

            return ApiMessage("Status do problema atualizado.");
        }

        // Votação em um problema (Upvote / Downvote)
        [Authorize]
        [HttpPost("{id}/vote")]
        public async Task<ActionResult<ApiResponse<object>>> VoteProblem(int id, [FromBody] int voteValue)
        {
            var problem = await _context.Problems.FindAsync(id);
            if (problem == null)
                return ApiNotFound("Problema não encontrado.");

            if (voteValue == 1){
                problem.Upvotes++;
            }
            else if (voteValue == -1)
            {
                problem.Downvotes++;
            }

            await _context.SaveChangesAsync();
            return ApiOk((object)problem);
        }

        // Endpoint para obter a resposta oficial
        [HttpGet("{id}/official-response")]
        public async Task<ActionResult<ApiResponse<object>>> GetOfficialResponse(int id)
        {
            var problem = await _context.Problems.FindAsync(id);
            if (problem == null)
                return ApiNotFound("Problema não encontrado.");

            var response = new
            {
                problem.OfficialResponse,
                problem.OfficialResponseAt,
                problem.OfficialResponderId
            };

            return ApiOk((object)response, "Resposta oficial obtida com sucesso.");
        }

        // Atualizar a resposta oficial
        [HttpPut("{id}/official-response")]
        [Authorize(Roles = "Desenvolvedor,Admin,AdminRegional,Verificado")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateOfficialResponse(int id, [FromBody] OfficialResponseModel model)
        {
            var problem = await _context.Problems.FindAsync(id);
            if (problem == null)
                return ApiNotFound("Problema não encontrado.");

            var userId = User.FindFirst("UserId")?.Value; 
            if (string.IsNullOrEmpty(userId))
                return ApiUnauthorized("Usuário não autenticado.");

            // Atualiza os dados da resposta oficial
            problem.OfficialResponse = model.Response;
            problem.OfficialResponseAt = DateTime.UtcNow;  
            problem.OfficialResponderId = userId; 

            await _context.SaveChangesAsync();

            return ApiMessage("Resposta oficial atualizada com sucesso.");
        }
    }
}
