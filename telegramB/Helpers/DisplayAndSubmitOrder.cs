using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using telegramB.Menus;
using telegramB.Objects;

namespace telegramB.Helpers
{
    public static class DisplayAndSubmitOrder
    {
        public static async Task DisplayOrderSummary(long chatId, ITelegramBotClient botClient, UserOrder userOrder, CancellationToken cancellationToken)
        {
            string orderSummary = $"סיכום ההזמנה שלך:\n" +
                                  $"נקודת איסוף: {userOrder.FromAddress}\n" +
                                  $"יעד: {userOrder.ToAddress}\n" +
                                  //$"מספר נוסעים: {userOrder.NumberOfPassengers}\n" +
                                  $"מספר טלפון: {userOrder.PhoneNumber}\n" +
                                  $"הערות:  {userOrder.Remarks} \n";

            var confirmationButtons = MenuMethods.OrderConfirmationButtons();
            

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: orderSummary,
                replyMarkup: confirmationButtons,
                cancellationToken: cancellationToken
            );
        }
    }
}
