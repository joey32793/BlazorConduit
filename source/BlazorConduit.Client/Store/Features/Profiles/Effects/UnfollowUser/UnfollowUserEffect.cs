﻿using BlazorConduit.Client.Models.Common;
using BlazorConduit.Client.Models.Profiles;
using BlazorConduit.Client.Services;
using BlazorConduit.Client.Store.Features.Profiles.Actions.UnfollowUser;
using Fluxor;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BlazorConduit.Client.Store.Features.Profiles.Effects.UnfollowUser
{
    public class UnfollowUserEffect : Effect<UnfollowUserAction>
    {
        private readonly ILogger<UnfollowUserEffect> _logger;
        private readonly ConduitApiService _apiService;
        private readonly SecurityTokenService _tokenService;

        public UnfollowUserEffect(ILogger<UnfollowUserEffect> logger, ConduitApiService apiService, SecurityTokenService tokenService) =>
            (_logger, _apiService, _tokenService) = (logger, apiService, tokenService);

        protected override async Task HandleAsync(UnfollowUserAction action, IDispatcher dispatcher)
        {
            try
            {
                // Call the profile user endpoint with the username
                var followResponse = await _apiService.DeleteNoContentAsync($"profiles/{action.Username}/follow", await _tokenService.GetTokenAsync());

                if (followResponse is null || !followResponse.IsSuccessStatusCode)
                {
                    // Throw the exception to issue the failure action
                    throw new ConduitApiException($"Could not unfollow user profile {action.Username}", HttpStatusCode.InternalServerError);
                }

                // Get the returned user profile
                var profile = await followResponse.Content.ReadFromJsonAsync<UserProfileResponse>();

                if (profile is null || profile.Profile is null)
                {
                    throw new ConduitApiException($"No profile returned in response to unfollow for user {action.Username}", HttpStatusCode.InternalServerError);
                }

                dispatcher.Dispatch(new UnfollowUserSuccessAction(profile.Profile));
            }
            catch (ConduitApiException e)
            {
                _logger.LogError($"Validation error during profile unfollow for user {action.Username}");
                dispatcher.Dispatch(new UnfollowUserFailureAction(e.Message, e.ApiErrors));
            }
            catch (Exception e)
            {
                _logger.LogError($"Error during profile unfollow for user {action.Username}");
                dispatcher.Dispatch(new UnfollowUserFailureAction(e.Message));
            }
        }
    }
}
