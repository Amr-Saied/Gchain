using System.Security.Claims;
using Gchain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Gchain.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Join a team chat group
        /// </summary>
        public async Task JoinTeamChat(int gameSessionId, int teamId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    await Clients.Caller.SendAsync("Error", "User not authenticated");
                    return;
                }

                // Join the team chat group
                var groupName = $"team_{gameSessionId}_{teamId}";
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

                // Notify other team members that user joined chat
                await Clients
                    .OthersInGroup(groupName)
                    .SendAsync(
                        "UserJoinedChat",
                        new
                        {
                            UserId = userId,
                            GameSessionId = gameSessionId,
                            TeamId = teamId,
                            Timestamp = DateTime.UtcNow
                        }
                    );

                _logger.LogInformation(
                    "User {UserId} joined team {TeamId} chat in game {GameId}",
                    userId,
                    teamId,
                    gameSessionId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to join team chat for team {TeamId} in game {GameId}",
                    teamId,
                    gameSessionId
                );
                await Clients.Caller.SendAsync("Error", "Failed to join team chat");
            }
        }

        /// <summary>
        /// Leave a team chat group
        /// </summary>
        public async Task LeaveTeamChat(int gameSessionId, int teamId)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Remove from team chat group
                var groupName = $"team_{gameSessionId}_{teamId}";
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

                // Notify other team members that user left chat
                await Clients
                    .OthersInGroup(groupName)
                    .SendAsync(
                        "UserLeftChat",
                        new
                        {
                            UserId = userId,
                            GameSessionId = gameSessionId,
                            TeamId = teamId,
                            Timestamp = DateTime.UtcNow
                        }
                    );

                _logger.LogInformation(
                    "User {UserId} left team {TeamId} chat in game {GameId}",
                    userId,
                    teamId,
                    gameSessionId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to leave team chat for team {TeamId} in game {GameId}",
                    teamId,
                    gameSessionId
                );
            }
        }

        /// <summary>
        /// Send a message to the team chat
        /// </summary>
        public async Task SendTeamMessage(int gameSessionId, int teamId, string message)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    await Clients.Caller.SendAsync("Error", "User not authenticated");
                    return;
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    await Clients.Caller.SendAsync("Error", "Message cannot be empty");
                    return;
                }

                // Send message to all team members
                var groupName = $"team_{gameSessionId}_{teamId}";
                await Clients
                    .Group(groupName)
                    .SendAsync(
                        "TeamMessageReceived",
                        new
                        {
                            UserId = userId,
                            Message = message,
                            GameSessionId = gameSessionId,
                            TeamId = teamId,
                            Timestamp = DateTime.UtcNow
                        }
                    );

                _logger.LogInformation(
                    "Team message sent by user {UserId} in team {TeamId} game {GameId}: {Message}",
                    userId,
                    teamId,
                    gameSessionId,
                    message
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send team message for user {UserId} in team {TeamId} game {GameId}",
                    GetCurrentUserId(),
                    teamId,
                    gameSessionId
                );
                await Clients.Caller.SendAsync("Error", "Failed to send message");
            }
        }

        /// <summary>
        /// Send a private message to another user
        /// </summary>
        public async Task SendPrivateMessage(string targetUserId, string message)
        {
            try
            {
                var senderUserId = GetCurrentUserId();
                if (string.IsNullOrEmpty(senderUserId))
                {
                    await Clients.Caller.SendAsync("Error", "User not authenticated");
                    return;
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    await Clients.Caller.SendAsync("Error", "Message cannot be empty");
                    return;
                }

                // Send private message to target user
                await Clients
                    .User(targetUserId)
                    .SendAsync(
                        "PrivateMessageReceived",
                        new
                        {
                            SenderUserId = senderUserId,
                            Message = message,
                            Timestamp = DateTime.UtcNow
                        }
                    );

                // Confirm message sent to sender
                await Clients.Caller.SendAsync(
                    "PrivateMessageSent",
                    new
                    {
                        TargetUserId = targetUserId,
                        Message = message,
                        Timestamp = DateTime.UtcNow
                    }
                );

                _logger.LogInformation(
                    "Private message sent from user {SenderId} to user {TargetId}: {Message}",
                    senderUserId,
                    targetUserId,
                    message
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send private message from user {SenderId} to user {TargetId}",
                    GetCurrentUserId(),
                    targetUserId
                );
                await Clients.Caller.SendAsync("Error", "Failed to send private message");
            }
        }

        /// <summary>
        /// Get current user ID from claims
        /// </summary>
        private string? GetCurrentUserId()
        {
            return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// Called when a client connects
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation(
                "Chat client connected: {ConnectionId}, User: {UserId}",
                Context.ConnectionId,
                userId
            );
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation(
                "Chat client disconnected: {ConnectionId}, User: {UserId}",
                Context.ConnectionId,
                userId
            );
            await base.OnDisconnectedAsync(exception);
        }
    }
}
