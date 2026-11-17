using TodoApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ToDoDbContext>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
}); builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});
var app = builder.Build();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();
var hasher = new PasswordHasher<User>();
app.MapPost("/register", async (ToDoDbContext db, User user) =>
{
    if (await db.Users.AnyAsync(u => u.Username == user.Username))        return Results.BadRequest("שם המשתמש כבר קיים");
    user.Password = hasher.HashPassword(user, user.Password);
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Ok(new { token = CreateToken(user, builder.Configuration) });

});
app.MapPost("/login", async (ToDoDbContext db, User loginUser) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == loginUser.Username);
    if (user is null)
        return Results.Unauthorized();

    var result = hasher.VerifyHashedPassword(user, user.Password, loginUser.Password);
    if (result == PasswordVerificationResult.Failed)
        return Results.Unauthorized();
    return Results.Ok(new { token = CreateToken(user, builder.Configuration) });
});
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/items", async (ToDoDbContext db) =>
         await db.Items.ToListAsync()).RequireAuthorization();

app.MapPost("/items", async (ToDoDbContext db, Item newItem) =>
{
    db.Items.Add(newItem);
    await db.SaveChangesAsync();

    return Results.Created($"/items/{newItem.Id}", newItem);
}).RequireAuthorization();
app.MapPut("/items/{id}", async (int id, ToDoDbContext db, Item updatedItem) =>
{
    var item = await db.Items.FindAsync(id);

    if (item is null) return Results.NotFound();

    if (updatedItem.Name is not null)
        item.Name = updatedItem.Name;
    if (updatedItem.IsComplete is not null)
        item.IsComplete = updatedItem.IsComplete;

    await db.SaveChangesAsync();

    return Results.Ok(item);
}).RequireAuthorization();
app.MapDelete("/items/{id}", async (int id, ToDoDbContext db) =>
{
    if (await db.Items.FindAsync(id) is Item item)
    {
        db.Items.Remove(item);
        await db.SaveChangesAsync();
        return Results.Ok(item);
    }

    return Results.NotFound();
}).RequireAuthorization();

app.Run();
string CreateToken(User user, IConfiguration config)
{
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username)
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: config["Jwt:Issuer"],
        audience: config["Jwt:Audience"],
        claims: claims,
        expires: DateTime.Now.AddHours(5),
        signingCredentials: creds);

    return new JwtSecurityTokenHandler().WriteToken(token);
}