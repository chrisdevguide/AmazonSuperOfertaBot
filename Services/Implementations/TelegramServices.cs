using AmazonApi.Data;
using AmazonApi.Models;
using AmazonSuperOfertaBot.Data.Repositories.Implementations;
using AmazonSuperOfertaBot.Dtos;
using AmazonSuperOfertaBot.Models;
using AmazonSuperOfertaBot.Services.Interfaces;
using AutoMapper;
using ElAhorrador.Data.Repositories.Implementations;
using ElAhorrador.Data.Repositories.Interfaces;
using ElAhorrador.Models;
using ElAhorrador.Models.Enum;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ElAhorrador.Services.Implementations
{
    public class TelegramServices
    {
        private readonly TelegramBotClient _botClient;
        private readonly TelegramConfiguration _telegramConfiguration;
        private readonly ITelegramChatRepository _telegramChatRepository;
        private readonly IScrapingServices _scrapingServices;
        private readonly IAmazonAlertRepository _amazonAlertRepository;
        private readonly ILogsRepository _logsRepository;
        private readonly IMapper _mapper;
        private readonly IAmazonProductsTelegramRepository _amazonProductsTelegramRepository;
        private readonly DataContext _dataContext;

        public TelegramServices(IConfigurationRepository configurationRepository, ITelegramChatRepository telegramChatRepository, IScrapingServices scrapingServices,
            IAmazonAlertRepository amazonAlertRepository, ILogsRepository logsRepository, IMapper mapper, IAmazonProductsTelegramRepository amazonProductsTelegramRepository, DataContext dataContext)
        {
            _telegramConfiguration = configurationRepository.GetConfiguration<TelegramConfiguration>().Result;
            _botClient = new(_telegramConfiguration.ApiKey);
            _telegramChatRepository = telegramChatRepository;
            _scrapingServices = scrapingServices;
            _amazonAlertRepository = amazonAlertRepository;
            _logsRepository = logsRepository;
            _mapper = mapper;
            _amazonProductsTelegramRepository = amazonProductsTelegramRepository;
            _dataContext = dataContext;
        }

        public void StartBot()
        {
            _botClient.StartReceiving(HandleUpdateAsync, HandlePollingErrorAsync);
        }

        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                List<AmazonProduct> amazonProducts = new();
                ScrapeRequestDto scrapeRequestDto;
                long chatId = (update.Message is null) ? update.CallbackQuery.Message.Chat.Id : update.Message.Chat.Id;
                string receivedText = (update.Message is null) ? update.CallbackQuery.Data : update?.Message.Text;
                TelegramChat telegramChat = await _telegramChatRepository.GetTelegramChat(chatId);

                if (telegramChat?.StartedTime.AddMinutes(5) < DateTime.UtcNow)
                {
                    await _telegramChatRepository.DeleteTelegramChat(telegramChat.Id);
                    telegramChat = null;
                }

                if (receivedText == _telegramConfiguration.ExitCommand)
                {
                    if (telegramChat is not null) await _telegramChatRepository.DeleteTelegramChat(telegramChat.Id);
                    await botClient.SendTextMessageAsync(chatId, "Se ha reiniciado la conversación.", cancellationToken: cancellationToken);
                    return;
                }

                telegramChat ??= new()
                {
                    Id = chatId,
                };

                if (receivedText == _telegramConfiguration.ScrapeCommand || telegramChat.ChatCommand == _telegramConfiguration.ScrapeCommand)
                {
                    switch (telegramChat.ChatStep)
                    {
                        case (int)ScrapeTelegramChatSteps.Started:
                            telegramChat.ChatCommand = _telegramConfiguration.ScrapeCommand;
                            telegramChat.ChatStep = (int)ScrapeTelegramChatSteps.AskForSearchType;

                            string textToSend = "Perfecto, vamos a encontrar productos en Amazon. Deseas realizar una búsqueda avanzada?";
                            InlineKeyboardMarkup inlineKeyboard = new(new[]
                                        {
                                    new[]
                                    {
                                        InlineKeyboardButton.WithCallbackData("Sí", "true"),
                                        InlineKeyboardButton.WithCallbackData("No", "false")
                                    }
                                });

                            await botClient.SendTextMessageAsync(chatId, textToSend, replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);

                            await _telegramChatRepository.CreateTelegramChat(telegramChat);
                            break;
                        case (int)ScrapeTelegramChatSteps.AskForSearchType:
                            if (!bool.TryParse(receivedText, out bool parsedText))
                            {
                                await botClient.SendTextMessageAsync(chatId, $"Selección no válida. Busqueda terminada.", cancellationToken: cancellationToken);
                                await _telegramChatRepository.DeleteTelegramChat(telegramChat.Id);
                                return;
                            }

                            await botClient.SendTextMessageAsync(chatId, $"De acuerdo. Indica la palabra clave por la cual quieres buscar.", cancellationToken: cancellationToken);
                            telegramChat.ChatStep = parsedText ? (int)ScrapeTelegramChatSteps.AskForMinimumDiscountAdvancedSearch : (int)ScrapeTelegramChatSteps.ReadyToSimpleSearch;

                            await _telegramChatRepository.UpdateTelegramChat(telegramChat);
                            break;
                        case (int)ScrapeTelegramChatSteps.AskForMinimumDiscountAdvancedSearch:

                            scrapeRequestDto = new(receivedText);
                            telegramChat.Data = JsonConvert.SerializeObject(scrapeRequestDto);
                            telegramChat.ChatStep = (int)ScrapeTelegramChatSteps.AskForMinimumStarsAdvancedSearch;
                            await botClient.SendTextMessageAsync(chatId, $"Ahora indica el porcentaje de descuento mínimo que un producto debe tener. Insertar un valor de 0 a 99.", cancellationToken: cancellationToken);

                            await _telegramChatRepository.UpdateTelegramChat(telegramChat);


                            break;
                        case (int)ScrapeTelegramChatSteps.AskForMinimumStarsAdvancedSearch:

                            scrapeRequestDto = JsonConvert.DeserializeObject<ScrapeRequestDto>(telegramChat.Data);
                            if (!decimal.TryParse(receivedText, out decimal parsedDiscount) || parsedDiscount < 0 || parsedDiscount > 99)
                            {
                                await botClient.SendTextMessageAsync(chatId, $"Valor no válido. Busqueda terminada.", cancellationToken: cancellationToken);
                                await _telegramChatRepository.DeleteTelegramChat(telegramChat.Id);
                                return;
                            }
                            scrapeRequestDto.MinimumDiscount = parsedDiscount;
                            telegramChat.Data = JsonConvert.SerializeObject(scrapeRequestDto);
                            telegramChat.ChatStep = (int)ScrapeTelegramChatSteps.AskForMinimumReviewsAdvancedSearch;
                            await botClient.SendTextMessageAsync(chatId, $"Ahora indica el numero de estrellas mínimo que un producto debe tener. Insertar un valor de 0.0 a 5.0.", cancellationToken: cancellationToken);
                            await _telegramChatRepository.UpdateTelegramChat(telegramChat);
                            break;
                        case (int)ScrapeTelegramChatSteps.AskForMinimumReviewsAdvancedSearch:

                            scrapeRequestDto = JsonConvert.DeserializeObject<ScrapeRequestDto>(telegramChat.Data);
                            if (!decimal.TryParse(receivedText, out decimal parsedStars) || parsedStars < 0 || parsedStars > 5)
                            {
                                await botClient.SendTextMessageAsync(chatId, $"Valor no válido. Busqueda terminada.", cancellationToken: cancellationToken);
                                await _telegramChatRepository.DeleteTelegramChat(telegramChat.Id);
                                return;
                            }
                            scrapeRequestDto.MinimumStars = parsedStars;
                            telegramChat.Data = JsonConvert.SerializeObject(scrapeRequestDto);
                            telegramChat.ChatStep = (int)ScrapeTelegramChatSteps.AskForProductNameMustContainKeyword;
                            await _telegramChatRepository.UpdateTelegramChat(telegramChat);
                            await botClient.SendTextMessageAsync(chatId, $"Ahora indica el numero de reseñas mínimo que un producto debe tener. Insertar un valor númerico.", cancellationToken: cancellationToken);
                            break;
                        case (int)ScrapeTelegramChatSteps.AskForProductNameMustContainKeyword:
                            if (!int.TryParse(receivedText, out int parsedReviews) || parsedReviews < 0 || parsedReviews > 1000000)
                            {
                                await botClient.SendTextMessageAsync(chatId, $"Valor no válido. Busqueda terminada.", cancellationToken: cancellationToken);
                                await _telegramChatRepository.DeleteTelegramChat(telegramChat.Id);
                                return;
                            }
                            scrapeRequestDto = JsonConvert.DeserializeObject<ScrapeRequestDto>(telegramChat.Data);
                            scrapeRequestDto.MinimumReviews = parsedReviews;
                            telegramChat.Data = JsonConvert.SerializeObject(scrapeRequestDto);
                            telegramChat.ChatStep = (int)ScrapeTelegramChatSteps.ReadyToAdvancedSearch;
                            await _telegramChatRepository.UpdateTelegramChat(telegramChat);
                            textToSend = "Por ultimo, el nombre del producto debe contener la palabra clave por la cual se va a buscar?";
                            inlineKeyboard = new(new[]
                                       {
                                    new[]
                                    {
                                        InlineKeyboardButton.WithCallbackData("Sí", "true"),
                                        InlineKeyboardButton.WithCallbackData("No", "false")
                                    }
                                });
                            await botClient.SendTextMessageAsync(chatId, textToSend, replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
                            break;
                        case (int)ScrapeTelegramChatSteps.ReadyToSimpleSearch:
                            await SendSearchingWaitingMessage(telegramChat.Id);
                            scrapeRequestDto = new(receivedText);
                            amazonProducts = await _scrapingServices.Scrape(scrapeRequestDto);
                            await botClient.SendTextMessageAsync(chatId, $"Se han encontrado {amazonProducts.Count} productos. Los productos se ordenan por descuento de mayor a menor.", cancellationToken: cancellationToken);
                            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
                            amazonProducts.ForEach(async p => await SendAmazonProduct(p, chatId));
                            await _telegramChatRepository.DeleteTelegramChat(telegramChat.Id);
                            break;
                        case (int)ScrapeTelegramChatSteps.ReadyToAdvancedSearch:
                            if (!bool.TryParse(receivedText, out parsedText))
                            {
                                await botClient.SendTextMessageAsync(chatId, $"Valor no válido. Busqueda terminada.", cancellationToken: cancellationToken);
                                await _telegramChatRepository.DeleteTelegramChat(telegramChat.Id);
                                return;
                            }
                            await SendSearchingWaitingMessage(telegramChat.Id);
                            scrapeRequestDto = JsonConvert.DeserializeObject<ScrapeRequestDto>(telegramChat.Data);
                            scrapeRequestDto.MustContainSearchText = parsedText;
                            amazonProducts = await _scrapingServices.Scrape(scrapeRequestDto);
                            await botClient.SendTextMessageAsync(chatId, $"Se han encontrado {amazonProducts.Count} productos. Los productos se ordenan por descuento de mayor a menor.", cancellationToken: cancellationToken);
                            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
                            amazonProducts.ForEach(async p => await SendAmazonProduct(p, chatId));
                            await _telegramChatRepository.DeleteTelegramChat(telegramChat.Id);
                            break;

                    }
                }
                else if (receivedText == _telegramConfiguration.ScrapeProductCommand || telegramChat.ChatCommand == _telegramConfiguration.ScrapeProductCommand)
                {
                    switch (telegramChat.ChatStep)
                    {
                        case (int)ScrapeProductTelegramChatSteps.Started:
                            await botClient.SendTextMessageAsync(chatId, $"Perfecto, vamos a buscar un producto en Amazon por su ASIN. Insertar el ASIN del producto.", cancellationToken: cancellationToken);
                            telegramChat.ChatCommand = _telegramConfiguration.ScrapeProductCommand;
                            telegramChat.ChatStep = (int)ScrapeProductTelegramChatSteps.AskForSearchText;
                            await _telegramChatRepository.CreateTelegramChat(telegramChat);
                            break;
                        case (int)ScrapeProductTelegramChatSteps.AskForSearchText:
                            await SendSearchingWaitingMessage(telegramChat.Id);
                            AmazonProduct amazonProduct = await _scrapingServices.ScrapeProduct(receivedText);
                            if (amazonProduct == null)
                            {
                                await botClient.SendTextMessageAsync(chatId, $"No se ha encontrado ningún producto con el ASIN insertado.", cancellationToken: cancellationToken);
                            }
                            else
                            {
                                await SendAmazonProduct(amazonProduct, chatId);
                            }
                            await _telegramChatRepository.DeleteTelegramChat(telegramChat.Id);
                            break;
                        default:
                            break;
                    }
                }
                else if (receivedText == _telegramConfiguration.CreateAlertCommand || telegramChat.ChatCommand == _telegramConfiguration.CreateAlertCommand)
                {
                    switch (telegramChat.ChatStep)
                    {
                        case (int)CreateAlertTelegramChatSteps.Started:
                            await botClient.SendTextMessageAsync(chatId, $"Perfecto, vamos a crear una alerta de un producto de Amazon. Se enviará una notificación cada vez que el precio del producto cambie. Como quieres llamar el alerta? (e.g. iPhone)", cancellationToken: cancellationToken);
                            telegramChat.ChatCommand = _telegramConfiguration.CreateAlertCommand;
                            telegramChat.ChatStep = (int)CreateAlertTelegramChatSteps.AskForAmazonAlertName;
                            await _telegramChatRepository.CreateTelegramChat(telegramChat);
                            break;
                        case (int)CreateAlertTelegramChatSteps.AskForAmazonAlertName:
                            await botClient.SendTextMessageAsync(chatId, $"Ahora inserta el ASIN del producto.", cancellationToken: cancellationToken);
                            AmazonAlert amazonAlert = new()
                            {
                                CreatedTime = DateTime.UtcNow,
                                ChatId = chatId,
                                Name = receivedText
                            };
                            telegramChat.Data = JsonConvert.SerializeObject(amazonAlert);
                            telegramChat.ChatStep = (int)CreateAlertTelegramChatSteps.AskForProductAsin;
                            await _telegramChatRepository.UpdateTelegramChat(telegramChat);
                            break;
                        case (int)CreateAlertTelegramChatSteps.AskForProductAsin:
                            await botClient.SendTextMessageAsync(chatId, $"Creando...", cancellationToken: cancellationToken);
                            if (await _amazonAlertRepository.AmazonAlertExists(telegramChat.Id, receivedText))
                            {
                                await botClient.SendTextMessageAsync(chatId, $"Una alerta por este producto ya existe. La creación de la alerta ha terminado.", cancellationToken: cancellationToken);
                                await _telegramChatRepository.DeleteTelegramChat(telegramChat.Id);
                                return;
                            }
                            AmazonProduct amazonProduct = await _scrapingServices.ScrapeProduct(receivedText);
                            if (amazonProduct is null)
                            {
                                await botClient.SendTextMessageAsync(chatId, $"No se ha encontrado ningún producto con el ASIN insertado. La creación de la alerta ha terminado.", cancellationToken: cancellationToken);
                                await _telegramChatRepository.DeleteTelegramChat(telegramChat.Id);
                                return;
                            }
                            amazonAlert = JsonConvert.DeserializeObject<AmazonAlert>(telegramChat.Data);
                            amazonAlert.Prices.Add(amazonProduct.CurrentPrice);
                            amazonAlert.ProductAsin = amazonProduct.Asin;
                            await _amazonAlertRepository.CreateAmazonAlert(amazonAlert);
                            await botClient.SendTextMessageAsync(chatId, $"La alerta se ha creado correctamente. Este es el precio del producto ahora.", cancellationToken: cancellationToken);
                            await SendAmazonProduct(amazonProduct, chatId);
                            await _telegramChatRepository.DeleteTelegramChat(telegramChat.Id);
                            break;
                        default:
                            break;
                    }
                }
                else if (receivedText == _telegramConfiguration.EditAlertsCommand || telegramChat.ChatCommand == _telegramConfiguration.EditAlertsCommand)
                {
                    switch (telegramChat.ChatStep)
                    {
                        case (int)EditAlertsTelegramChatSteps.Started:
                            telegramChat.ChatCommand = _telegramConfiguration.EditAlertsCommand;
                            telegramChat.ChatStep = (int)EditAlertsTelegramChatSteps.EditAlert;
                            List<List<InlineKeyboardButton>> inlineKeyboardButtons = new() { new() };
                            List<AmazonAlert> amazonAlerts = await _amazonAlertRepository.GetAmazonAlerts(telegramChat.Id);
                            string textToSend = "";
                            InlineKeyboardMarkup inlineKeyboard = null;
                            if (amazonAlerts.Count > 0)
                            {
                                textToSend = "Estas son alertas que has creado. Haz clic para editar el producto correspondiente.";
                                amazonAlerts.ForEach(x => inlineKeyboardButtons.First().Add(InlineKeyboardButton.WithCallbackData(x.Name, x.ProductAsin)));
                                inlineKeyboard = new(inlineKeyboardButtons);
                                await _telegramChatRepository.CreateTelegramChat(telegramChat);
                            }
                            else
                            {
                                textToSend = "No hay alertas activas.";
                            }
                            await botClient.SendTextMessageAsync(chatId, textToSend, replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
                            break;
                        case (int)EditAlertsTelegramChatSteps.EditAlert:
                            AmazonAlert amazonAlert = await _amazonAlertRepository.GetAmazonAlert(telegramChat.Id, receivedText);
                            if (amazonAlert is null)
                            {
                                await botClient.SendTextMessageAsync(chatId, $"Producto no encontrado, edicción terminada.", cancellationToken: cancellationToken);
                                await _telegramChatRepository.DeleteTelegramChat(telegramChat.Id);
                                return;
                            }
                            inlineKeyboardButtons = new() { new()
                            {
                                InlineKeyboardButton.WithCallbackData("Ver producto", nameof(EditAlertsTelegramChatSteps.SearchAlert)),
                                InlineKeyboardButton.WithCallbackData("Eliminar alerta", nameof(EditAlertsTelegramChatSteps.DeleteAlert)),
                            }
                        };
                            inlineKeyboard = new(inlineKeyboardButtons);
                            await botClient.SendTextMessageAsync(chatId, $"Aquí tienes la alerta con nombre <b>'{amazonAlert.Name}'</b>. ¿Que deseas hacer?", replyMarkup: inlineKeyboard, parseMode: ParseMode.Html, cancellationToken: cancellationToken);
                            telegramChat.Data = amazonAlert.ProductAsin;
                            telegramChat.ChatStep = (int)EditAlertsTelegramChatSteps.SetEditAction;
                            await _telegramChatRepository.UpdateTelegramChat(telegramChat);
                            break;
                        case (int)EditAlertsTelegramChatSteps.SetEditAction:
                            switch (receivedText)
                            {
                                case nameof(EditAlertsTelegramChatSteps.SearchAlert):
                                    await SendSearchingWaitingMessage(telegramChat.Id);
                                    AmazonProduct amazonProduct = await _scrapingServices.ScrapeProduct(telegramChat.Data);
                                    if (amazonProduct == null)
                                    {
                                        await botClient.SendTextMessageAsync(chatId, $"No se ha encontrado ningún producto con el ASIN insertado.", cancellationToken: cancellationToken);
                                    }
                                    else
                                    {
                                        await SendAmazonProduct(amazonProduct, chatId);
                                    }
                                    await _telegramChatRepository.DeleteTelegramChat(telegramChat.Id);
                                    break;
                                case nameof(EditAlertsTelegramChatSteps.DeleteAlert):
                                    amazonAlert = await _amazonAlertRepository.GetAmazonAlert(telegramChat.Id, telegramChat.Data);
                                    if (amazonAlert is not null)
                                    {
                                        textToSend = $"Alerta con nombre <b>'{amazonAlert.Name}'</b> borrada correctamente.";
                                        await _amazonAlertRepository.DeleteAmazonAlert(amazonAlert.Id);
                                    }
                                    else
                                    {
                                        textToSend = $"No se ha encontrado ninguna alerta con el nombre insertado. Edicción terminada.";
                                    }
                                    await botClient.SendTextMessageAsync(chatId, textToSend, parseMode: ParseMode.Html, cancellationToken: cancellationToken);
                                    await _telegramChatRepository.DeleteTelegramChat(telegramChat.Id);
                                    break;
                                default:
                                    await botClient.SendTextMessageAsync(chatId, $"Selección no valida, edicción terminada.", cancellationToken: cancellationToken);
                                    await _telegramChatRepository.DeleteTelegramChat(telegramChat.Id);
                                    break;
                            }
                            break;
                    }
                }
                else if (receivedText == _telegramConfiguration.AdminCommand || telegramChat.ChatCommand == _telegramConfiguration.AdminCommand)
                {
                    switch (telegramChat.ChatStep)
                    {
                        case (int)AdminTelegramChatSteps.Started:
                            telegramChat.ChatCommand = _telegramConfiguration.AdminCommand;
                            telegramChat.ChatStep = (int)AdminTelegramChatSteps.AskForPassword;
                            await botClient.SendTextMessageAsync(chatId, $"Password:", cancellationToken: cancellationToken);
                            await _telegramChatRepository.CreateTelegramChat(telegramChat);
                            break;
                        case (int)AdminTelegramChatSteps.AskForPassword:
                            HMACSHA512 hmac = new(_telegramConfiguration.AdminPasswordHash);
                            if (!hmac.ComputeHash(Encoding.UTF8.GetBytes(receivedText)).SequenceEqual(_telegramConfiguration.AdminPassword))
                            {
                                await botClient.SendTextMessageAsync(chatId, $"Fin de la conversación.", cancellationToken: cancellationToken);
                                await _telegramChatRepository.DeleteTelegramChat(telegramChat.Id);
                                return;
                            };
                            List<List<InlineKeyboardButton>> inlineKeyboardButtons = new() { new()
                            {
                                InlineKeyboardButton.WithCallbackData("Enviar producto", nameof(AdminTelegramChatSteps.SendProductToChannel)),
                                InlineKeyboardButton.WithCallbackData("Buscar Categorías", nameof(AdminTelegramChatSteps.SearchCategories)),
                            }
                        };
                            InlineKeyboardMarkup inlineKeyboard = new(inlineKeyboardButtons);
                            await botClient.SendTextMessageAsync(chatId, $"Hola Admin. ¿Que deseas hacer?", replyMarkup: inlineKeyboard, parseMode: ParseMode.Html, cancellationToken: cancellationToken);
                            telegramChat.ChatStep = (int)AdminTelegramChatSteps.SetAction;
                            await _telegramChatRepository.UpdateTelegramChat(telegramChat);
                            break;
                        case (int)AdminTelegramChatSteps.SetAction:
                            switch (receivedText)
                            {
                                case nameof(AdminTelegramChatSteps.SendProductToChannel):
                                    telegramChat.ChatStep = (int)AdminTelegramChatSteps.SendProductToChannel;
                                    await _telegramChatRepository.UpdateTelegramChat(telegramChat);
                                    await botClient.SendTextMessageAsync(chatId, $"Insertar el ASIN del producto para enviar.", cancellationToken: cancellationToken);
                                    break;
                                case nameof(AdminTelegramChatSteps.SearchCategories):
                                    telegramChat.ChatStep = (int)AdminTelegramChatSteps.SearchCategories;
                                    await _telegramChatRepository.UpdateTelegramChat(telegramChat);
                                    await botClient.SendTextMessageAsync(chatId, $"Se buscarán las categorías en Amazon y se enviarán los productos que no se hayan enviado antes. ¿Cual es el descuento mínimo por el cual buscar?", cancellationToken: cancellationToken);
                                    break;
                                default:
                                    await botClient.SendTextMessageAsync(chatId, $"Acción no valida. Fin de la conversación.", cancellationToken: cancellationToken);
                                    await _telegramChatRepository.DeleteTelegramChat(telegramChat.Id);
                                    return;
                            }
                            break;
                        case (int)AdminTelegramChatSteps.SendProductToChannel:
                            await botClient.SendTextMessageAsync(chatId, $"Enviando...", cancellationToken: cancellationToken);
                            AmazonProduct amazonProduct = await _scrapingServices.ScrapeProduct(receivedText);
                            if (amazonProduct is null)
                            {
                                await botClient.SendTextMessageAsync(chatId, $"No se ha encontrado ningún producto con el ASIN insertado. Fin de la conversación.", cancellationToken: cancellationToken);
                                await _telegramChatRepository.DeleteTelegramChat(telegramChat.Id);
                                return;
                            }
                            await SendAmazonProduct(amazonProduct, _telegramConfiguration.ChannelName);
                            await botClient.SendTextMessageAsync(chatId, $"Producto enviado correctamente. Fin de la conversación.", cancellationToken: cancellationToken);
                            await _telegramChatRepository.DeleteTelegramChat(telegramChat.Id);
                            break;
                        case (int)AdminTelegramChatSteps.SearchCategories:
                            if (!decimal.TryParse(receivedText, out decimal parsedDiscount) || parsedDiscount < 0 || parsedDiscount > 99)
                            {
                                await botClient.SendTextMessageAsync(chatId, $"Valor no válido. Busqueda terminada.", cancellationToken: cancellationToken);
                                await _telegramChatRepository.DeleteTelegramChat(telegramChat.Id);
                                return;
                            }
                            await botClient.SendTextMessageAsync(chatId, $"Buscando...", cancellationToken: cancellationToken);
                            await SearchAmazonCategories(parsedDiscount);
                            await botClient.SendTextMessageAsync(chatId, $"Busqueda terminada correctamente.", cancellationToken: cancellationToken);
                            await _telegramChatRepository.DeleteTelegramChat(telegramChat.Id);
                            await SendAmazonProductsTelegramToChannel();
                            break;
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "Por favor, enviar un comando válido.", cancellationToken: cancellationToken);
                }
            }
            catch (Exception e)
            {
                await _logsRepository.CreateLog("Error TelegramBot", e);
            }
        }

        async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            await _logsRepository.CreateLog("Error TelegramBot", exception);
            await Task.CompletedTask;
        }

        private async Task SendAmazonProduct(AmazonProduct amazonProduct, string recipient)
        {
            string textToSend = $"<b>{amazonProduct.Name}</b>\n\n" +
                                $"<a href='{amazonProduct.ProductUrl}'>🔗 Ver en Amazon</a>\n\n" +
                                $"🏷 Precio: {(amazonProduct.Discount > 0 ? $"<b><s>{amazonProduct.OriginalPrice}€</s></b> 👉 " : "")}<b>{amazonProduct.CurrentPrice}€</b>\n\n" +
                                $"{(amazonProduct.Discount > 0 ? $"💰 Descuento del <b>{amazonProduct.Discount}% (-{amazonProduct.AmountDiscounted}€)</b>\n\n" : "")}" +
                                $"🏆 Estrellas: <b>{amazonProduct.Stars}/5</b> {new string('⭐', (int)amazonProduct.Stars)}\n\n" +
                                $"✍️ Numero de Reseñas: <b>{amazonProduct.ReviewsCount}</b>\n\n" +
                                $"🔍 ASIN: <b>{amazonProduct.Asin}</b>";

            await _botClient.SendPhotoAsync(recipient, InputFile.FromUri(amazonProduct.ImageUrl), caption: textToSend, parseMode: ParseMode.Html);

        }

        private async Task SendAmazonProduct(AmazonProduct amazonProduct, long recipient)
        {
            await SendAmazonProduct(amazonProduct, recipient.ToString());
        }

        public async Task CheckAlerts()
        {
            List<AmazonAlert> alerts = await _amazonAlertRepository.GetAmazonAlerts();
            alerts.ForEach(async alert =>
            {
                AmazonProduct amazonProduct = await _scrapingServices.ScrapeProduct(alert.ProductAsin);
                if (amazonProduct is null)
                {
                    await _botClient.SendTextMessageAsync(alert.ChatId, $"Hola, el producto con ASIN '{alert.ProductAsin}' ya no es disponible en Amazon. Se ha borrado la correspondiente alerta con nombre '{alert.Name}'.");
                    await _amazonAlertRepository.DeleteAmazonAlert(alert.Id);
                    return;
                }
                if (amazonProduct?.CurrentPrice != alert.Prices.Last())
                {
                    await _botClient.SendTextMessageAsync(alert.ChatId, $"Hola, el precio del producto con ASIN '{alert.ProductAsin}' ha cambiado. El precio anterior era de {alert.Prices.Last()}€ mientras ahora es de {amazonProduct.CurrentPrice}€.");
                    await SendAmazonProduct(amazonProduct, alert.ChatId);
                    alert.Prices.Add(amazonProduct.CurrentPrice);
                    await _amazonAlertRepository.UpdateAmazonAlert(alert);
                }
            });
        }

        public async Task SendAmazonProductsTelegramToChannel()
        {
            List<AmazonProductTelegram> amazonProductsTelegramToSend = await _amazonProductsTelegramRepository.GetAmazonProductsTelegramToSend();

            foreach (AmazonProductTelegram amazonProductTelegram in amazonProductsTelegramToSend)
            {
                AmazonProduct amazonProduct = await _scrapingServices.ScrapeProduct(amazonProductTelegram.Asin);
                if (amazonProduct is null)
                {
                    await _amazonProductsTelegramRepository.DeleteAmazonProductTelegram(amazonProductTelegram.Asin);
                    continue;
                }
                await SendAmazonProduct(amazonProduct, _telegramConfiguration.ChannelName);
                amazonProductTelegram.LastSentTime = DateTime.UtcNow;
                amazonProductTelegram.SentToTelegram = true;
                await _amazonProductsTelegramRepository.UpdateAmazonProductTelegram(amazonProductTelegram);
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        public async Task SearchAmazonCategories(decimal minimumDiscount)
        {
            List<AmazonProduct> amazonProducts = await _scrapingServices.ScrapeCategories(new() { MinimumDiscount = minimumDiscount });
            List<AmazonProductTelegram> amazonProductsTelegram = _mapper.Map<List<AmazonProductTelegram>>(amazonProducts);
            List<AmazonProductTelegram> existingAmazonProductsTelegram = await _amazonProductsTelegramRepository.GetAmazonProductsTelegram();
            List<AmazonProductTelegram> amazonProductsTelegramToAdd = amazonProductsTelegram.FindAll(x => !existingAmazonProductsTelegram.Any(y => y.Asin == x.Asin));
            List<AmazonProductTelegram> amazonProductsTelegramToUpdate = amazonProductsTelegram
                .FindAll(x => existingAmazonProductsTelegram.Any(y => y.Asin == x.Asin && y.LastPrice != x.LastPrice))
                .Select(newProduct =>
                {
                    var existingProduct = existingAmazonProductsTelegram.Find(p => p.Asin == newProduct.Asin);

                    var sentToTelegram = existingProduct.SentToTelegram
                        && existingProduct.LastSentTime.AddDays(7) > DateTime.UtcNow
                        && ((Math.Abs(newProduct.LastPrice - existingProduct.LastPrice) / existingProduct.LastPrice) * 100) < 20;

                    var updatedProduct = new AmazonProductTelegram
                    {
                        Asin = newProduct.Asin,
                        LastPrice = newProduct.LastPrice,
                        SentToTelegram = sentToTelegram
                    };
                    return updatedProduct;
                })
                .ToList();



            await _amazonProductsTelegramRepository.AddAmazonProductsTelegram(amazonProductsTelegramToAdd);
            _dataContext.ChangeTracker.Clear();
            await _amazonProductsTelegramRepository.UpdateAmazonProductsTelegram(amazonProductsTelegramToUpdate);
        }

        private async Task SendSearchingWaitingMessage(long chatId)
        {
            await _botClient.SendTextMessageAsync(chatId, "Buscando...");
        }

        public async Task SendMessage(string text, long chatId)
        {
            await _botClient.SendTextMessageAsync(chatId, text);
        }

    }

}
