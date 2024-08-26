using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using telegramB.Menus;
using telegramB.Objects;

namespace BL.Menus
{
    public   class ConfirmOrder
    {
        public static async Task ConfirmCurrentOrder222(long chatId, ITelegramBotClient botClient, CancellationToken cancellationToken, Dictionary<long, UserOrder> userOrders, Dictionary<long, string> userStates)
        {
            var userOrder = userOrders[chatId];

            // Display the order summary with the bid amount
            string orderSummary = $"סיכום ההזמנה שלך:\n" +
                                  $"נקודת איסוף: {userOrder.FromAddress.GetFormattedAddress()}\n" +
                                  $"יעד: {userOrder.ToAddress.GetFormattedAddress()}\n" +
                                  $"מספר טלפון: {userOrder.PhoneNumber}\n" +
                                  $"הערות: {userOrder.Remarks}\n" +
                                  $"הצעת מחיר: {userOrder.BidAmount:F2} ₪";

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: orderSummary,
                cancellationToken: cancellationToken
            );

            var confirmationButtons = MenuMethods.OrderConfirmationButtons();

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "האם לאשר את ההזמנה?",
                replyMarkup: confirmationButtons,
                cancellationToken: cancellationToken
            );

            userStates[chatId] = "awaiting_order_confirmation";
        }
    }
}
