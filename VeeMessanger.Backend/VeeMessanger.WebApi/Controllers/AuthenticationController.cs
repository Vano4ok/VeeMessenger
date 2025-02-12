﻿using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using VeeMessanger.WebApi.Extensions;
using VeeMessenger.Data.Entities;
using VeeMessenger.Domain.Dto;
using VeeMessenger.Domain.Dto.User;
using VeeMessenger.Domain.Models.AuthenticationModels;
using VeeMessenger.Domain.Services.Interfaces;

namespace VeeMessanger.WebApi.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class AuthenticationController : Controller
    {
        private readonly IAuthenticationService authenticationService;
        private readonly IMapper mapper;

        public AuthenticationController(IAuthenticationService authenticationService, IMapper mapper)
        {
            this.authenticationService = authenticationService;
            this.mapper = mapper;
        }

        [HttpPost]
        [Route("registering")]
        public async Task<IActionResult> RegisterUser([FromBody] UserForRegistrationDto userDto)
        {
            User user = mapper.Map<User>(userDto);

            var result = await authenticationService.CreateUserAsync(user, userDto.Password);

            if (result.Succeeded)
            {
                return Ok("We have sent you a confirmation code <3");
            }

            AddModelErrors(result.Errors);

            return BadRequest(ModelState.GenerateValidation());
        }

        [HttpPost]
        [Route("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] UserForConfirmDto userDto)
        {
            var result = await authenticationService.ConfrimEmailAsync(userDto.Email, userDto.Code, userDto.FingerPrint);

            if (result.Succeeded)
            {
                SetRefreshToken(result.Data);

                return Ok(new { result.Data.AccessToken, refreshToken = result.Data.RefreshSession.Id });
            }

            AddModelErrors(result.Errors);

            return Unauthorized(ModelState.GenerateValidation());
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> LoginUser([FromBody] UserForLoginDto userDto)
        {
            var result = await authenticationService.LoginAsync(userDto.UserName, userDto.Password, userDto.FingerPrint);

            if (result.Succeeded)
            {
                SetRefreshToken(result.Data);

                return Ok(new { result.Data.AccessToken, RefreshToken = result.Data.RefreshSession.Id });
            }

            AddModelErrors(result.Errors);

            return Unauthorized(ModelState.GenerateValidation());
        }

        [HttpPost]
        [Route("logout")]
        public async Task<IActionResult> LogOutUser()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (refreshToken is null)
            {
                return BadRequest("Refresh token is null");
            }

            var result = await authenticationService.LogoutAsync(new Guid(refreshToken));

            if (result.Succeeded)
            {
                DeleteRefheshToken();

                return Ok();
            }

            return BadRequest("This account is already logged out");
        }

        [HttpPost]
        [Route("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokensDto refreshTokensDto)
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (refreshToken is null)
            {
                return BadRequest("Refresh token is null");
            }

            var result = await authenticationService.RefreshSessionAsync(new Guid(refreshToken), refreshTokensDto.FingerPrint);

            if (result.Succeeded)
            {
                SetRefreshToken(result.Data);

                return Ok(new { result.Data.AccessToken, refreshToken = result.Data.RefreshSession.Id });
            }

            DeleteRefheshToken();

            AddModelErrors(result.Errors);

            return Unauthorized(ModelState.GenerateValidation());
        }

        private void SetRefreshToken(TokensDto result)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = result.RefreshSession.Expires
            };

            Response.Cookies.Append("refreshToken", result.RefreshSession.Id.ToString(), cookieOptions);
        }

        private void DeleteRefheshToken()
        {
            Response.Cookies.Delete("refreshToken");
        }

        private void AddModelErrors(IEnumerable<Error> authenticationErrors)
        {
            foreach (var error in authenticationErrors)
            {
                ModelState.TryAddModelError(error.Code, error.Description);
            }
        }
    }
}
