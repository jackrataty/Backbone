﻿using forCrowd.Backbone.BusinessObjects.Entities;

namespace forCrowd.Backbone.WebApi.Providers
{
    using Facade;
    using Microsoft.AspNet.Identity.Owin;
    using Microsoft.Owin.Security;
    using Microsoft.Owin.Security.OAuth;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class ApplicationOAuthProvider : OAuthAuthorizationServerProvider
    {
        private readonly string _publicClientId;

        public ApplicationOAuthProvider(string publicClientId)
        {
            _publicClientId = publicClientId ?? throw new ArgumentNullException(nameof(publicClientId));
        }

        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            var form = await context.Request.ReadFormAsync();
            var userName = context.UserName;
            var password = context.Password;
            var singleUseToken = form.Get("singleUseToken");

            User user;
            var userManager = context.OwinContext.GetUserManager<AppUserManager>();

            // Single use token case
            if (string.IsNullOrWhiteSpace(userName) && string.IsNullOrWhiteSpace(password) && !string.IsNullOrWhiteSpace(singleUseToken))
            {
                user = await userManager.FindBySingleUseTokenAsync(singleUseToken);

                if (user == null)
                {
                    context.SetError("invalid_grant", "Single use token is incorrect.");
                    return;
                }
            }
            else
            {
                user = await userManager.FindAsync(userName, password);

                if (user == null)
                {
                    // User can also login with email address
                    user = await userManager.FindByEmailAsync(userName);

                    if (user == null)
                    {
                        context.SetError("invalid_grant", "The username or password is incorrect.");
                        return;
                    }

                    var result = await userManager.CheckPasswordAsync(user, password);

                    if (!result)
                    {
                        context.SetError("invalid_grant", "The username or password is incorrect.");
                        return;
                    }
                }
            }

            // Ticket
            var oAuthIdentity = await userManager.CreateIdentityAsync(user, context.Options.AuthenticationType);
            var properties = CreateProperties(user.UserName);
            var ticket = new AuthenticationTicket(oAuthIdentity, properties);
            context.Validated(ticket);
        }

        public override async Task TokenEndpoint(OAuthTokenEndpointContext context)
        {
            // Remember me
            var tokenData = await context.Request.ReadFormAsync();

            bool.TryParse(tokenData.Get("rememberMe"), out var rememberMe);
            if (rememberMe)
            {
                context.Properties.ExpiresUtc = DateTime.UtcNow.AddYears(1);
            }

            foreach (var property in context.Properties.Dictionary)
            {
                context.AdditionalResponseParameters.Add(property.Key, property.Value);
            }
        }

        public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            // Resource owner password credentials does not provide a client ID.
            if (context.ClientId == null)
            {
                context.Validated();
            }

            return Task.FromResult<object>(null);
        }

        // TODO It's not clear when this method being called
        public override Task ValidateClientRedirectUri(OAuthValidateClientRedirectUriContext context)
        {
            if (context.ClientId == _publicClientId)
            {
                var expectedRootUri = new Uri(context.Request.Uri, "/");

                if (expectedRootUri.AbsoluteUri == context.RedirectUri)
                {
                    context.Validated();
                }
            }

            return Task.FromResult<object>(null);
        }

        public static AuthenticationProperties CreateProperties(string userName)
        {
            var data = new Dictionary<string, string>
            {
                { "userName", userName }
            };
            return new AuthenticationProperties(data);
        }
    }
}