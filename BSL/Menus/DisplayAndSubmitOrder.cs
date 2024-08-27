using BL.Helpers;
using BL.Helpers.FareCalculate;
using Common.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using telegramB.Menus;
using telegramB.Objects;

namespace telegramB.Helpers
{
    public static class DisplayAndSubmitOrder
    {
        private static readonly SessionManager _sessionManager;

        static DisplayAndSubmitOrder()
        {
            _sessionManager = new SessionManager("localhost:6379");
        }
        public static async Task<bool> DisplayOrderSummary(long chatId, ITelegramBotClient botClient, UserOrder userOrder, CancellationToken cancellationToken)
        {
            var LS = new LocationService(_sessionManager);
            var distanceKm = await LS.CalculateDistance(userOrder.FromAddress, userOrder.ToAddress, botClient, cancellationToken, chatId);
            var distanceKmInt = (int)Math.Floor(distanceKm);

            if (distanceKm == -1) return true ;

            var fareCalculator = new TaxiFareCalculate(new FareStructure
            {
                BookingFee = 5.63,
                InitialMeter = 12.2,
                FarePerMinuteA = 1.91,
                FarePerKmA = 1.91,
                FarePerMinuteB = 2.29,
                FarePerKmB = 2.29,
                FarePerMinuteC = 2.67,
                FarePerKmC = 2.67
            });

            var fareType = fareCalculator.DetermineFareType(DateTime.Now);
            var averageSpeedKmh = 60.0;
            var rideDurationHours = distanceKm / averageSpeedKmh;
            var rideDurationMinutes = rideDurationHours * 60.0;

            var fare = distanceKm==0? distanceKm: fareCalculator.CalculateFare(fareType, distanceKm, rideDurationMinutes);
            var fareDetails = $"מחיר משוער של מונית רגילה הינו: {fare:F2} ₪\n";

            var orderSummary = $"סיכום ההזמנה שלך:\n" +
                              $"נקודת איסוף: {userOrder.FromAddress.GetFormattedAddress()}\n" +
                              $"יעד: {userOrder.ToAddress.GetFormattedAddress()}\n" +
                              $"מרחק משוער : {distanceKmInt} קמ\n " +
                              fareDetails +
                              $"מספר טלפון: {userOrder.PhoneNumber}";

            var confirmationButtons = new InlineKeyboardMarkup(new[]
                                {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData(" ✅ להציע מחיר", "enter_bid"),
                                InlineKeyboardButton.WithCallbackData(" ❌ בטל", "cancel_order")
                            }
                        });

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: orderSummary,
                replyMarkup: confirmationButtons,
                cancellationToken: cancellationToken
            );



            return true;
        }


    }
}

