using _004_JWT_Custom;
using _004_JWT_Custom.Service;
using _004_JWT_Custom.Service.Authorization;
using _004_JWT_Custom.Service.��Ȩ���;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(new Appsettings(builder.Configuration));

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // ���� JWT Bearer �İ�ȫ����
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "��������� 'Bearer ' ǰ׺��Token",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.OperationFilter<TokenOperationFilter>();
});

#region ע���Ȩ��Ȩ����

//��Ȩ��ط���
builder.Services.AddScoped<IAuthenticationHandlerProvider, CXLAuthenticationHandlerProvider>();
builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, CXLAuthorizationHandler>());
builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, CXLAuthorizationDelegateHandler>());
builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, CXLAuthorizationAllRequirementHandler>());
builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, CXLAuthorizationHandler>());


//��Ȩ��ط���
builder.Services.AddTransient<IAuthorizationService, CXLAuthorizationService>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, CXLAuthorizationPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandlerContextFactory, CXLAuthorizationHandlerContextFactory>();
builder.Services.AddSingleton<IAuthorizationEvaluator, CXLAuthorizationEvaluator>();

#endregion

#region JWT ��Ȩ

string Issuer = builder.Configuration["Audience:Issuer"];
string Audience = builder.Configuration["Audience:Audience"];
byte[] SecreityBytes = Encoding.UTF8.GetBytes(builder.Configuration["Audience:Secret"]);
SecurityKey securityKey = new SymmetricSecurityKey(SecreityBytes);

builder.Services.AddCXLAuthentication(options =>
{
    options.DefaultScheme = CXLConstantScheme.Scheme;
    options.DefaultForbidScheme = CXLConstantScheme.Scheme;
    options.DefaultChallengeScheme = CXLConstantScheme.Scheme;
}).AddScheme<CXLAuthenticationSchemeOptions, CXLAuthenticationHandler>(CXLConstantScheme.Scheme, options =>
{
    options.Age = 18;
    options.DisplayName = "ZWJ";
    //������
    options.Issuer = Issuer;
    options.ValidateAudience = true;
    options.Audience = Audience;
    options.ValidateIssuer = true;
    options.SecretKey = securityKey;
    options.DefualtChallageMessage = "��Ч�� Token ��δ�ҵ����ʵ� Token";
    options.RedirectUrl = "https://google.com";
    ////�Զ����Ȩ�߼�
    //options.AuthEvent += logger =>
    //{
    //    options.UseEventResult = true;
    //    return Task.FromResult(AuthenticateResult.Fail("���"));
    //};
});

#endregion

#region JWT ��Ȩ

builder.Services.AddAuthorization(options =>
{
    //���������ɫClaim
    options.AddPolicy("SystemRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CustomRole", policy => policy.RequireClaim(ClaimTypes.Role));

    //������ɫ Claim ��ֵ����Ϊ Admin
    options.AddPolicy("SystemRoleValue", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CustomRoleValue", policy => policy.RequireClaim(ClaimTypes.Role, "Admin"));

    //������ɫ Claim ��ֵ����Ϊ Admin ���� Agent
    options.AddPolicy("SystemRoleValueAnyOne", policy => policy.RequireRole("Admin", "Agent"));
    options.AddPolicy("CustomRoleValueAnyOne", policy => policy.RequireClaim(ClaimTypes.Role, "Admin", "Agent"));

    //������ɫ Claim ��ֵ������� Admin �� Agent��������ʽ�ſ���ͨ����֤
    options.AddPolicy("CustomRoleValueAll", policy => policy.RequireClaim(ClaimTypes.Role, "Admin").RequireClaim(ClaimTypes.Role, "Agent"));
    options.AddPolicy("SystemRoleValueAll", policy => policy.RequireRole("Admin").RequireRole("Agent"));

    //���� Age Claim
    options.AddPolicy("CustomAgeAndWork", policy => policy.RequireClaim("Age"));

    //���� Age Claim �� Work Claim
    options.AddPolicy("CustomAgeAndWork", policy => policy.RequireClaim("Age").RequireClaim("Work"));

    //���� Age ����ֵΪ 18
    options.AddPolicy("CustomAgeValue", policy => policy.RequireClaim("Age", "18"));

    //���� Age ����ֵΪ 18 ���� 21
    options.AddPolicy("CustomAgeValueAnyOne", policy => policy.RequireClaim("Age", "18", "21"));

    //���� StoreName ����ֵΪ Root �� Admin
    options.AddPolicy("CustomAgeValueAnyOne", policy => policy.RequireClaim("StoreName", "Root").RequireClaim("Admin"));


    //������ɫ Claim ���� ���� Claim
    options.AddPolicy("CustomRoleOrName", policy =>
    {
        policy.RequireAssertion(context =>
        {
            return context.User.HasClaim(c => c.Type == ClaimTypes.Role) ||
             context.User.HasClaim(c => c.Type == ClaimTypes.Name);
        });
    });

    //�Զ���򵥲�����Ȩ
    options.AddPolicy("CustomValidationAge", policy => policy.Requirements.Add(new CXLPermissionRequirement(18)));
    options.AddPolicy("CustomDelegateValidation", policy => policy.Requirements.Add(new CXLPermissionRequirementDelegate(options =>
    {
        //�Զ�����֤�߼�
        if (options.UserName.Equals("���޼�"))
            return true;
        return false;
    }, "���޼�", "���̽���", "������", "��������Ǭ����Ų�ƻ���")));
});

#endregion

var app = builder.Build();

CXLAuthorizationService defaultAuthorizationService = app.Services.GetService<IAuthorizationService>() as CXLAuthorizationService;

DefaultAuthorizationHandlerProvider handlerprovider = app.Services.GetService<IAuthorizationHandlerProvider>() as DefaultAuthorizationHandlerProvider;

DefaultAuthorizationEvaluator evaluator = app.Services.GetService<IAuthorizationEvaluator>() as DefaultAuthorizationEvaluator;

DefaultAuthorizationHandlerContextFactory contexfactory = app.Services.GetService<IAuthorizationHandlerContextFactory>() as DefaultAuthorizationHandlerContextFactory;

AuthorizationHandler<IAuthorizationRequirement> handler = app.Services.GetService<IAuthorizationHandler>() as AuthorizationHandler<IAuthorizationRequirement>;

PolicyEvaluator policy = app.Services.GetService<IPolicyEvaluator>() as PolicyEvaluator;

AuthorizationMiddlewareResultHandler authorizationMiddlewareResultHandler = app.Services.GetService<IAuthorizationMiddlewareResultHandler>() as AuthorizationMiddlewareResultHandler;

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();

//app.UseMiddleware<CXLAuthorizationMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
