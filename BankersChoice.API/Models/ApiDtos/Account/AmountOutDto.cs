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
    [JsonSubTypes.JsonSubtypes.KnownSubType(typeof(WizardingCurrencyOutDto), CurrencyEnum.WC)]
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
                WizardingCurrencyEntity wizardingCurrencyEntity => new WizardingCurrencyOutDto()
                {
                    Currency = amountEntity.Currency,
                    Galleons = wizardingCurrencyEntity.Galleons,
                    Sickles = wizardingCurrencyEntity.Sickles,
                    Knuts = wizardingCurrencyEntity.Knuts,
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
                WizardingCurrencyOutDto wizardingCurrencyOutDto => new WizardingCurrencyEntity()
                {
                    Currency = this.Currency,
                    Galleons = wizardingCurrencyOutDto.Galleons,
                    Sickles = wizardingCurrencyOutDto.Sickles,
                    Knuts = wizardingCurrencyOutDto.Knuts,
                },
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public class GalacticCurrencyStandardOutDto : AmountOutDto
    {
        public long Amount { get; set; }
    }

    public class WizardingCurrencyOutDto : AmountOutDto
    {
        public long Galleons { get; set; }
        public long Sickles { get; set; }
        public long Knuts { get; set; }
    }

    //public class AmountConverter : JsonConverter
    //{
    //    public override bool CanConvert(Type objectType)
    //    {
    //        return objectType == typeof(AmountInDto);
    //    }

    //    public override bool CanWrite
    //    {
    //        get { return false; }
    //    }

    //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //    {
    //        var token = JObject.Load(reader);
    //        var currencyTokenStr = token.GetValue("currency", StringComparison.OrdinalIgnoreCase)?.Value<string>();
    //        if (string.IsNullOrWhiteSpace(currencyTokenStr))
    //        {
    //            throw new JsonSerializationException("Missing Currency property");
    //        }

    //        Enum.TryParse(typeof(CurrencyEnum), currencyTokenStr, out var outEnum);
    //        if (outEnum is CurrencyEnum currencyEnum)
    //        {
    //            Type subType;
    //            switch (currencyEnum)
    //            {
    //                case CurrencyEnum.GSC:
    //                    subType = typeof(GalacticCurrencyStandardOutDto);
    //                    break;
    //                case CurrencyEnum.WC:
    //                    subType = typeof(WizardingCurrencyOutDto);
    //                    break;
    //                default:
    //                    throw new JsonSerializationException("Unknown currency type");
    //            }


    //            if (existingValue == null || existingValue.GetType() != subType)
    //            {
    //                var contract = serializer.ContractResolver.ResolveContract(subType);
    //                existingValue = contract.DefaultCreator();
    //            }

    //            using (var subReader = token.CreateReader())
    //            {
    //                // Using "populate" avoids infinite recursion.
    //                serializer.Populate(subReader, existingValue);
    //            }

    //            return existingValue;
    //        }
    //        else
    //        {
    //            throw new JsonSerializationException("Invalid currency type");
    //        }
    //    }

    //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}