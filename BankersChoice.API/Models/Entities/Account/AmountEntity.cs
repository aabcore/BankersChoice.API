using System;
using System.Security.Cryptography.X509Certificates;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BankersChoice.API.Models.Entities.Account
{
    [BsonDiscriminator(RootClass = true)]
    [BsonKnownTypes(typeof(GalacticCurrencyStandardEntity), typeof(WizardingCurrencyEntity))]
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
                case WizardingCurrencyEntity wizardingCurrencyEntity when this is WizardingCurrencyEntity thisWizarding:
                    return new WizardingCurrencyEntity()
                    {
                        Currency = CurrencyEnum.WC,
                        Galleons = wizardingCurrencyEntity.Galleons + thisWizarding.Galleons,
                        Sickles = wizardingCurrencyEntity.Sickles + thisWizarding.Sickles,
                        Knuts = wizardingCurrencyEntity.Knuts + thisWizarding.Knuts
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
                case WizardingCurrencyEntity wizardingCurrencyEntity when this is WizardingCurrencyEntity thisWizarding:
                    return new WizardingCurrencyEntity()
                    {
                        Currency = CurrencyEnum.WC,
                        Galleons = thisWizarding.Galleons - wizardingCurrencyEntity.Galleons,
                        Sickles = thisWizarding.Sickles - wizardingCurrencyEntity.Sickles,
                        Knuts = thisWizarding.Knuts - wizardingCurrencyEntity.Knuts
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
                case WizardingCurrencyEntity wizardingCurrencyEntity:
                    return new WizardingCurrencyEntity()
                    {
                        Currency = this.Currency,
                        Galleons = wizardingCurrencyEntity.Galleons,
                        Sickles = wizardingCurrencyEntity.Sickles,
                        Knuts = wizardingCurrencyEntity.Knuts
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
                case WizardingCurrencyEntity wizardingCurrencyEntity when compareAmountEntity is WizardingCurrencyEntity compareWizardingEntity:
                    return wizardingCurrencyEntity.Galleons == compareWizardingEntity.Galleons &&
                           wizardingCurrencyEntity.Sickles == compareWizardingEntity.Sickles &&
                           wizardingCurrencyEntity.Knuts == compareWizardingEntity.Knuts;
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

    public class WizardingCurrencyEntity : AmountEntity
    {
        public long Galleons { get; set; }
        public long Sickles { get; set; }
        public long Knuts { get; set; }

        public override decimal AmountInUSD()
        {
            var totalGalleons = Galleons + (Sickles / 17m) + ((Knuts / 29m) / 17m);
            return totalGalleons / 6.3m;
        }
    }
}