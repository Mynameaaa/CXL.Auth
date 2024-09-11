using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


#region ��������֤�м��

//��������֤�м������ AddAuthentication����ʹ�� AddCookie ����ע�� cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme/* ʹ�� Cookie ���Խ��������֤ */)
    .AddCookie(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromMinutes(2);
        options.SlidingExpiration = true;
        options.AccessDeniedPath = null;
    });

#endregion


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

/* ��Ȩ��Ȩ�м���������ʱע��˳�� */

//���ü�Ȩ�м��
app.UseAuthentication();

//������Ȩ�м��
app.UseAuthorization();

app.MapControllers();

app.Run();
