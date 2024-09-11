using _003_JWT;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    foreach (var item in typeof(SwaggerGroupEnum).GetEnumValues())
    {
        c.SwaggerDoc(item.ToString(), new OpenApiInfo()
        {
            Description = item.ToString(),
            Title = "This is Title",
            Version = "6.6.0.6",
        });
    }

    c.DocInclusionPredicate((docName, api) =>
    {
        if (!api.TryGetMethodInfo(out MethodInfo method)) return false;
        var attr = method.DeclaringType
        .GetCustomAttributes(true)
        .OfType<SwaggerGroupAttribute>()
        .FirstOrDefault() ?? method.GetCustomAttributes(true)
            .OfType<SwaggerGroupAttribute>()
            .FirstOrDefault();

        if (attr == null && docName == "Development")
        {
            return true;
        }
        else if (attr?.Group.ToString() == docName)
        {
            return true;
        }
        else
        {
            return false;
        }
    });

    // ����С��
    c.OperationFilter<AddResponseHeadersFilter>();
    c.OperationFilter<AppendAuthorizeToSummaryOperationFilter>();

    c.DocumentFilter<CustomSwaggerDocumentFilter>();

    // ��header�����token�����ݵ���̨
    c.OperationFilter<SecurityRequirementsOperationFilter>();

    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {

        Description = "JWT��Ȩ(���ݽ�������ͷ�н��д���) ֱ�����¿�������Bearer {token}��ע������֮����һ���ո�\"",
        Name = "Authorization",//jwtĬ�ϵĲ�������
        In = ParameterLocation.Header,//jwtĬ�ϴ��Authorization��Ϣ��λ��(����ͷ��)
        Type = SecuritySchemeType.ApiKey
    });
});

builder.Services.AddSingleton(new Appsettings(builder.Configuration));

#region JWT ��Ȩ

builder.Services.AddAuthenticationCore(options =>
{
    options.DefaultAuthenticateScheme = "Bearer";
});

//ע���Ȩ������Ӽ�Ȩ����
//ע�� Scheme Ϊ Options
//��Ҫ���� AddAuthenticationCore
builder.Services.AddAuthentication(options =>
{
    //Ĭ�ϵĲ���
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    //
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    //Ĭ�ϵ���Ȩ����
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    //Ĭ�ϵ���Ȩ����
    options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
})
/**
 * ע�� IConfigureOptions<JwtBearerOptions>, JwtBearerConfigureOptions
 * ע�� IPostConfigureOptions<JwtBearerOptions>, JwtBearerPostConfigureOptions
 * AddScheme<JwtBearerOptions, JwtBearerHandler> //�� Scheme Ԫ���������� SchemeName + JwtBearerHandler
 */
.AddJwtBearer(options =>
{
    string Issuer = builder.Configuration["JWT:Issuer"];
    string Audience = builder.Configuration["JWT:Audience"];
    byte[] SecreityBytes = Encoding.UTF8.GetBytes(builder.Configuration["JWT:SecretKey"]);
    SecurityKey securityKey = new SymmetricSecurityKey(SecreityBytes);

    //��ֵ�� AuthenticationSchemeBuilder ����ӵ� _schemes ������
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        //�Ƿ���֤ Issuer(������)
        ValidateIssuer = true,
        //������
        ValidIssuer = Issuer,
        //�Ƿ���֤ Audience(������)
        ValidateAudience = true,
        //������
        ValidAudience = Audience,
        //�Ƿ���֤ SecreityKey
        ValidateIssuerSigningKey = true,
        //SecreityKey
        IssuerSigningKey = securityKey,
        //�Ƿ���֤����ʱ��
        ValidateLifetime = true,
        //�����ݴ�ʱ�䣬�����������ʱ�䲻ͬ��������(��)
        ClockSkew = TimeSpan.FromSeconds(30),
        //�Ƿ�Ҫ�����ʱ��
        RequireExpirationTime = true,
    };
    options.Events = new JwtBearerEvents()
    {
        //�������֤�Ĺ������κ�ʧ�ܶ���ִ�дδ��¼�
        //�������ڴ��������֤ʱ���ֵ��쳣
        OnAuthenticationFailed = async context =>
        {
            var Logger = new LoggerFactory().CreateLogger("Authorization");
            Logger.LogError(context.Exception.Message);
            await Task.CompletedTask;
        },
        //�����֤ʧ�ܺ�Authentication ʧ�ܣ���Ȩʧ��
        //��������Ӧ 401 ���͵���Ϣ�����߶Է���ǰ�����֤ʧ����
        OnChallenge = async context =>
        {
            await context.Response.WriteAsync("�����֤ʧ��");
            await context.Response.WriteAsync("�޷�����ԴȨ�� 401");
        },
        //�����֤ʧ�ܺ�Authorization ʧ�ܣ���Ȩʧ��
        //�����Զ��� 403 ���ص���Ӧ
        OnForbidden = async context =>
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("�޷�����ԴȨ�� 403");
        },
        //���ȴ������¼�
        //��������ȡ Token ��Ϣ
        OnMessageReceived = async context =>
        {
            await Task.CompletedTask;
        },
        // Token ���Ʊ���֤�ɹ���
        //�������ִ��һЩ���� Token ���ƵĲ�����������ȡ�û���Ϣ
        OnTokenValidated = async context =>
        {
            await Task.CompletedTask;
        }
    };
});

//ע����Ȩ����
/**
 * ���� AddAuthorizationCore
 * 
 */
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ClientResource", policy => policy.RequireRole("Client").Build());//������ɫ
    options.AddPolicy("AdminResource", policy => policy.RequireRole("Admin").Build());
    options.AddPolicy("SystemOrAdmin", policy => policy.RequireRole("Admin", "System"));//��Ĺ�ϵ
    options.AddPolicy("SystemAndAdmin", policy => policy.RequireRole("Admin").RequireRole("System"));//�ҵĹ�ϵ
});

#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "/swagger/{documentName}/swagger.json";
    });
    app.UseSwaggerUI(options =>
    {
        //options.RoutePrefix = "";
        foreach (var item in typeof(SwaggerGroupEnum).GetEnumValues())
        {
            options.SwaggerEndpoint($"/swagger/{item}/swagger.json", $"{item}");
        }
    });
}

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
