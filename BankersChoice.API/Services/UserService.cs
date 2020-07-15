using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankersChoice.API.Controllers;
using BankersChoice.API.Models;
using BankersChoice.API.Models.ApiDtos.User;
using BankersChoice.API.Models.Entities;
using BankersChoice.API.Results;
using MongoDB.Driver;

namespace BankersChoice.API.Services
{
    public class UserService : DbService
    {
        private IMongoCollection<UserEntity> UsersCollection { get; }

        public UserService(DatabaseSettings dbSettings)
        {
            var database = BuildDatabaseClient(dbSettings);
            UsersCollection = database.GetCollection<UserEntity>(dbSettings.UsersCollectionName);
        }

        public async Task<TypedResult<IEnumerable<UserOutDto>>> GetAll()
        {
            try
            {
                var foundUsers = (await UsersCollection.FindAsync(u => true)).ToList();
                return new SuccessfulTypedResult<IEnumerable<UserOutDto>>(foundUsers.Select(UserOutDto.EntityToOutDto));
            }
            catch (Exception e)
            {
                return new FailedTypedResult<IEnumerable<UserOutDto>>(e);
            }
        }

        public async Task<TypedResult<UserEntity>> Get(Guid userId)
        {
            try
            {
                var foundUser = (await UsersCollection.FindAsync(u => u.UserId == userId)).FirstOrDefault();
                if (foundUser == null)
                {
                    return new NotFoundTypedResult<UserEntity>();
                }

                return new SuccessfulTypedResult<UserEntity>(foundUser);
            }
            catch (Exception e)
            {
                return new FailedTypedResult<UserEntity>(e);
            }
        }

        public async Task<TypedResult<UserOutDto>> Create(NewUserInDto newUserIn)
        {
            try
            {
                var existingUserByEmail = (await UsersCollection.FindAsync(u => u.Email == newUserIn.Email)).FirstOrDefault();
                if (existingUserByEmail != null)
                {
                    return new BadRequestTypedResult<UserOutDto>("User with given email already exists.");
                }

                var newUser = new UserEntity()
                {
                    UserId = Guid.NewGuid(),
                    FirstName = newUserIn.FirstName,
                    LastName = newUserIn.LastName,
                    Email = newUserIn.Email
                };

                await UsersCollection.InsertOneAsync(newUser);
                return new SuccessfulTypedResult<UserOutDto>(UserOutDto.EntityToOutDto(newUser));
            }
            catch (Exception e)
            {
                return new FailedTypedResult<UserOutDto>(e);
            }
        }

        public async Task<TypedResult<UserOutDto>> Update(Guid userId, UpdateUserDto updatedUserIn)
        {
            try
            {
                var foundUser = (await UsersCollection.FindAsync(u => u.UserId == userId)).FirstOrDefault();
                if (foundUser == null)
                {
                    return new NotFoundTypedResult<UserOutDto>();
                }

                var updates = new List<UpdateDefinition<UserEntity>>();
                if (!string.IsNullOrWhiteSpace(updatedUserIn.FirstName))
                {
                    updates.Add(Builders<UserEntity>.Update.Set(u => u.FirstName, updatedUserIn.FirstName));
                }

                if (!string.IsNullOrWhiteSpace(updatedUserIn.LastName))
                {
                    updates.Add(Builders<UserEntity>.Update.Set(u => u.LastName, updatedUserIn.LastName));
                }

                if (!string.IsNullOrWhiteSpace(updatedUserIn.Email))
                {
                    updates.Add(Builders<UserEntity>.Update.Set(u => u.Email, updatedUserIn.Email));
                }

                var updatedUser = await UsersCollection.FindOneAndUpdateAsync<UserEntity>(u => u.UserId == userId,
                    Builders<UserEntity>.Update.Combine(updates), GetEntityAfterUpdateOption<UserEntity>());

                return new SuccessfulTypedResult<UserOutDto>(UserOutDto.EntityToOutDto(updatedUser));
            }
            catch (Exception e)
            {
                return new FailedTypedResult<UserOutDto>(e);
            }
        }
    }
}