using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BankersChoice.API.Models.Entities.Account;
using JsonSubTypes;
using Newtonsoft.Json;

namespace BankersChoice.API.Models.ApiDtos
{
    [JsonConverter(typeof(JsonSubtypes), nameof(AmountInDto.Currency))]
    [JsonSubTypes.JsonSubtypes.KnownSubType(typeof(GalacticCurrencyStandardInDto), CurrencyEnum.GSC)]
    [JsonSubTypes.JsonSubtypes.KnownSubType(typeof(BluCoinCurrencyInDto), CurrencyEnum.BLC)]
    public abstract class AmountInDto: IValidatableObject
    {
        [Required]
        public CurrencyEnum Currency { get; set; }

        public AmountEntity ToEntity()
        {
            return this switch
            {
                GalacticCurrencyStandardInDto galacticCurrencyStandardOutDto => new GalacticCurrencyStandardEntity()
                {
                    Currency = this.Currency,
                    Amount = galacticCurrencyStandardOutDto.Amount.Value
                },
                BluCoinCurrencyInDto bluCoinCurrencyOutDto => new BluCoinCurrencyEntity()
                {
                    Currency = this.Currency,
                    Tuns = bluCoinCurrencyOutDto.Tuns.Value,
                    Scraposts = bluCoinCurrencyOutDto.Scraposts.Value,
                    Katts = bluCoinCurrencyOutDto.Katts.Value,
                    Kibels = bluCoinCurrencyOutDto.Kibels.Value
                },
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public abstract IEnumerable<ValidationResult> Validate(ValidationContext validationContext);
    }
}