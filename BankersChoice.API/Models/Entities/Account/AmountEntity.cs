using System;
using System.Security.Cryptography.X509Certificates;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BankersChoice.API.Models.Entities.Account
{
    [BsonDiscriminator(RootClass = true)]
    [BsonKnownTypes(typeof(GalacticCurrencyStandardEntity), typeof(BluCoinCurrencyEntity))]
    public abstract class AmountEntity
    {
        public abstract Decimal AmountInUSD();

        [BsonRepresentation(BsonType.String)]
        public CurrencyEnum Currency { get; set; }

        public AmountEntity Add(AmountEntity amountEntity)
        {
            switch (amountEntity)
            {
                case GalacticCurrencyStandardEntity galacticCurrencyStandardEntity when this is GalacticCurrencyStandardEntity thisGcs:
                    return new GalacticCurrencyStandardEntity()
                    {
                        Currency = CurrencyEnum.GSC,
                        Amount = galacticCurrencyStandardEntity.Amount + thisGcs.Amount
                    };
                case BluCoinCurrencyEntity bluCoinCurrencyEntity when this is BluCoinCurrencyEntity thisBlc:
                    return new BluCoinCurrencyEntity()
                    {
                        Currency = CurrencyEnum.BLC,
                        Tuns = bluCoinCurrencyEntity.Tuns + thisBlc.Tuns,
                        Scraposts = bluCoinCurrencyEntity.Scraposts + thisBlc.Scraposts,
                        Katts = bluCoinCurrencyEntity.Katts + thisBlc.Katts,
                        Kibels = bluCoinCurrencyEntity.Kibels + thisBlc.Kibels
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(amountEntity));
            }
        }

        public AmountEntity Subtract(AmountEntity amountEntity)
        {
            switch (amountEntity)
            {
                case GalacticCurrencyStandardEntity galacticCurrencyStandardEntity when this is GalacticCurrencyStandardEntity thisGcs:
                    return new GalacticCurrencyStandardEntity()
                    {
                        Currency = CurrencyEnum.GSC,
                        Amount = thisGcs.Amount - galacticCurrencyStandardEntity.Amount
                    };
                case BluCoinCurrencyEntity bluCoinCurrencyEntity when this is BluCoinCurrencyEntity thisBlc:
                    return new BluCoinCurrencyEntity()
                    {
                        Currency = CurrencyEnum.BLC,
                        Tuns =  thisBlc.Tuns - bluCoinCurrencyEntity.Tuns,
                        Scraposts = thisBlc.Scraposts - bluCoinCurrencyEntity.Scraposts,
                        Katts = thisBlc.Katts - bluCoinCurrencyEntity.Katts,
                        Kibels = thisBlc.Kibels - bluCoinCurrencyEntity.Kibels
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(amountEntity));
            }
        }

        public AmountEntity RealCopy()
        {
            switch (this)
            {
                case GalacticCurrencyStandardEntity galacticCurrencyStandardEntity:
                    return new GalacticCurrencyStandardEntity()
                    {
                        Currency = this.Currency,
                        Amount = galacticCurrencyStandardEntity.Amount
                    };
                case BluCoinCurrencyEntity bluCoinCurrencyEntity:
                    return new BluCoinCurrencyEntity()
                    {
                        Currency = this.Currency,
                        Tuns = bluCoinCurrencyEntity.Tuns,
                        Scraposts = bluCoinCurrencyEntity.Scraposts,
                        Katts = bluCoinCurrencyEntity.Katts,
                        Kibels = bluCoinCurrencyEntity.Kibels
                    };
                default:
                    throw new ArgumentOutOfRangeException(this.GetType().Name);
            }
        }

        public bool AreEqual(AmountEntity compareAmountEntity)
        {
            switch (this)
            {
                case GalacticCurrencyStandardEntity galacticCurrencyStandardEntity when compareAmountEntity is GalacticCurrencyStandardEntity compareGcsEntity:
                    return galacticCurrencyStandardEntity.Amount == compareGcsEntity.Amount;
                case BluCoinCurrencyEntity bluCoinCurrencyEntity when compareAmountEntity is BluCoinCurrencyEntity compareBlcEntity:
                    return bluCoinCurrencyEntity.Tuns == compareBlcEntity.Tuns &&
                           bluCoinCurrencyEntity.Scraposts == compareBlcEntity.Scraposts &&
                           bluCoinCurrencyEntity.Katts == compareBlcEntity.Katts &&
                           bluCoinCurrencyEntity.Kibels == compareBlcEntity.Kibels;
                default:
                    throw new ArgumentOutOfRangeException(this.GetType().Name);
            }
        }
    }

    public class GalacticCurrencyStandardEntity : AmountEntity
    {
        public long Amount { get; set; }

        public override decimal AmountInUSD()
        {
            return new decimal(Amount * 0.05);
        }
    }

    public class BluCoinCurrencyEntity : AmountEntity
    {
        public long Tuns { get; set; }
        public long Scraposts { get; set; }
        public long Katts { get; set; }
        public long Kibels { get; set; }
        public override decimal AmountInUSD()
        {
            var totalTuns = Tuns + (Scraposts / 31m) + ((Katts / 8m) / 31m) + (((Kibels / 14m) / 8m) / 31m);
            return totalTuns / 9.5m;
        }
    }
}