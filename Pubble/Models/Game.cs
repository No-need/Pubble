using Microsoft.AspNetCore.SignalR;
using Pubble.Enums;
using Pubble.Hubs;
using System.Collections.Concurrent;
using static System.Net.Mime.MediaTypeNames;

namespace Pubble.Models
{
    public class Game
    {
        public List<User> Users { get; set; } =  new List<User>();

        public ConcurrentBag<Shape> Shapes { get; set; } = new ConcurrentBag<Shape>();

		public int Width { get; set; }

        public int Height { get; set; }

        public Status Status { get; set; } = Status.Stop;

        public IHubContext<PubbleHub> Hub { get; set; }

		public Task MainThread { get; set; }

        public int Frequency { get; set; } = 60;

        public void Start()
        {
            this.Status = Status.Processing;
            this.MainThread = new Task(async () =>
            {
                while (true)
                {
                    if (this.Status==Status.Stop)
                    {
                        break;
                    }
                    var deadUsers = new List<User>();
                    foreach(var u in Users)
                    {
                        if(Shapes.Where(x=>x.Type == ShapeType.Ball).Any(x=>Math.Sqrt(Math.Pow(u.Ball.X - x.X, 2) + Math.Pow(u.Ball.Y - x.Y, 2)) <= (((Ball)x).Radius + u.Ball.Radius)))
                        {
                            deadUsers.Add(u);
                        }
                    }
                    foreach(var u in deadUsers)
                    {
                        await PubbleHub.Dead(this.Hub,u);
                    }

                    foreach (var s in Shapes)
                    {
                        double dx = (double)(s.Speed)/(double)this.Frequency * Math.Cos(s.Direction * Math.PI / 180);
                        double dy = (double)(s.Speed)/ (double)this.Frequency * Math.Sin(s.Direction * Math.PI / 180);

                        switch (s.Type)
                        {
                            case ShapeType.Ball:
                                var ball = (Ball)s;
                                double nextX = ball.X + dx;
                                double nextY = ball.Y + dy;
                                // 检查边界碰撞
                                if (nextX - ball.Radius < 0 || nextX + ball.Radius > this.Width)
                                {
                                    // 如果碰到左右边界，计算反射角度
                                    ball.Direction = 180 - ball.Direction;
                                }
                                if (nextY - ball.Radius < 0 || nextY + ball.Radius > this.Height)
                                {
                                    // 如果碰到上下边界，计算反射角度
                                    ball.Direction = -ball.Direction;
                                }

                                // 检查碰撞
                                foreach (Ball ball2 in Shapes.Where(x=>x.Type==ShapeType.Ball))
                                {
                                    if (ball != ball2 && Math.Sqrt(Math.Pow(ball2.X - nextX, 2) + Math.Pow(ball2.Y - nextY, 2)) <= (ball.Radius + ball2.Radius))
                                    {
										//                              // 计算碰撞后的角度变化
										//                              double collisionAngle = Math.Atan2(ball2.Y - nextY, ball2.X - nextX) * 180 / Math.PI;

										//                              // 更新球体的角度
										//                              ball.Direction = (int)collisionAngle;
										//                              ball2.Direction = (int)(collisionAngle + 180);
										//// 修正碰撞后的位置，直到不再发生碰撞为止
										//while (Math.Sqrt(Math.Pow(ball2.X - nextX, 2) + Math.Pow(ball2.Y - nextY, 2)) <= (ball.Radius + ball2.Radius))
										//{
										//	nextX -= dx; // 减小步长
										//	nextY -= dy; // 减小步长
										//}

										// 计算碰撞点的法线向量
										double nx = ball2.X - nextX;
										double ny = ball2.Y - nextY;
										// 计算法线角度
										double normalAngle = Math.Atan2(ny, nx);

										// 计算球体的反射角度
										double reflectAngle = 2 * normalAngle - ball.Direction * Math.PI / 180;

										// 更新球体的角度
										ball.Direction = (int)(reflectAngle * 180 / Math.PI);
										ball2.Direction = (int)(reflectAngle * 180 / Math.PI + 180);

										// 修正位置，避免球体重叠
										double overlap = ball.Radius + ball2.Radius - Math.Sqrt(Math.Pow(ball2.X - nextX, 2) + Math.Pow(ball2.Y - nextY, 2));
										nextX -= overlap * Math.Cos(normalAngle);
										nextY -= overlap * Math.Sin(normalAngle);
									}
								}
                                ball.X = nextX;
                                ball.Y = nextY;
                                break;
                            default:
                                break;
                        }
                    }
                    PubbleHub.UpdateShapeList(this.Hub.Clients.All);
                    Thread.Sleep(1000/this.Frequency);
                }
            });
            this.MainThread.Start();
        }

        public void Stop()
        {
            this.Status = Status.Stop;
        }
    }
}
