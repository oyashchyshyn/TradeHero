using Newtonsoft.Json;
using Telegram.Bot;
using TradeHero.Contracts.Base.Constants;
using TradeHero.Contracts.Base.Enums;

namespace TradeHero.Runner.Helpers;

internal static class FirstRunScreen
{
    private static bool _isNeedToRunFirstScreenLogic;

    public static async Task RunAsync(EnvironmentType environmentType, string baseDirectory)
    {
        Console.Clear();
        
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
        else
        {
            var jsonData = await GetJsonObjectDataByPathAsync(userPath);
            if (jsonData == null || !jsonData.Any())
            {
                await File.WriteAllTextAsync(userPath, string.Empty);
                await File.WriteAllTextAsync(userPath, "[]");
                
                _isNeedToRunFirstScreenLogic = true;
            }
        }
        
        var connectionPath = Path.Combine(pathToDatabase, DatabaseConstants.ConnectionFileName);
        if (!File.Exists(connectionPath))
        {
            await File.WriteAllTextAsync(connectionPath, "[]");
        }
        else
        {
            var jsonData = await GetJsonObjectDataByPathAsync(connectionPath);
            if (jsonData == null)
            {
                await File.WriteAllTextAsync(connectionPath, string.Empty);
                await File.WriteAllTextAsync(connectionPath, "[]");
            }
        }
        
        var strategyPath = Path.Combine(pathToDatabase, DatabaseConstants.StrategyFileName);
        if (!File.Exists(strategyPath))
        {
            await File.WriteAllTextAsync(strategyPath, "[]");
        }
        else
        {
            var jsonData = await GetJsonObjectDataByPathAsync(strategyPath);
            if (jsonData == null)
            {
                await File.WriteAllTextAsync(strategyPath, string.Empty);
                await File.WriteAllTextAsync(strategyPath, "[]");
            }
        }

        if (!_isNeedToRunFirstScreenLogic)
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
            
            var jsonObject = new
            {
                Id = Guid.NewGuid(),
                Name = userName,
                TelegramBotToken = botTelegramApiKey,
                TelegramUserId = userTelegramId,
                IsActive = true
            };

            var jsonString = JsonConvert.SerializeObject(new List<object> { jsonObject }, Formatting.Indented);
            
            await File.WriteAllTextAsync(userPath, string.Empty);
            await File.WriteAllTextAsync(userPath, jsonString);
            
            break;
        }
    }

    private static async Task<List<object>?> GetJsonObjectDataByPathAsync(string path)
    {
        try
        {
            var jsonData = await File.ReadAllTextAsync(path);
            if (string.IsNullOrWhiteSpace(jsonData))
            {
                return null;
            }

            var jsonObject = JsonConvert.DeserializeObject<List<object>>(jsonData);
            return jsonObject ?? null;
        }
        catch (Exception)
        {
            return null;
        }
    }
    
    private static void ErrorMessage(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}