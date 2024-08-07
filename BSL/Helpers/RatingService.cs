using DAL;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using telegramB.Objects;

namespace BL.Helpers
{
    public class RatingService
    {
        OrderRepository orderRepository = new OrderRepository();
        DriverRepository driverRepository = new DriverRepository();
        public async Task HandleDriverRating(long chatId, int rating, CancellationToken cancellationToken, ITelegramBotClient botClient, Dictionary<long, UserOrder> userOrders)
        {
            var userOrder = userOrders[chatId];

            if (userOrder == null)
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "No active order found to rate. Please start a new order.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            bool isUpdated = await orderRepository.SaveDriverRatingAsync(userOrder.Id, rating);

            if (isUpdated)
            {
                // Notify the user that the rating has been saved
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"Thank you for rating the driver with {rating} stars!",
                    cancellationToken: cancellationToken
                );

                // Optionally, you can also notify the driver about their rating
                var driverChatId = await driverRepository.GetDriverChatIdByOrderIdAsync(userOrder.Id);
                await botClient.SendTextMessageAsync(
                    chatId: driverChatId,
                    text: $"You have been rated {rating} stars by the customer.",
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "There was an error saving your rating. Please try again.",
                    cancellationToken: cancellationToken
                );
            }
        }


    }
}
