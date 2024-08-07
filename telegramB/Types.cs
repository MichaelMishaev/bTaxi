using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot;
using telegramB.Objects;

namespace telegramB
{
    public static class Types
    {
        public static readonly string BotToken = "7475215265:AAFEa19kMUqUUestsO6yQEOxRFJB0sQV9e8"; // Replace with your bot token
        public static readonly string GeocodingApiKey = "AIzaSyCN9mjkjtdbynYR2zAhWmXzoPyl_wVSIaY";
        public static readonly string GroupInviteLink = "";
        public static TelegramBotClient botClient;
        public static Dictionary<long, UserOrder> userOrders = new Dictionary<long, UserOrder>(); // Define userOrders dictionary
    }
}
