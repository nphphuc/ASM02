using ChatbotStudent.Data;
using ChatbotStudent.Data.Models;
using ChatbotStudent.Business.Services;
using ChatbotStudent.Web.Hubs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Load .env file for sensitive configuration ──────────
// Values from .env override appsettings.json
// .env is gitignored - NEVER commit it to version control
var envPath = Path.Combine(builder.Environment.ContentRootPath, "..", ".env");
if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
}
else
{
    var localEnvPath = Path.Combine(builder.Environment.ContentRootPath, ".env");
    if (File.Exists(localEnvPath))
    {
        DotNetEnv.Env.Load(localEnvPath);
    }
}

// ── Database ──────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? builder.Configuration.GetConnectionString("DefaultConnection"),
        sql =>
        {
            sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
            sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
        }));

// ── Identity & Authentication ─────────────────────────────────────────
builder.Services.AddIdentity<User, Role>(options =>
    {
        // Password settings
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;

        // Lockout
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;

        // User
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireLecturer", policy => policy.RequireRole("Lecturer", "Admin"));
    options.AddPolicy("RequireStudent", policy => policy.RequireRole("Student", "Lecturer", "Admin"));
});

// ── Configuration ─────────────────────────────────────────────────────
builder.Services.Configure<OpenAiOptions>(
    builder.Configuration.GetSection("OpenAI"));
// Map OPENAI_API_KEY env var into OpenAiOptions.ApiKey (GeminiChatClient reads from options, not http headers)
builder.Services.PostConfigure<OpenAiOptions>(options =>
{
    var envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    if (!string.IsNullOrEmpty(envKey))
        options.ApiKey = envKey;
});
builder.Services.Configure<EmbeddingServiceOptions>(
    builder.Configuration.GetSection("EmbeddingService"));
builder.Services.Configure<RagSettings>(
    builder.Configuration.GetSection("RagSettings"));
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
// Override email credentials from environment variables (loaded from .env)
builder.Services.PostConfigure<EmailSettings>(options =>
{
    var sender = Environment.GetEnvironmentVariable("EMAIL_SENDER");
    var password = Environment.GetEnvironmentVariable("EMAIL_PASSWORD");
    if (!string.IsNullOrEmpty(sender))
    {
        options.SenderEmail = sender;
        options.Username = sender;
    }
    if (!string.IsNullOrEmpty(password))
        options.Password = password;
});

// ── HTTP Clients ──────────────────────────────────────────────────────
builder.Services.AddHttpClient<IEmbeddingService, OpenAIEmbeddingService>(client =>
{
    var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                 ?? builder.Configuration["OpenAI:ApiKey"] ?? "";
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    client.BaseAddress = new Uri(builder.Configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1");
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddHttpClient<ILocalEmbeddingService, LocalEmbeddingService>(client =>
{
    var url = builder.Configuration["EmbeddingService:LocalApiUrl"] ?? "http://localhost:8000";
    client.BaseAddress = new Uri(url);
    client.Timeout = TimeSpan.FromSeconds(120);
});

builder.Services.AddHttpClient<IOpenAiChatClient, GeminiChatClient>(client =>
{
    // Gemini API: API key goes in query parameter (?key=), NOT in Authorization header
    client.BaseAddress = new Uri(
        builder.Configuration["OpenAI:BaseUrl"] ?? "https://generativelanguage.googleapis.com");
    client.Timeout = TimeSpan.FromSeconds(120);
});

// Keep OpenAiChatClient for fallback / future use (OpenRouter / OpenAI compatible APIs)
builder.Services.AddHttpClient<OpenAiChatClient>(client =>
{
    var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                 ?? builder.Configuration["OpenAI:ApiKey"] ?? "";
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.BaseAddress = new Uri(
        builder.Configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1");
    client.Timeout = TimeSpan.FromSeconds(120);
});

// ── Business Layer Services ───────────────────────────────────────────
builder.Services.AddScoped<IDocumentParserService, DocumentParserService>();
builder.Services.AddScoped<IChunkingService, ChunkingService>();
builder.Services.AddScoped<IRagService, RagService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IBenchmarkService, BenchmarkService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IOtpService, OtpService>();

// ── New Business Services (for decoupling pages from DbContext) ───────
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<ISystemConfigService, SystemConfigService>();
builder.Services.AddScoped<IDocumentManagementService, DocumentManagementService>();
builder.Services.AddScoped<IChatFeedbackService, ChatFeedbackService>();

// ── SignalR ───────────────────────────────────────────────────────────
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(30);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
});

// ── Razor Pages ───────────────────────────────────────────────────────
builder.Services.AddRazorPages();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true).AllowCredentials());
});

var app = builder.Build();

// ── Auto-migrate on startup ──────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Check if database schema is up-to-date
    var canConnect = await db.Database.CanConnectAsync();
    if (!canConnect)
    {
        // Database doesn't exist yet - create it
        Console.WriteLine("Database not found. Creating...");
        await db.Database.EnsureCreatedAsync();
    }
    else
    {
        try
        {
            // Verify schema is up-to-date by querying new columns/tables
            await db.Users.AnyAsync();
            await db.Documents.Where(d => !d.IsDeleted).AnyAsync();
        }
        catch (SqlException ex) when (ex.Number == 207 || ex.Number == 208)
        {
            // 207 = Invalid column name, 208 = Invalid object name (table missing)
            Console.WriteLine($"[Schema] Outdated schema detected: {ex.Message}");
            Console.WriteLine("Recreating database with current schema...");
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
        }
    }

    await SeedData.InitializeAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapHub<ChatHub>("/hubs/chat");

await app.RunAsync();
