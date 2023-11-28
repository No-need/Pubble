using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pubble.Models;
using System.Text.Json;

namespace Pubble.Hubs
{
    public class ChatHub : Hub
    {
        public static Game game = new Game();
        public async Task SendMessage(string user, string message)
        {
            var id = this.Context.ConnectionId;
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
        private static List<User> users = new List<User>();

        #region event
        public async Task Start(User user)
        {
            var rand = new Random();
            user.ConnectionId = this.Context.ConnectionId;
            var ball = new Ball();
            ball.X = rand.Next(0, game.Width);
            ball.Y = rand.Next(0, game.Height);
            user.Shape = ball;
            users.Add(user);
            await Started(user);
            await UpdateUserList(Clients.All);
        }

        public async Task MoveBall(string direction)
        {
            var user = users.FirstOrDefault(u => u.ConnectionId == Context.ConnectionId);

            if (user != null)
            {
                // Update user's ball movement
                UpdateUserBallPosition(user, direction);

                // Broadcast the updated user list to all connected clients
                await UpdateUserList(Clients.All);
            }
        }

        private void UpdateUserBallPosition(User user, string direction)
        {
            // Update the user's ball position based on the direction
            switch (direction)
            {
                case "up":
                    user.Shape.Y -= 1;
                    break;
                case "down":
                    user.Shape.Y += 1;
                    break;
                case "left":
                    user.Shape.X -= 1;
                    break;
                case "right":
                    user.Shape.X += 1;
                    break;
            }
        }


        public override async Task OnConnectedAsync()
        {
            await UpdateUserList(Clients.Caller);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var user = users.FirstOrDefault(u => u.ConnectionId == Context.ConnectionId);

            if (user != null)
            {
                users.Remove(user);

                // Broadcast the updated user list to all connected clients
                await UpdateUserList(Clients.All);
            }

            await base.OnDisconnectedAsync(exception);
        }

        #endregion

        #region broadcast
        public async Task Started(User user)
        {
            Clients.Caller.SendAsync("Started", JsonConvert.SerializeObject(user));
        }

        public async Task UpdateUserList(IClientProxy clientProxy)
        {
            clientProxy.SendAsync("UpdateUserList", JsonConvert.SerializeObject(users));
        }


        #endregion

    }
}
