using BL.Helpers;
using BL.Helpers.FareCalculate;
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

        public static async Task DisplayOrderSummary(long chatId, ITelegramBotClient botClient, UserOrder userOrder, CancellationToken cancellationToken, Dictionary<long, string> userStates)
        {
            var LS = new LocationService();
            // Calculate the distance

            double distanceKm = await LS.CalculateDistance(userOrder.FromAddress, userOrder.ToAddress,  botClient,  cancellationToken,  chatId);

            // Determine the fare type
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
            double averageSpeedKmh = 60.0;
            double rideDurationHours = distanceKm / averageSpeedKmh;
            double rideDurationMinutes = rideDurationHours * 60.0;

            double fare = fareCalculator.CalculateFare(fareType, distanceKm, rideDurationMinutes);

            string fareDetails = $"מחיר משוער של מונית רגילה הינו: {fare:F2} ₪\n";

            string orderSummary = $"סיכום ההזמנה שלך:\n" +
                                  $"נקודת איסוף: {userOrder.FromAddress}\n" +
                                  $"יעד: {userOrder.ToAddress}\n" +
                                  $"מחיר מוצע: {userOrder.BidAmount:F2} ₪\n" +
                                  $"מספר טלפון: {userOrder.PhoneNumber}\n" +
                                  $"הערות: {userOrder.Remarks}\n" +
                                  fareDetails;

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

