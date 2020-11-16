using System;
using BankersChoice.API.Controllers;
using BankersChoice.API.Models.Entities.Account;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BankersChoice.API.Models.ApiDtos.Account
{
    [JsonConverter(typeof(JsonSubTypes.JsonSubtypes), nameof(AmountOutDto.Currency))]
    [JsonSubTypes.JsonSubtypes.KnownSubType(typeof(GalacticCurrencyStandardOutDto), CurrencyEnum.GSC)]
    [JsonSubTypes.JsonSubtypes.KnownSubType(typeof(BluCoinCurrencyOutDto), CurrencyEnum.BLC)]
    public abstract class AmountOutDto
    {
        public CurrencyEnum Currency { get; set; }

        public static AmountOutDto EntityToOutDto(AmountEntity amountEntity)
        {
            return amountEntity switch
            {
                GalacticCurrencyStandardEntity galacticCurrencyStandardEntity => new GalacticCurrencyStandardOutDto()
                {
                    Amount = galacticCurrencyStandardEntity.Amount, Currency = amountEntity.Currency
                },
                BluCoinCurrencyEntity bluCoinCurrencyEntity => new BluCoinCurrencyOutDto()
                {
                    Currency = amountEntity.Currency,
                    Tuns = bluCoinCurrencyEntity.Tuns,
                    Scraposts = bluCoinCurrencyEntity.Scraposts,
                    Katts = bluCoinCurrencyEntity.Katts,
                    Kibels = bluCoinCurrencyEntity.Kibels
                },

                _ => throw new ArgumentOutOfRangeException(nameof(amountEntity))
            };
        }

        public AmountEntity ToEntity()
        {
            return this switch
            {
                GalacticCurrencyStandardOutDto galacticCurrencyStandardOutDto => new GalacticCurrencyStandardEntity()
                {
                    Currency = this.Currency, Amount = galacticCurrencyStandardOutDto.Amount
                },
                BluCoinCurrencyOutDto blueCoinCurrencyOutDto => new BluCoinCurrencyEntity()
                {
                    Currency = this.Currency,
                    Tuns = blueCoinCurrencyOutDto.Tuns,
                    Scraposts = blueCoinCurrencyOutDto.Scraposts,
                    Katts = blueCoinCurrencyOutDto.Katts,
                    Kibels = blueCoinCurrencyOutDto.Kibels
                },
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}