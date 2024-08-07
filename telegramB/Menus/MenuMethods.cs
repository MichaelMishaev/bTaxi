//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using Telegram.Bot.Types.ReplyMarkups;
//using telegramB.Objects;

//namespace telegramB.Menus
//{
//    public static class MenuMethods
//    {
//        public static InlineKeyboardMarkup mainMenuButtons()
//        {
//            return new InlineKeyboardMarkup(new[]
//                        {
//                            new []
//                            {
//                                InlineKeyboardButton.WithCallbackData("Order Taxi", "order_taxi")
//                            }
//                        });
//        }

//        public static InlineKeyboardMarkup orderTaxiButtons()
//        {
//            return new InlineKeyboardMarkup(new[]
//                        {
//                new[]
//                            {
//                    InlineKeyboardButton.WithCallbackData("נקודת איסוף", "from"),
//                    InlineKeyboardButton.WithCallbackData("יעד", "to")
//                },
//                new[]
//                            {
//                    InlineKeyboardButton.WithCallbackData("מספר טלפון", "phone"),
//                    InlineKeyboardButton.WithCallbackData("הערות", "remarks")
//                },
//                new[]
//                            {
//                    InlineKeyboardButton.WithCallbackData("שלח הזמנה", "submit")
//                }
//            });
//        }

//        public static InlineKeyboardMarkup OrderConfirmationButtons()
//        {
//            return new InlineKeyboardMarkup(new[]
//            {
//                new []
//                {
//                    InlineKeyboardButton.WithCallbackData("שלח הזמנה", "confirm_order"),
//                    InlineKeyboardButton.WithCallbackData("בטל", "cancel_order")
//                }
//            });
//        }

//        public static InlineKeyboardMarkup ShowUpdatedMenu(UserOrder userOrder)
//        {
//            return new InlineKeyboardMarkup(new[]
//            {
//        new []
//        {
//            InlineKeyboardButton.WithCallbackData(
//                string.IsNullOrEmpty(userOrder.FromAddress) ? "נקודת איסוף" : "נקודת איסוף ✅",
//                "from"
//            ),
//            InlineKeyboardButton.WithCallbackData(
//                string.IsNullOrEmpty(userOrder.ToAddress) ? "יעד" : "יעד ✅",
//                "to"
//            )
//        },
//        new []
//        {

//            InlineKeyboardButton.WithCallbackData(
//                string.IsNullOrEmpty(userOrder.PhoneNumber) ? "מספר טלפון" : "מספר טלפון ✅",
//                "phone"
//            ),
//              InlineKeyboardButton.WithCallbackData(
//                string.IsNullOrEmpty(userOrder.Remarks) ? "הערות" : "הערות ✅",
//                "remarks"
//            )
//        },
//        new []
//        {
//            InlineKeyboardButton.WithCallbackData(
//                string.IsNullOrEmpty(userOrder.Remarks) ? "שלח הזמנה" : "שלח הזמנה ",
//                "submit"
//            )
//        }
//     });
//        }
//    }
//}
