using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot;
using telegramB.Objects;

namespace telegramB
{
    public static class TypesManual
    {
        public static readonly string BotToken = "7475215265:AAFEa19kMUqUUestsO6yQEOxRFJB0sQV9e8"; // Replace with your bot token
        public static readonly string DriverBotToken = "7417263377:AAGZwYHQaAEZpeQB4vfKtIIg3758mHVcNE4";
        public static readonly string GeocodingApiKey = "AIzaSyAQ24H5hWTc1Jl9yUJk6vvNVQ0BOdF7d7g";//"AIzaSyCN9mjkjtdbynYR2zAhWmXzoPyl_wVSIaY";
        public static readonly string GroupInviteLink = "";
        public static TelegramBotClient botClient;
        public static TelegramBotClient botDriver;
        public static TelegramBotClient botGudenko;
        public static Dictionary<long, UserOrder> userOrders = new Dictionary<long, UserOrder>(); // Define userOrders dictionary
    }
}
