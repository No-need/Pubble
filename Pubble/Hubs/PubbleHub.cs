using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pubble.Enums;
using Pubble.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Pubble.Hubs
{
    public class PubbleHub : Hub
    {
        public static Game _game;
        private static List<User> _users;
        private static ConcurrentBag<Shape> _shapes;

        public  PubbleHub([FromKeyedServices("Pubble")] Game game)
        {
            if (_game == null)
            {
                _game = game;
                _users = game.Users;
                _shapes = game.Shapes;
            }
		}

        public async Task SendMessage(string user, string message)
        {
            var id = this.Context.ConnectionId;
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        #region event

        public override async Task OnConnectedAsync()
        {
            await UpdateUserList(Clients.Caller);
            await UpdateShapeList(Clients.All);
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

        public async Task Start(User user)
        {
			if (_game.Status == Status.Stop)
			{
				_game.Start();
			}
			var rand = new Random();
            user.ConnectionId = this.Context.ConnectionId;
            user.Ball.X = rand.Next(0 + user.Ball.Radius, _game.Width - user.Ball.Radius);
            user.Ball.Y = rand.Next(0 + user.Ball.Radius, _game.Height - user.Ball.Radius);
            _users.Add(user);
            await Started(user);
            await UpdateUserList(Clients.All);
        }

        public void StopGame()
        {
            _game.Stop();
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

        public async Task AddBall()
        {
            Ball ball = new Ball();
            var rand = new Random();
            ball.Radius = rand.Next(50,100);
            ball.X = rand.Next(0 + ball.Radius, _game.Width - ball.Radius);
            ball.Y = rand.Next(0 + ball.Radius, _game.Height - ball.Radius);
            ball.Speed = rand.Next(600, 900);
            ball.Color = $"rgb({rand.Next(0, 255)},{rand.Next(0, 255)},{rand.Next(0, 255)})";
            ball.Direction = rand.Next(0, 360);
            lock (_shapes)
            {
                _shapes.Add(ball);
            }

            await UpdateShapeList(Clients.All);
        }
        #endregion

        #region broadcast
        public async Task Started(User user)
        {
            Clients.Caller.SendAsync("Started", JsonConvert.SerializeObject(user));
        }

        public static async Task UpdateUserList(IClientProxy clientProxy)
        {
            clientProxy.SendAsync("UpdateUserList", JsonConvert.SerializeObject(_users));
        }

        public static async Task UpdateShapeList(IClientProxy clientProxy)
        {
            clientProxy.SendAsync("UpdateShapeList", JsonConvert.SerializeObject(_shapes));
        }

        public async Task GameOver(List<User> users)
        {
            foreach(var u in users)
            {
                Clients.Clients(u.ConnectionId).SendAsync("GameOver", "Collision with " + string.Join(',',users.Where(x=>x.ConnectionId != u.ConnectionId).Select(x=>x.Name))+" !");
            }
        }

        public static async Task Dead(IHubContext<PubbleHub> hubContext, User user)
        {
            _users.Remove(user);
            await UpdateUserList(hubContext.Clients.All);
            await hubContext.Clients.Clients(user.ConnectionId).SendAsync("Dead", "Kill by Ball !");
            WorldAnnouncement(hubContext.Clients.All, $"{user.Name} Dead !");
		}

        public static async Task WorldAnnouncement(IClientProxy all,string message)
        {
            all.SendAsync("WorldAnnouncement", message);
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
                WorldAnnouncement(Clients.All, string.Join(",",collisionUser.Select(x=>x.Name))+ " Collision !");
            }
        }
        #endregion
    }
}
