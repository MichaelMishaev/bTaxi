using Common.DTO;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using telegramB.Objects;

namespace telegramB.Menus
{
    public static class MenuMethods
    {
        //*********************************************
        //*****************USER MENUS******************
        //*********************************************
        public static InlineKeyboardMarkup mainMenuButtons()
        {
            return new InlineKeyboardMarkup(new[]
                        {
                            new []
                            {
                                InlineKeyboardButton.WithCallbackData("חפש נסיעה", "order_taxi")
                            }
                        });
        }

        public static InlineKeyboardMarkup AcceptbidOrMakeNewMenu(UserOrder userOrder)
        {
            return new InlineKeyboardMarkup(new[]
                            {
                            new InlineKeyboardButton[]
                            {
                                InlineKeyboardButton.WithCallbackData("קבל הזמנה", $"accept_order:{userOrder.OrderId}"),
                                InlineKeyboardButton.WithCallbackData("הצע מחיר חדש", $"bid_order:{userOrder.OrderId}")
                            }
                        });
        }

        public static InlineKeyboardMarkup orderActionsMenu(long chatId)
        {
            return new InlineKeyboardMarkup(new[]
                                 {
                            new InlineKeyboardButton[]
                            {
                                InlineKeyboardButton.WithCallbackData("קבל הזמנה", $"accept_order:{chatId}"),
                                InlineKeyboardButton.WithCallbackData("הצע מחיר חדש", $"bid_order:{chatId}")
                            }
                            });

        }


        public static InlineKeyboardMarkup AwaitDriverCustomerBidResponse(long parentId, decimal driverBid, long bidId)
        {
            return new InlineKeyboardMarkup(new[]
            {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("קבל הצעה", $"accept_bid:{parentId}:{bidId}"),
            InlineKeyboardButton.WithCallbackData("הצע מחיר חדש", $"new_bid:{parentId}:{bidId}")
        }
                });
        }


        public static InlineKeyboardMarkup AwaitCustomerBidResponse(long parentId, decimal driverBid)
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("קבל הצעה", $"accept_bid:{parentId}:{driverBid}"),
                    InlineKeyboardButton.WithCallbackData("הצע מחיר חדש", $"new_bid:{parentId}")
                }
            });
        }


        public static InlineKeyboardMarkup AwaitDriverBidResponse(long parentId, decimal customerBid)
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("קבל הצעה", $"accept_bid:{parentId}:{customerBid}"),
                    InlineKeyboardButton.WithCallbackData("הצע מחיר חדש", $"new_bid:{parentId}")
                }
            });
        }


        public static InlineKeyboardMarkup GetRatingButtons(int orderId)
        {
            var ratingButtons = new InlineKeyboardButton[][]
            {
        new InlineKeyboardButton[]
        {
            InlineKeyboardButton.WithCallbackData("1 ⭐", $"rate_{orderId}_1"),
            InlineKeyboardButton.WithCallbackData("2 ⭐⭐", $"rate_{orderId}_2"),
            InlineKeyboardButton.WithCallbackData("3 ⭐⭐⭐", $"rate_{orderId}_3")
        },
        new InlineKeyboardButton[]
        {
            InlineKeyboardButton.WithCallbackData("4 ⭐⭐⭐⭐", $"rate_{orderId}_4"),
            InlineKeyboardButton.WithCallbackData("5 ⭐⭐⭐⭐⭐", $"rate_{orderId}_5")
        }
            };
            return new InlineKeyboardMarkup(ratingButtons);
        }

        public static InlineKeyboardMarkup OrderConfirmationButtons()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData("✅ אישור", "confirm_order"),
                    InlineKeyboardButton.WithCallbackData("❌ ביטל", "cancel_order")
                }
            });
        }

        public static InlineKeyboardMarkup YesNoAnswer()
        {
            return new InlineKeyboardMarkup(new[]
            {
        new []
                    {
                        InlineKeyboardButton.WithCallbackData("Yes", "confirm_yes"),
                        InlineKeyboardButton.WithCallbackData("No", "confirm_no")
                    }

                });
        }

        public static InlineKeyboardMarkup ShowUpdatedMenu(UserOrder userOrder)
        {
            string GetAddressString(AddressDTO address)
            {
                if (address == null || string.IsNullOrEmpty(address.City) || string.IsNullOrEmpty(address.Street) || address.StreetNumber <= 0)
                {
                    return null;
                }
                return $"{address.Street} {address.StreetNumber}, {address.City}";
            }

            string fromAddressString = GetAddressString(userOrder.FromAddress);
            string toAddressString = GetAddressString(userOrder.ToAddress);

            return new InlineKeyboardMarkup(new[]
            {
        new []
        {
            InlineKeyboardButton.WithCallbackData(
                string.IsNullOrEmpty(toAddressString) ? "יעד" : "יעד ✅",
                "to"
            ),
            InlineKeyboardButton.WithCallbackData(
                string.IsNullOrEmpty(fromAddressString) ? "נקודת איסוף" : "נקודת איסוף ✅",
                "from"
            )
        },
        new []
        {
            InlineKeyboardButton.WithCallbackData(
                string.IsNullOrEmpty(userOrder.PhoneNumber) ? "מספר טלפון" : "מספר טלפון ✅",
                "phone"
            ),
            InlineKeyboardButton.WithCallbackData(
                string.IsNullOrEmpty(userOrder.Remarks) ? "הערות" : "הערות ✅",
                "remarks"
            )
        },
        new []
        {
            InlineKeyboardButton.WithCallbackData(
                "שלח הזמנה",
                "submit"
            )
        }
    });
        }


        public static InlineKeyboardMarkup FinishRideButton(int orderId, string toAddress)
        {
            return new InlineKeyboardMarkup(new[]
            {
        new[]
        {
            InlineKeyboardButton.WithCallbackData($"סיום נסיעה ל {toAddress}", $"finish_ride:{orderId}")
        }
    });
        }

        //*********************************************
        //*****************DRIVER MENUS******************
        //*********************************************

        public static InlineKeyboardMarkup DriverMainMenu()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData("הגש בקשה להיות נהג bTrip", "start_registration")
                }
            });
        }

        public static InlineKeyboardMarkup StartGetOrdersMenu()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData("Yes", "start_orders"),
                    InlineKeyboardButton.WithCallbackData("No", "no_orders")
                }
            });
        }

        public static InlineKeyboardMarkup IntroMenu()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData("התחל לקבל הזמנות", "start_get_rides")
                }
            });
        }

        public static InlineKeyboardMarkup ConfirmYesNo()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData("כן", "confirm_yes"),
                    InlineKeyboardButton.WithCallbackData("לא", "confirm_no")
                }
            });
        }

        // Define the registration menu method
        public static InlineKeyboardMarkup RegistrationMenu()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData("הגש בקשה להיות נהג bTrip", "start_registration"),
                    InlineKeyboardButton.WithCallbackData("נהג רשום? ", "registered_driver")
                }
            });
        }

        public static InlineKeyboardMarkup StopReceivingOrdersMenu()
        {
            return new InlineKeyboardMarkup(new[]
            {
                   new []
                {
                    InlineKeyboardButton.WithCallbackData("לקבל הזמנות ✅", "continue_orders")
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData("לא לקבל הזמנות ❌", "no_orders")
                }
            });
        }

        public static ReplyKeyboardMarkup StopReceivingOrdersMenuMultipleUse()
        {
            var replyKeyboardMarkup = new ReplyKeyboardMarkup(new[]
                   {
                        new KeyboardButton[] { "/stop_orders", "/continue_orders" }
                   })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };

            return replyKeyboardMarkup;
        }


        public static InlineKeyboardMarkup GetOrderActionsMenu(int orderId)
        {
            return new InlineKeyboardMarkup(new[]
            {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("קח את הנסיעה", $"take_order:{orderId}"),
                InlineKeyboardButton.WithCallbackData("הצג הזמנות זמינות", "view_orders")
            }
        });
        }

        public static InlineKeyboardMarkup cancelOrder(int orderId)
        {
            return new InlineKeyboardMarkup(new[]
                        {
                                    InlineKeyboardButton.WithCallbackData("בטל הזמנה", $"cancel_order:{orderId}")
                          });

        }

    }
}
