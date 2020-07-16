using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using BankersChoice.API.Models.ApiDtos.User;
using BankersChoice.API.Models.Entities;
using BankersChoice.API.Results;
using BankersChoice.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BankersChoice.API.Controllers
{
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private UserService UserService { get; }

        public UsersController(UserService userService)
        {
            UserService = userService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<UserOutDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllUsers()
        {
            var getAllUsersResult = await UserService.GetAll();
            switch (getAllUsersResult)
            {
                case FailedTypedResult<IEnumerable<UserOutDto>> failedTypedResult:
                    return StatusCode(StatusCodes.Status500InternalServerError, failedTypedResult.Error);
                case SuccessfulTypedResult<IEnumerable<UserOutDto>> successfulTypedResult:
                    return Ok(successfulTypedResult.Value);
                default:
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new ArgumentOutOfRangeException(nameof(getAllUsersResult)));
            }
        }

        [HttpGet]
        [ProducesResponseType(typeof(UserOutDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("{userId:Guid}")]
        public async Task<IActionResult> GetSingleUser(Guid userId)
        {
            var getUserResult = await UserService.Get(userId);

            switch (getUserResult)
            {
                case FailedTypedResult<UserEntity> failedTypedResult:
                    return StatusCode(StatusCodes.Status500InternalServerError, failedTypedResult.Error);
                case NotFoundTypedResult<UserEntity> _:
                    return NotFound();
                case SuccessfulTypedResult<UserEntity> successfulTypedResult:
                    return Ok(UserOutDto.EntityToOutDto(successfulTypedResult.Value));
                default:
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new ArgumentOutOfRangeException(nameof(getUserResult)));
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(UserOutDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateUser(NewUserInDto newUserIn)
        {
            var createdAccountResult = await UserService.Create(newUserIn);
            switch (createdAccountResult)
            {
                case FailedTypedResult<UserOutDto> failedTypedResult:
                    return StatusCode(StatusCodes.Status500InternalServerError, failedTypedResult.Error);
                case SuccessfulTypedResult<UserOutDto> successfulTypedResult:
                    return Ok(successfulTypedResult.Value);
                case BadRequestTypedResult<UserOutDto> badRequestTypedResult:
                    return BadRequest(badRequestTypedResult.Problem);
                default:
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new ArgumentOutOfRangeException(nameof(createdAccountResult)));
            }
        }

        [HttpPatch]
        [Route("{userId:Guid}")]
        public async Task<IActionResult> UpdateUser(Guid userId, UpdateUserDto updatedUserIn)
        {
            var updatedUserResult = await UserService.Update(userId, updatedUserIn);
            switch (updatedUserResult)
            {
                case FailedTypedResult<UserOutDto> failedTypedResult:
                    return StatusCode(StatusCodes.Status500InternalServerError, failedTypedResult.Error);
                case NotFoundTypedResult<UserOutDto> _:
                    return NotFound();
                case SuccessfulTypedResult<UserOutDto> successfulTypedResult:
                    return Ok(successfulTypedResult.Value);
                default:
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new ArgumentOutOfRangeException(nameof(updatedUserResult)));
            }
        }
    }

    public class UpdateUserDto
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string Email { get; set; }
    }

    public class NewUserInDto
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string Email { get; set; }
    }
}