﻿using Microsoft.EntityFrameworkCore;
using VeeMessenger.Data.Constants;
using VeeMessenger.Data.Entities;
using VeeMessenger.Data.Infrastructure;
using VeeMessenger.Domain.Dto;
using VeeMessenger.Domain.Models.AuthenticationModels;
using VeeMessenger.Domain.Models.EmailModels;
using VeeMessenger.Domain.Services.Interfaces;

namespace VeeMessenger.Domain.Services.Implementation
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IRepository<User> userRepository;
        private readonly IPasswordHasherService passwordHasherService;
        private readonly IAuthenticationErrorDescriberService errorDescriberService;
        private readonly IPasswordValidationService passwordValidationService;
        private readonly IUserNameValidationService userNameValidationService;
        private readonly IAccessTokenService accessTokenService;
        private readonly IEmailSenderService emailSenderService;
        private readonly ICodeGeneratorService codeGeneratorService;
        private readonly IRefreshSessionService refreshSessionService;

        public AuthenticationService(IRepository<User> userRepository, IPasswordHasherService passwordHasherService, IAuthenticationErrorDescriberService errorDescriberService, IPasswordValidationService passwordValidationService, IUserNameValidationService userNameValidationService, IAccessTokenService accessTokenService, IEmailSenderService emailSenderService, ICodeGeneratorService codeGeneratorService, IRefreshSessionService refreshSessionService)
        {
            this.userRepository = userRepository;
            this.passwordHasherService = passwordHasherService;
            this.errorDescriberService = errorDescriberService;
            this.passwordValidationService = passwordValidationService;
            this.userNameValidationService = userNameValidationService;
            this.accessTokenService = accessTokenService;
            this.emailSenderService = emailSenderService;
            this.codeGeneratorService = codeGeneratorService;
            this.refreshSessionService = refreshSessionService;
        }

        public async Task<Result> CreateUserAsync(User user, string password)
        {
            Result authenticationResult = new Result();

            if (await userRepository.Query().AnyAsync(u => u.UserName.ToLower().Equals(user.UserName.ToLower())))
            {
                authenticationResult.AddErrors(errorDescriberService.DuplicateUserName(user.UserName));
            }

            authenticationResult.AddErrors(userNameValidationService.IsValid(user.UserName));

            if (await userRepository.Query().AnyAsync(u => u.Email.Equals(user.Email)))
            {
                authenticationResult.AddErrors(errorDescriberService.DuplicateEmail(user.Email));
            }

            authenticationResult.AddErrors(passwordValidationService.IsValid(password));

            if (authenticationResult.Failed)
            {
                return authenticationResult;
            }

            user.UserTempData.EmailConfirmCode = codeGeneratorService.GenerateNumberCode();

            EmailRequest emailRequest = new EmailRequest()
            {
                ToEmail = user.Email,
                Subject = "VeeRegistration",
                Body = $"Code : {user.UserTempData.EmailConfirmCode}"
            };

            await emailSenderService.SendEmailAsync(emailRequest);

            user.PasswordHash = passwordHasherService.Hash(password);

            await userRepository.AddAsync(user);

            await userRepository.SaveChangesAsync();

            return authenticationResult;
        }

        public async Task<Result<TokensDto>> ConfrimEmailAsync(string email, string code, string fingerPrint)
        {
            Result<TokensDto> result = new Result<TokensDto>(new TokensDto());

            var user = await userRepository
                .Query()
                .Include(u => u.UserTempData)
                .Where(u => u.Email.Equals(email))
                .FirstOrDefaultAsync();

            if (user is null)
            {
                result.AddErrors(errorDescriberService.UserWithSuchEmailDoesntExist(email));

                return result;
            }

            if (user.EmailConfirmed)
            {
                result.AddErrors(errorDescriberService.UserIsAlreadyConfirmed(user.Email));

                return result;
            }

            if (code.Equals(user.UserTempData.EmailConfirmCode))
            {
                user.EmailConfirmed = true;

                await userRepository.SaveChangesAsync();

                result.Data.RefreshSession = await refreshSessionService.CreateRefreshSessionAsync(user.Id, fingerPrint);
                result.Data.AccessToken = await accessTokenService.GetAccessToken(user);

                return result;
            }

            result.AddErrors(errorDescriberService.CodeIsinvalid(code));

            user.UserTempData.NumbersOfAttempts++;

            if (user.UserTempData.NumbersOfAttempts >= Constants.MaxNumbersOfAttempts)
            {
                userRepository.Delete(user);

                result.AddErrors(errorDescriberService.RegistrationFailed());
            }

            await userRepository.SaveChangesAsync();

            return result;
        }

        public async Task<Result<TokensDto>> LoginAsync(string userName, string password, string fingerPrint)
        {
            Result<TokensDto> result = new Result<TokensDto>(new TokensDto());

            var user = await userRepository
                .Query()
                .Include(u => u.UserTempData)
                .Include(u => u.RefreshSessions)
                .Where(u => u.UserName.Equals(userName))
                .FirstOrDefaultAsync();

            if (user is null)
            {
                result.AddErrors(errorDescriberService.UsernameDoesNotExist(userName));

                return result;
            }

            if (user.LockOutDate > DateTime.Now)
            {
                result.AddErrors(errorDescriberService.UserIsBlockedForLogin(user.LockOutDate));

                return result;
            }

            if (!passwordHasherService.Check(user.PasswordHash, password))
            {
                result.AddErrors(errorDescriberService.PasswordIsInvalid());

                user.UserTempData.NumbersOfAttempts++;

                if (user.UserTempData.NumbersOfAttempts >= Constants.MaxNumbersOfAttempts)
                {
                    user.LockOutDate = DateTime.Now.AddHours(3);

                    user.UserTempData.NumbersOfAttempts = 0;

                    result.AddErrors(errorDescriberService.UserIsBlockedForLogin(user.LockOutDate));
                }

                await userRepository.SaveChangesAsync();

                return result;
            }

            if (!user.EmailConfirmed)
            {
                result.AddErrors(errorDescriberService.EmailIsNotConfirmed(user.Email));

                return result;
            }

            if (user.RefreshSessions.Any(r => r.FingerPrint.Equals(fingerPrint)))
            {
                result.AddErrors(errorDescriberService.FingerPrintIsExist());

                return result;
            }

            result.Data.RefreshSession = await refreshSessionService.CreateRefreshSessionAsync(user.Id, fingerPrint);
            result.Data.AccessToken = await accessTokenService.GetAccessToken(user);

            return result;
        }

        public async Task<Result> LogoutAsync(Guid refreshSessionId)
        {
            Result result = new Result();

            result.AddErrors(await refreshSessionService.DeleteRefreshSessionAsync(refreshSessionId));

            return result;
        }

        public async Task<Result<TokensDto>> RefreshSessionAsync(Guid refreshSessionId, string fingerPrint)
        {
            Result<TokensDto> result = new Result<TokensDto>(new TokensDto());

            var updateResult = await refreshSessionService.UpdateRefreshSessionAsync(refreshSessionId, fingerPrint);

            if (updateResult.Failed)
            {
                result.AddErrors(updateResult.Errors);

                return result;
            }

            result.Data.RefreshSession = updateResult.Data;

            var user = await userRepository.GetByIdAsync(updateResult.Data.UserId);

            result.Data.AccessToken = await accessTokenService.GetAccessToken(user);

            return result;           
        }
    }
}
