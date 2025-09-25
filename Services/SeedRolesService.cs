using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace ScspApi.Services
{
    public class SeedRolesService
    {
        public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            if (await roleManager.FindByNameAsync("Desenvolvedor") == null)
            {
                await roleManager.CreateAsync(new IdentityRole("Desenvolvedor"));
            }

            if (await roleManager.FindByNameAsync("Admin") == null)
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            if (await roleManager.FindByNameAsync("AdminRegional") == null)
            {
                await roleManager.CreateAsync(new IdentityRole("AdminRegional"));
            }

            if (await roleManager.FindByNameAsync("Usuario") == null)
            {
                await roleManager.CreateAsync(new IdentityRole("Usuario"));
            }

            if (await roleManager.FindByNameAsync("Verificado") == null)
            {
                await roleManager.CreateAsync(new IdentityRole("Verificado"));
            }

            if (await roleManager.FindByNameAsync("Testador") == null)
            {
                await roleManager.CreateAsync(new IdentityRole("Testador"));
            }
        }
    }
}

