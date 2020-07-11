using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BankersChoice.API.Models.Entities
{
    public enum ExternalCashAccountType1Code
    {
        // https://open-banking.pass-consulting.com/json_ExternalCashAccountType1Code.html
        OTHR,
        SVGS,
        LOAN,
        MOMA,
        CASH,
        CHAR,
        CACC,
        TRAN // Most Common
    }
}