﻿namespace VeeMessenger.Domain.Models.AuthenticationModels
{
    public class AuthenticationError
    {
        public string Code { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }
}
