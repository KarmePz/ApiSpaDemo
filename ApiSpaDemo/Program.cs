using Microsoft.EntityFrameworkCore;
using AutoMapper;
using ApiSpaDemo.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ApiSpaDemo
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers().AddNewtonsoftJson();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddDbContext<ApiSpaDbContext>(opt =>
                opt.UseSqlServer(builder.Configuration.GetConnectionString("cadenaSql")));

            builder.Services.AddCors(opt =>
            {
                opt.AddPolicy("PermitirTodo", builder => {builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                });
            });

            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Events = new CookieAuthenticationEvents
                    {
                        OnRedirectToLogin = context =>
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.SlidingExpiration = true;
                options.LoginPath = "/api/Account/login";
                options.LogoutPath = "/api/Account/logout";
            });

            builder.Services.AddAuthorization();
            builder.Services.AddIdentity<Usuario, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireDigit = false;
                options.Password.RequireUppercase = false;
            })
            .AddEntityFrameworkStores<ApiSpaDbContext>()
            .AddDefaultTokenProviders();

            var app = builder.Build();

            //if (app.Environment.IsDevelopment())
            //{
            //    app.UseSwagger();
            //    app.UseSwaggerUI();
            //}
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();
            app.UseCors("PermitirTodo");
            app.UseAuthentication();
            app.UseAuthorization();

            await CreateRolesAndUsers(app);

            app.MapControllers();

            await app.RunAsync();
        }

        private static async Task CreateRolesAndUsers(WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();

                string[] roleNames = { "Admin", "Cliente", "Empleado" };
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                string email = "adminUser@thisIsAdmin.com";
                if (await userManager.FindByEmailAsync(email) == null)
                {
                    var user = new Usuario
                    {
                        UserName = "Administrator",
                        Email = email
                    };

                    await userManager.CreateAsync(user, "835NoOneAdmin047-");
                    await userManager.AddToRoleAsync(user, "Admin");
                }
            }
        }
    }
}
