using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankersChoice.API.Models.Entities;

namespace BankersChoice.API.Models.ApiDtos.User
{
    public class UserOutDto
    {
        public Guid UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }

        public static UserOutDto EntityToOutDto(UserEntity userEntity)
        {
            return new UserOutDto()
            {
                UserId = userEntity.UserId,
                FirstName = userEntity.FirstName,
                LastName = userEntity.LastName,
                Email = userEntity.Email
            };
        }
    }
}
