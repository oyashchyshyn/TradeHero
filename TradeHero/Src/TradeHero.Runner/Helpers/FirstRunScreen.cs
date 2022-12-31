using Telegram.Bot;
using TradeHero.Contracts.Base.Constants;
using TradeHero.Contracts.Base.Enums;

namespace TradeHero.Runner.Helpers;

internal static class FirstRunScreen
{
    private static bool _isNeedToRunFirstScreenLogic;
    
    public static async Task<bool> RunAsync(EnvironmentType environmentType, string baseDirectory)
    {
        _isNeedToRunFirstScreenLogic = false;
        
        var pathToDatabase = Path.Combine(baseDirectory, FolderConstants.DataFolder,
            FolderConstants.DatabaseFolder);
        
        if (!Directory.Exists(pathToDatabase))
        {
            Directory.CreateDirectory(pathToDatabase);
        }

        var userPath = Path.Combine(pathToDatabase, DatabaseConstants.UserFileName);
        if (!File.Exists(userPath))
        {
            await File.WriteAllTextAsync(userPath, "[]");

            _isNeedToRunFirstScreenLogic = true;
        }
        
        var connectionPath = Path.Combine(pathToDatabase, DatabaseConstants.ConnectionFileName);
        if (!File.Exists(connectionPath))
        {
            await File.WriteAllTextAsync(connectionPath, "[]");
        }
        
        var strategyPath = Path.Combine(pathToDatabase, DatabaseConstants.UserFileName);
        if (!File.Exists(strategyPath))
        {
            await File.WriteAllTextAsync(strategyPath, "[]");
        }

        if (!_isNeedToRunFirstScreenLogic)
        {
            return true;
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

                userTelegramId = int.Parse(telegramIdString);
                if (userTelegramId == 0)
                {
                    errorMessage = "User telegram id cannot be zero.";
                    Console.Clear();
                    continue;
                }
                
                break;
            }
            
            TelegramBotClient telegramClient;
            while (true)
            {
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    ErrorMessage(errorMessage);
                    Console.WriteLine();
                }
            
                Console.WriteLine("Write down bot 'Telegram api key':");
                var botTelegramApiKey = Console.ReadLine();
            
                if (string.IsNullOrWhiteSpace(botTelegramApiKey))
                {
                    errorMessage = "'Telegram api key' cannot be empty";
                    Console.Clear();
                    continue;
                }

                telegramClient = new TelegramBotClient(botTelegramApiKey);
                if (!await telegramClient.TestApiAsync())
                {
                    errorMessage = "Cannot connect to bot by this api key";
                    Console.Clear();
                    continue;
                }

                break;
            }

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
                    "Also, make sure that user send '/start' command or send a message to bot." +
                    "Make sure that you solve problems above and insert data one more time.";
                Console.Clear();
                continue;
            }

            break;
        }

        return true;
    }

    private static void ErrorMessage(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}