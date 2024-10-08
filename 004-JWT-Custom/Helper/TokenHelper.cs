﻿using _004_JWT_Custom.Model;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace _004_JWT_Custom.Helper
{
    public class TokenHelper : ITokenHelper
    {
        private readonly RSA _privateKey;
        private readonly RSA _publicKey;
        private readonly KeyHelper _keyHelper;

        public TokenHelper()
        {
            _privateKey = RSA.Create();
            _publicKey = RSA.Create();
            _keyHelper = new KeyHelper();
        }

        public async Task<string> GenerateJwtToken(string privateKeyPath, Dictionary<string, string> dataModel = null)
        {
            // 加载私钥
            RSA privateRsa = await _keyHelper.LoadPrivateKeyFromPEM(privateKeyPath);

            // 创建 RSA 安全密钥
            var privateKey = new RsaSecurityKey(privateRsa);

            // 创建 JWT 令牌处理器
            var tokenHandler = new JwtSecurityTokenHandler();

            var claims = new List<Claim>();

            if (dataModel != null && dataModel.Any())
                foreach (var data in dataModel)
                {
                    claims.Add(new Claim(data.Key, data.Value));
                }

            // 创建令牌描述符
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims.ToArray()),
                Expires = DateTime.UtcNow.AddMinutes(Appsettings.app<int?>("TokenParameter:AccessTokenExpiration") ?? 30),  // 令牌过期时间
                SigningCredentials = new SigningCredentials(privateKey, SecurityAlgorithms.RsaSha256)  // 使用 RSA-SHA256 签名
            };

            // 生成令牌
            var token = tokenHandler.CreateToken(tokenDescriptor);

            // 返回序列化的 JWT 令牌字符串
            return tokenHandler.WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            // 使用随机生成器生成 refresh_token
            return Guid.NewGuid().ToString().Replace("-", "");
        }

        // 验证 JWT 令牌
        public async Task<ClaimsPrincipal> ValidateToken(string token, string publicKeyPath)
        {
            // 读取公钥
            _publicKey.ImportRSAPublicKey(await File.ReadAllBytesAsync(publicKeyPath), out _);

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new RsaSecurityKey(_publicKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // 可选：设置时钟偏移
            };

            return tokenHandler.ValidateToken(token, validationParameters, out _);
        }
    }
}

