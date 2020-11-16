using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BankersChoice.API.Controllers;
using BankersChoice.API.Models;
using BankersChoice.API.Models.ApiDtos;
using BankersChoice.API.Models.ApiDtos.Account;
using BankersChoice.API.Models.ApiDtos.Transaction;
using BankersChoice.API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Converters;

namespace BankersChoice.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<DatabaseSettings>(Configuration.GetSection(nameof(DatabaseSettings)));

            services.AddSingleton<DatabaseSettings>(sp => sp.GetRequiredService<IOptions<DatabaseSettings>>().Value);
            services.AddSingleton<AccountService>();
            services.AddSingleton<UserService>();
            services.AddSingleton<TransactionService>();

            services.AddControllers()
                .AddNewtonsoftJson(o => o.SerializerSettings.Converters.Add(new StringEnumConverter()));

            services.AddSwaggerGen(c =>
            {
                c.GeneratePolymorphicSchemas(
                    discriminatorSelector: d =>
                    {
                        if (d == typeof(TransactionDetailsOutDto))
                            return "transactionType";
                        if (d == typeof(AmountOutDto))
                            return "Currency";
                        if (d == typeof(AmountInDto))
                            return "Currency";
                        return null;
                    },
                    subTypesResolver: type =>
                    {
                        if (type == typeof(TransactionDetailsOutDto))
                        {
                            return new Type[]
                            {
                                typeof(CreditTransactionDetailsOutDto),
                                typeof(DebitTransactionDetailsOutDto)
                            };
                        }

                        if (type == typeof(AmountOutDto))
                        {
                            return new Type[]
                            {
                                typeof(GalacticCurrencyStandardOutDto),
                                typeof(BluCoinCurrencyOutDto)
                            };
                        }

                        if (type == typeof(AmountInDto))
                        {
                            return new Type[]
                            {
                                typeof(GalacticCurrencyStandardInDto),
                                typeof(BluCoinCurrencyInDto)
                            };
                        }

                        return Enumerable.Empty<Type>();
                    });
            });
            services.AddSwaggerGenNewtonsoftSupport(); // Must be places after .AddSwaggerGen();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UsePathBase("/bankerschoice");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();

            app.UseSwaggerUI(c => { c.SwaggerEndpoint("v1/swagger.json", "BankersChoice API"); });

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}