using Common.Services;
using DAL;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using telegramB;
using telegramB.Menus;

namespace BL.Services.Drivers.Functionalities
{
    public class NotifyCustomer
    {
        DriverRepository driverRepository = new DriverRepository();
        OrderRepository orderRepository = new OrderRepository();
        private readonly SessionManager _sessionManager;

        public NotifyCustomer(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }
        public async Task NotifyCustomerAboutETA(int orderId, int etaMinutes, ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            var order = await orderRepository.GetOrderByIdAsync(orderId);
            if (order != null)
            {
                var driverDetails = await driverRepository.GetDriverDetailsById(order.assignToDriver);

                string notification = $"נהג בדרכו אליך!\n" +
                                      $"שם הנהג: {driverDetails.FullName}\n" +
                                      $"פרטי רכב: {driverDetails.CarDetails}\n" +
                                      $"טלפון: {driverDetails.PhoneNumber}\n" +
                                      $"הזמן המשוער להגעה: {etaMinutes} דקות";

                await TypesManual.botClient.SendTextMessageAsync(
                    chatId: order.userId,
                    text: notification,
                    cancellationToken: cancellationToken
                );

                // Add the "ride finished" button
                var finishRideButton = MenuMethods.FinishRideButton(orderId,order.ToAddress.GetFormattedAddress());
                var finishRideMessage = await TypesManual.botClient.SendTextMessageAsync(
                    chatId: order.userId,
                    text: "אנא לחץ על הכפתור כשנסיעה הושלמה:",
                    replyMarkup: finishRideButton,
                    cancellationToken: cancellationToken
                );

                // Debugging information
                Console.WriteLine($"Storing state for user: {order.userId}, messageId: {finishRideMessage.MessageId}");

                // Store the message ID of the "ride finished" button message
                var userState = $"awaiting_finish:{orderId}:{finishRideMessage.MessageId}";
                await _sessionManager.SetSessionData(order.userId, "UserState", userState);
            }
            else
            {
                Console.WriteLine("Order not found");
            }
        }
    }
}
