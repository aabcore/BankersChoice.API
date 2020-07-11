using BankersChoice.API.Models.Entities;
using Newtonsoft.Json;

namespace BankersChoice.API.Models.Account
{
    public class RoutingNumbersOutDto
    {
        public string Ach { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Wire { get; set; }

        public static RoutingNumbersOutDto EntityToOutDto(
            AccountReferenceEntity.RoutingNumbersEntity routingNumbersEntity)
        {
            return new RoutingNumbersOutDto()
            {
                Ach = routingNumbersEntity.Ach,
                Wire = routingNumbersEntity.Wire
            };
        }
    }
}