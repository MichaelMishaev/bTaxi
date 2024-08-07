using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using telegramB.Menus;
using telegramB.Objects;
using telegramB.Services;

namespace telegramB.Helpers
{
    public class HandleUser
    {
        GetAddressFromLocation _getAddressFromLocation;
        public HandleUser(GetAddressFromLocation getAddressFromLocation)
        {
            _getAddressFromLocation = getAddressFromLocation;
        }
        public  async Task HandleUserInput(long chatId, string input, CancellationToken cancellationToken, ITelegramBotClient botClient, Dictionary<long, UserOrder> userOrders,Message message)
        {
            var userOrder = userOrders[chatId];

            switch (userOrder.CurrentStep)
            {
                case "from":
                    if (message.Type == MessageType.Location)
                    {
                        input =  await _getAddressFromLocation.GetAddressFromLocationAsync(message.Location.Latitude, message.Location.Longitude);
                    }
                    userOrder.FromAddress = input;
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"נקודת איסוף {input} נשמרה. אנא בחר באפשרות הבאה.",
                        replyMarkup: MenuMethods.ShowUpdatedMenu(userOrder),
                        cancellationToken: cancellationToken
                    );
                    break;

                case "to":
                    userOrder.ToAddress = input;
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"היעד {input} נשמר. אנא בחר באפשרות הבאה.",
                        replyMarkup: MenuMethods.ShowUpdatedMenu(userOrder),
                        cancellationToken: cancellationToken
                    );
                    break;

                case "passengers":
                    if (int.TryParse(input, out int passengers))
                    {
                        userOrder.NumberOfPassengers = passengers;
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: $"מספר נוסעים {input} נשמר. אנא בחר באפשרות הבאה.",
                            replyMarkup: MenuMethods.ShowUpdatedMenu(userOrder),
                            cancellationToken: cancellationToken
                        );
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "אנא הזן מספר נוסעים תקין.",
                            replyMarkup: MenuMethods.ShowUpdatedMenu(userOrder),
                            cancellationToken: cancellationToken
                        );
                    }
                    break;

                case "phone":
                    userOrder.PhoneNumber = input;
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "מספר טלפון נשמר. ההזמנה שלך הושלמה!",
                        replyMarkup: MenuMethods.ShowUpdatedMenu(userOrder),
                        cancellationToken: cancellationToken
                    );
                    // Optionally, you can send the order details to a specific chat or save it to a database
                    //   userOrders.Remove(chatId);
                    break;
                case "remarks":
                    userOrder.Remarks = input;
                    await botClient.SendTextMessageAsync(
                      chatId: chatId,
                      text: "ההערות נשמרו בהצלחה",
                      replyMarkup: MenuMethods.ShowUpdatedMenu(userOrder),
                      cancellationToken: cancellationToken
                  );
                    break;

                default:
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "התרחשה שגיאה. אנא נסה שוב.",
                        cancellationToken: cancellationToken
                    );
                    break;
            }
        }



    }
}
