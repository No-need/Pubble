using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pubble.Models;
using System.Text.Json;

namespace Pubble.Hubs
{
    public class PubbleHub : Hub
    {
        public static Game _game;
        private static List<User> _users = new List<User>();

        public  PubbleHub([FromKeyedServices("Pubble")] Game game)
        {
            _game = game;
        }

        public async Task SendMessage(string user, string message)
        {
            var id = this.Context.ConnectionId;
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        #region event
        public async Task Start(User user)
        {
            var rand = new Random();
            user.ConnectionId = this.Context.ConnectionId;
            user.Ball.X = rand.Next(0 + user.Ball.Radius, _game.Width - user.Ball.Radius);
            user.Ball.Y = rand.Next(0 + user.Ball.Radius, _game.Height - user.Ball.Radius);
            _users.Add(user);
            await Started(user);
            await UpdateUserList(Clients.All);
        }

        public async Task MoveBall(string direction)
        {
            var user = _users.FirstOrDefault(u => u.ConnectionId == Context.ConnectionId);

            if (user != null)
            {
                // Update user's ball movement
                UpdateUserBallPosition(user.Ball, direction);

                // Broadcast the updated user list to all connected clients
                await UpdateUserList(Clients.All);
                CheckCollision(user);
            }
        }

        public override async Task OnConnectedAsync()
        {
            await UpdateUserList(Clients.Caller);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var user = _users.FirstOrDefault(u => u.ConnectionId == Context.ConnectionId);

            if (user != null)
            {
                _users.Remove(user);

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
            clientProxy.SendAsync("UpdateUserList", JsonConvert.SerializeObject(_users));
        }

        public async Task GameOver(List<User> users)
        {
            foreach(var u in users)
            {
                Clients.Clients(u.ConnectionId).SendAsync("GameOver", "Collision with " + string.Join(',',users.Where(x=>x.ConnectionId != u.ConnectionId).Select(x=>x.Name))+" !");
            }
        }

        public async Task WorldAnnouncement(string message)
        {
            Clients.All.SendAsync("WorldAnnouncement", message);
        }
        #endregion


        #region function
        private void UpdateUserBallPosition(Ball ball, string direction)
        {
            // Update the user's ball position based on the direction
            switch (direction)
            {
                case "up":
                    ball.Y -= 1;
                    break;
                case "down":
                    ball.Y += 1;
                    break;
                case "left":
                    ball.X -= 1;
                    break;
                case "right":
                    ball.X += 1;
                    break;
            }
            int maxX = _game.Width - ball.Radius;
            int minX = ball.Radius;
            int maxY = _game.Height - ball.Radius;
            int minY = ball.Radius;
            if (ball.X <= minX) ball.X = minX;
            if (ball.X >= maxX) ball.X = maxX;
            if (ball.Y <= minY) ball.Y = minY;
            if (ball.Y >= maxY) ball.Y = maxY;
        }

        private async Task CheckCollision(User user)
        {
            var collisionUser = _users.Where(x => x.ConnectionId != user.ConnectionId && Math.Sqrt(Math.Pow(user.Ball.X - x.Ball.X, 2) + Math.Pow(user.Ball.Y - x.Ball.Y, 2)) <= (x.Ball.Radius + user.Ball.Radius)).ToList();
            if (collisionUser.Any())
            {
                collisionUser.Add(user);
                _users.RemoveAll(x=>collisionUser.Any(y=>x.ConnectionId==y.ConnectionId));
                await GameOver(collisionUser);
                await UpdateUserList(Clients.All);
                WorldAnnouncement(string.Join(",",collisionUser.Select(x=>x.Name))+ " Collision !");
            }
        }
        #endregion
    }
}
