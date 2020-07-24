using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using BankersChoice.API.Models.ApiDtos.Account;
using BankersChoice.API.Models.Entities.Account;
using BankersChoice.API.Models.Entities.Transaction;
using Newtonsoft.Json;

namespace BankersChoice.API.Models.ApiDtos.Transaction
{
    public class AccountTransactionsListOutDto
    {
        public AccountReferenceOutDto Account { get; set; }
        public TransactionListsOutDto Transactions { get; set; }

        public static AccountTransactionsListOutDto BuildFromEntities(AccountDetailEntity accountDetailEntity,
            ICollection<TransactionEntity> transactionEntities)
        {
            return new AccountTransactionsListOutDto()
            {
                Account = AccountReferenceOutDto.EntityToOutDto<AccountReferenceOutDto>(accountDetailEntity),
                Transactions = new TransactionListsOutDto()
                {
                    Booked = transactionEntities.Where(t => t.BookingStatus == BookingStatusEnum.BOOKED)
                        .Select(TransactionDetailsOutDto.EntityToOutDto),
                    Pending = transactionEntities.Where(t => t.BookingStatus == BookingStatusEnum.PENDING)
                        .Select(TransactionDetailsOutDto.EntityToOutDto)
                }
            };
        }
    }

    public class TransactionListsOutDto
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<TransactionDetailsOutDto> Booked { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<TransactionDetailsOutDto> Pending { get; set; }
    }

    public class CreditTransactionDetailsOutDto: TransactionDetailsOutDto
    {
        public string CreditorName { get; set; }
        public AccountReferenceOutDto CreditorAccount { get; set; }
        public DateTimeOffset? ValueDate { get; set; }
    }

    public class DebitTransactionDetailsOutDto : TransactionDetailsOutDto
    {
        public string DebtorName { get; set; }
        public AccountReferenceOutDto DebtorAccount { get; set; }
    }

    [KnownType(typeof(DebitTransactionDetailsOutDto))]
    [KnownType(typeof(CreditTransactionDetailsOutDto))]
    
    public class TransactionDetailsOutDto
    {
        public Guid TransactionId { get; set; }
        public TransactionTypeEnum TransactionType { get; set; }
        public string CheckId { get; set; }
        public AmountOutDto TransactionAmount { get; set; }
        public DateTimeOffset? BookingDate { get; set; }
        public DateTimeOffset EntryDate { get; set; }
        public BookingStatusEnum BookingStatus { get; set; }

        public static TransactionDetailsOutDto EntityToOutDto(TransactionEntity transactionEntity)
        {
            TransactionDetailsOutDto createdTransactionDetails = transactionEntity switch
            {
                CreditTransactionEntity creditTransactionEntity => new CreditTransactionDetailsOutDto()
                {
                    CreditorName = creditTransactionEntity.CreditorName,
                    CreditorAccount =
                        AccountReferenceOutDto.EntityToOutDto<AccountReferenceOutDto>(creditTransactionEntity
                            .CreditorAccount),
                    ValueDate = creditTransactionEntity.ValueDate,
                    TransactionType = TransactionTypeEnum.CREDIT
                },
                DebitTransactionEntity debitTransactionEntity => new DebitTransactionDetailsOutDto()
                {
                    DebtorName = debitTransactionEntity.DebtorName,
                    DebtorAccount =
                        AccountReferenceOutDto.EntityToOutDto<AccountReferenceOutDto>(debitTransactionEntity
                            .DebtorAccount),
                    TransactionType = TransactionTypeEnum.DEBIT
                },
                _ => throw new ArgumentOutOfRangeException(nameof(transactionEntity))
            };

            createdTransactionDetails.TransactionId = transactionEntity.TransactionId;
            createdTransactionDetails.CheckId = transactionEntity.CheckId;
            createdTransactionDetails.BookingDate = transactionEntity.BookingDate;
            createdTransactionDetails.TransactionAmount = AmountOutDto.EntityToOutDto(transactionEntity.TransactionAmount);
            createdTransactionDetails.BookingStatus = transactionEntity.BookingStatus;
            createdTransactionDetails.EntryDate = transactionEntity.EntryDate;

            return createdTransactionDetails;
        }
    }
}