using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Repositories.Models;

namespace TradeHero.Runner.Screens;

internal static class FirstRunScreen
{
    public static async Task RunAsync(IServiceProvider serviceProvider)
    {
        var userRepository = serviceProvider.GetRequiredService<IUserRepository>();

        Console.Clear();

        var activeUser = await userRepository.GetActiveUserAsync();
        if (activeUser != null)
        {
            return;
        }

        var errorMessage = string.Empty;
        
        while (true)
        {
            int userTelegramId;
            while (true)
            {
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    ErrorMessage(errorMessage);
                    Console.WriteLine();
                }
                
                Console.WriteLine("Write down your telegram id:");
                var telegramIdString = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(telegramIdString))
                {
                    errorMessage = "User telegram id cannot be empty.";
                    Console.Clear();
                    continue;
                }

                if (!int.TryParse(telegramIdString, out userTelegramId))
                {
                    errorMessage = "Cannot read user telegram id.";
                    Console.Clear();
                    continue;
                }
                
                if (userTelegramId == 0)
                {
                    errorMessage = "User telegram id cannot be zero.";
                    Console.Clear();
                    continue;
                }
                
                break;
            }
            
            errorMessage = string.Empty;
            Console.Clear();
            
            TelegramBotClient telegramClient;
            string? botTelegramApiKey;
            while (true)
            {
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    ErrorMessage(errorMessage);
                    Console.WriteLine();
                }
            
                Console.WriteLine("Write down bot telegram api key:");
                botTelegramApiKey = Console.ReadLine();
            
                if (string.IsNullOrWhiteSpace(botTelegramApiKey))
                {
                    errorMessage = "Telegram api key cannot be empty.";
                    Console.Clear();
                    continue;
                }
                
                try
                {
                    telegramClient = new TelegramBotClient(botTelegramApiKey);
                    if (!await telegramClient.TestApiAsync())
                    {
                        errorMessage = "Cannot connect to bot by this api key.";
                        Console.Clear();
                        continue;
                    }
                }
                catch (Exception)
                {
                    errorMessage = "Cannot connect to bot by this api key.";
                    Console.Clear();
                    continue;
                }

                break;
            }

            errorMessage = string.Empty;
            Console.Clear();

            var isError = false;
            try
            {
                var getUserChat = await telegramClient.GetChatAsync(userTelegramId);
                if (getUserChat.Id == 0)
                {
                    isError = true;
                }
            }
            catch (Exception)
            {
                isError = true;
            }

            if (isError)
            {
                errorMessage = 
                    $"Cannot get chat with user.{Environment.NewLine}" +
                    $"Please, be attentive when writing data.{Environment.NewLine}" +
                    $"Also, make sure that user send '/start' command or send a message to bot.{Environment.NewLine}" +
                    "Make sure that you solve problems above and insert data one more time.";
                Console.Clear();
                continue;
            }

            string? userName;
            while (true)
            {
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    ErrorMessage(errorMessage);
                    Console.WriteLine();
                }
            
                Console.WriteLine("Write down name for current data (Minimum length 3 symbols, Maximum length 40 symbols, Do not contain spaces):");
                userName = Console.ReadLine();
            
                if (string.IsNullOrWhiteSpace(userName))
                {
                    errorMessage = "Name cannot be empty.";
                    Console.Clear();
                    continue;
                }
                
                if (userName.Contains(" "))
                {
                    errorMessage = "Name contains spaces.";
                    Console.Clear();
                    continue;
                }

                switch (userName.Length)
                {
                    case < 3:
                        errorMessage = $"Minimum length 3. Your length {userName.Length}.";
                        Console.Clear();
                        continue;
                    case > 40:
                        errorMessage = $"Maximum length 40. Your length {userName.Length}.";
                        Console.Clear();
                        continue;
                }

                break;
            }

            Console.Clear();

            var isCreated = await userRepository.AddUserAsync(new UserDto
            {
                Name = userName,
                TelegramBotToken = botTelegramApiKey,
                TelegramUserId = userTelegramId
            });

            if (!isCreated)
            {
                throw new Exception("User did not created. See logs.");
            }
        }
    }

    private static void ErrorMessage(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}