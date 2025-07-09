using GameGuild.Modules.Authentication.Commands;
using GameGuild.Modules.Authentication.Dtos;
using GameGuild.Modules.Authentication.Services;
using MediatR;


namespace GameGuild.Modules.Authentication.Handlers;

/// <summary>
/// Handler for generating Web3 challenge
/// </summary>
public class GenerateWeb3ChallengeHandler(IAuthService authService) : IRequestHandler<GenerateWeb3ChallengeCommand, Web3ChallengeResponseDto> {
  public async Task<Web3ChallengeResponseDto> Handle(
    GenerateWeb3ChallengeCommand request,
    CancellationToken cancellationToken
  ) {
    var challengeRequest = new Web3ChallengeRequestDto { WalletAddress = request.WalletAddress, ChainId = request.ChainId };

    return await authService.GenerateWeb3ChallengeAsync(challengeRequest);
  }
}
